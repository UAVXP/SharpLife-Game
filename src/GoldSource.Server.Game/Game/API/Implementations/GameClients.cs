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

using GoldSource.Server.Engine.API;
using GoldSource.Server.Engine.Game.API;
using GoldSource.Server.Engine.Networking;
using GoldSource.Server.Game.Game.Entities;
using GoldSource.Server.Game.Game.Entities.Characters;
using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Shared.Entities;
using System;

namespace GoldSource.Server.Game.Game.API.Implementations
{
    public sealed class GameClients : IGameClients
    {
        private IEntityRegistry EntityRegistry { get; }

        public GameClients(IEntityRegistry entityRegistry)
        {
            EntityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));
        }

        public bool Connect(Edict entity, string name, string address, out string rejectReason)
        {
            rejectReason = string.Empty;

            return true;
        }

        public void Disconnect(Edict entity)
        {

        }

        public void Kill(Edict pEntity)
        {

        }

        public void PutInServer(Edict pEntity)
        {
            var player = EntityRegistry.CreateInstance<BasePlayer>(pEntity);

            player.SetCustomDecalFrames(-1); // Assume none;

            // Allocate a CBasePlayer for pev, and call spawn
            player.Spawn();

            // Reset interpolation during first frame
            player.Effects |= EntityEffects.NoInterp;

            player.pev.UserInt1 = 0;   // disable any spec modes
            player.pev.UserInt2 = 0;
        }

        public void Command(Edict pEntity, ICommand command)
        {
            // Is the client spawned yet?
            var player = pEntity.TryGetEntity<BasePlayer>();

            if (player == null)
            {
                return;
            }

            var commandName = command.Name;

            //TODO: implement commands

            if (commandName == PlayerUtils.SayCommandName)
            {
                PlayerUtils.HostSay(player, command, false);
            }
            else if (commandName == PlayerUtils.SayTeamCommandName)
            {
                PlayerUtils.HostSay(player, command, true);
            }
            else if (commandName == "closemenus")
            {
                // just ignore it
            }
            else if (Engine.GameRules.ClientCommand(player, command))
            {
                // MenuSelect returns true only if the command is properly handled,  so don't print a warning
            }
            else
            {
                // check the length of the command (prevents crash)
                // max total length is 192 ...and we're adding a string below ("Unknown command: %s\n")
                var printableName = commandName.Length > 127 ? commandName.Substring(0, 127) : commandName;

                // tell the user they entered an unknown command
                PlayerUtils.ClientPrint(player, HudPrint.Console, $"Unknown command: {printableName}\n");
            }
        }

        public void UserInfoChanged(Edict pEntity, IInfoBuffer infoBuffer)
        {

        }

        public void PreThink(Edict pEntity)
        {
            pEntity.TryGetEntity<BasePlayer>()?.PreThink();
        }

        public void PostThink(Edict pEntity)
        {
            pEntity.TryGetEntity<BasePlayer>()?.PostThink();
        }

        public void Customization(Edict entity, Customization custom)
        {
        }
    }
}
