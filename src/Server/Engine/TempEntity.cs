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

using Server.Game;
using Server.Game.Entities;
using System;

namespace Server.Engine
{
    public static class TempEntity
    {
        public static void Explosion(in Vector position, short fireballSpriteIndex, int scale, int framerate, ExplosionFlags flags = ExplosionFlags.None)
        {
            var message = NetMessage.Begin(MsgDest.PAS, ServerCommand.TempEntity, position);
            message.WriteByte((int)TempEntityMsg.Explosion);
            message.WriteCoord(position.x);
            message.WriteCoord(position.y);
            message.WriteCoord(position.z);
            message.WriteShort(fireballSpriteIndex);
            message.WriteByte(scale); // scale * 10
            message.WriteByte(framerate); // framerate
            message.WriteByte((int)flags);
            message.End();
        }

        public static void Smoke(in Vector position, short smokeSpriteIndex, int scale, int framerate)
        {
            var message = NetMessage.Begin(MsgDest.PAS, ServerCommand.TempEntity, position);
            message.WriteByte((int)TempEntityMsg.Smoke);
            message.WriteCoord(position.x);
            message.WriteCoord(position.y);
            message.WriteCoord(position.z);
            message.WriteShort(smokeSpriteIndex);
            message.WriteByte(scale); // scale * 10
            message.WriteByte(framerate); // framerate
            message.End();
        }

        public static void Sparks(in Vector position)
        {
            var message = NetMessage.Begin(MsgDest.PVS, ServerCommand.TempEntity, position);
            message.WriteByte((int)TempEntityMsg.Sparks);
            message.WriteCoord(position.x);
            message.WriteCoord(position.y);
            message.WriteCoord(position.z);
            message.End();
        }

        public static void Ricochet(in Vector position, float scale)
        {
            var message = NetMessage.Begin(MsgDest.PVS, ServerCommand.TempEntity, position);
            message.WriteByte((int)TempEntityMsg.ArmorRicochet);
            message.WriteCoord(position.x);
            message.WriteCoord(position.y);
            message.WriteCoord(position.z);
            message.WriteByte((int)(scale * 10));
            message.End();
        }

        public static void MortarSpray(in Vector position, in Vector direction, int spriteModel, int count)
        {
            var message = NetMessage.Begin(MsgDest.PVS, ServerCommand.TempEntity, position);
            message.WriteByte((int)TempEntityMsg.SpriteSpray);
            message.WriteCoord(position.x);    // pos
            message.WriteCoord(position.y);
            message.WriteCoord(position.z);
            message.WriteCoord(direction.x);   // dir
            message.WriteCoord(direction.y);
            message.WriteCoord(direction.z);
            message.WriteShort(spriteModel);   // model
            message.WriteByte(count);          // count
            message.WriteByte(130);            // speed
            message.WriteByte(80);         // noise ( client will divide by 100 )
            message.End();
        }

        public static void StreakSplash(in Vector origin, in Vector direction, int color, int count, int speed, int velocityRange)
        {
            var message = NetMessage.Begin(MsgDest.PVS, ServerCommand.TempEntity, origin);
            message.WriteByte((int)TempEntityMsg.StreakSplash);
            message.WriteCoord(origin.x);      // origin
            message.WriteCoord(origin.y);
            message.WriteCoord(origin.z);
            message.WriteCoord(direction.x);   // direction
            message.WriteCoord(direction.y);
            message.WriteCoord(direction.z);
            message.WriteByte(color);  // Streak color 6
            message.WriteShort(count); // count
            message.WriteShort(speed);
            message.WriteShort(velocityRange); // Random velocity modifier
            message.End();
        }

