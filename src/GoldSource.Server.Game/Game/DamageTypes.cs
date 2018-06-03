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

using System;

namespace Server.Game
{
    [Flags]
    public enum DamageTypes
    {
        Generic			= 0,		// generic damage was done
        Crush			= 1 << 0,	// crushed by falling or moving object
        Bullet			= 1 << 1,	// shot
        Slash			= 1 << 2,	// cut, clawed, stabbed
        Burn			= 1 << 3,	// heat burned
        Freeze			= 1 << 4,	// frozen
        Fall			= 1 << 5,	// fell too far
        Blast			= 1 << 6,	// explosive blast damage
        Club			= 1 << 7,	// crowbar, punch, headbutt
        Shock			= 1 << 8,	// electric shock
        Sonic			= 1 << 9,	// sound pulse shockwave
        EnergyBeam		= 1 << 10,	// laser or other high energy beam 
        NeverGib		= 1 << 12,	// with this bit OR'd in, no damage type will be able to gib victims upon death
        AlwaysGib		= 1 << 13,	// with this bit OR'd in, any damage type can be made to gib victims upon death.
        Drown			= 1 << 14,	// Drowning
        // time-based damage
        TimeBased		= ~(0x3fff),// mask for time-based damage

        Paralyze		= 1 << 15,	// slows affected creature down
        NerveGas		= 1 << 16,	// nerve toxins, very bad
        Poison			= 1 << 17,	// blood poisioning
        Radiation		= 1 << 18,	// radiation exposure
        DrownRecover	= 1 << 19,	// drowning recovery
        Acid			= 1 << 20,	// toxic chemicals or acid burns
        SlowBurn		= 1 << 21,	// in an oven
        SlowFreeze		= 1 << 22,	// in a subzero freezer
        Mortar			= 1 << 23,  // Hit by air raid (done to distinguish grenade from mortar)

        // these are the damage types that are allowed to gib corpses
        GibCorpse		= Crush | Fall | Blast | Sonic | Club,

        // these are the damage types that have client hud art
        ShownHud		= Poison | Acid | Freeze | SlowFreeze | Drown | Burn | SlowBurn | NerveGas | Radiation | Shock,
    }
}
