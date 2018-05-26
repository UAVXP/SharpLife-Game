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

namespace Server.Engine
{
    /// <summary>
    /// The Explosion effect has some flags to control performance/aesthetic features
    /// </summary>
    [Flags]
    public enum ExplosionFlags
    {
        /// <summary>
        /// all flags clear makes default Half-Life explosion
        /// </summary>
        None = 0,

        /// <summary>
        /// sprite will be drawn opaque (ensure that the sprite you send is a non-additive sprite)
        /// </summary>
        NoAdditive = 1,

        /// <summary>
        /// do not render dynamic lights
        /// </summary>
        NoDLights = 2,

        /// <summary>
        /// do not play client explosion sound
        /// </summary>
        NoSound = 4,

        /// <summary>
        /// do not draw particles
        /// </summary>
        NoParticles = 8,
    }
}
