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

using GoldSource.Shared.Entities;
using Server.Game.Entities.MetaData;

namespace Server.Game.Entities.BrushModels
{
    /// <summary>
    /// A simple entity that looks solid but lets you walk through it
    /// </summary>
    [LinkEntityToClass("func_illusionary")]
    public class FuncIllusionary : BaseToggle
    {
        //skin is used for content type

        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;

        public override void Spawn()
        {
            Angles = WorldConstants.g_vecZero;
            MoveType = MoveType.None;
            Solid = Solid.Not;// always solid_not 
            SetModel(ModelName);

            // I'd rather eat the network bandwidth of this than figure out how to save/restore
            // these entities after they have been moved to the client, or respawn them ala Quake
            // Perhaps we can do this in deathmatch only.
            //	MAKE_STATIC(ENT(pev));
        }
    }
}
