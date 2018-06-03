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

using GoldSource.Server.Engine;
using GoldSource.Server.Engine.API;
using GoldSource.Server.Engine.CVar;
using GoldSource.Server.Game.Engine;
using GoldSource.Server.Game.Game.Entities;
using GoldSource.Server.Game.Game.Entities.Characters;
using GoldSource.Server.Game.Utility;
using GoldSource.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;

namespace GoldSource.Server.Game.Game.Sound
{
    /// <summary>
    /// Manages which clients can hear which other clients
    /// TODO: this class will need revisiting if the maximum number of players changes
    /// </summary>
    public sealed class VoiceGameManager
    {
        /// <summary>
        /// (todo: this should just be set to MAX_CLIENTS)
        /// </summary>
        public const int VOICE_MAX_PLAYERS = 32;
        public const int VOICE_MAX_PLAYERS_DW = ((VOICE_MAX_PLAYERS / 32) + (VOICE_MAX_PLAYERS & 31) != 0 ? 1 : 0);

        public const double UPDATE_INTERVAL = 0.3;

        //TODO: this needs to be stored in a separate instance so the data can be kept after map changes
        private class PlayerData
        {
            /// <summary>
            /// Set to 1 for each player if the player wants to use voice in this mod.
            /// (If it's zero, then the server reports that the game rules are saying the player can't hear anyone).
            /// </summary>
            public bool EnableVoice;

            public bool WantEnable;

            /// <summary>
            /// Tells which players don't want to hear each other.
            /// These are indexed as clients and each bit represents a client (so player entity is bit+1).
            /// </summary>
            public BitVector32 BanMasks = new BitVector32();

            /// <summary>
            /// These store the masks we last sent to each client so we can determine if we need to resend them.
            /// </summary>
            public BitVector32 SentBanMasks = new BitVector32();

            public BitVector32 SentGameRulesMasks = new BitVector32();
        }

        private static readonly ConVar VoiceServerDebug = new ConVar("voice_serverdebug", "0");

        /// <summary>
        /// Set game rules to allow all clients to talk to each other
        /// Muted players still can't talk to each other
        /// </summary>
        private static readonly ConVar ServerAllTalk = new ConVar("sv_alltalk", "0", CVarFlags.Server);

        public IVoiceGameManagerHelper Helper { get; set; }

        private int MaxPlayers;
        private double UpdateInterval;						// How long since the last update.

        private readonly List<PlayerData> Players = new List<PlayerData>(VOICE_MAX_PLAYERS);

        private static void VoiceServerDebugLog(string message)
        {
            if (VoiceServerDebug.Float == 0)
            {
                return;
            }

            Log.Alert(AlertType.Console, message);
        }

        public bool Init(
            IVoiceGameManagerHelper helper,
            int maxClients
            )
        {
            Helper = helper;
            MaxPlayers = VOICE_MAX_PLAYERS < maxClients ? VOICE_MAX_PLAYERS : maxClients;
            Engine.Server.PrecacheModel("sprites/voiceicon.spr");

            // register voice_serverdebug if it hasn't been registered already
            if (CVar.GetCVar(VoiceServerDebug.Name) == null)
            {
                CVar.EngineCVar.Register(VoiceServerDebug);
            }

            if (CVar.GetCVar(ServerAllTalk.Name) == null)
            {
                CVar.EngineCVar.Register(ServerAllTalk);
            }

            return true;
        }

        /// <summary>
        /// Updates which players can hear which other players
        /// If gameplay mode is DM, then only players within the PVS can hear each other
        /// If gameplay mode is teamplay, then only players on the same team can hear each other
        /// Player masks are always applied
        /// </summary>
        /// <param name="frametime"></param>
        public void Update(double frametime)
        {
            // Only update periodically.
            UpdateInterval += frametime;
            if (UpdateInterval < UPDATE_INTERVAL)
            {
                return;
            }

            UpdateMasks();
        }

        /// <summary>
        /// Called when a new client connects (unsquelches its entity for everyone)
        /// </summary>
        /// <param name="edict"></param>
        public void ClientConnected(Edict edict)
        {
            var index = EntUtils.EntIndex(edict) - 1;

            var data = Players[index];

            // Clear out everything we use for deltas on this guy.
            data.WantEnable = true;
            data.SentGameRulesMasks = new BitVector32();
            data.SentBanMasks = new BitVector32();
        }

