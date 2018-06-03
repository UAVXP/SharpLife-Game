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
using GoldSource.Server.Engine;
using GoldSource.Server.Game.Engine;
using GoldSource.Shared.Entities;

namespace GoldSource.Server.Game.Game.Entities.Weapons
{
    public static class WeaponUtils
    {
        public static float WeaponTimeBase()
        {
#if CLIENT_WEAPONS
            return 0.0f;
#else
            return Engine.Globals.Time;
#endif
        }

        public static bool CanAttack(float attack_time, float curtime, bool isPredicted)
        {
#if CLIENT_WEAPONS
            if (!isPredicted)
#else
            if (true)
#endif
            {
                return attack_time <= curtime;
            }
            else
            {
                return attack_time <= 0.0;
            }
        }

#pragma warning disable RCS1163 // Unused parameter.
        public static unsafe void FindHullIntersection(in Vector vecSrc, ref TraceResult tr, in Vector mins, in Vector maxs, Edict pEntity)
#pragma warning restore RCS1163 // Unused parameter.
        {
            var vecHullEnd = vecSrc + ((tr.EndPos - vecSrc) * 2);
            Trace.Line(vecSrc, vecHullEnd, TraceFlags.None, pEntity, out var tmpTrace);
            if (tmpTrace.Fraction < 1.0)
            {
                tr = tmpTrace;
                return;
            }

            //TODO: find a safe way to do this efficiently
            var minmaxs = stackalloc Vector[]
            {
                mins,
                maxs
            };

            var distance = 1e6f;

            for (var i = 0; i < 2; ++i)
            {
                for (var j = 0; j < 2; ++j)
                {
                    for (var k = 0; k < 2; ++k)
                    {
                        var vecEnd = new Vector(
                            vecHullEnd.x + minmaxs[i][0],
                            vecHullEnd.y + minmaxs[j][1],
                            vecHullEnd.z + minmaxs[k][2]
                        );

                        Trace.Line(vecSrc, vecEnd, TraceFlags.None, pEntity, out tmpTrace);
                        if (tmpTrace.Fraction < 1.0)
                        {
                            var thisDistance = (tmpTrace.EndPos - vecSrc).Length();

                            if (thisDistance < distance)
                            {
                                tr = tmpTrace;
                                distance = thisDistance;
                            }
                        }
                    }
                }
            }
        }
    }
}
