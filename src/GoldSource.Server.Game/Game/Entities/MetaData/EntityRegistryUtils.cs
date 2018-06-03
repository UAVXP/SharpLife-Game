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

using GoldSource.Server.Game.MetaData;
using GoldSource.Server.Game.MetaData.Accessors;
using GoldSource.Server.Game.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GoldSource.Server.Game.Game.Entities.MetaData
{
    public static class EntityRegistryUtils
    {
        /// <summary>
        /// Collects all public linked entity classes from the given assembly
        /// </summary>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static List<EntityInfo> CollectEntityClasses(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var list = new List<EntityInfo>();

            //TODO: should define the type used as the base class somewhere
            var entityClasses = assembly.ExportedTypes.Where(t => t.IsClass && t.IsSubclassOf(typeof(BaseEntity))).ToList();

            var dataBuilder = new MetaDataBuilder();

            entityClasses.ForEach(dataBuilder.AddClass);

            var entityDatas = dataBuilder.Build();

            foreach (var type in entityClasses)
            {
                var mapNames = type.GetCustomAttributes<LinkEntityToClassAttribute>();

                if (mapNames.Any())
                {
                    if (!type.IsAbstract)
                    {
                        if (!entityDatas.TryGetValue(type, out var entityData))
                        {
                            //This should never happen since both use the same list
                            throw new InvalidOperationException($"Missing entity data for class {type.FullName}");
                        }

                        string preferredName = null;

                        foreach (var mapName in mapNames)
                        {
                            if (mapName.IsPreferredName)
                            {
                                if (preferredName == null)
                                {
                                    preferredName = mapName.MapName;
                                }
                                else
                                {
                                    Log.Message($"Entity class {type.FullName} has multiple preferred names, ignoring remainder");
                                    //Only need to show this once
                                    break;
                                }
                            }
                        }

                        if (preferredName == null)
                        {
                            preferredName = mapNames.First().MapName;

                            if (mapNames.Count() > 1)
                            {
                                Log.Message($"Entity class {type.FullName} has {mapNames.Count()} map names and specified no preferred name, using {preferredName}");
                            }
                        }

                        var keyValues = GetKeyValueAccessorsForType(type, entityData);

                        var persistedMembers = GetPersistAccessorsForType(type, entityData);

                        list.Add(new EntityInfo(type, preferredName, mapNames.Select(l => l.MapName).ToList(), entityData, keyValues, persistedMembers));
                    }
                    else
                    {
                        Log.Message($"Cannot add entity class {type.FullName} because it is abstract");
                    }
                }
            }

            return list;
        }

        private static void VisitAccessibleMembers<TAttribute, TMember>(TMember[] members, Action<TAttribute, MemberInfo> visitor)
            where TAttribute : Attribute
            where TMember : MemberInfo
        {
            foreach (var member in members)
            {
                var attribute = member.GetCustomAttribute<TAttribute>();

                if (attribute != null)
                {
                    visitor(attribute, member);
                }
            }
        }

        /// <summary>
        /// Visits all accessible members and invokes the given visitor on them
        /// An accessible member is one that has a given attribute set on them
        /// </summary>
        /// <typeparam name="TAttribute">Type of the attribute to look for</typeparam>
        /// <param name="type"></param>
        /// <param name="visitor"></param>
        private static void VisitAccessibleMembers<TAttribute>(Type type, Action<TAttribute, MemberInfo> visitor)
            where TAttribute : Attribute
        {
            const BindingFlags sharedFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            VisitAccessibleMembers(type.GetFields(sharedFlags), visitor);
            VisitAccessibleMembers(type.GetProperties(sharedFlags), visitor);
            VisitAccessibleMembers(type.GetMethods(sharedFlags), visitor);
        }

        /// <summary>
        /// Gets all of the members of the given type marked as being a keyvalue
        /// </summary>
        /// <param name="type"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        private static IReadOnlyDictionary<string, IMemberAccessor> GetKeyValueAccessorsForType(Type type, ClassMetaData metaData)
        {
            //Use case insensitive comparison to match the engine
            var keyValues = new Dictionary<string, IMemberAccessor>(StringComparer.OrdinalIgnoreCase);

            VisitAccessibleMembers<KeyValueAttribute>(type, (kv, info) =>
            {
                var name = kv.KeyName ?? info.Name;

                if (!string.IsNullOrWhiteSpace(name))
                {
                    var accessor = metaData.GetAccessor(info);

                    if (!keyValues.ContainsKey(name))
                    {
                        keyValues.Add(name, accessor);
                    }
                    else
                    {
                        //TODO: consider allowing derived keyvalues to override base?
                        Log.Message($"Warning: Cannot consider member {type.FullName}.{info.Name} for keyvalue usage because another keyvalue with name \"{name}\" already exists");
                    }
                }
                else
                {
                    Log.Message($"Warning: Cannot consider member {type.FullName}.{info.Name} for keyvalue usage because it has an invalid name \"{name}\"");
                }
            }
            );

            return keyValues;
        }

        /// <summary>
        /// Gets all of the members of the given type marked as needing to be persisted
        /// </summary>
        /// <param name="type"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        private static IReadOnlyList<IMemberAccessor> GetPersistAccessorsForType(Type type, ClassMetaData metaData)
        {
            //Use case insensitive comparison to match the engine
            var persistedMembers = new List<IMemberAccessor>();

            VisitAccessibleMembers<PersistAttribute>(type, (_, info) =>
            {
                var accessor = metaData.GetAccessor(info);

                if (accessor.CanRead && accessor.CanWrite)
                {
                    persistedMembers.Add(accessor);
                }
                else
                {
                    var messageBuilder = new StringBuilder();

                    messageBuilder.Append("Warning: cannot persist member ")
                    .Append(type.FullName)
                    .Append(".")
                    .Append(accessor.Info.Name)
                    .Append(" because it is not ");

                    if (!accessor.CanRead)
                    {
                        messageBuilder.Append("readable ");

                        if (!accessor.CanWrite)
                        {
                            messageBuilder.Append(" and not");
                        }
                    }

                    if (!accessor.CanWrite)
                    {
                        messageBuilder.Append("writable");
                    }

                    Log.Message(messageBuilder.ToString());
                }
            }
            );

            return persistedMembers;
        }
    }
}
