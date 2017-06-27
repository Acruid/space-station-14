﻿using Lidgren.Network;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Network;
using SS14.Shared;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using System;
using SS14.Shared.Configuration;

namespace SS14.Client.Network
{
    public class NetworkManager : INetworkManager
    {
        private const string ServerName = "SS13 Server";
        private readonly NetPeerConfiguration _netConfig = new NetPeerConfiguration("SS13_NetTag");
        public NetClient NetClient { get; private set; }
        private GameType _serverGameType;

        #region INetworkManager Members

        public void Initialize()
        {
            if (NetClient != null)
            {
                throw new InvalidOperationException("Start() has been called already.");
            }

            var config = IoCManager.Resolve<IConfigurationManager>();

            config.RegisterCVar("net.server", "127.0.0.1", CVarFlags.ARCHIVE);
            config.RegisterCVar("net.updaterate", 20, CVarFlags.ARCHIVE);
            config.RegisterCVar("net.cmdrate", 30, CVarFlags.ARCHIVE);
            config.RegisterCVar("net.interpolation", 0.1f, CVarFlags.ARCHIVE);
            config.RegisterCVar("net.rate", 10240, CVarFlags.REPLICATED | CVarFlags.ARCHIVE);

#if DEBUG
            config.RegisterCVar("net.fakelag", false, CVarFlags.CHEAT);
            config.RegisterCVar("net.fakeloss", 0.0f, CVarFlags.CHEAT);
            config.RegisterCVar("net.fakelagmin", 0.0f, CVarFlags.CHEAT);
            config.RegisterCVar("net.fakelagrand", 0.0f, CVarFlags.CHEAT);

            //Simulate Latency
            if (config.GetCVar<bool>("net.fakelag"))
            {
                _netConfig.SimulatedLoss = config.GetCVar<float>("net.fakeloss");
                _netConfig.SimulatedMinimumLatency = config.GetCVar<float>("net.fakelagmin");
                _netConfig.SimulatedRandomLatency = config.GetCVar<float>("net.fakelagrand");
            }

            _netConfig.ConnectionTimeout = 30000f;
#endif

            NetClient = new NetClient(_netConfig);
            NetClient.Start();
        }

        public bool IsConnected
        { get; private set; } = false;

        public NetPeerStatistics CurrentStatistics
        {
            get { return NetClient.Statistics; }
        }

        public long UniqueId
        {
            get { return NetClient.UniqueIdentifier; }
        }

        public event EventHandler<IncomingNetworkMessageArgs> MessageArrived; //Called when we recieve a new message.

        public event EventHandler Connected; //Called when we connect to a server.

        public event EventHandler Disconnected; //Called when we Disconnect from a server.

        public void ConnectTo(string host)
        {
            NetClient.Connect(host, 1212);
        }

        public void Disconnect()
        {
            Restart();
        }

        public void UpdateNetwork()
        {
            if (IsConnected)
            {
                NetIncomingMessage msg;
                while ((msg = NetClient.ReadMessage()) != null)
                {
                    OnMessageArrived(msg);
                    NetClient.Recycle(msg);
                }
            }

            if (!IsConnected && NetClient.ServerConnection != null)
            {
                OnConnected();
                IsConnected = true;
            }
            else if (IsConnected && NetClient.ServerConnection == null)
            {
                OnDisconnected();
                IsConnected = false;
            }
        }

        public void RequestMap()
        {
            NetOutgoingMessage message = NetClient.CreateMessage();
            message.Write((byte)NetMessage.RequestMap);
            NetClient.SendMessage(message, NetDeliveryMethod.ReliableUnordered);
        }

        public NetOutgoingMessage CreateMessage()
        {
            return NetClient.CreateMessage();
        }

        public void SendClientName(string name)
        {
            NetOutgoingMessage message = NetClient.CreateMessage();
            message.Write((byte)NetMessage.ClientName);
            message.Write(name);
            NetClient.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(NetOutgoingMessage message, NetDeliveryMethod deliveryMethod)
        {
            if (message != null)
            {
                NetClient.SendMessage(message, deliveryMethod);
            }
        }

        #endregion INetworkManager Members

        protected virtual void OnMessageArrived(NetIncomingMessage message)
        {
            if (MessageArrived != null) MessageArrived(this, new IncomingNetworkMessageArgs(message));
        }

        protected virtual void OnConnected()
        {
            if (Connected != null) Connected(this, null);
        }

        protected virtual void OnDisconnected()
        {
            if (Disconnected != null) Disconnected(this, null);
        }

        public void Restart()
        {
            NetClient.Shutdown("Leaving");
            NetClient = new NetClient(_netConfig);
            NetClient.Start();
        }

        public void ShutDown()
        {
            NetClient.Shutdown("Quitting");
        }

        public void SetGameType(NetIncomingMessage msg)
        {
            _serverGameType = (GameType)msg.ReadByte();
        }

        public void SendChangeTile(int x, int y, Tile newTile)
        {
            NetOutgoingMessage netMessage = NetClient.CreateMessage();
            netMessage.Write((int)x);
            netMessage.Write((int)y);
            netMessage.Write((uint)newTile);
            NetClient.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public NetIncomingMessage GetNetworkUpdate()
        {
            NetIncomingMessage msg;
            return (msg = NetClient.ReadMessage()) != null ? msg : null;
        }

        public string GetServerName()
        {
            return ServerName;
        }

        public string GetServerAddress()
        {
            return String.Format("{0}:{1}", NetClient.ServerConnection.RemoteEndPoint.Address, NetClient.Port);
        }

        public GameType GetGameType()
        {
            return _serverGameType;
        }
    }
}
