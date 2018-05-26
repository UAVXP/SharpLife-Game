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
using Server.Engine.API;
using Server.Game.Entities.Characters;
using Server.Game.Entities.Weapons;
using Server.GameRules;
using System;

namespace Server.Game.Entities
{
    public static class PlayerUtils
    {
        public static BasePlayer PlayerByIndex(int playerIndex)
        {
            BasePlayer player = null;

            if (playerIndex > 0 && playerIndex <= Engine.Globals.MaxClients)
            {
                player = EntUtils.IndexEnt<BasePlayer>(playerIndex);
            }

            return player;
        }

        /// <summary>
        /// Find a player with a case-insensitive name search
        /// </summary>
        /// <param name="testName"></param>
        /// <returns></returns>
        public static BasePlayer FindPlayerByName(string testName)
        {
            if (testName == null)
            {
                throw new ArgumentNullException(nameof(testName));
            }

            for (var i = 1; i <= Engine.Globals.MaxClients; ++i)
            {
                var entity = EntUtils.IndexEnt(i);

                if (entity != null && entity?.IsPlayer() == true && entity.NetName.Equals(testName, StringComparison.OrdinalIgnoreCase))
                {
                    return (BasePlayer)entity;
                }
            }

            return null;
        }

        /*
        *	==============
        *	CountPlayers
        *	Determine the current # of active players on the server for map cycling logic
        *	==============
        */
        public static int CountPlayers()
        {
            int num = 0;

            for (int i = 1; i <= Engine.Globals.MaxClients; ++i)
            {
                BaseEntity pEnt = PlayerByIndex(i);

                if (pEnt != null)
                {
                    ++num;
                }
            }

            return num;
        }

        //checks if the spot is clear of players
        public static bool IsSpawnPointValid(BaseEntity pPlayer, BaseEntity pSpot)
        {
            if (!pSpot.IsTriggered(pPlayer))
            {
                return false;
            }

            for (BaseEntity ent = null; (ent = EntUtils.FindEntityInSphere(ent, pSpot.Origin, 128)) != null;)
            {
                // if ent is a client, don't spawn on 'em
                if (ent.IsPlayer() && ent != pPlayer)
                {
                    return false;
                }
            }

            return true;
        }

        public static void CheckPowerups(BaseEntity entity)
        {
            if (entity.Health <= 0)
            {
                return;
            }

            entity.ModelIndex = Globals.g_ulModelIndexPlayer;    // don't use eyes
        }

        /*
        *	This is a glorious hack to find free space when you've crouched into some solid space
        *	Our crouching collisions do not work correctly for some reason and this is easier
        *	than fixing the problem :(
        */
        public static void FixPlayerCrouchStuck(BaseEntity pPlayer)
        {
            // Move up as many as 18 pixels if the player is stuck.
            var origin = pPlayer.Origin;

            for (int i = 0; i < 18; ++i)
            {
                Trace.Hull(origin, origin, TraceFlags.None, Hull.Head, pPlayer.Edict(), out var trace);
                if (!trace.StartSolid)
                {
                    break;
                }

                origin.z++;
            }

            pPlayer.Origin = origin;
        }

        public static BaseEntity g_pLastSpawn;

