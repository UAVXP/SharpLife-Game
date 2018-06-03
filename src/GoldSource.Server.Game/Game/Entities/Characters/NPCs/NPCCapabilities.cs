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

namespace Server.Game.Entities.Characters.NPCs
{
    [Flags]
    public enum NPCCapabilities
    {
        None            = 0,
        Duck			= 1 << 0, // crouch
        Jump			= 1 << 1, // jump/leap
        Strafe			= 1 << 2, // strafe ( walk/run sideways)
        Squad			= 1 << 3, // can form squads
        Swim			= 1 << 4, // proficiently navigate in water
        Climb			= 1 << 5, // climb ladders/ropes
        Use			    = 1 << 6, // open doors/push buttons/pull levers
        Hear			= 1 << 7, // can hear forced sounds
        AutoDoors		= 1 << 8, // can trigger auto doors
        OpenDoors		= 1 << 9, // can open manual doors
        TurnHead		= 1 << 10,// can turn head, always bone controller 0
        
        RangeAttack1	= 1 << 11,// can do a range attack 1
        RangeAttack2	= 1 << 12,// can do a range attack 2
        MeleeAttack1	= 1 << 13,// can do a melee attack 1
        MeleeAttack2	= 1 << 14,// can do a melee attack 2
        
        Fly			    = 1 << 15,// can fly, move all around
        
        DoorsGroup      = Use | AutoDoors | OpenDoors,
    }
}
