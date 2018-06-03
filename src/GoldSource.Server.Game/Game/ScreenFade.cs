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
/*
*	used to indicate the fade can be longer than 16 seconds (added for czero)
*	This structure is sent over the net to describe a screen fade event
*/
using System.Runtime.InteropServices;

namespace Server.Game
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ScreenFade
    {
        public ushort duration;

        public ushort holdTime; //FIXED 4.12 seconds duration until reset (fade & hold)

        public short fadeFlags;

        //flags
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }
}
