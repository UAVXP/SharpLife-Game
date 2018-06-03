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
using System.Reflection;

namespace GoldSource.Server.Game.MetaData.Accessors
{
    /// <summary>
    /// Provides a way to access a specific member type
    /// </summary>
    public interface IMemberAccessor
    {
        /// <summary>
        /// Whether the member can be read from
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// Whether the member can be written to
        /// </summary>
        bool CanWrite { get; }

        /// <summary>
        /// Gets the info object that represents the member
        /// </summary>
        MemberInfo Info { get; }

        /// <summary>
        /// Type of the value
        /// </summary>
        Type Type { get; }

        object Get(object instance);

        void Set(object instance, object value);
    }
}
