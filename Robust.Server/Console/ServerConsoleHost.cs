using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.Interfaces.Log;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Maths;
using Robust.Shared.Network.Messages;
using Robust.Shared.Players;
using Robust.Shared.Utility;

namespace Robust.Server.Console
{
    /// <summary>
    /// The server console shell that executes commands.
    /// </summary>
    public interface IServerConsoleHost : IConsoleHost
    {
        /// <summary>
        /// The local console shell that is always available.
        /// </summary>
        IServerConsoleShell LocalShell { get; }

        /// <summary>
        /// A map of (commandName -> ICommand) of every registered command in the shell.
        /// </summary>
        IReadOnlyDictionary<string, IServerCommand> AvailableCommands { get; }

        /// <summary>
        /// Initializes the ConsoleShell service.
        /// </summary>
        void Initialize();
        
        /// <summary>
        ///     Scans all loaded assemblies for console commands and registers them. This will NOT sync with connected clients, and
        ///     should only be used during server initialization.
        /// </summary>
        void ReloadCommands();

        /// <summary>
        /// Execute a command string on the local shell.
        /// </summary>
        /// <param name="command">Command string to execute.</param>
        void ExecuteCommand(string command);

        /// <summary>
        /// Execute a command string as a player.
        /// </summary>
        /// <param name="session">Session of the remote player. If this is null, the command is executed as the local console.</param>
        /// <param name="command">Command string to execute.</param>
        void ExecuteCommand(IPlayerSession? session, string command);

        /// <summary>
        /// Sends a text string to the remote player.
        /// </summary>
        /// <param name="session">Remote player to send the text message to. If this is null, the text is sent to the local console.</param>
        /// <param name="text">Text message to send.</param>
        void SendText(IPlayerSession? session, string text);

        /// <summary>
        /// Sends a text string to the remote console.
        /// </summary>
        /// <param name="target">Net channel to send the text string to.</param>
        /// <param name="text">Text message to send.</param>
        void SendText(INetChannel target, string text);

        void ExecuteCommand(ICommonSession? session, string command);
    }

    /// <inheritdoc />
    internal class ServerConsoleHost : IServerConsoleHost
    {
        private const string SawmillName = "con";

        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IPlayerManager _players = default!;
        [Dependency] private readonly IServerNetManager _net = default!;
        [Dependency] private readonly ISystemConsoleManager _systemConsole = default!;
        [Dependency] private readonly ILogManager _logMan = default!;
        [Dependency] private readonly IConGroupController _groupController = default!;

        private readonly Dictionary<string, IServerCommand> _availableCommands =
            new Dictionary<string, IServerCommand>();

        public ServerConsoleHost()
        {
            LocalShell = new ConsoleShellAdapter(this, null);
        }

