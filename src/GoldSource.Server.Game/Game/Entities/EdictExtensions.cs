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

namespace GoldSource.Server.Game.Game.Entities
{
    public static class EdictExtensions
    {
        public static T TryGetEntity<T>(this Edict edict)
            where T : BaseEntity
        {
            return edict.PrivateData as T;
        }

        public static BaseEntity TryGetEntity(this Edict edict)
        {
            return edict.PrivateData as BaseEntity;
        }

        public static T Entity<T>(this Edict edict)
            where T : BaseEntity
        {
            return (T)edict.PrivateData;
        }

        public static BaseEntity Entity(this Edict edict)
        {
            return (BaseEntity)edict.PrivateData;
        }
    }
}
