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
using GoldSource.Server.Engine.CVar;
using Server.Game.Entities;
using Server.Game.Navigation;

namespace Server.Game
{
    public static class Globals
    {
        public const int k_cubSaltSize = 8;

        public static int g_Language;

        public static int g_iSkillLevel;

        public static int g_sModelIndexLaser;

        public static string g_pModelNameLaser;

        public static int g_sModelIndexLaserDot;

        public static int g_sModelIndexFireball;

        public static int g_sModelIndexSmoke;

        public static int g_sModelIndexWExplosion;

        public static int g_sModelIndexBubbles;

        public static int g_sModelIndexBloodDrop;

        public static int g_sModelIndexBloodSpray;

        public static int iHornetPuff;

        public static int iAgruntMuzzleFlash;

        public static int gSpitSprite;

        public static int gSpitDebrisSprite;
        //how close the squid has to get before starting to sprint and refusing to swerve
        public static int iSquidSpitSprite;

        public static bool giPrecacheGrunt;

        public static int g_teamplay = 0;

        public static float g_flWeaponCheat;

        public static int g_serveractive = 0;

        public static char[] m_soundNames; //TODO refactor

        public const float GARG_ATTACKDIST = 80;

        public static int gStompSprite = 0;

        public static int gGargGibModel = 0;

        public static int g_ulModelIndexPlayer;

        public static int gDisplayTitle;

        public static int g_fGruntQuestion;

        public static int iHornetTrail;

        public static int gEvilImpulse101;

        public static float g_flIntermissionStartTime;

        public static int[] Primes; //TODO: implement

        public static char[] m_szFriends; //TODO: implement

        public static char[][] team_names; //[32][16]

        public static int[] team_scores; //32

        public static int num_teams = 0;

        public static char[] st_szNextMap; //32

        public static char[] st_szNextSpot; //32

        public static int[] gSizes; //array of 18

        public static int giAmmoIndex = 0;

        public static GlobalState.GlobalState GlobalState = new GlobalState.GlobalState();

        public static MultiDamage MultiDamage = new MultiDamage();

        public static Language Language = Language.English;

        public static Vector g_vecAttackDir = WorldConstants.g_vecZero;

        public static DecalEntry[] gDecals;

        public static CGraph WorldGraph = new CGraph();

        public static EngineCVar g_psv_gravity;
        public static EngineCVar g_psv_aim;
        public static EngineCVar g_footsteps;
    }
}
