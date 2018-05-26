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

using Server.Engine.API;
using Server.Game.Entities.MetaData;
using Server.Game.Sound;
using Server.GameRules;

namespace Server.Game
{
    /// <summary>
    /// Globals from the engine and the game
    /// </summary>
    public static class Engine
    {
        public static IEngineServer Server { get; set; }

        public static IGlobalVars Globals { get; set; }

        public static IFileSystem FileSystem { get; set; }

        public static IEntityDictionary EntityDictionary { get; set; }

        public static IEntityRegistry EntityRegistry { get; set; }

        public static IEngineEntities Entities { get; set; }

        public static ISoundSystem Sound { get; set; }

        public static VoiceGameManager VoiceGameManager { get; } = new VoiceGameManager();

        public static IGameRules GameRules { get; set; }
    }
}
