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

namespace Server.MetaData.Accessors
{
    public abstract class BaseAccessor : IMemberAccessor
    {
        public abstract bool CanRead { get; }

        public abstract bool CanWrite { get; }

        public abstract MemberInfo Info { get; }

        public abstract Type Type { get; }

        public abstract object Get(object instance);

        public abstract void Set(object instance, object value);
    }
}