        private static BaseEntity EntDetermineSpawnPoint(BaseEntity pPlayer)
        {
            BaseEntity pSpot;

            // choose a info_player_deathmatch point
            if (Engine.GameRules.IsCoOp())
            {
                pSpot = EntUtils.FindEntityByClassName(g_pLastSpawn, "info_player_coop");
                if (pSpot != null)
                {
                    return pSpot;
                }

                pSpot = EntUtils.FindEntityByClassName(g_pLastSpawn, "info_player_start");
                if (pSpot != null)
                {
                    return pSpot;
                }
            }
            else if (Engine.GameRules.IsDeathmatch())
            {
                pSpot = g_pLastSpawn;
                // Randomize the start spot
                for (var i = EngineRandom.Long(1, 5); i > 0; i--)
                {
                    pSpot = EntUtils.FindEntityByClassName(pSpot, "info_player_deathmatch");
                }

                if (pSpot == null)  // skip over the null point
                {
                    pSpot = EntUtils.FindEntityByClassName(pSpot, "info_player_deathmatch");
                }

                var pFirstSpot = pSpot;

                do
                {
                    if (pSpot != null)
                    {
                        // check if pSpot is valid
                        if (IsSpawnPointValid(pPlayer, pSpot))
                        {
                            if (pSpot.Origin == WorldConstants.g_vecZero)
                            {
                                pSpot = EntUtils.FindEntityByClassName(pSpot, "info_player_deathmatch");
                                continue;
                            }

                            // if so, go to pSpot
                            return pSpot;
                        }
                    }
                    // increment pSpot
                    pSpot = EntUtils.FindEntityByClassName(pSpot, "info_player_deathmatch");
                } while (pSpot != pFirstSpot); // loop if we're not back to the start

                // we haven't found a place to spawn yet,  so kill any guy at the first spawn point and spawn there
                if (pSpot != null)
                {
                    for (BaseEntity ent = null; (ent = EntUtils.FindEntityInSphere(ent, pSpot.Origin, 128)) != null;)
                    {
                        // if ent is a client, kill em (unless they are ourselves)
                        if (ent.IsPlayer() && ent != pPlayer)
                        {
                            ent.TakeDamage(World.WorldInstance, World.WorldInstance, 300, DamageTypes.Generic);
                        }
                    }
                    return pSpot;
                }
            }

            // If startspot is set, (re)spawn there.
            if (string.IsNullOrEmpty(Engine.Globals.StartSpot))
            {
                pSpot = EntUtils.FindEntityByClassName(null, "info_player_start");
                if (pSpot != null)
                {
                    return pSpot;
                }
            }
            else
            {
                pSpot = EntUtils.FindEntityByTargetName(null, Engine.Globals.StartSpot);
                if (pSpot != null)
                {
                    return pSpot;
                }
            }

            Log.Alert(AlertType.Error, "PutClientInServer: no info_player_start on level");
            return null;
        }

        public static BaseEntity EntSelectSpawnPoint(BaseEntity pPlayer)
        {
            var pSpot = EntDetermineSpawnPoint(pPlayer);

            if (pSpot == null)
            {
                Log.Alert(AlertType.Error, "PutClientInServer: no info_player_start on level");
                return World.WorldInstance;
            }

            g_pLastSpawn = pSpot;
            return pSpot;
        }

        public static void ShowMessage(string str, BaseEntity pPlayer)
        {
            if (pPlayer?.IsNetClient() != true)
            {
                return;
            }

            var message = NetMessage.Begin(MsgDest.One, "HudText", pPlayer.Edict());
            message.WriteString(str);
            message.End();
        }

        public static void ShowMessageAll(string str)
        {
            // loop through all players
            for (var i = 1; i <= Engine.Globals.MaxClients; ++i)
            {
                BaseEntity pPlayer = PlayerByIndex(i);
                if (pPlayer != null)
                {
                    ShowMessage(str, pPlayer);
                }
            }
        }

        /// <summary>
        /// used by kill command and disconnect command
        /// ROBIN: Moved here from player.cpp, to allow multiple player models
        /// </summary>
        /// <param name="entity"></param>
        public static void SetSuicideFrame(BaseEntity entity)
        {
            if (entity.ModelName != "models/player.mdl")
            {
                return; // allready gibbed
            }

            //	entity.Frame		= $deatha11;
            entity.Solid = Solid.Not;
            entity.MoveType = MoveType.Toss;
            entity.DeadFlag = DeadFlag.Dead;
            entity.SetNextThink(-1);
        }

        private static uint glSeed;

