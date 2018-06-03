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

namespace Server.Game.Entities
{
    //TODO: this class is used to accumulate damage, so pass it as an argument instead of using it as a global
    public sealed class MultiDamage
    {
        public BaseEntity Entity { get; set; }
        public float Amount { get; set; }
        public DamageTypes Type { get; set; }

        /// <summary>
        /// Resets the global multi damage accumulator
        /// </summary>
        public void Clear()
        {
            Entity = null;
            Amount = 0;
            Type = 0;
        }

        /// <summary>
        /// Inflicts contents of global multi damage register on Entity
        /// </summary>
        /// <param name="inflictor"></param>
        /// <param name="attacker"></param>
        public void ApplyMultiDamage(BaseEntity inflictor, BaseEntity attacker)
        {
            Entity?.TakeDamage(inflictor, attacker, Amount, Type);
        }

        public void AddMultiDamage(BaseEntity inflictor, BaseEntity entity, float flDamage, DamageTypes bitsDamageType)
        {
            if (entity == null)
                return;

            Type |= bitsDamageType;

            if (entity != Entity)
            {
                ApplyMultiDamage(inflictor, inflictor); // UNDONE: wrong attacker!
                Entity = entity;
                Amount = 0;
            }

            Amount += flDamage;
        }
    }
}