        public IServerConsoleShell LocalShell { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, IServerCommand> AvailableCommands => _availableCommands;

        private void HandleRegistrationRequest(INetChannel senderConnection)
        {
            var netMgr = IoCManager.Resolve<IServerNetManager>();
            var message = netMgr.CreateNetMessage<MsgConCmdReg>();

            var counter = 0;
            message.Commands = new MsgConCmdReg.Command[AvailableCommands.Count];
            foreach (var command in AvailableCommands.Values)
            {
                message.Commands[counter++] = new MsgConCmdReg.Command
                {
                    Name = command.Command,
                    Description = command.Description,
                    Help = command.Help
                };
            }

            netMgr.ServerSendMessage(message, senderConnection);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            ReloadCommands();

            // setup networking with clients
            _net.RegisterNetMessage<MsgConCmd>(MsgConCmd.NAME, ProcessCommand);
            _net.RegisterNetMessage<MsgConCmdAck>(MsgConCmdAck.NAME);
            _net.RegisterNetMessage<MsgConCmdReg>(MsgConCmdReg.NAME,
                message => HandleRegistrationRequest(message.MsgChannel));
        }

        /// <inheritdoc />
        public void ReloadCommands()
        {
            // search for all client commands in all assemblies, and register them
            _availableCommands.Clear();
            foreach (var type in _reflectionManager.GetAllChildren<IServerCommand>())
            {
                var instance = (IServerCommand) Activator.CreateInstance(type, null)!;
                if (AvailableCommands.TryGetValue(instance.Command, out var duplicate))
                    throw new InvalidImplementationException(instance.GetType(), typeof(IServerCommand),
                        $"Command name already registered: {instance.Command}, previous: {duplicate.GetType()}");

                _availableCommands[instance.Command] = instance;
            }
        }

        private void ProcessCommand(MsgConCmd message)
        {
            var text = message.Text;
            var sender = message.MsgChannel;
            var session = _players.GetSessionByChannel(sender);

            _logMan.GetSawmill(SawmillName).Info($"{FormatPlayerString(session)}:{text}");

            ExecuteCommand(session, text);
        }

        /// <inheritdoc />
        public void ExecuteCommand(string command)
        {
            ExecuteCommand(null, command);
        }

        /// <inheritdoc />
        public void ExecuteCommand(IPlayerSession? session, string command)
        {
            try
            {
                var args = new List<string>();
                CommandParsing.ParseArguments(command, args);

                // missing cmdName
                if (args.Count == 0)
                    return;

                var cmdName = args[0];

                if (_availableCommands.TryGetValue(cmdName, out var conCmd)) // command registered
                {
                    if (session != null) // remote client
                    {
                        if (_groupController.CanCommand(session, cmdName)) // client has permission
                        {
                            args.RemoveAt(0);
                            conCmd.Execute(new ConsoleShellAdapter(this, session), session, args.ToArray());
                        }
                        else
                            SendText(session, $"Unknown command: '{cmdName}'");
                    }
                    else // system console
                    {
                        args.RemoveAt(0);
                        conCmd.Execute(new ConsoleShellAdapter(this, session), null, args.ToArray());
                    }
                }
                else
                    SendText(session, $"Unknown command: '{cmdName}'");
            }
            catch (Exception e)
            {
                _logMan.GetSawmill(SawmillName).Warning($"{FormatPlayerString(session)}: ExecuteError - {command}:\n{e}");
                SendText(session, $"There was an error while executing the command: {e}");
            }
        }

        /// <inheritdoc />
        public void SendText(IPlayerSession? session, string text)
        {
            if (session != null)
                SendText(session.ConnectedClient, text);
            else
                _systemConsole.Print(text + "\n");
        }

        /// <inheritdoc />
        public void SendText(INetChannel target, string text)
        {
            var replyMsg = _net.CreateNetMessage<MsgConCmdAck>();
            replyMsg.Text = text;
            _net.ServerSendMessage(replyMsg, target);
        }

        public void ExecuteCommand(ICommonSession? session, string command)
        {
            ExecuteCommand(session as IPlayerSession, command);
        }

        private static string FormatPlayerString(IPlayerSession? session)
        {
            return session != null ? $"{session.Name}" : "[HOST]";
        }

        private class SudoCommand : IServerCommand
        {
            public string Command => "sudo";
            public string Description => "sudo make me a sandwich";
            public string Help => "sudo";

            public void Execute(IServerConsoleShell shell, IPlayerSession? player, string[] args)
            {
                var command = args[0];
                var cArgs = args[1..].Select(CommandParsing.Escape);

                var localShell = shell.ConsoleHost.LocalShell;
                localShell.ExecuteCommand($"{command} {string.Join(' ', cArgs)}");
            }
        }

        IConsoleShell IConsoleHost.LocalShell => LocalShell;

        public IConsoleShell GetSessionShell(ICommonSession session)
        {
            if (session.Status >= SessionStatus.Disconnected)
                throw new InvalidOperationException("Tried to get the session shell of a disconnected peer.");

            return new ConsoleShellAdapter(this, session);
        }

        public void WriteLine(ICommonSession? session, string text)
        {
            if (session is IPlayerSession playerSession)
            {
                SendText(playerSession, text);
            }
            else
            {
                SendText(null as IPlayerSession, text);
            }
        }
    }

    public class ConsoleShellAdapter : IServerConsoleShell
    {
        private IServerConsoleHost _host;
        private ICommonSession? _session;

        public ConsoleShellAdapter(IServerConsoleHost host, ICommonSession? session)
        {
            _host = host;
            _session = session;
        }

        public IConsoleHost ConsoleHost => _host;
        public bool IsServer => true;
        public ICommonSession? Player => _session;

        public void ExecuteCommand(string command)
        {
            _host.ExecuteCommand(_session, command);
        }

        public void WriteLine(string text)
        {
            _host.WriteLine(_session, text);
        }

        public void WriteLine(string text, Color color)
        {
            //TODO: Make the color work!
            _host.WriteLine(_session, text);
        }

        public void Clear()
        {
            //TODO: Make me work!
        }

        public IReadOnlyDictionary<string, IServerCommand> RegisteredCommands => _host.AvailableCommands;
    }
}
