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

namespace GoldSource.Shared.Game.Materials
{
    /// <summary>
    /// Represents the emitting of a material sound
    /// </summary>
    public struct MaterialEmitEvent
    {
        public Bullet BulletType { get; set; }
        public MaterialType Type { get; set; }
        public int SoundIndex { get; set; }
        public float Volume { get; set; }
        public float CrowbarVolume { get; set; }
        public float Attenuation { get; set; }
    }
}
