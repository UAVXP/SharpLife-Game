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

using Server.Engine.API;

namespace Server.Engine
{
    public static class Trace
    {
        public static ITrace EngineTrace { get; set; }

        public static void Line(in Vector vecStart, in Vector vecEnd, TraceFlags flags, Edict pentIgnore, out TraceResult ptr)
        {
            ptr = new TraceResult();

            EngineTrace.Line(in vecStart, in vecEnd, flags, pentIgnore, out ptr);
        }

        public static void Hull(in Vector start, in Vector end, TraceFlags flags, Hull hullNumber, Edict entToSkip, out TraceResult tr)
        {
            EngineTrace.TraceHull(start, end, flags, hullNumber, entToSkip, out tr);
        }
    }
}
