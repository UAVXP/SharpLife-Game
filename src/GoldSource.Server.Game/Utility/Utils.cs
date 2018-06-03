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
using System.Collections.Generic;
using System.Collections.Specialized;

namespace GoldSource.Server.Game.Utility
{
    /// <summary>
    /// General purpose utility code
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Swap two values
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T tmp = lhs;
            lhs = rhs;
            rhs = tmp;
        }

        public static void StringToVector(string str, out Vector vector)
        {
            vector = new Vector();

            var stringIndex = 0;

            //TODO: verify that this is correct
            for (var j = 0; j < 3; ++j)
            {
                float.TryParse(str.Substring(stringIndex), out var result);

                vector[j] = result;

                stringIndex = str.IndexOf(' ', stringIndex + 1);

                if (-1 == stringIndex)
                    break;

                ++stringIndex;
            }
        }

        public static void StringToIntArray(string str, IList<int> vector)
        {
            var stringIndex = 0;

            int j;

            //TODO: verify that this is correct
            for (j = 0; j < vector.Count; ++j)
            {
                int.TryParse(str.Substring(stringIndex), out var result);

                vector[j] = result;

                stringIndex = str.IndexOf(' ', stringIndex + 1);

                if (-1 == stringIndex)
                    break;

                ++stringIndex;
            }

            for (j++; j < vector.Count; ++j)
            {
                vector[j] = 0;
            }
        }

        /// <summary>
        /// Compares two bit vectors and returns true if the vectors contain the same sequence of bits
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool BitVectorsEqual(ref BitVector32 lhs, ref BitVector32 rhs)
        {
            for (var i = 0; i < 32; ++i)
            {
                if (lhs[i] != rhs[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
