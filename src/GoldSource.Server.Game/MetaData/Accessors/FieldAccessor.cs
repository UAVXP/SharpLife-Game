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
    /// <summary>
    /// A member accessor that can access fields
    /// </summary>
    public sealed class FieldAccessor : BaseAccessor
    {
        private FieldInfo FieldInfo { get; }

        public override bool CanRead => true;

        public override bool CanWrite => !FieldInfo.IsInitOnly && !FieldInfo.IsLiteral;

        public override MemberInfo Info => FieldInfo;

        public override Type Type => FieldInfo.FieldType;

        public FieldAccessor(FieldInfo info)
        {
            FieldInfo = info ?? throw new ArgumentNullException(nameof(info));

            //Either the get or set accessor will be used here
            if (info.IsStatic)
            {
                throw new InvalidOperationException("Cannot provide field accessor for static fields");
            }
        }

        public override object Get(object instance)
        {
            return FieldInfo.GetValue(instance);
        }

        public override void Set(object instance, object value)
        {
            FieldInfo.SetValue(instance, value);
        }
    }
}
