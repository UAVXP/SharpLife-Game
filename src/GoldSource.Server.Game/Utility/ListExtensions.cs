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

using System;
using System.Collections.Generic;

namespace GoldSource.Server.Game.Utility
{
    public static class ListExtensions
    {
        public static bool SequenceEqual<T>(this IList<T> first, int startIndex, IList<T> second, int secondIndex, int count)
        {
            if (startIndex >= first.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (startIndex + count >= first.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (null == second)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (secondIndex >= second.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(secondIndex));
            }

            if (secondIndex + count >= second.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            for (var i = 0; i < count; ++i)
            {
                if (!first[startIndex + i].Equals(second[i + secondIndex]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