        private static readonly uint[] seed_table = new uint[256]
        {
            28985, 27138, 26457, 9451, 17764, 10909, 28790, 8716, 6361, 4853, 17798, 21977, 19643, 20662, 10834, 20103,
            27067, 28634, 18623, 25849, 8576, 26234, 23887, 18228, 32587, 4836, 3306, 1811, 3035, 24559, 18399, 315,
            26766, 907, 24102, 12370, 9674, 2972, 10472, 16492, 22683, 11529, 27968, 30406, 13213, 2319, 23620, 16823,
            10013, 23772, 21567, 1251, 19579, 20313, 18241, 30130, 8402, 20807, 27354, 7169, 21211, 17293, 5410, 19223,
            10255, 22480, 27388, 9946, 15628, 24389, 17308, 2370, 9530, 31683, 25927, 23567, 11694, 26397, 32602, 15031,
            18255, 17582, 1422, 28835, 23607, 12597, 20602, 10138, 5212, 1252, 10074, 23166, 19823, 31667, 5902, 24630,
            18948, 14330, 14950, 8939, 23540, 21311, 22428, 22391, 3583, 29004, 30498, 18714, 4278, 2437, 22430, 3439,
            28313, 23161, 25396, 13471, 19324, 15287, 2563, 18901, 13103, 16867, 9714, 14322, 15197, 26889, 19372, 26241,
            31925, 14640, 11497, 8941, 10056, 6451, 28656, 10737, 13874, 17356, 8281, 25937, 1661, 4850, 7448, 12744,
            21826, 5477, 10167, 16705, 26897, 8839, 30947, 27978, 27283, 24685, 32298, 3525, 12398, 28726, 9475, 10208,
            617, 13467, 22287, 2376, 6097, 26312, 2974, 9114, 21787, 28010, 4725, 15387, 3274, 10762, 31695, 17320,
            18324, 12441, 16801, 27376, 22464, 7500, 5666, 18144, 15314, 31914, 31627, 6495, 5226, 31203, 2331, 4668,
            12650, 18275, 351, 7268, 31319, 30119, 7600, 2905, 13826, 11343, 13053, 15583, 30055, 31093, 5067, 761,
            9685, 11070, 21369, 27155, 3663, 26542, 20169, 12161, 15411, 30401, 7580, 31784, 8985, 29367, 20989, 14203,
            29694, 21167, 10337, 1706, 28578, 887, 3373, 19477, 14382, 675, 7033, 15111, 26138, 12252, 30996, 21409,
            25678, 18555, 13256, 23316, 22407, 16727, 991, 9236, 5373, 29402, 6117, 15241, 27715, 19291, 19888, 19847
        };

        public static uint U_Random()
        {
            glSeed *= 69069;
            glSeed += seed_table[glSeed & 0xff];

            return ++glSeed & 0x0fffffff;
        }

        public static void U_Srand(uint seed)
        {
            glSeed = seed_table[seed & 0xff];
        }

        public static int SharedRandomLong(uint seed, int low, int high)
        {
            U_Srand((uint)(seed + low + high));

            var range = (uint)(high - low + 1);
            if ((range - 1) == 0)
            {
                return low;
            }
            else
            {
                var rnum = (int)U_Random();

                var offset = (int)(rnum % range);

                return low + offset;
            }
        }

        public static float SharedRandomFloat(uint seed, float low, float high)
        {
            U_Srand((uint)((int)seed + (int)BitConverter.DoubleToInt64Bits(low) + (int)BitConverter.DoubleToInt64Bits(high)));

            U_Random();
            U_Random();

            var range = (uint)(high - low);
            if (range == 0)
            {
                return low;
            }
            else
            {
                var tensixrand = (int)U_Random() & 65535;

                var offset = tensixrand / 65536.0f;

                return low + (offset * range);
            }
        }

        public static bool GetNextBestWeapon(BasePlayer pPlayer, BasePlayerItem pCurrentWeapon)
        {
            return Engine.GameRules.GetNextBestWeapon(pPlayer, pCurrentWeapon);
        }

