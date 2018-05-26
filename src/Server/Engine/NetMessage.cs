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

using Server.Engine.API;
using Server.Game.Networking;

namespace Server.Engine
{
    public static class NetMessage
    {
        public static IEngineNetworking EngineNetworking { get; set; }

        public static INetworkMessages NetworkMessages { get; set; }

        public static INetworkMessage Begin(MsgDest msg_dest, ServerCommand msg_type, in Vector pOrigin, Edict ed = null)
        {
            return EngineNetworking.Begin(msg_dest, (int)msg_type, pOrigin, ed);
        }

        public static INetworkMessage Begin(MsgDest msg_dest, ServerCommand msg_type, Edict ed = null)
        {
            return EngineNetworking.Begin(msg_dest, (int)msg_type, ed);
        }

        public static INetworkMessage Begin(MsgDest msg_dest, int msg_type, in Vector pOrigin, Edict ed = null)
        {
            return EngineNetworking.Begin(msg_dest, msg_type, pOrigin, ed);
        }

        public static INetworkMessage Begin(MsgDest msg_dest, int msg_type, Edict ed = null)
        {
            return EngineNetworking.Begin(msg_dest, msg_type, ed);
        }

        public static INetworkMessage Begin(MsgDest msg_dest, string msgType, in Vector pOrigin, Edict ed = null)
        {
            return EngineNetworking.Begin(msg_dest, NetworkMessages.GetMessage(msgType), pOrigin, ed);
        }

        public static INetworkMessage Begin(MsgDest msg_dest, string msgType, Edict ed = null)
        {
            return EngineNetworking.Begin(msg_dest, NetworkMessages.GetMessage(msgType), ed);
        }
    }
}
