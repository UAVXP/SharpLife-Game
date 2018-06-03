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

namespace GoldSource.Server.Game.Game
{
    /// <summary>
    /// when calling KILLED(), a value that governs gib behavior is expected to be one of these three values
    /// </summary>
    public enum GibAction
    {
        Normal	= 0, // gib if entity was overkilled
        Never	= 1, // never gib, no matter how much death damage is done ( freezing, etc )
        Always	= 2, // always gib ( Houndeye Shock, Barnacle Bite )
    }
}
