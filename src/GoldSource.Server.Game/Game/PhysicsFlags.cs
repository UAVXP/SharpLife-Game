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

namespace GoldSource.Server.Game.Game
{
    /// <summary>
    /// Player PHYSICS FLAGS bits
    /// </summary>
    [Flags]
    public enum PhysicsFlags : uint
    {
        None = 0,

        OnLadder = 1 << 0,
        ONSwing = 1 << 0,
        OnTrain = 1 << 1,
        OnBarnacle = 1 << 2,

        /// <summary>
        /// In the process of ducking, but totally squatted yet
        /// </summary>
        Ducking = 1 << 3,

        /// <summary>
        /// Using a continuous entity
        /// </summary>
        Using = 1 << 4,

        /// <summary>
        /// player is locked in stationary cam mode. Spectators can move, observers can't
        /// </summary>
        Observer = 1 << 5,
    }
}
