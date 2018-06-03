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

namespace GoldSource.Server.Game.Game.Entities
{
    /// <summary>
    /// These are caps bits to indicate what an object's capabilities (currently used for save/restore and level transitions)
    /// </summary>
    [Flags]
    public enum EntityCapabilities : uint
    {
        None = 0,

        CustomSave = 0x00000001,
        AcrossTransition = 0x00000002,  // should transfer between transitions
        MustSpawn = 0x00000004,         // Spawn after restore
        ImpulseUse = 0x00000008,        // can be used by the player
        ContinuousUse = 0x00000010,     // can be used by the player
        OnOffUse = 0x00000020,          // can be used by the player
        DirectionalUse = 0x00000040,    // Player sends +/- 1 when using (currently only tracktrains)
        Master = 0x00000080,            // Can be used to "master" other entities (like multisource)
        // UNDONE: This will ignore transition volumes (trigger_transition), but not the PVS!!!
        ForceTransition= 0x00000080,	// ALWAYS goes across transitions TODO same value as Master
        DontSave = 0x80000000,          // Don't save this
    }
}
