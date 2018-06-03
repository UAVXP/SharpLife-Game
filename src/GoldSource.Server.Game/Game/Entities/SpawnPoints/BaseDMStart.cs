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

using Server.Game.Entities.MetaData;

namespace Server.Game.Entities.SpawnPoints
{
    [LinkEntityToClass("info_player_deathmatch")]
    public class BaseDMStart : PointEntity
    {
        public override bool KeyValue(string key, string value)
        {
            if (key == "master")
            {
                NetName = value;
                return true;
            }

            return base.KeyValue(key, value);
        }

        public override bool IsTriggered(BaseEntity pActivator)
        {
            return EntUtils.IsMasterTriggered(NetName, pActivator);
        }
    }
}
