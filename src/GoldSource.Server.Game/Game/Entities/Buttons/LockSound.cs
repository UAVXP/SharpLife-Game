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

namespace GoldSource.Server.Game.Game.Entities.Buttons
{
    /// <summary>
    /// sounds that doors and buttons make when locked/unlocked
    /// </summary>
    public struct LockSound
    {
        /// <summary>
        /// sound a door makes when it's locked
        /// </summary>
        public string LockedSound;

        /// <summary>
        /// sentence group played when door is locked
        /// </summary>
        public string LockedSentence;

        /// <summary>
        /// sound a door makes when it's unlocked
        /// </summary>
        public string UnlockedSound;

        /// <summary>
        /// sentence group played when door is unlocked
        /// </summary>
        public string UnlockedSentence;

        /// <summary>
        /// which sentence in sentence group to play next
        /// </summary>
        public int NextLockedSentence;

        /// <summary>
        /// which sentence in sentence group to play next
        /// </summary>
        public int NextUnlockedSentence;

        /// <summary>
        /// time delay between playing consecutive 'locked/unlocked' sounds
        /// </summary>
        public float WaitSound;

        /// <summary>
        /// time delay between playing consecutive sentences
        /// </summary>
        public float WaitSentence;

        /// <summary>
        /// true if hit end of list of locked sentences
        /// </summary>
        public bool EOFLocked;

        /// <summary>
        /// true if hit end of list of unlocked sentences
        /// </summary>
        public bool EOFUnlocked;
    }
}
