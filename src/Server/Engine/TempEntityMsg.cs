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

namespace Server.Engine
{
    public enum TempEntityMsg
    {
        Explosion = 3,
        Smoke = 5,
        Sparks = 9,
        StreakSplash = 25,
        TextMessage = 29,
        BloodStream = 101,
        ShowLine = 102,
        Decal = 104,
        Model = 106,
        GunShotDecal = 109,
        SpriteSpray = 110,
        ArmorRicochet = 111,
        PlayerDecal = 112,
        Bubbles = 113,
        BubbleTrail = 114,
        WorldDecal = 116,
        WorldDecalHigh = 117,
        DecalHigh = 118,
    }
}
