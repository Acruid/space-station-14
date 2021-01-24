// This file is for commands that do something to the console itself.
// Not some generic console command type.
// Couldn't think of a better name sorry.

using System;
using Robust.Shared.Console;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.Console.Commands
{
    class ClearCommand : IClientCommand
    {
        public string Command => "cls";
        public string Help => "Clears the debug console of all messages.";
        public string Description => "Clears the console.";

        public void Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            shell.Clear();
        }
    }

    class FillCommand : IClientCommand
    {
        public string Command => "fill";
        public string Help => "Fills the console with some nonsense for debugging.";
        public string Description => "Fill up the console for debugging.";

        public void Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            Color[] colors = { Color.Green, Color.Blue, Color.Red };
            var random = IoCManager.Resolve<IRobustRandom>();
            for (int x = 0; x < 50; x++)
            {
                shell.WriteLine("filling...", colors[random.Next(0, colors.Length)]);
            }
        }
    }
}
