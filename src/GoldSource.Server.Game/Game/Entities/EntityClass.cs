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

namespace GoldSource.Server.Game.Game.Entities
{
    /// <summary>
    /// Classification IDs for entity relationships
    /// </summary>
    public enum EntityClass
    {
        None = 0,
        Machine = 1,
        Player = 2,
        HumanPassive = 3,
        HumanMilitary = 4,
        AlienMilitary = 5,
        AlienPassive = 6,
        AlienMonster = 7,
        AlienPrey = 8,
        AlienPredator = 9,
        Insect = 10,
        PlayerAlly = 11,

        /// <summary>
        /// Hornets and snarks
        /// Launched by players
        /// </summary>
        PlayerBioweapon = 12,

        /// <summary>
        /// Hornets and snarks
        /// Launched by the alien menace
        /// </summary>
        AlienBioweapon = 13,

        /// <summary>
        /// Special because no one pays attention to it, and it eats a wide cross-section of creatures.
        /// </summary>
        Barnacle = 99,
    }
}
