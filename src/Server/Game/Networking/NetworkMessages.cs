/***
*
*	Copyright (c) 1996-2001, Valve LLC. All rights reserved.
*	
*	This product contains software technology licensed from Id 
*	Software, Inc. ("Id Technology").  Id Technology (c) 1996 Id Software, Inc. 
*	All Rights Reserved.
*
*   This source code contains proprietary and confidential information of
*   Valve LLC and its suppliers.  Access to this code is restricted to
*   persons who have executed a written SDK license with Valve.  Any access,
*   use or distribution of this code by or to any unlicensed person is illegal.
*
****/

using GoldSource.Server.Engine.API;
using System;
using System.Collections.Generic;

namespace Server.Game.Networking
{
    public sealed class NetworkMessages : INetworkMessages
    {
        private IEngineNetworking EngineNetworking { get; }

        private Dictionary<string, int> Messages { get; } = new Dictionary<string, int>();

        public NetworkMessages(IEngineNetworking engineNetworking)
        {
            EngineNetworking = engineNetworking ?? throw new ArgumentNullException(nameof(engineNetworking));
        }

        public void RegisterMessage(string name, int size)
        {
            if (Messages.ContainsKey(name))
            {
                throw new InvalidOperationException($"Network message {name} is already registered");
            }

            var index = EngineNetworking.RegisterMessage(name, size);

            if (index > 0)
            {
                Messages.Add(name, index);
            }
        }

        public int GetMessage(string name)
        {
            return Messages[name];
        }
    }
}
