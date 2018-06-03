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
using Server.Engine;
using Server.Game.Entities.MetaData;

namespace Server.Game.Entities.Effects
{
    /// <summary>
    /// Spark Shower
    /// </summary>
    [LinkEntityToClass("spark_shower")]
    public class Shower : BaseEntity
    {
        public override EntityCapabilities ObjectCaps() => EntityCapabilities.DontSave;

        public override void Spawn()
        {
            var velocity = EngineRandom.Float(200, 300) * Angles;
            velocity.x += EngineRandom.Float(-100.0f, 100.0f);
            velocity.y += EngineRandom.Float(-100.0f, 100.0f);

            if (velocity.z >= 0)
            {
                velocity.z += 200;
            }
            else
            {
                velocity.z -= 200;
            }

            Velocity = velocity;

            MoveType = MoveType.Bounce;
            Gravity = 0.5f;
            SetNextThink(Engine.Globals.Time + 0.1f);
            Solid = Solid.Not;
            SetModel("models/grenade.mdl");   // Need a model, just use the grenade, we don't draw it anyway
            SetSize(WorldConstants.g_vecZero, WorldConstants.g_vecZero);
            Effects |= EntityEffects.NoDraw;
            Speed = EngineRandom.Float(0.5f, 1.5f);

            Angles = WorldConstants.g_vecZero;
        }

        public override void Think()
        {
            TempEntity.Sparks(Origin);

            Speed -= 0.1f;
            if (Speed > 0)
            {
                SetNextThink(Engine.Globals.Time + 0.1f);
            }
            else
            {
                EntUtils.Remove(this);
            }

            Flags &= ~EntFlags.OnGround;
        }

        public override void Touch(BaseEntity pOther)
        {
            if ((Flags & EntFlags.OnGround) != 0)
            {
                Velocity *= 0.1;
            }
            else
            {
                Velocity *= 0.6;
            }

            if (((Velocity.x * Velocity.x) + (Velocity.y * Velocity.y)) < 10.0)
            {
                Speed = 0;
            }
        }
    }
}
