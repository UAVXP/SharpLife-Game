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

namespace GoldSource.Server.Game.MetaData
{
    /// <summary>
    /// Builds metadata from a set of classes
    /// </summary>
    public sealed class MetaDataBuilder
    {
        private ISet<Type> Classes { get; } = new HashSet<Type>();

        public void AddClass(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Classes.Add(type);
        }

        /// <summary>
        /// Builds a map of class types to metadata
        /// </summary>
        /// <returns></returns>
        public IDictionary<Type, ClassMetaData> Build()
        {
            var map = new Dictionary<Type, ClassMetaData>();

            //Build an object for each class that can retrieve metadata for it
            //Metadata is generated on-demand

            foreach (var clazz in Classes)
            {
                BuildClass(map, clazz);
            }

            return map;
        }

        private ClassMetaData BuildClass(IDictionary<Type, ClassMetaData> map, Type type)
        {
            if (type == typeof(object))
            {
                return null;
            }

            //Already in map, just return
            if (map.TryGetValue(type, out var value))
            {
                return value;
            }

            var parent = BuildClass(map, type.BaseType);

            var data = new ClassMetaData(type, parent);

            map.Add(type, data);

            return data;
        }
    }
}
