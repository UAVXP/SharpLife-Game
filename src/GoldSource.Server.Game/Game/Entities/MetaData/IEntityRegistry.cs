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

using GoldSource.Shared.Entities;
using System.Collections.Generic;

namespace Server.Game.Entities.MetaData
{
    /// <summary>
    /// Contains all known entity classes
    /// </summary>
    public interface IEntityRegistry
    {
        /// <summary>
        /// Gets the list of all entity classes that have been registered
        /// </summary>
        IReadOnlyList<EntityInfo> Entities { get; }

        /// <summary>
        /// Attempts to add an entity class
        /// </summary>
        /// <param name="info"></param>
        /// <returns>Whether the entity class was added</returns>
        bool AddEntityClass(EntityInfo info);

        /// <summary>
        /// Looks up an entity class by one of its map names
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        EntityInfo FindEntityByMapName(string mapName);

        /// <summary>
        /// Tries to create an instance of an entity
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns>An entity instance, or null if it could not be created</returns>
        BaseEntity CreateInstance(string mapName);

        /// <summary>
        /// Creates an entity from an info object using the given edict
        /// </summary>
        /// <param name="info"></param>
        /// <param name="edict"></param>
        /// <returns>An entity instance, or null if it could not be created</returns>
        BaseEntity CreateInstance(EntityInfo info, Edict edict);

        /// <summary>
        /// Creates an entity from an info object
        /// </summary>
        /// <param name="info"></param>
        /// <returns>An entity instance, or null if it could not be created</returns>
        BaseEntity CreateInstance(EntityInfo info);

        /// <summary>
        /// Creates an entity from an entity class type
        /// </summary>
        /// <typeparam name="T">An entity class type. This type must have a map name defined for it</typeparam>
        /// <returns>An entity instance, or null if it could not be created</returns>
        T CreateInstance<T>() where T : BaseEntity;

        /// <summary>
        /// Creates an entity from an entity class type, and assigns it the given edict
        /// </summary>
        /// <typeparam name="T">An entity class type. This type must have a map name defined for it</typeparam>
        /// <param name="edict">An existing edict to assign to the entity</param>
        /// <returns>An entity instance, or null if it could not be created</returns>
        T CreateInstance<T>(Edict edict) where T : BaseEntity;
    }
}
