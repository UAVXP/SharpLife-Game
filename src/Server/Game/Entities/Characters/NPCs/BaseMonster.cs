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

using Server.Engine;

namespace Server.Game.Entities.Characters.NPCs
{
    public class BaseMonster : BaseCharacter
    {
        //TODO: could move these to BaseEntity
        public void RadiusDamage(BaseEntity inflictor, BaseEntity attacker, float damage, EntityClass classIgnore, DamageTypes damageTypes)
        {
            EntUtils.RadiusDamage(Origin, inflictor, attacker, damage, damage * 2.5f, classIgnore, damageTypes);
        }

        public void RadiusDamage(in Vector vecSrc, BaseEntity inflictor, BaseEntity attacker, float damage, EntityClass classIgnore, DamageTypes damageTypes)
        {
            EntUtils.RadiusDamage(vecSrc, inflictor, attacker, damage, damage * 2.5f, classIgnore, damageTypes);
        }
    }
}