        public static void ScreenShake(Vector center, float amplitude, float frequency, float duration, float radius)
        {
            var shake = new ScreenShake
            {
                duration = TempEntity.FixedUnsigned16(duration, 1 << 12),  // 4.12 fixed
                frequency = TempEntity.FixedUnsigned16(frequency, 1 << 8)  // 8.8 fixed
            };

            for (var i = 1; i <= Engine.Globals.MaxClients; ++i)
            {
                BaseEntity pPlayer = PlayerByIndex(i);

                if (pPlayer == null || (pPlayer.Flags & EntFlags.OnGround) == 0) // Don't shake if not onground
                {
                    continue;
                }

                float localAmplitude = 0;

                if (radius <= 0)
                {
                    localAmplitude = amplitude;
                }
                else
                {
                    var delta = center - pPlayer.Origin;
                    var distance = delta.Length();

                    // Had to get rid of this falloff - it didn't work well
                    if (distance < radius)
                    {
                        localAmplitude = amplitude;//radius - distance;
                    }
                }
                if (localAmplitude != 0)
                {
                    shake.amplitude = TempEntity.FixedUnsigned16(localAmplitude, 1 << 12);     // 4.12 fixed

                    var message = NetMessage.Begin(MsgDest.One, "ScreenShake", pPlayer.Edict());       // use the magic #1 for "one client"

                    message.WriteShort(shake.amplitude);               // shake amount
                    message.WriteShort(shake.duration);                // shake lasts this long
                    message.WriteShort(shake.frequency);               // shake noise frequency

                    message.End();
                }
            }
        }

        public static void ScreenShakeAll(Vector center, float amplitude, float frequency, float duration)
        {
            ScreenShake(center, amplitude, frequency, duration, 0);
        }

        public static ScreenFade ScreenFadeBuild(Vector color, float fadeTime, float fadeHold, int alpha, int flags)
        {
            return new ScreenFade
            {
                duration = TempEntity.FixedUnsigned16(fadeTime, 1 << 12),     // 4.12 fixed
                holdTime = TempEntity.FixedUnsigned16(fadeHold, 1 << 12),     // 4.12 fixed
                r = (byte)color.x,
                g = (byte)color.y,
                b = (byte)color.z,
                a = (byte)alpha,
                fadeFlags = (short)flags
            };
        }

        public static void ScreenFadeWrite(in ScreenFade fade, BaseEntity pEntity)
        {
            if (pEntity?.IsNetClient() == false)
            {
                return;
            }

            var message = NetMessage.Begin(MsgDest.One, "ScreenFade", pEntity.Edict());        // use the magic #1 for "one client"

            message.WriteShort(fade.duration);     // fade lasts this long
            message.WriteShort(fade.holdTime);     // fade lasts this long
            message.WriteShort(fade.fadeFlags);        // fade type (in / out)
            message.WriteByte(fade.r);             // fade red
            message.WriteByte(fade.g);             // fade green
            message.WriteByte(fade.b);             // fade blue
            message.WriteByte(fade.a);             // fade blue

            message.End();
        }

        public static void ScreenFadeAll(in Vector color, float fadeTime, float holdTime, int alpha, int flags)
        {
            var fade = ScreenFadeBuild(color, fadeTime, holdTime, alpha, flags);

            for (var i = 1; i <= Engine.Globals.MaxClients; ++i)
            {
                var pPlayer = PlayerByIndex(i);

                ScreenFadeWrite(fade, pPlayer);
            }
        }

        public static void ScreenFade(BaseEntity pEntity, in Vector color, float fadeTime, float fadeHold, int alpha, int flags)
        {
            var fade = ScreenFadeBuild(color, fadeTime, fadeHold, alpha, flags);
            ScreenFadeWrite(fade, pEntity);
        }

        public static void ClientPrintAll(HudPrint msg_dest, string msg_name, string param1, string param2, string param3, string param4)
        {
            var message = NetMessage.Begin(MsgDest.All, "TextMsg");
            message.WriteByte((int)msg_dest);
            message.WriteString(msg_name);

            if (param1 != null)
            {
                message.WriteString(param1);
            }

            if (param2 != null)
            {
                message.WriteString(param2);
            }

            if (param3 != null)
            {
                message.WriteString(param3);
            }

            if (param4 != null)
            {
                message.WriteString(param4);
            }

            message.End();
        }

        public static void CenterPrintAll(string msg_name, string param1, string param2, string param3, string param4)
        {
            ClientPrintAll(HudPrint.Center, msg_name, param1, param2, param3, param4);
        }

