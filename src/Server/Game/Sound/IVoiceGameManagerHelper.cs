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

using Server.Game.Entities.Characters;

namespace Server.Game.Sound
{
    public interface IVoiceGameManagerHelper
    {
        /// <summary>
        /// Called each frame to determine which players are allowed to hear each other 
        /// This overrides whatever squelch settings players have
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="talker"></param>
        /// <returns></returns>
        bool CanPlayerHearPlayer(BasePlayer listener, BasePlayer talker);
    }
}
