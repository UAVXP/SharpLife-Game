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

using Server.Engine;
using Server.Engine.API;
using System;
using System.Collections.Generic;

namespace Server.Game.Entities.MetaData
{
    public sealed class EntityRegistry : IEntityRegistry
    {
        private IEntityDictionary EntityDictionary { get; }

        private List<EntityInfo> Entities { get; } = new List<EntityInfo>();

        private Dictionary<string, EntityInfo> MapNameMapping { get; } = new Dictionary<string, EntityInfo>();

        IReadOnlyList<EntityInfo> IEntityRegistry.Entities => Entities;

        public EntityRegistry(IEntityDictionary entityDictionary)
        {
            EntityDictionary = entityDictionary ?? throw new ArgumentNullException(nameof(entityDictionary));
        }

        public bool AddEntityClass(EntityInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (Entities.Contains(info))
            {
                throw new ArgumentException("Cannot add the same entity info twice", nameof(info));
            }

            //Constrain the type so we don't have errors while trying to create them later
            var defaultConstructor = info.EntityClass.GetConstructor(new Type[] { });

            if (defaultConstructor == null)
            {
                throw new ArgumentException($"The entity class {info.EntityClass.FullName} does not define a public default constructor and cannot be instantiated", nameof(info));
            }

            var failed = 0;

            foreach (var mapName in info.MapNames)
            {
                if (!MapNameMapping.TryAdd(mapName, info))
                {
                    Log.Message($"An entity with map name \"{mapName}\" has already been added (While adding entity with preferred name\"{info.PreferredName}\")");
                    ++failed;
                }
            }

            //Don't add it if all names are taken
            if (failed < info.MapNames.Count)
            {
                Entities.Add(info);
                return true;
            }

            return false;
        }

        public EntityInfo FindEntityByMapName(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
            {
                throw new ArgumentException("Entity map name must be valid");
            }

            MapNameMapping.TryGetValue(mapName, out var value);

            return value;
        }

        public BaseEntity CreateInstance(string mapName)
        {
            var info = FindEntityByMapName(mapName);

            if (info == null)
            {
                Log.Message($"Could not find an entity class \"{mapName}\" to create an instance of");
                return null;
            }

            return CreateInstance(info);
        }

        public BaseEntity CreateInstance(EntityInfo info, Edict edict)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            try
            {
                var entity = (BaseEntity)Activator.CreateInstance(info.EntityClass);

                edict.PrivateData = entity;

                entity.pev = edict.Vars;

                entity.ClassName = info.PreferredName;

                //Inform of creation
                entity.OnCreate();

                return entity;
            }
            catch (Exception e)
            {
                Log.Message($"Couldn't create entity \"{info.PreferredName}\":");
                Log.Exception(e);

                //On failure always free the edict
                //This will also free the entity instance if it has been assigned
                EntityDictionary.Free(edict);

                return null;
            }
        }

        public BaseEntity CreateInstance(EntityInfo info)
        {
            return CreateInstance(info, EntityDictionary.Allocate());
        }

        private EntityInfo LookupInfoFromType(Type type)
        {
            var info = Entities.Find(ei => ei.EntityClass.Equals(type));

            //A class must have a map name because it needs to be set, and it needs to match what level designers use to refer to it
            if (info == null)
            {
                Log.Message($"Entity class \"{type.FullName}\" does not have a map name assigned to it and cannot be created");
                return null;
            }

            return info;
        }

        public T CreateInstance<T>() where T : BaseEntity
        {
            var info = LookupInfoFromType(typeof(T));

            return info != null ? (T)CreateInstance(info) : null;
        }

        public T CreateInstance<T>(Edict edict) where T : BaseEntity
        {
            var info = LookupInfoFromType(typeof(T));

            return info != null ? (T)CreateInstance(info, edict) : null;
        }
    }
}