        /// <summary>
        /// Called on ClientCommand. Checks for the squelch and unsquelch commands
        /// Returns true if it handled the command
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool ClientCommand(BasePlayer pPlayer, ICommand command)
        {
            int playerClientIndex = pPlayer.EntIndex() - 1;

            if (playerClientIndex < 0 || playerClientIndex >= MaxPlayers)
            {
                VoiceServerDebugLog($"CVoiceGameMgr::ClientCommand: cmd {command.Name} from invalid client ({playerClientIndex})\n");
                return true;
            }

            if (command.Name.Equals("vban", StringComparison.OrdinalIgnoreCase) && command.Count >= 1)
            {
                for (var i = 0; i < command.Count; ++i)
                {
                    int.TryParse(command[0], NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out var mask);

                    if (i <= VOICE_MAX_PLAYERS_DW)
                    {
                        VoiceServerDebugLog($"CVoiceGameMgr::ClientCommand: vban (0x{mask:x}) from {playerClientIndex}\n");
                        Players[playerClientIndex].BanMasks = new BitVector32(mask);
                    }
                    else
                    {
                        VoiceServerDebugLog($"CVoiceGameMgr::ClientCommand: invalid index ({i})\n");
                    }
                }

                // Force it to update the masks now.
                //UpdateMasks();		
                return true;
            }
            else if (command.Name.Equals("VModEnable", StringComparison.OrdinalIgnoreCase) && command.Count >= 1)
            {
                int.TryParse(command[0], out var value);

                VoiceServerDebugLog($"CVoiceGameMgr::ClientCommand: VModEnable ({value != 0})\n");
                Players[playerClientIndex].EnableVoice = value != 0;
                Players[playerClientIndex].WantEnable = false;
                //UpdateMasks();		
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Called to determine if the Receiver has muted (blocked) the Sender
        /// Returns true if the receiver has blocked the sender
        /// </summary>
        /// <param name="receiver"></param>
        /// <param name="sender"></param>
        /// <returns></returns>
        public bool PlayerHasBlockedPlayer(BasePlayer receiver, BasePlayer sender)
        {
            if (receiver == null || sender == null)
            {
                return false;
            }

            var iReceiverIndex = receiver.EntIndex() - 1;
            var iSenderIndex = sender.EntIndex() - 1;

            if (iReceiverIndex < 0 || iReceiverIndex >= MaxPlayers || iSenderIndex < 0 || iSenderIndex >= MaxPlayers)
            {
                return false;
            }

            return Players[iReceiverIndex].BanMasks[iSenderIndex];
        }

        /// <summary>
        /// Force it to update the client masks
        /// </summary>
        private void UpdateMasks()
        {
            UpdateInterval = 0;

            var allTalk = ServerAllTalk.Float != 0;

            for (var iClient = 0; iClient < MaxPlayers; ++iClient)
            {
                var pEnt = PlayerUtils.PlayerByIndex(iClient + 1);

                if (pEnt == null || !pEnt.IsPlayer())
                {
                    continue;
                }

                var data = Players[iClient];

                // Request the state of their "VModEnable" cvar.
                if (data.WantEnable)
                {
                    var message = NetMessage.Begin(MsgDest.One, "ReqState", pEnt.Edict());
                    message.End();
                }

                var pPlayer = (BasePlayer)pEnt;

                var gameRulesMask = new BitVector32();

                if (data.EnableVoice)
                {
                    // Build a mask of who they can hear based on the game rules.
                    for (var iOtherClient = 0; iOtherClient < MaxPlayers; ++iOtherClient)
                    {
                        var pOtherPlayer = (BasePlayer)PlayerUtils.PlayerByIndex(iOtherClient + 1);
                        if (pOtherPlayer != null && (allTalk || Helper.CanPlayerHearPlayer(pPlayer, pOtherPlayer)))
                        {
                            gameRulesMask[iOtherClient] = true;
                        }
                    }
                }

                // If this is different from what the client has, send an update. 
                if (!Utils.BitVectorsEqual(ref gameRulesMask, ref data.SentGameRulesMasks)
                    || !Utils.BitVectorsEqual(ref data.BanMasks, ref data.SentBanMasks))
                {
                    data.SentGameRulesMasks = gameRulesMask;
                    data.SentBanMasks = data.BanMasks;

                    var message = NetMessage.Begin(MsgDest.One, "VoiceMask", pPlayer.Edict());

                    for (var dw = 0; dw < VOICE_MAX_PLAYERS_DW; ++dw)
                    {
                        message.WriteLong(gameRulesMask.Data);
                        message.WriteLong(data.BanMasks.Data);
                    }
                    message.End();
                }

                // Tell the engine.
                for (int iOtherClient = 0; iOtherClient < MaxPlayers; ++iOtherClient)
                {
                    var canHear = gameRulesMask[iOtherClient] && !data.BanMasks[iOtherClient];
                    Engine.Server.SetClientListening(iClient + 1, iOtherClient + 1, canHear);
                }
            }
        }
    }
}
