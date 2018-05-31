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

using GoldSource.Mathlib;

namespace Server.Game
{
    public static class WorldConstants
    {
        public static readonly Vector g_vecZero = new Vector(0, 0, 0);

        public const float MAX_COORDINATE = 4096;
        public const float MAX_SPEED = 2000;

        public const int NUM_HULLS = 4;

        public static readonly Vector HULL_MIN = new Vector(-16, -16, -36);
        public static readonly Vector HULL_MAX = new Vector(16, 16, 36);

        public static readonly Vector POINT_HULL_MIN = new Vector(-12, -12, 0);

        public static readonly Vector HUMAN_HULL_MIN = new Vector(-16, -16, 0);
        public static readonly Vector HUMAN_HULL_MAX = new Vector(16, 16, 72);
        public static readonly Vector HUMAN_HULL_DUCK = new Vector(16, 16, 36);

        public static readonly Vector DUCK_HULL_MIN = new Vector(-16, -16, -18);
        public static readonly Vector DUCK_HULL_MAX = new Vector(16, 16, 18);

        public static readonly Vector ViewOffset = new Vector(0, 0, ViewHeight);

        public const int DuckViewHeight = 12;
        public const int ViewHeight = 28;

        public static readonly Vector LARGE_HULL_MIN = new Vector(-32, -32, 0);

        public const int DeadViewHeight = -8;

        public const int MaxClimbSpeed = 200;

        /// <summary>
        /// how fast we longjump
        /// </summary>
        public const int PlayerLongJumpSpeed = 350;

        // Only allow bunny jumping up to 1.7x server / player maxspeed setting
        public const float BunnyJumpMaxSpeedFactor = 1.7f;

        /// <summary>
        /// won't punch player's screen/make scrape noise unless player falling at least this fast.
        /// </summary>
        public const float PlayerFallPunchTreshold = 350;

        /// <summary>
        /// approx 60 feet
        /// </summary>
        public const float PlayerFatalFallSpeed = 1024;

        /// <summary>
        /// approx 20 feet
        /// </summary>
        public const float PlayerMaxSafeFallSpeed = 580;

        /// <summary>
        /// damage per unit per second
        /// </summary>
        public const float DamageForFallSpeed = 100 / (PlayerFatalFallSpeed - PlayerMaxSafeFallSpeed);

        public const float PlayerMinBounceSpeed = 200;

        public const string SoundFlashlightOn = "items/flashlight1.wav";
        public const string SoundFlashlightOff = "items/flashlight1.wav";

        public const float PlayerUseSearchRadius = 64;

        public const float PlayerChatInterval = 1.0f;
    }
}
