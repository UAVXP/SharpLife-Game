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

using GoldSource.Mathlib;
using GoldSource.Server.Game.Engine;
using GoldSource.Shared.Engine.Sound;

namespace GoldSource.Server.Game.Game.Entities.Buttons
{
    public static class ButtonUtils
    {
        public const float DoorSentenceWait = 6;
        public const float DoorSoundWait = 3;
        public const float ButtonSoundWait = 0.5f;

        /// <summary>
        /// Button sound table. 
        /// Also used by BaseDoor to get 'touched' door lock/unlock sounds
        /// </summary>
        /// <param name="sound"></param>
        /// <returns></returns>
        public static string ButtonSound(int sound)
        {
            switch (sound)
            {
                case 0:
                    return "common/null.wav";
                case 1:
                    return "buttons/button1.wav";
                case 2:
                    return "buttons/button2.wav";
                case 3:
                    return "buttons/button3.wav";
                case 4:
                    return "buttons/button4.wav";
                case 5:
                    return "buttons/button5.wav";
                case 6:
                    return "buttons/button6.wav";
                case 7:
                    return "buttons/button7.wav";
                case 8:
                    return "buttons/button8.wav";
                case 9:
                    return "buttons/button9.wav";
                case 10:
                    return "buttons/button10.wav";
                case 11:
                    return "buttons/button11.wav";
                case 12:
                    return "buttons/latchlocked1.wav";
                case 13:
                    return "buttons/latchunlocked1.wav";
                case 14:
                    return "buttons/lightswitch2.wav";
                // next 6 slots reserved for any additional sliding button sounds we may add

                case 21:
                    return "buttons/lever1.wav";
                case 22:
                    return "buttons/lever2.wav";
                case 23:
                    return "buttons/lever3.wav";
                case 24:
                    return "buttons/lever4.wav";
                case 25:
                    return "buttons/lever5.wav";
                default:
                    return "buttons/button9.wav";
            }
        }

        public static string DoorMoveSound(int sound)
        {
            switch (sound)
            {
                case 0:
                    return "common/null.wav";
                case 1:
                    return "doors/doormove1.wav";
                case 2:
                    return "doors/doormove2.wav";
                case 3:
                    return "doors/doormove3.wav";
                case 4:
                    return "doors/doormove4.wav";
                case 5:
                    return "doors/doormove5.wav";
                case 6:
                    return "doors/doormove6.wav";
                case 7:
                    return "doors/doormove7.wav";
                case 8:
                    return "doors/doormove8.wav";
                case 9:
                    return "doors/doormove9.wav";
                case 10:
                    return "doors/doormove10.wav";
                default:
                    return "common/null.wav";
            }
        }

        public static string DoorStopSound(int sound)
        {
            switch (sound)
            {
                case 0:
                    return "common/null.wav";
                case 1:
                    return "doors/doorstop1.wav";
                case 2:
                    return "doors/doorstop2.wav";
                case 3:
                    return "doors/doorstop3.wav";
                case 4:
                    return "doors/doorstop4.wav";
                case 5:
                    return "doors/doorstop5.wav";
                case 6:
                    return "doors/doorstop6.wav";
                case 7:
                    return "doors/doorstop7.wav";
                case 8:
                    return "doors/doorstop8.wav";
                default:
                    return "common/null.wav";
            }
        }

