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
using System;

namespace Server.Utility
{
    /// <summary>
    /// 3x4 matrix
    /// Quick and dirty matrix class until we drop support for engine interop
    /// </summary>
    public sealed class Mat3x4
    {
        public const int NumRows = 3;
        public const int NumColumns = 4;

        private readonly float[,] _matrix = new float[NumRows, NumColumns];

        public static Mat3x4 AngleToMatrix(in Vector angles)
        {
            var angle = angles[MathUtils.YAW] * (Math.PI * 2 / 360);
            var sy = Math.Sin(angle);
            var cy = Math.Cos(angle);
            angle = angles[MathUtils.PITCH] * (Math.PI * 2 / 360);
            var sp = Math.Sin(angle);
            var cp = Math.Cos(angle);
            angle = angles[MathUtils.ROLL] * (Math.PI * 2 / 360);
            var sr = Math.Sin(angle);
            var cr = Math.Cos(angle);

            var matrix = new Mat3x4();

            // matrix = (YAW * PITCH) * ROLL
            matrix._matrix[0, 0] = (float)(cp * cy);
            matrix._matrix[1, 0] = (float)(cp * sy);
            matrix._matrix[2, 0] = (float)(-sp);
            matrix._matrix[0, 1] = (float)((sr * sp * cy) + (cr * -sy));
            matrix._matrix[1, 1] = (float)((sr * sp * sy) + (cr * cy));
            matrix._matrix[2, 1] = (float)(sr * cp);
            matrix._matrix[0, 2] = (float)((cr * sp * cy) + (-sr * -sy));
            matrix._matrix[1, 2] = (float)((cr * sp * sy) + (-sr * cy));
            matrix._matrix[2, 2] = (float)(cr * cp);
            matrix._matrix[0, 3] = 0.0f;
            matrix._matrix[1, 3] = 0.0f;
            matrix._matrix[2, 3] = 0.0f;

            return matrix;
        }

        public static Mat3x4 AngleToMatrixInverted(in Vector angles)
        {
            var angle = angles[MathUtils.YAW] * (Math.PI * 2 / 360);
            var sy = Math.Sin(angle);
            var cy = Math.Cos(angle);
            angle = angles[MathUtils.PITCH] * (Math.PI * 2 / 360);
            var sp = Math.Sin(angle);
            var cp = Math.Cos(angle);
            angle = angles[MathUtils.ROLL] * (Math.PI * 2 / 360);
            var sr = Math.Sin(angle);
            var cr = Math.Cos(angle);

            var matrix = new Mat3x4();

            // matrix = (YAW * PITCH) * ROLL
            matrix._matrix[0, 0] = (float)(cp * cy);
            matrix._matrix[0, 1] = (float)(cp * sy);
            matrix._matrix[0, 2] = (float)-sp;
            matrix._matrix[1, 0] = (float)((sr * sp * cy) + (cr * -sy));
            matrix._matrix[1, 1] = (float)((sr * sp * sy) + (cr * cy));
            matrix._matrix[1, 2] = (float)(sr * cp);
            matrix._matrix[2, 0] = ((float)((cr * sp * cy) + (-sr * -sy)));
            matrix._matrix[2, 1] = ((float)((cr * sp * sy) + (-sr * cy)));
            matrix._matrix[2, 2] = (float)(cr * cp);
            matrix._matrix[0, 3] = 0.0f;
            matrix._matrix[1, 3] = 0.0f;
            matrix._matrix[2, 3] = 0.0f;

            return matrix;
        }

        public Vector VectorTransform(in Vector vector)
        {
            return new Vector(
                    ((vector.x * _matrix[0, 0]) + (vector.y * _matrix[0, 1]) + (vector.z * _matrix[0, 2])) + _matrix[0, 3],
                    ((vector.x * _matrix[1, 0]) + (vector.y * _matrix[1, 1]) + (vector.z * _matrix[1, 2])) + _matrix[1, 3],
                    ((vector.x * _matrix[2, 0]) + (vector.y * _matrix[2, 1]) + (vector.z * _matrix[2, 2])) + _matrix[2, 3]
                );
        }
    }
}
