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

using Server.MetaData.Accessors;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.MetaData
{
    /// <summary>
    /// Contains metadata about objects
    /// </summary>
    public sealed class ClassMetaData
    {
        private Type Type { get; }

        private ClassMetaData Parent { get; }

        private Dictionary<MemberInfo, IMemberAccessor> Members { get; } = new Dictionary<MemberInfo, IMemberAccessor>();

        public ClassMetaData(Type type, ClassMetaData parent)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Parent = parent;
        }

        /// <summary>
        /// Gets the accessor for a member
        /// The accessor is created if it does not exist
        /// </summary>
        /// <param name="info"></param>
        /// <returns>An accessor to access the given member with. Never null</returns>
        public IMemberAccessor GetAccessor(MemberInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (Members.TryGetValue(info, out var accessor))
            {
                return accessor;
            }

            //See if it's part of this type or a parent
            if (Type.Equals(info.DeclaringType))
            {
                if (info is PropertyInfo prop)
                {
                    accessor = new PropertyAccessor(prop);
                }
                else if(info is FieldInfo field)
                {
                    accessor = new FieldAccessor(field);
                }
                else if(info is MethodInfo method)
                {
                    accessor = new MethodAccessor(method);
                }

                //Cache and return it
                if (accessor != null)
                {
                    Members.Add(info, accessor);
                }

                return accessor;
            }

            //If this fails then the given member was not a member of this type's class hierarchy
            return Parent?.GetAccessor(info) ?? throw new InvalidOperationException("Could not create accessor for member");
        }
    }
}
