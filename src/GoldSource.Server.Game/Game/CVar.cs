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

using GoldSource.Server.Engine.CVar;

namespace GoldSource.Server.Game.Game
{
    public static class CVar
    {
        public static ICVar EngineCVar { get; set; }

        public static string GetString(string name)
        {
            return EngineCVar.GetString(name);
        }

        public static float GetFloat(string name)
        {
            return EngineCVar.GetFloat(name);
        }

        public static EngineCVar GetCVar(string name)
        {
            return EngineCVar.GetCVar(name);
        }
    }
}
