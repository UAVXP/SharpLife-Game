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

using GoldSource.Mathlib;

namespace GoldSource.Server.Game.Game.Entities.Characters
{
    /// <summary>
    /// Base class for all characters, both players and NPCs
    /// </summary>
    public class BaseCharacter : BaseToggle
    {
        protected DamageTypes m_bitsDamageType;	// what types of damage has monster (player) taken

        protected float m_flFieldOfView;// width of monster's field of view ( dot product )

        protected BloodColor m_bloodColor;		// color of blood particless

        public float m_flNextAttack;		// cannot attack again until this time

        protected Vector m_HackedGunPos;	// HACK until we can query end of gun
    }
}
