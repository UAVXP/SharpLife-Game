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

namespace Server.Game.Entities.MetaData
{
    /// <summary>
    /// Links an entity class to a map entity name
    /// A class can be mapped from multiple map names
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class LinkEntityToClassAttribute : Attribute
    {
        public string MapName { get; }

        /// <summary>
        /// Whether this is the preferred map name to use
        /// If an entity has more than one map name, set a preferred name to ensure it is always used for instances
        /// Otherwise, the first found map name will be used
        /// Default false
        /// </summary>
        public bool IsPreferredName { get; set; }

        public LinkEntityToClassAttribute(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                throw new ArgumentException("Entity mapname must be valid", nameof(mapName));
            }

            MapName = mapName;
        }
    }
}
