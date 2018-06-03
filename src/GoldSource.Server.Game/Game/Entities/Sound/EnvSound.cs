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
using GoldSource.Server.Game.Engine;
using GoldSource.Server.Game.Game.Entities.Characters;
using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Server.Game.Persistence;

namespace GoldSource.Server.Game.Game.Entities.Sound
{
    /// <summary>
    /// env_sound - spawn a sound entity that will set player roomtype
    /// when player moves in range and sight.
    /// </summary>
    [LinkEntityToClass("env_sound")]
    public class EnvSound : PointEntity
    {
        private const float SlowInterval = 0.75f;
        private const float FastInterval = 0.25f;

        [KeyValue]
        [Persist]
        public float Radius;

        [KeyValue]
        [Persist]
        public float RoomType;

        public override void Spawn()
        {
            // spread think times
            SetNextThink(Engine.Globals.Time + EngineRandom.Float(0.0f, 0.5f));
        }

        /// <summary>
        /// <para>
        /// A client that is visible and in range of a sound entity will
        /// have its room_type set by that sound entity.  If two or more
        /// sound entities are contending for a client, then the nearest
        /// sound entity to the client will set the client's room_type.
        /// A client's room_type will remain set to its prior value until
        /// a new in-range, visible sound entity resets a new room_type.
        /// </para>
        /// <para>CONSIDER: if player in water state, autoset roomtype to 14,15 or 16. </para>
        /// </summary>
        public override void Think()
        {
            SetNextThink(Engine.Globals.Time + CheckSoundState());
        }

        private float CheckSoundState()
        {
            // get pointer to client if visible; FindClientInPVS will
            // cycle through visible clients on consecutive calls.

            var player = Engine.Server.FindClientInPVS(Edict())?.TryGetEntity<BasePlayer>();

            if (player == null)
            {
                return SlowInterval; // no player in pvs of sound entity, slow it down
            }

            // check to see if this is the sound entity that is 
            // currently affecting this player

            if (player.LastSound && (player.LastSound.Entity == this))
            {
                // this is the entity currently affecting player, check
                // for validity

                if (player.SoundRoomType != 0 && player.SoundRange != 0)
                {
                    // we're looking at a valid sound entity affecting
                    // player, make sure it's still valid, update range

                    if (FEnvSoundInRange(this, player, out var flRange))
                    {
                        player.SoundRange = flRange;
                        return FastInterval;
                    }
                    else
                    {
                        // current sound entity affecting player is no longer valid,
                        // flag this state by clearing room_type and range.
                        // NOTE: we do not actually change the player's room_type
                        // NOTE: until we have a new valid room_type to change it to.

                        player.SoundRange = 0;
                        player.SoundRoomType = 0;
                        return SlowInterval;
                    }
                }
                else
                {
                    // entity is affecting player but is out of range,
                    // wait passively for another entity to usurp it...
                    return SlowInterval;
                }
            }

            // if we got this far, we're looking at an entity that is contending
            // for current player sound. the closest entity to player wins.

            {
                if (FEnvSoundInRange(this, player, out var flRange))
                {
                    if (flRange < player.SoundRange || player.SoundRange == 0)
                    {
                        // new entity is closer to player, so it wins.
                        player.LastSound.Set(this);
                        player.SoundRoomType = RoomType;
                        player.SoundRange = flRange;

                        // send room_type command to player's server.
                        // this should be a rare event - once per change of room_type
                        // only!

                        //CLIENT_COMMAND(pentPlayer, "room_type %f", m_flRoomtype);

                        var message = NetMessage.Begin(MsgDest.One, ServerCommand.RoomType, player.Edict());     // use the magic #1 for "one client"
                        message.WriteShort((short)RoomType);                   // sequence number
                        message.End();

                        // crank up nextthink rate for new active sound entity
                        // by falling through to think_fast...
                    }
                    // player is not closer to the contending sound entity,
                    // just fall through to think_fast. this effectively
                    // cranks up the think_rate of entities near the player.
                }
            }

            // player is in pvs of sound entity, but either not visible or
            // not in range. do nothing, fall through to think_fast...
            return FastInterval;
        }

        /// <summary>
        /// returns true if the given sound entity (pev) is in range 
        /// and can see the given player entity(pevTarget)
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="target"></param>
        /// <param name="pflRange"></param>
        /// <returns></returns>
        public static bool FEnvSoundInRange(EnvSound sound, BaseEntity target, out float pflRange)
        {
            pflRange = 0;

            var vecSpot1 = sound.Origin + sound.ViewOffset;
            var vecSpot2 = target.Origin + target.ViewOffset;

            Trace.Line(vecSpot1, vecSpot2, TraceFlags.IgnoreMonsters, sound.Edict(), out var tr);

            // check if line of sight crosses water boundary, or is blocked

            if ((tr.InOpen && tr.InWater) || tr.Fraction != 1)
            {
                return false;
            }

            // calc range from sound entity to player

            var vecRange = tr.EndPos - vecSpot1;
            var flRange = vecRange.Length();

            if (sound.Radius < flRange)
            {
                return false;
            }

            pflRange = flRange;

            return true;
        }
    }
}