        /// <summary>
        /// play door or button locked or unlocked sounds. 
        /// pass in pointer to valid locksound struct. 
        /// if flocked is true, play 'door is locked' sound,
        /// otherwise play 'door is unlocked' sound
        /// NOTE: this routine is shared by doors and buttons
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ls"></param>
        /// <param name="flocked"></param>
        /// <param name="fbutton"></param>
        public static void PlayLockSounds(BaseEntity entity, ref LockSound ls, bool flocked, bool fbutton)
        {
            // LOCKED SOUND

            // CONSIDER: consolidate the locksound_t struct (all entries are duplicates for lock/unlock)
            // CONSIDER: and condense this code.
            var flsoundwait = fbutton ? ButtonSoundWait : DoorSoundWait;

            if (flocked)
            {
                var fplaysound = (ls.LockedSound != null && Engine.Globals.Time > ls.WaitSound);
                var fplaysentence = (ls.LockedSentence != null && !ls.EOFLocked && Engine.Globals.Time > ls.WaitSentence);

                var fvol = (fplaysound && fplaysentence) ? 0.25f : 1.0f;

                // if there is a locked sound, and we've debounced, play sound
                if (fplaysound)
                {
                    // play 'door locked' sound
                    entity.EmitSound(SoundChannel.Item, ls.LockedSound, fvol);
                    ls.WaitSound = Engine.Globals.Time + flsoundwait;
                }

                // if there is a sentence, we've not played all in list, and we've debounced, play sound
                if (fplaysentence)
                {
                    // play next 'door locked' sentence in group
                    var iprev = ls.NextLockedSentence;

                    ls.NextLockedSentence = Engine.Sound.PlaySequentialSentence(entity.Edict(), ls.LockedSentence,
                        0.85f, Attenuation.Normal, SoundFlags.None, Pitch.Normal, ls.NextLockedSentence, false);
                    ls.NextUnlockedSentence = 0;

                    // make sure we don't keep calling last sentence in list
                    ls.EOFLocked = (iprev == ls.NextLockedSentence);

                    ls.WaitSentence = Engine.Globals.Time + DoorSentenceWait;
                }
            }
            else
            {
                // UNLOCKED SOUND

                var fplaysound = (ls.UnlockedSound != null && Engine.Globals.Time > ls.WaitSound);
                var fplaysentence = (ls.UnlockedSentence != null && !ls.EOFUnlocked && Engine.Globals.Time > ls.WaitSentence);

                // if playing both sentence and sound, lower sound volume so we hear sentence
                float fvol = (fplaysound && fplaysentence) ? 0.25f : 1.0f;

                // play 'door unlocked' sound if set
                if (fplaysound)
                {
                    entity.EmitSound(SoundChannel.Item, ls.UnlockedSound, fvol);
                    ls.WaitSound = Engine.Globals.Time + flsoundwait;
                }

                // play next 'door unlocked' sentence in group
                if (fplaysentence)
                {
                    var iprev = ls.NextUnlockedSentence;

                    ls.NextUnlockedSentence = Engine.Sound.PlaySequentialSentence(entity.Edict(), ls.UnlockedSentence,
                        0.85f, Attenuation.Normal, SoundFlags.None, Pitch.Normal, ls.NextUnlockedSentence, false);
                    ls.NextLockedSentence = 0;

                    // make sure we don't keep calling last sentence in list
                    ls.EOFUnlocked = (iprev == ls.NextUnlockedSentence);
                    ls.WaitSentence = Engine.Globals.Time + DoorSentenceWait;
                }
            }
        }

        /// <summary>
        /// Makes flagged buttons spark when turned off
        /// </summary>
        /// <param name="pev"></param>
        /// <param name=""></param>
        public static void DoSpark(BaseEntity entity, in Vector location)
        {
            var tmp = location + (entity.Size * 0.5f);
            TempEntity.Sparks(tmp);

            var flVolume = EngineRandom.Float(0.25f, 0.75f) * 0.4f;//random volume range
            switch ((int)(EngineRandom.Float(0, 1) * 6))
            {
                case 0: entity.EmitSound(SoundChannel.Voice, "buttons/spark1.wav", flVolume); break;
                case 1: entity.EmitSound(SoundChannel.Voice, "buttons/spark2.wav", flVolume); break;
                case 2: entity.EmitSound(SoundChannel.Voice, "buttons/spark3.wav", flVolume); break;
                case 3: entity.EmitSound(SoundChannel.Voice, "buttons/spark4.wav", flVolume); break;
                case 4: entity.EmitSound(SoundChannel.Voice, "buttons/spark5.wav", flVolume); break;
                case 5: entity.EmitSound(SoundChannel.Voice, "buttons/spark6.wav", flVolume); break;
            }
        }
    }
}
