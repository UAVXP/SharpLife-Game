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
    /// Spectator Movement modes (stored in pev->iuser1, so the physics code can get at them)
    /// </summary>
    public enum ObserverMode
    {
        None = 0,
        ChaseLocked = 1,
        ChaseFree = 2,
        Roaming = 3,
        InEye = 4,
        MapFree = 5,
        MapChase = 6,
    }
}
