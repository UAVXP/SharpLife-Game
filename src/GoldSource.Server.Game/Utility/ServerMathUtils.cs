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
using GoldSource.Server.Engine.API;
using GoldSource.Shared.Game.Utility;

namespace GoldSource.Server.Game.Utility
{
    public static class ServerMathUtils
    {
        public static TraceResult GetGlobalTrace()
        {
            return Game.Engine.Globals.GlobalTrace;
        }

        public static void MakeVectors(Vector vecAngles)
        {
            MathUtils.AngleVectors(vecAngles, out var forward, out var right, out var up);
            Game.Engine.Globals.ForwardVector = forward;
            Game.Engine.Globals.RightVector = right;
            Game.Engine.Globals.UpVector = up;
        }

        public static void MakeAimVectors(Vector vecAngles)
        {
            vecAngles.x = -vecAngles.x;

            MakeVectors(vecAngles);
        }

        public static void MakeInvVectors(Vector vec, IGlobalVars pgv)
        {
            MakeVectors(vec);

            pgv.RightVector *= -1;

            var forward = pgv.ForwardVector;
            var right = pgv.RightVector;
            var up = pgv.UpVector;

            Utils.Swap(ref forward.y, ref right.x);
            Utils.Swap(ref forward.z, ref up.x);
            Utils.Swap(ref right.z, ref up.y);

            pgv.ForwardVector = forward;
            pgv.RightVector = right;
            pgv.UpVector = up;
        }
    }
}
