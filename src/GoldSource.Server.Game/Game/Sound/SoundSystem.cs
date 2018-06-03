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
using GoldSource.Server.Engine;
using GoldSource.Server.Engine.API;
using GoldSource.Server.Engine.Sound;
using GoldSource.Server.Game.Engine;
using GoldSource.Server.Game.Game.Entities;
using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;
using GoldSource.Shared.Game;
using GoldSource.Shared.Game.Materials;
using GoldSource.Shared.Game.Sound;
using System;
using System.Linq;

namespace GoldSource.Server.Game.Game.Sound
{
    public sealed class SoundSystem : ISoundSystem
    {
        private ITrace Trace { get; }

        private IEngineSound EngineSound { get; }

        private SentencesSystem Sentences { get; }

        private MaterialsSystem Materials { get; }

        public SoundSystem(ITrace trace, IEngineSound engineSound, SentencesSystem sentences, MaterialsSystem materials)
        {
            Trace = trace ?? throw new ArgumentNullException(nameof(trace));
            EngineSound = engineSound ?? throw new ArgumentNullException(nameof(engineSound));
            Sentences = sentences ?? throw new ArgumentNullException(nameof(sentences));
            Materials = materials ?? throw new ArgumentNullException(nameof(materials));
        }

        public void EmitSound(Edict entity, SoundChannel channel, string sample, float volume, float attenuation, SoundFlags flags, int pitch)
        {
            if (sample?.FirstOrDefault() == '!')
            {
                if (Sentences.GetSentenceIndex(sample, out var name) >= 0)
                {
                    EmitSound2(entity, channel, name, volume, attenuation, flags, pitch);
                }
                else
                {
                    Log.Alert(AlertType.AIConsole, $"Unable to find ^{sample} in sentences.txt");
                }
            }
            else
            {
                EmitSound2(entity, channel, sample, volume, attenuation, flags, pitch);
            }
        }

        public void EmitSound2(Edict entity, SoundChannel channel, string sample, float volume, float attenuation, SoundFlags flags, int pitch)
        {
            EngineSound.EmitSound(entity, channel, sample, volume, attenuation, flags, pitch);
        }

        public void EmitSound(Edict entity, SoundChannel channel, string sample, float volume, float attenuation)
        {
            EmitSound(entity, channel, sample, volume, attenuation, SoundFlags.None, Pitch.Normal);
        }

        public void StopSound(Edict entity, SoundChannel channel, string sample)
        {
            EmitSound(entity, channel, sample, 0, 0, SoundFlags.Stop, Pitch.Normal);
        }

        public void EmitAmbientSound(Edict entity, in Vector vecOrigin, string sample, float volume, float attenuation, SoundFlags flags, int pitch)
        {
            if (sample?.FirstOrDefault() == '!')
            {
                if (Sentences.GetSentenceIndex(sample, out var name) >= 0)
                {
                    EngineSound.EmitAmbientSound(entity, vecOrigin, name, volume, attenuation, flags, pitch);
                }
            }
            else
            {
                EngineSound.EmitAmbientSound(entity, vecOrigin, sample, volume, attenuation, flags, pitch);
            }
        }

        public void StopSentence(Edict entity, string sentenceGroup, int pick)
        {
            if (pick == SentencesSystem.InvalidSentenceIndex)
            {
                return;
            }

            StopSound(entity, SoundChannel.Voice, $"!{sentenceGroup}{pick}");
        }

        /// <summary>
        /// Given a sentence group, play random sentence for given entity
        /// Returns which sentence was picked to play from the group, or InvalidSentenceIndex if the sentence group does not exist
        /// This is only needed if you plan on stopping the sound before playback is done (see Stop)
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="sentenceGroup"></param>
        /// <param name="volume"></param>
        /// <param name="attenuation"></param>
        /// <param name="flags"></param>
        /// <param name="pitch"></param>
        /// <returns>Which sentence was picked to play fro mthe group, or InvalidSentenceIndex if the sentence group does not exist</returns>
        public int PlayRandomSentence(Edict entity, string sentenceGroup, float volume, float attenuation, SoundFlags flags, int pitch)
        {
            var pick = Sentences.PickLeastRecentlyUsed(sentenceGroup, out var name);

            if (pick == SentencesSystem.InvalidGroupIndex)
            {
                Log.Alert(AlertType.Console, $"No such sentence group {sentenceGroup}");
                return SentencesSystem.InvalidSentenceIndex;
            }

            if (pick >= 0 && name.Length > 0)
            {
                EmitSound(entity, SoundChannel.Voice, name, volume, attenuation, flags, pitch);
            }

            return pick;
        }