        public static void EjectBrass(Vector vecOrigin, Vector vecVelocity, float rotation, int model, int soundtype)
        {
            // FIX: when the player shoots, their gun isn't in the same position as it is on the model other players see.

            var message = NetMessage.Begin(MsgDest.PVS, ServerCommand.TempEntity, vecOrigin);
            message.WriteByte((int)TempEntityMsg.Model);
            message.WriteCoord(vecOrigin.x);
            message.WriteCoord(vecOrigin.y);
            message.WriteCoord(vecOrigin.z);
            message.WriteCoord(vecVelocity.x);
            message.WriteCoord(vecVelocity.y);
            message.WriteCoord(vecVelocity.z);
            message.WriteAngle(rotation);
            message.WriteShort(model);
            message.WriteByte(soundtype);
            message.WriteByte(25);// 2.5 seconds
            message.End();
        }

        public static void UTIL_BloodStream(Vector origin, Vector direction, BloodColor color, int amount)
        {
            if (!EntUtils.ShouldShowBlood(color))
            {
                return;
            }

            if (Globals.Language == Language.German && color == BloodColor.Red)
            {
                color = 0;
            }

            var message = NetMessage.Begin(MsgDest.PVS, ServerCommand.TempEntity, origin);
            message.WriteByte((int)TempEntityMsg.BloodStream);
            message.WriteCoord(origin.x);
            message.WriteCoord(origin.y);
            message.WriteCoord(origin.z);
            message.WriteCoord(direction.x);
            message.WriteCoord(direction.y);
            message.WriteCoord(direction.z);
            message.WriteByte((int)color);
            message.WriteByte(Math.Min(amount, 255));
            message.End();
        }

        public static void UTIL_Bubbles(in Vector mins, in Vector maxs, int count)
        {
            var mid = (mins + maxs) * 0.5f;

            var flHeight = EntUtils.CalculateWaterLevel(mid, mid.z, mid.z + 1024);
            flHeight -= mins.z;

            var message = NetMessage.Begin(MsgDest.PAS, ServerCommand.TempEntity, mid);
            message.WriteByte((int)TempEntityMsg.Bubbles);
            message.WriteCoord(mins.x);    // mins
            message.WriteCoord(mins.y);
            message.WriteCoord(mins.z);
            message.WriteCoord(maxs.x);    // maxz
            message.WriteCoord(maxs.y);
            message.WriteCoord(maxs.z);
            message.WriteCoord(flHeight);          // height
            message.WriteShort(Globals.g_sModelIndexBubbles);
            message.WriteByte(count); // count
            message.WriteCoord(8); // speed
            message.End();
        }

        public static void UTIL_BubbleTrail(in Vector from, in Vector to, int count)
        {
            float flHeight = EntUtils.CalculateWaterLevel(from, from.z, from.z + 256);
            flHeight -= from.z;

            if (flHeight < 8)
            {
                flHeight = EntUtils.CalculateWaterLevel(to, to.z, to.z + 256);
                flHeight -= to.z;
                if (flHeight < 8)
                {
                    return;
                }

                // UNDONE: do a ploink sound
                flHeight = flHeight + to.z - from.z;
            }

            if (count > 255)
            {
                count = 255;
            }

            var message = NetMessage.Begin(MsgDest.Broadcast, ServerCommand.TempEntity);
            message.WriteByte((int)TempEntityMsg.BubbleTrail);
            message.WriteCoord(from.x);    // mins
            message.WriteCoord(from.y);
            message.WriteCoord(from.z);
            message.WriteCoord(to.x);  // maxz
            message.WriteCoord(to.y);
            message.WriteCoord(to.z);
            message.WriteCoord(flHeight);          // height
            message.WriteShort(Globals.g_sModelIndexBubbles);
            message.WriteByte(count); // count
            message.WriteCoord(8); // speed
            message.End();
        }

        public static ushort FixedUnsigned16(float value, float scale)
        {
            int output = (int)(value * scale);
            if (output < 0)
                output = 0;
            if (output > 0xFFFF)
                output = 0xFFFF;

            return (ushort)output;
        }

        public static short FixedSigned16(float value, float scale)
        {
            int output = (int)(value * scale);

            if (output > 32767)
                output = 32767;

            if (output < -32768)
                output = -32768;

            return (short)output;
        }

