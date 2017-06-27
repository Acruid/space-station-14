﻿using Lidgren.Network;
using SS14.Server.Interfaces.Network;
using SS14.Shared.Interfaces.Configuration;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace SS14.Server.Network
{
    public class SS14NetServer : NetServer, ISS14NetServer
    {
        public SS14NetServer()
            : base(LoadNetPeerConfig())
        {
        }

        #region ISS13NetServer Members

        public void SendToAll(NetOutgoingMessage message)
        {
            SendToAll(message, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMessage(NetOutgoingMessage message, NetConnection client)
        {
            SendMessage(message, client, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendToMany(NetOutgoingMessage message, List<NetConnection> recipients)
        {
            SendMessage(message, recipients, NetDeliveryMethod.ReliableOrdered, 0);
        }

        #endregion ISS13NetServer Members

        public static NetPeerConfiguration LoadNetPeerConfig()
        {
            var cfgMgr = IoCManager.Resolve<IConfigurationManager>();
            cfgMgr.RegisterCVar("net.port", 1212);
            var _config = new NetPeerConfiguration("SS13_NetTag");
            _config.Port = cfgMgr.GetCVar<int>("net.port");
#if DEBUG
            _config.ConnectionTimeout = 30000f;
#endif

            return _config;
        }
    }
}