        public static void ClientPrint(BaseEntity client, HudPrint msg_dest, string msg_name, string param1 = null, string param2 = null, string param3 = null, string param4 = null)
        {
            var message = NetMessage.Begin(MsgDest.One, "TextMsg", client.Edict());
            message.WriteByte((int)msg_dest);
            message.WriteString(msg_name);

            if (param1 != null)
            {
                message.WriteString(param1);
            }

            if (param2 != null)
            {
                message.WriteString(param2);
            }

            if (param3 != null)
            {
                message.WriteString(param3);
            }

            if (param4 != null)
            {
                message.WriteString(param4);
            }

            message.End();
        }

        public static void SayText(string pText, BaseEntity pEntity)
        {
            if (!pEntity.IsNetClient())
            {
                return;
            }

            var message = NetMessage.Begin(MsgDest.One, "SayText", pEntity.Edict());
            message.WriteByte(pEntity.EntIndex());
            message.WriteString(pText);
            message.End();
        }

        public static void SayTextAll(string pText, BaseEntity pEntity)
        {
            var message = NetMessage.Begin(MsgDest.All, "SayText");
            message.WriteByte(pEntity.EntIndex());
            message.WriteString(pText);
            message.End();
        }

        private static EHandle<BaseEntity> _bodyQueueHead;

        public static BaseEntity BodyQueueHead
        {
            get => _bodyQueueHead.Entity;
            set => _bodyQueueHead.Set(value);
        }

        public static void InitBodyQue()
        {
            var entity = Engine.EntityRegistry.CreateInstance<Corpse>();

            BodyQueueHead = entity;

            // Reserve 3 more slots for dead bodies
            for (var i = 0; i < 3; ++i)
            {
                var corpse = Engine.EntityRegistry.CreateInstance<Corpse>();
                entity.Owner = corpse;
                entity = corpse;
            }

            entity.Owner = BodyQueueHead;
        }

        public static void CopyToBodyQue(BaseEntity entity)
        {
            if ((entity.Effects & EntityEffects.NoDraw) != 0)
            {
                return;
            }

            var head = BodyQueueHead;

            head.Angles = entity.Angles;
            head.ModelName = entity.ModelName;
            head.ModelIndex = entity.ModelIndex;
            head.Frame = entity.Frame;
            head.ColorMap = entity.ColorMap;
            head.MoveType = MoveType.Toss;
            head.Velocity = entity.Velocity;
            head.Flags = 0;
            head.DeadFlag = entity.DeadFlag;
            head.RenderEffect = RenderEffect.DeadPlayer;
            head.RenderAmount = entity.EntIndex();

            head.Effects = entity.Effects | EntityEffects.NoInterp;

            head.Sequence = entity.Sequence;
            head.AnimationTime = entity.AnimationTime;

            head.SetOrigin(entity.Origin);
            head.SetSize(entity.Mins, entity.Maxs);

            BodyQueueHead = head.Owner;
        }

        public static void EmitSoundSuit(BaseEntity entity, string sample)
        {
            var pitch = Pitch.Normal;

            var fvol = CVar.GetFloat("suitvolume");
            if (EngineRandom.Long(0, 1) != 0)
            {
                pitch = EngineRandom.Long(0, 6) + 98;
            }

            if (fvol > 0.05)
            {
                entity.EmitSound(SoundChannel.Static, sample, fvol, pitch: pitch);
            }
        }

        //TODO
        /*
        public static void EMIT_GROUPID_SUIT(Edict entity, int isentenceg)
        {
            var pitch = Pitch.Normal;

            var fvol = CVar.GetFloat("suitvolume");
            if (EngineRandom.Long(0, 1) != 0)
            {
                pitch = EngineRandom.Long(0, 6) + 98;
            }

            if (fvol > 0.05)
            {
                Engine.Sound.PlayRandomSentence(entity, isentenceg, fvol, pitch: pitch);
            }
        }
        */
        public static void EmitGroupSuit(Edict entity, string groupname)
        {
            var pitch = Pitch.Normal;

            var fvol = CVar.GetFloat("suitvolume");
            if (EngineRandom.Long(0, 1) != 0)
            {
                pitch = EngineRandom.Long(0, 6) + 98;
            }

            if (fvol > 0.05)
            {
                Engine.Sound.PlayRandomSentence(entity, groupname, fvol, pitch: pitch);
            }
        }

