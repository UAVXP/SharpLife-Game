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
    /// Specifies that a field, property or method should be initialized with a value from the entity data for the declaring entity
    /// If specified on a method, the method must take exactly one parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
    public sealed class KeyValueAttribute : Attribute
    {
        /// <summary>
        /// If specified, use this instead of the field, property or method name for key lookup
        /// </summary>
        public string KeyName { get; set; }
    }
}
