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

namespace GoldSource.Server.Game.Game
{
    /// <summary>
    /// Dot products for view cone checking
    /// </summary>
    public static class ViewField
    {
        /// <summary>
        /// +-180 degrees
        /// </summary>
        public const float Full = -1.0f;

        /// <summary>
        /// +-135 degrees 0.1 // +-85 degrees, used for full FOV checks 
        /// </summary>
        public const float Wide = -0.7f;

        /// <summary>
        /// +-45 degrees, more narrow check used to set up ranged attacks
        /// </summary>
        public const float Narrow = 0.7f;

        /// <summary>
        /// +-25 degrees, more narrow check used to set up ranged attacks
        /// </summary>
        public const float UltraNarrow = 0.9f;
    }
}
