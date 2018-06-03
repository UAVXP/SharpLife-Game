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
using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;

namespace GoldSource.Server.Game.Game.Sound
{
    /*
    *	Pitch of 100 is no pitch shift.
    *	Pitch > 100 up to 255 is a higher pitch, pitch < 100 down to 1 is a lower pitch.   150 to 70 is the realistic range.
    *	EmitSound with pitch != 100 should be used sparingly, as it's not quite as fast as EmitSound with normal pitch (the pitchshift mixer is not native coded).
    *	TODO: still valid now?
    */
    public interface ISoundSystem
    {
        void EmitSound(Edict entity, SoundChannel channel, string sample, float volume = Volume.Normal, float attenuation = Attenuation.Normal, SoundFlags flags = SoundFlags.None, int pitch = Pitch.Normal);

        void EmitSound2(Edict entity, SoundChannel channel, string sample, float volume = Volume.Normal, float attenuation = Attenuation.Normal, SoundFlags flags = SoundFlags.None, int pitch = Pitch.Normal);

        void EmitSound(Edict entity, SoundChannel channel, string sample, float volume, float attenuation);

        void StopSound(Edict entity, SoundChannel channel, string sample);

        void EmitAmbientSound(Edict entity, in Vector vecOrigin, string sample, float volume = Volume.Normal, float attenuation = Attenuation.Normal, SoundFlags flags = SoundFlags.None, int pitch = Pitch.Normal);

        void StopSentence(Edict entity, string sentenceGroup, int pick);

        int PlayRandomSentence(Edict entity, string sentenceGroup, float volume = Volume.Normal, float attenuation = Attenuation.Normal, SoundFlags flags = SoundFlags.None, int pitch = Pitch.Normal);

        int PlaySequentialSentence(Edict entity, string sentenceGroup, float volume, float attenuation, SoundFlags flags, int pitch, int pick, bool reset);

        float PlayMaterialSound(in TraceResult ptr, in Vector vecSrc, in Vector vecEnd, Bullet bulletType);
    }
}
