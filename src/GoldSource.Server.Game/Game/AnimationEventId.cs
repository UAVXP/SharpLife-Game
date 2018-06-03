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

namespace GoldSource.Server.Game.Game
{
    public enum AnimationEventId
    {
        Specific = 0,
        Scripted = 1000,

        /// <summary>
        /// character is now dead
        /// </summary>
        ScriptDead = 1000,

        /// <summary>
        /// does not allow interrupt
        /// </summary>
        ScriptNoInterrupt = 1001,

        /// <summary>
        /// will allow interrupt
        /// </summary>
        ScriptCanInterrupt = 1002,

        /// <summary>
        /// event now fires
        /// </summary>
        ScriptFireEvent = 1003,

        /// <summary>
        /// Play named wave file (on SoundChannel.Body)
        /// </summary>
        ScriptSound = 1004,

        /// <summary>
        /// Play named sentence
        /// </summary>
        ScriptSentence = 1005,

        /// <summary>
        /// Leave the character in air at the end of the sequence (don't find the floor)
        /// </summary>
        ScriptInAir = 1006,

        /// <summary>
        /// Set the animation by name after the sequence completes
        /// </summary>
        ScriptEndAnimation = 1007,

        /// <summary>
        /// Play named wave file (on SoundChannel.Voice)
        /// </summary>
        ScriptSoundVoice = 1008,

        /// <summary>
        /// Play sentence group 25% of the time
        /// </summary>
        ScriptSentenceRandom1 = 1009,

        /// <summary>
        /// Bring back to life (for life/death sequences)
        /// </summary>
        ScriptNotDead = 1010,

        Shared = 2000,
        Client = 5000,
    }
}
