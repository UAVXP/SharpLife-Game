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

using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Shared.Entities;

namespace GoldSource.Server.Game.Game.Entities
{
    [LinkEntityToClass("info_player_start")]
    public class PointEntity : BaseEntity
    {
        public override void Spawn()
        {
            Solid = Solid.Not;
            //	SetSize(WorldConstants.g_vecZero, WorldConstants.g_vecZero);
        }

        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;
    }
}