        public int PlaySequentialSentence(Edict entity, string sentenceGroup, float volume, float attenuation, SoundFlags flags, int pitch, int pick, bool reset)
        {
            var pickNext = Sentences.PickSequentialLeastRecentlyUsed(sentenceGroup, out var name, pick, reset);

            if (pickNext >= 0 && name.Length > 0)
            {
                EmitSound(entity, SoundChannel.Voice, name, volume, attenuation, flags, pitch);
            }

            return pickNext;
        }

        public float PlayMaterialSound(in TraceResult ptr, in Vector vecSrc, in Vector vecEnd, Bullet bulletType)
        {
            // hit the world, try to play sound based on texture material type
            if (!Engine.GameRules.PlayTextureSounds())
                return 0.0f;

            var pEntity = ptr.Hit.TryGetEntity();

            var chTextureType = MaterialTypeCode.None;

            if (pEntity != null && pEntity.Classify() != EntityClass.None && pEntity.Classify() != EntityClass.Machine)
            {
                // hit body
                chTextureType = MaterialTypeCode.Flesh;
            }
            else
            {
                // hit world

                // find texture under strike, get material type

                // get texture from entity or world

                var pTextureName = Trace.TraceTexture((pEntity != null) ? pEntity.Edict() : World.WorldInstance.Edict(), vecSrc, vecEnd);

                if (pTextureName != null)
                {
                    pTextureName = SoundUtils.GetTextureBaseName(pTextureName);

                    // Log.Alert ( AlertType.Console, $"texture hit: {pTextureName}");

                    // get texture type
                    chTextureType = Materials.Find(pTextureName);
                }
            }

            if (!Materials.MaterialTypes.TryGetValue(chTextureType, out var type))
            {
                if (!Materials.MaterialTypes.TryGetValue(MaterialsSystem.DefaultMaterialType, out type))
                {
                    Log.Alert(AlertType.Error, $"Couldn't get default material type {MaterialsSystem.DefaultMaterialType}");
                    return 0.0f;
                }
            }

            var emitEvent = new MaterialEmitEvent
            {
                BulletType = bulletType,
                Type = type,
                SoundIndex = 0,
                Volume = type.ImpactVolume,
                CrowbarVolume = type.CrowbarVolume,
                Attenuation = Attenuation.Normal
            };

            if (type.ImpactSounds.Count > 0)
            {
                emitEvent.SoundIndex = EngineRandom.Long(0, type.ImpactSounds.Count - 1);

                if (type.Filter != null)
                {
                    if (!type.Filter.Invoke(ref emitEvent))
                    {
                        return 0.0f;
                    }

                    emitEvent.SoundIndex = Math.Clamp(emitEvent.SoundIndex, 0, type.ImpactSounds.Count - 1);
                }
            }

            // did we hit a breakable?

            if (pEntity?.ClassName == "func_breakable")
            {
                // drop volumes, the object will already play a damaged sound
                emitEvent.Volume /= 1.5f;
                emitEvent.CrowbarVolume /= 2.0f;
            }
            else if (chTextureType == MaterialTypeCode.Computer)
            {
                // play random spark if computer

                if (ptr.Fraction != 1.0 && EngineRandom.Long(0, 1) != 0)
                {
                    TempEntity.Sparks(ptr.EndPos);

                    float flVolume = EngineRandom.Float(0.7f, 1.0f);//random volume range
                    switch (EngineRandom.Long(0, 1))
                    {
                        case 0: EmitAmbientSound(World.WorldInstance.Edict(), ptr.EndPos, "buttons/spark5.wav", flVolume, Attenuation.Normal, 0, 100); break;
                        case 1: EmitAmbientSound(World.WorldInstance.Edict(), ptr.EndPos, "buttons/spark6.wav", flVolume, Attenuation.Normal, 0, 100); break;
                            // case 0: EMIT_SOUND(ENT(pev), SoundChannel.Voice, "buttons/spark5.wav", flVolume, Attenuation.Normal);	break;
                            // case 1: EMIT_SOUND(ENT(pev), SoundChannel.Voice, "buttons/spark6.wav", flVolume, Attenuation.Normal);	break;
                    }
                }
            }

            // play material hit sound
            if (type.ImpactSounds.Count > 0)
            {
                EmitAmbientSound(World.WorldInstance.Edict(), ptr.EndPos, type.ImpactSounds[emitEvent.SoundIndex], emitEvent.Volume, emitEvent.Attenuation, 0, 96 + EngineRandom.Long(0, 0xf));
                //EMIT_SOUND_DYN( ENT(m_pPlayer.pev), SoundChannel.Weapon, type.Sounds[soundIndex], fvol, Attenuation.Normal, 0, 96 + EngineRandom.Long(0,0xf));
            }

            return emitEvent.CrowbarVolume;
        }
    }
}