        public static void UTIL_HudMessageAll(in HudTextParms textparms, string pMessage)
        {
            for (var i = 1; i <= Game.Engine.Globals.MaxClients; ++i)
            {
                var pPlayer = PlayerUtils.PlayerByIndex(i);
                if (pPlayer != null)
                {
                    HudMessage(pPlayer, textparms, pMessage);
                }
            }
        }

        public static void HudMessage(BaseEntity pEntity, in HudTextParms textparms, string pMessage)
        {
            if (pEntity?.IsNetClient() == false)
            {
                return;
            }

            var message = NetMessage.Begin(MsgDest.One, ServerCommand.TempEntity, pEntity.Edict());
            message.WriteByte((int)TempEntityMsg.TextMessage);
            message.WriteByte(textparms.channel & 0xFF);

            message.WriteShort(FixedSigned16(textparms.x, 1 << 13));
            message.WriteShort(FixedSigned16(textparms.y, 1 << 13));
            message.WriteByte(textparms.effect);

            message.WriteByte(textparms.r1);
            message.WriteByte(textparms.g1);
            message.WriteByte(textparms.b1);
            message.WriteByte(textparms.a1);

            message.WriteByte(textparms.r2);
            message.WriteByte(textparms.g2);
            message.WriteByte(textparms.b2);
            message.WriteByte(textparms.a2);

            message.WriteShort(FixedUnsigned16(textparms.fadeinTime, 1 << 8));
            message.WriteShort(FixedUnsigned16(textparms.fadeoutTime, 1 << 8));
            message.WriteShort(FixedUnsigned16(textparms.holdTime, 1 << 8));

            //TODO: define constants
            if (textparms.effect == 2)
            {
                message.WriteShort(FixedUnsigned16(textparms.fxTime, 1 << 8));
            }

            const int MaxMessageLength = 511;

            if (pMessage.Length > MaxMessageLength)
            {
                pMessage = pMessage.Substring(0, MaxMessageLength);
            }

            message.WriteString(pMessage);

            message.End();
        }

        public static void PlayerDecalTrace(in TraceResult pTrace, int playernum, Decal decalNumber, bool bIsCustom)
        {
            int index;

            if (!bIsCustom)
            {
                if (decalNumber < 0)
                {
                    return;
                }

                index = Globals.gDecals[(int)decalNumber].index;
                if (index < 0)
                {
                    return;
                }
            }
            else
            {
                index = (int)decalNumber;
            }

            if (pTrace.Fraction == 1.0)
            {
                return;
            }

            var entity = pTrace.Hit.Entity();

            var message = NetMessage.Begin(MsgDest.Broadcast, ServerCommand.TempEntity);
            message.WriteByte((int)TempEntityMsg.PlayerDecal);
            message.WriteByte(playernum);
            message.WriteCoord(pTrace.EndPos.x);
            message.WriteCoord(pTrace.EndPos.y);
            message.WriteCoord(pTrace.EndPos.z);
            message.WriteShort((short)entity.EntIndex());
            message.WriteByte(index);
            message.End();
        }

        public static void GunshotDecalTrace(in TraceResult pTrace, Decal decalNumber)
        {
            if (decalNumber < 0)
            {
                return;
            }

            int index = Globals.gDecals[(int)decalNumber].index;
            if (index < 0)
            {
                return;
            }

            if (pTrace.Fraction == 1.0)
            {
                return;
            }

            var entity = pTrace.Hit.Entity();

            var message = NetMessage.Begin(MsgDest.PAS, ServerCommand.TempEntity, pTrace.EndPos);
            message.WriteByte((int)TempEntityMsg.GunShotDecal);
            message.WriteCoord(pTrace.EndPos.x);
            message.WriteCoord(pTrace.EndPos.y);
            message.WriteCoord(pTrace.EndPos.z);
            message.WriteShort((short)entity.EntIndex());
            message.WriteByte(index);
            message.End();
        }
    }
}
