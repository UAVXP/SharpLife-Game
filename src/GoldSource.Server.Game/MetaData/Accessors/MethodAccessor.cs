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
    public sealed class MethodAccessor : BaseAccessor
    {
        private MethodInfo MemberInfo { get; }

        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override MemberInfo Info => MemberInfo;

        public override Type Type { get; }

        public MethodAccessor(MethodInfo info)
        {
            MemberInfo = info ?? throw new ArgumentNullException(nameof(info));

            if(MemberInfo.GetParameters().Length != 1)
            {
                throw new ArgumentException($"The method {Info.DeclaringType.FullName}.{Info.Name} must take exactly one argument", nameof(info));
            }

            //Either the get or set accessor will be used here
            if (info.IsStatic)
            {
                throw new InvalidOperationException("Cannot provide method accessor for static methods");
            }

            Type = MemberInfo.GetParameters()[0].ParameterType;
        }

        public override object Get(object instance)
        {
            throw new NotSupportedException("Reading from methods is not supported");
        }

        public override void Set(object instance, object value)
        {
            MemberInfo.Invoke(instance, new[] { value });
        }
    }
}
