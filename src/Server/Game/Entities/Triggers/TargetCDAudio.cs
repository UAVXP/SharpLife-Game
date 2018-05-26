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
using Server.Game.Entities.MetaData;

namespace Server.Game.Entities.Triggers
{
    /// <summary>
    /// This plays a CD track when fired or when the player enters it's radius
    /// </summary>
    [LinkEntityToClass("target_cdaudio")]
    public class TargetCDAudio : PointEntity
    {
        public override bool KeyValue(string key, string value)
        {
            if (key == "radius")
            {
                float.TryParse(value, out var result);
                Scale = result;
                return true;
            }

            return base.KeyValue(key, value);
        }

        public override void Spawn()
        {
            Solid = Solid.Not;
            MoveType = MoveType.None;

            if (Scale > 0)
            {
                SetNextThink(Engine.Globals.Time + 1.0f);
            }
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            Play();
        }

        /// <summary>
        /// only plays for ONE client, so only use in single play!
        /// </summary>
        public override void Think()
        {
            // manually find the single player. 
            var client = EntUtils.IndexEnt(1);

            // Can't play if the client is not connected!
            if (client == null)
            {
                return;
            }

            SetNextThink(Engine.Globals.Time + 0.5f);

            if ((client.Origin - Origin).Length() <= Scale)
            {
                Play();
            }
        }

        private void Play()
        {
            EntUtils.PlayCDTrack((int)Health);
            EntUtils.Remove(this);
        }
    }
}
