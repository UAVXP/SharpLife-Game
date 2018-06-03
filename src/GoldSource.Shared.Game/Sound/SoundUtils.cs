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

namespace GoldSource.Shared.Game.Sound
{
    public static class SoundUtils
    {
        /// <summary>
        /// Given a texture name referenced by a map, returns the base name without modifiers
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetTextureBaseName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length >= 2)
            {
                // strip leading '-0' or '+0~' or '{' or '!'
                if (name[0] == '-' || name[0] == '+')
                {
                    name = name.Substring(2);
                }
            }

            if (name.Length > 0)
            {
                if (name[0] == '{' || name[0] == '!' || name[0] == '~' || name[0] == ' ')
                {
                    name = name.Substring(1);
                }
            }

            return name;
        }
    }
}
