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
    /// bullet types
    /// </summary>
    public enum Bullet
    {
        None = 0,

        Player9MM = 1,
        PlayerMP5 = 2,
        Player357 = 3,
        PlayerBuckShot = 4,
        PlayerCrowbar = 5,

        Monster9MM = 6,
        MonsterMP5 = 7,
        Monster12MM = 8,
    }
}
