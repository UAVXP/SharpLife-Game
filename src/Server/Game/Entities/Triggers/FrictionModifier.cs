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

using GoldSource.Shared.Entities;
using Server.Game.Entities.MetaData;
using Server.Persistence;

namespace Server.Game.Entities.Triggers
{
    /// <summary>
    /// Sets toucher's friction to FrictionFraction (1.0 = normal friction)
    /// </summary>
    [LinkEntityToClass("func_friction")]
    public class FrictionModifier : BaseEntity
    {
        /// <summary>
        /// Sorry, couldn't resist this name :)
        /// </summary>
        [Persist]
        public float FrictionFraction;

        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;

        public override bool KeyValue(string key, string value)
        {
            if (key == "modifier")
            {
                float.TryParse(value, out var result);
                FrictionFraction = result / 100.0f;
                return true;
            }

            return base.KeyValue(key, value);
        }

        public override void Spawn()
        {
            Solid = Solid.Trigger;
            SetModel(ModelName);    // set size and link into world
            MoveType = MoveType.None;
            SetTouch(ChangeFriction);
        }

        private void ChangeFriction(BaseEntity pOther)
        {
            if (pOther.MoveType != MoveType.BounceMissile && pOther.MoveType != MoveType.Bounce)
            {
                pOther.Friction = FrictionFraction;
            }
        }
    }
}
