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
using System.ComponentModel;

namespace GoldSource.Server.Game.Game.Entities.MetaData
{
    public static class KeyValueUtils
    {
        /// <summary>
        /// Tries to set a keyvalue on an entity
        /// Uses the TypeDescriptor conversion library, requires that types provide a <see cref="TypeConverterAttribute"/> to specify a converter
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="entityInfo"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TrySetKeyValue(object instance, EntityInfo entityInfo, string key, string value)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (entityInfo == null)
            {
                throw new ArgumentNullException(nameof(entityInfo));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!entityInfo.KeyValues.TryGetValue(key, out var accessor))
            {
                return false;
            }

            //Convert the type if possible
            //TODO: could test if the converter is available during startup to improve diagnostics
            try
            {
                var converter = TypeDescriptor.GetConverter(accessor.Type);

                if (converter == null)
                {
                    throw new NotSupportedException("No converter for type");
                }

                var input = converter.ConvertFromInvariantString(value);

                accessor.Set(instance, input);

                return true;
            }
            catch(Exception e)
            {
                Log.Message($"Couldn't convert keyvalue from string to type {accessor.Type.FullName}");
                Log.Exception(e);
                return false;
            }
        }
    }
}
