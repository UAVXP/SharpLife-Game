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

namespace Server.Game
{
    public struct HudTextParms
    {
        public float x;
        public float y;

        public int effect;

        public byte r1;
        public byte g1;
        public byte b1;
        public byte a1;

        public byte r2;
        public byte g2;
        public byte b2;
        public byte a2;

        public float fadeinTime;
        public float fadeoutTime;
        public float holdTime;

        public float fxTime;

        public int channel;
    }
}