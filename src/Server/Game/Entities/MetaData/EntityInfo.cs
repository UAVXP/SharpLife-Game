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

using Server.MetaData;
using Server.MetaData.Accessors;
using System;
using System.Collections.Generic;

namespace Server.Game.Entities.MetaData
{
    /// <summary>
    /// Contains metadata about an entity class
    /// </summary>
    public sealed class EntityInfo
    {
        public Type EntityClass { get; }

        /// <summary>
        /// If an entity class has multiple map names, this is the name that it prefers to use, and the name that it will be given when created
        /// </summary>
        public string PreferredName { get; }

        public IReadOnlyList<string> MapNames { get; }

        public ClassMetaData EntityData { get; }

        /// <summary>
        /// The keyvalues that this entity has
        /// </summary>
        public IReadOnlyDictionary<string, IMemberAccessor> KeyValues { get; }

        /// <summary>
        /// The persisted members that this entity has
        /// </summary>
        public IReadOnlyList<IMemberAccessor> PersistedMembers { get; }

        public EntityInfo(Type entityClass, string preferredName, IReadOnlyList<string> mapNames,
            ClassMetaData entityData,
            IReadOnlyDictionary<string, IMemberAccessor> keyValues,
            IReadOnlyList<IMemberAccessor> persistedMembers)
        {
            EntityClass = entityClass ?? throw new ArgumentNullException(nameof(entityClass));

            if (!entityClass.IsClass)
            {
                throw new ArgumentException("Entity class type must be a class", nameof(entityClass));
            }

            if (entityClass.IsAbstract)
            {
                throw new ArgumentException("Entity class must be a concrete type", nameof(entityClass));
            }

            PreferredName = preferredName ?? throw new ArgumentNullException(nameof(preferredName));
            MapNames = mapNames ?? throw new ArgumentNullException(nameof(mapNames));
            EntityData = entityData ?? throw new ArgumentNullException(nameof(entityData));
            KeyValues = keyValues ?? throw new ArgumentNullException(nameof(keyValues));
            PersistedMembers = persistedMembers ?? throw new ArgumentNullException(nameof(persistedMembers));
        }
    }
}