        public const string SayCommandName = "say";
        public const string SayTeamCommandName = "say_team";

        /*
        *	HOST_SAY
        *	String comes in as
        *	say blah blah blah
        *	or as
        *	blah blah blah
        */
        public static void HostSay(BasePlayer player, ICommand command, bool teamOnly)
        {
            // We can get a raw string now, without the "say " prepended
            if (command.Count == 0)
            {
                return;
            }

            //Not yet.
            if (player.NextChatTime > Engine.Globals.Time)
            {
                return;
            }

            var commandName = command[0];

            string text;
            if (SayCommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase) || SayTeamCommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase))
            {
                if (command.Count < 1)
                {
                    // say with a blank message, nothing to do
                    return;
                }

                text = command.ArgumentsAsString(false);
            }
            else  // Raw text, need to prepend argv[0]
            {
                if (command.Count >= 1)
                {
                    text = $"{commandName} {command.ArgumentsAsString(false)}";
                }
                else
                {
                    // Just a one word command, use the first word...sigh
                    text = commandName;
                }
            }

            // remove quotes if present
            if (text[0] == '"')
            {
                if (text.Length < 2)
                {
                    return;
                }

                text = text.Substring(1, text.Length - 2);
            }

            // make sure the text has content

            if (text.Length == 0)
            {
                return;  // no character found, so say nothing
            }

            string completeMessage;
            // turn on color set 2  (color on,  no sound)
            // turn on color set 2  (color on,  no sound)
            if (player.IsObserver() && (teamOnly))
            {
                completeMessage = $"{(char)2}(SPEC) {player.NetName}: ";
            }
            else if (teamOnly)
            {
                completeMessage = $"{(char)2}(TEAM) {player.NetName}: ";
            }
            else
            {
                completeMessage = $"{(char)2}{player.NetName}: ";
            }

            completeMessage = $"{completeMessage}{text}\n";

            player.NextChatTime = Engine.Globals.Time + WorldConstants.PlayerChatInterval;

            // loop through all players
            // Start with the first player.
            // This may return the world in single player if the client types something between levels or during spawn
            // so check it, or it will infinite loop

            for (BasePlayer client = null; (client = (BasePlayer)EntUtils.FindEntityByClassName(client, "player")) != null;)
            {
                if (client == player)
                {
                    continue;
                }

                if (!client.IsNetClient())    // Not a client ? (should never be true)
                {
                    continue;
                }

                // can the receiver hear the sender? or has he muted him?
                if (Engine.VoiceGameManager.PlayerHasBlockedPlayer(client, player))
                {
                    continue;
                }

                if (!player.IsObserver() && teamOnly && Engine.GameRules.PlayerRelationship(client, player) != Relationship.Teammate)
                {
                    continue;
                }

                // Spectators can only talk to other specs
                if (player.IsObserver() && teamOnly && !client.IsObserver())
                {
                    continue;
                }

                var message = NetMessage.Begin(MsgDest.One, "SayText", client.Edict());
                message.WriteByte(player.EntIndex());
                message.WriteString(completeMessage);
                message.End();
            }

            {
                // print to the sending client
                var message = NetMessage.Begin(MsgDest.One, "SayText", player.Edict());
                message.WriteByte(player.EntIndex());
                message.WriteString(completeMessage);
                message.End();
            }

            // echo to server console
            Engine.Server.ServerPrint(completeMessage);

            var commandToLog = teamOnly ? SayTeamCommandName : SayCommandName;

            var userId = Engine.Server.GetPlayerUserId(player.Edict());
            var authId = Engine.Server.GetPlayerAuthId(player.Edict());

            // team match?
            if (Engine.GameRules.IsTeamplay())
            {
                var infoBuffer = Engine.Server.GetClientInfoBuffer(player.Edict());

                Log.EngineLog($"\"{player.NetName}<{userId}><{authId}><{infoBuffer.GetValue("model")}>\" {commandToLog} \"{text}\"\n");
            }
            else
            {
                Log.EngineLog($"\"{player.NetName}<{userId}><{authId}><{userId}>\" {commandToLog} \"{text}\"\n");
            }
        }
    }
}
