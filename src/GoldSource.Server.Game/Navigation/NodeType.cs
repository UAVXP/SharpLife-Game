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

namespace GoldSource.Server.Game.Navigation
{
    [Flags]
    public enum NodeType
    {
        Land      = 1 << 0,  // Land node, so nudge if necessary.
        Air       = 1 << 1,  // Air node, don't nudge.
        Water     = 1 << 2,  // Water node, don't nudge.
        Group_Realm = Land | Air | Water
    }
}
