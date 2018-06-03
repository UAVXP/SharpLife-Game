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
    /// A member accessor that can access properties
    /// </summary>
    public sealed class PropertyAccessor : BaseAccessor
    {
        private PropertyInfo PropInfo { get; }

        public override bool CanRead => PropInfo.CanRead;

        public override bool CanWrite => PropInfo.CanWrite;

        public override MemberInfo Info => PropInfo;

        public override Type Type => PropInfo.PropertyType;

        public PropertyAccessor(PropertyInfo info)
        {
            PropInfo = info ?? throw new ArgumentNullException(nameof(info));

            //Either the get or set accessor will be used here
            if (info.GetAccessors()[0].IsStatic)
            {
                throw new InvalidOperationException("Cannot provide property accessor for static properties");
            }
        }

        public override object Get(object instance)
        {
            return PropInfo.GetValue(instance);
        }

        public override void Set(object instance, object value)
        {
            PropInfo.SetValue(instance, value);
        }
    }
}
