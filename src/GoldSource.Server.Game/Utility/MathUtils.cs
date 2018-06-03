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
using GoldSource.Shared.Entities;
using System;

namespace GoldSource.Server.Game.Utility
{
    public static class MathUtils
    {
        /// <summary>
        /// up / down
        /// </summary>
        public const int PITCH = 0;

        /// <summary>
        /// left / right
        /// </summary>
        public const int YAW = 1;

        /// <summary>
        /// fall over
        /// </summary>
        public const int ROLL = 2;

        public static TraceResult GetGlobalTrace()
        {
            return Game.Engine.Globals.GlobalTrace;
        }

        public static void MakeVectors(Vector vecAngles)
        {
            AngleVectors(vecAngles, out var forward, out var right, out var up);
            Game.Engine.Globals.ForwardVector = forward;
            Game.Engine.Globals.RightVector = right;
            Game.Engine.Globals.UpVector = up;
        }

        public static void MakeVectorsPrivate(Vector vecAngles, out Vector p_vForward, out Vector p_vRight, out Vector p_vUp)
        {
            AngleVectors(vecAngles, out p_vForward, out p_vRight, out p_vUp);
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

        public static void AngleVectors(Vector angles, out Vector forward, out Vector right, out Vector up)
        {
            var angle = angles[YAW] * (Math.PI * 2 / 360);
            var sy = Math.Sin(angle);
            var cy = Math.Cos(angle);
            angle = angles[PITCH] * (Math.PI * 2 / 360);
            var sp = Math.Sin(angle);
            var cp = Math.Cos(angle);
            angle = angles[ROLL] * (Math.PI * 2 / 360);
            var sr = Math.Sin(angle);
            var cr = Math.Cos(angle);

            forward = new Vector(
                (float)(cp * cy),
                (float)(cp * sy),
                (float)-sp
            );

            right = new Vector(
                (float)((-1 * sr * sp * cy) + (-1 * cr * -sy)),
                (float)((-1 * sr * sp * sy) + (-1 * cr * cy)),
                (float)(-1 * sr * cp)
            );

            up = new Vector(
                (float)((cr * sp * cy) + (-sr * -sy)),
                (float)((cr * sp * sy) + (-sr * cy)),
                (float)(cr * cp)
            );
        }

        public static void AngleVectorsTranspose(Vector angles, out Vector forward, out Vector right, out Vector up)
        {
            var angle = angles[YAW] * (Math.PI * 2 / 360);
            var sy = Math.Sin(angle);
            var cy = Math.Cos(angle);
            angle = angles[PITCH] * (Math.PI * 2 / 360);
            var sp = Math.Sin(angle);
            var cp = Math.Cos(angle);
            angle = angles[ROLL] * (Math.PI * 2 / 360);
            var sr = Math.Sin(angle);
            var cr = Math.Cos(angle);

            forward = new Vector(
                (float)(cp * cy),
                (float)((sr * sp * cy) + (cr * -sy)),
                (float)((cr * sp * cy) + (-sr * -sy))
            );

            right = new Vector(
                (float)(cp * sy),
                (float)((sr * sp * sy) + (cr * cy)),
                (float)((cr * sp * sy) + (-sr * cy))
            );

            up = new Vector(
                (float)-sp,
                (float)(sr * cp),
                (float)(cr * cp)
            );
        }

        public static float VecToYaw(Vector vec)
        {
            float yaw;

            if (vec[1] == 0 && vec[0] == 0)
            {
                yaw = 0;
            }
            else
            {
                yaw = ((float)(Math.Atan2(vec[1], vec[0]) * 180 / Math.PI));
                if (yaw < 0)
                {
                    yaw += 360;
                }
            }

            return yaw;
        }

        public static Vector VecToAngles(Vector vec)
        {
            VecToAngles(vec, out var angles);
            return angles;
        }

        public static void VecToAngles(Vector forward, out Vector angles)
        {
            float yaw, pitch;

            if (forward[1] == 0 && forward[0] == 0)
            {
                yaw = 0;
                if (forward[2] > 0)
                {
                    pitch = 90;
                }
                else
                {
                    pitch = 270;
                }
            }
            else
            {
                yaw = ((float)(Math.Atan2(forward[1], forward[0]) * 180 / Math.PI));
                if (yaw < 0)
                    yaw += 360;

                var tmp = Math.Sqrt((forward[0] * forward[0]) + (forward[1] * forward[1]));
                pitch = ((float)(Math.Atan2(forward[2], tmp) * 180 / Math.PI));
                if (pitch < 0)
                    pitch += 360;
            }

            angles = new Vector(
                pitch,
                yaw,
                0
            );
        }

        //This restricts the angle to the range [0, 65535]
        public static float ClampedAngleMod(float a)
        {
            return (float)((360.0 / 65536) * ((int)(a * (65536 / 360.0)) & 65535));
        }

        public static float AngleMod(float a)
        {
            if (a < 0)
            {
                a += 360 * ((int)(a / 360) + 1);
            }
            else if (a >= 360)
            {
                a -= 360 * ((int)(a / 360));
            }
            // a = (360.0/65536) * ((int)(a*(65536/360.0)) & 65535);
            return a;
        }

        public static void VectorMatrix(Vector forward, out Vector right, out Vector up)
        {
            if (forward[0] == 0 && forward[1] == 0)
            {
                right = new Vector(
                    1,
                    0,
                    0
                );

                up = new Vector(
                    -forward[2],
                    0,
                    0
                );
                return;
            }

            var tmp = new Vector(0, 0, 1);

            right = forward.CrossProduct(tmp);
            right = right.Normalize();
            up = right.CrossProduct(forward);
            up = up.Normalize();
        }

        public static float AngleBetweenVectors(Vector v1, Vector v2)
        {
            float l1 = v1.Length();
            float l2 = v2.Length();

            if (l1 == 0 || l2 == 0)
                return 0.0f;

            var angle = (float)(Math.Acos(v1.DotProduct(v2)) / (l1 * l2));
            return (float)((angle * 180.0f) / Math.PI);
        }

        public static void NormalizeAngles(ref Vector angles)
        {
            // Normalize angles
            for (var i = 0; i < 3; i++)
            {
                if (angles[i] > 180.0)
                {
                    angles[i] -= 360.0f;
                }
                else if (angles[i] < -180.0)
                {
                    angles[i] += 360.0f;
                }
            }
        }

        public static void InterpolateAngles(Vector start, Vector end, out Vector output, float frac)
        {
            NormalizeAngles(ref start);
            NormalizeAngles(ref end);

            output = new Vector();

            for (var i = 0; i < 3; i++)
            {
                var ang1 = start[i];
                var ang2 = end[i];

                var d = ang2 - ang1;
                if (d > 180)
                {
                    d -= 360;
                }
                else if (d < -180)
                {
                    d += 360;
                }

                output[i] = ang1 + (d * frac);
            }

            NormalizeAngles(ref output);
        }

        public static int Q_log2(int val)
        {
            int answer = 0;
            while ((val >>= 1) != 0)
                answer++;
            return answer;
        }

        public static float AngleDiff(float destAngle, float srcAngle)
        {
            float delta = destAngle - srcAngle;
            if (destAngle > srcAngle)
            {
                if (delta >= 180)
                    delta -= 360;
            }
            else
            {
                if (delta <= -180)
                    delta += 360;
            }
            return delta;
        }

        //TODO: not a math function
        public static Vector GetAimVector(Edict edict, float speed)
        {
            return Game.Engine.Server.GetAimVector(edict, speed);
        }

        public static Vector ClampVectorToBox(Vector input, Vector clampSize)
        {
            Vector sourceVector = input;

            if (sourceVector.x > clampSize.x)
                sourceVector.x -= clampSize.x;
            else if (sourceVector.x < -clampSize.x)
                sourceVector.x += clampSize.x;
            else
                sourceVector.x = 0;

            if (sourceVector.y > clampSize.y)
                sourceVector.y -= clampSize.y;
            else if (sourceVector.y < -clampSize.y)
                sourceVector.y += clampSize.y;
            else
                sourceVector.y = 0;

            if (sourceVector.z > clampSize.z)
                sourceVector.z -= clampSize.z;
            else if (sourceVector.z < -clampSize.z)
                sourceVector.z += clampSize.z;
            else
                sourceVector.z = 0;

            return sourceVector.Normalize();
        }

        public static float Approach(float target, float value, float speed)
        {
            float delta = target - value;

            if (delta > speed)
                value += speed;
            else if (delta < -speed)
                value -= speed;
            else
                value = target;

            return value;
        }

        public static float ApproachAngle(float target, float value, float speed)
        {
            target = AngleMod(target);
            value = AngleMod(target);

            float delta = target - value;

            // Speed is assumed to be positive
            if (speed < 0)
                speed = -speed;

            if (delta < -180)
                delta += 360;
            else if (delta > 180)
                delta -= 360;

            if (delta > speed)
                value += speed;
            else if (delta < -speed)
                value -= speed;
            else
                value = target;

            return value;
        }

        public static float AngleDistance(float next, float cur)
        {
            float delta = next - cur;

            if (delta < -180)
            {
                delta += 360;
            }
            else if (delta > 180)
            {
                delta -= 360;
            }

            return delta;
        }

        public static float SplineFraction(float value, float scale)
        {
            value = scale * value;
            float valueSquared = value * value;

            // Nice little ease-in, ease-out spline-like curve
            return (3 * valueSquared) - (2 * valueSquared * value);
        }

        public static float DotPoints(Vector vecSrc, Vector vecCheck, Vector vecDir)
        {
            var vec2LOS = (vecCheck - vecSrc).Make2D().Normalize();

            return vec2LOS.DotProduct(vecDir.Make2D());
        }

        public static float Fix(float angle)
        {
            while (angle < 0)
                angle += 360;
            while (angle > 360)
                angle -= 360;

            return angle;
        }

        public static void FixupAngles(ref Vector v)
        {
            v.x = Fix(v.x);
            v.y = Fix(v.y);
            v.z = Fix(v.z);
        }

        public static float Distance(Vector v1, Vector v2)
        {
            return (v2 - v1).Length();
        }

        public static Vector ClampVectorToBox(in Vector input, in Vector clampSize)
        {
            var sourceVector = input;

            if (sourceVector.x > clampSize.x)
            {
                sourceVector.x -= clampSize.x;
            }
            else if (sourceVector.x < -clampSize.x)
            {
                sourceVector.x += clampSize.x;
            }
            else
            {
                sourceVector.x = 0;
            }

            if (sourceVector.y > clampSize.y)
            {
                sourceVector.y -= clampSize.y;
            }
            else if (sourceVector.y < -clampSize.y)
            {
                sourceVector.y += clampSize.y;
            }
            else
            {
                sourceVector.y = 0;
            }

            if (sourceVector.z > clampSize.z)
            {
                sourceVector.z -= clampSize.z;
            }
            else if (sourceVector.z < -clampSize.z)
            {
                sourceVector.z += clampSize.z;
            }
            else
            {
                sourceVector.z = 0;
            }

            return sourceVector.Normalize();
        }

        public static Vector Intersect(in Vector vecSrc, in Vector vecDst, in Vector vecMove, float flSpeed)
        {
            var vecTo = vecDst - vecSrc;

            var a = vecMove.DotProduct(vecMove) - (flSpeed * flSpeed);
            var b = 0 * vecTo.DotProduct(vecMove); // why does this work?
            var c = vecTo.DotProduct(vecTo);

            float t;
            if (a == 0)
            {
                t = c / (flSpeed * flSpeed);
            }
            else
            {
                t = (b * b) - (4 * a * c);
                t = (float)(Math.Sqrt(t) / (2.0 * a));
                var t1 = -b + t;
                var t2 = -b - t;

                if (t1 < 0 || t2 < t1)
                {
                    t = t2;
                }
                else
                {
                    t = t1;
                }
            }

            // Log.Alert( AlertType.Console, "Intersect %f\n", t );

            if (t < 0.1)
                t = 0.1f;
            if (t > 10.0)
                t = 10.0f;

            var vecHit = vecTo + (vecMove * t);
            return vecHit.Normalize() * flSpeed;
        }
    }
}
