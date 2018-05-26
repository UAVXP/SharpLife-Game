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
using Server.Game.Entities.Characters.NPCs;
using Server.Game.Entities.MetaData;
using Server.Persistence;
using System.Diagnostics;

namespace Server.Game.Entities.Effects
{
    [LinkEntityToClass("env_explosion")]
    public class EnvExplosion : BaseMonster
    {
        public static class SF
        {
            /// <summary>
            /// when set, env_explosion will not actually inflict damage
            /// </summary>
            public const uint NoDamage = 1 << 0;

            /// <summary>
            /// can this entity be refired?
            /// </summary>
            public const uint Repeatable = 1 << 1;

            /// <summary>
            /// don't draw the fireball
            /// </summary>
            public const uint NoFireball = 1 << 2;

            /// <summary>
            /// don't draw the smoke
            /// </summary>
            public const uint NoSmoke = 1 << 3;

            /// <summary>
            /// don't make a scorch mark
            /// </summary>
            public const uint NoDecal = 1 << 4;

            /// <summary>
            /// don't make sparks
            /// </summary>
            public const uint NoSparks = 1 << 5;
        }

        /// <summary>
        /// how large is the fireball? how much damage?
        /// </summary>
        [KeyValue(KeyName = "iMagnitude")]
        [Persist]
        public int Magnitude;

        /// <summary>
        /// what's the exact fireball sprite scale?
        /// </summary>
        [Persist]
        public int SpriteScale;

        public override void Spawn()
        {
            Solid = Solid.Not;
            Effects = EntityEffects.NoDraw;

            MoveType = MoveType.None;
            /*
            if (Magnitude > 250)
            {
                Magnitude = 250;
            }
            */

            var spriteScale = (Magnitude - 50) * 0.6f;

            /*
            if (spriteScale > 50)
            {
                spriteScale = 50;
            }
            */
            if (spriteScale < 10)
            {
                spriteScale = 10;
            }

            SpriteScale = (int)spriteScale;
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            ModelName = null;//invisible
            Solid = Solid.Not;// intangible

            var vecSpot = Origin + new Vector(0, 0, 8);// trace starts here!

            Server.Engine.Trace.Line(vecSpot, vecSpot + new Vector(0, 0, -40), TraceFlags.IgnoreMonsters, Edict(), out var tr);

            // Pull out of the wall a bit
            if (tr.Fraction != 1.0)
            {
                Origin = tr.EndPos + (tr.PlaneNormal * (Magnitude - 24) * 0.6);
            }

            // draw decal
            if ((SpawnFlags & SF.NoDecal) == 0)
            {
                if (EngineRandom.Float(0, 1) < 0.5f)
                {
                    EntUtils.DecalTrace(tr, Decal.Scorch1);
                }
                else
                {
                    EntUtils.DecalTrace(tr, Decal.Scorch2);
                }
            }

            // draw fireball
            TempEntity.Explosion(Origin, (short)Globals.g_sModelIndexFireball, (SpawnFlags & SF.NoFireball) == 0 ? SpriteScale : 0, 15);

            // do damage
            if ((SpawnFlags & SF.NoDamage) == 0)
            {
                RadiusDamage(this, this, Magnitude, EntityClass.None, DamageTypes.Blast);
            }

            SetThink(Smoke);
            SetNextThink(Engine.Globals.Time + 0.3f);

            // draw sparks
            if ((SpawnFlags & SF.NoSparks) == 0)
            {
                int sparkCount = EngineRandom.Long(0, 3);

                for (var i = 0; i < sparkCount; ++i)
                {
                    Create("spark_shower", Origin, tr.PlaneNormal);
                }
            }
        }

        private void Smoke()
        {
            if ((SpawnFlags & SF.NoSmoke) == 0)
            {
                TempEntity.Smoke(Origin, (short)Globals.g_sModelIndexSmoke, SpriteScale, 12);
            }

            if ((SpawnFlags & SF.Repeatable) == 0)
            {
                EntUtils.Remove(this);
            }
        }

        /// <summary>
        /// Create an explosion
        /// </summary>
        /// <param name="center">Center point of the explosion</param>
        /// <param name="magnitude">How much damage the explosion deals and how large the fireball is</param>
        /// <param name="delay">Delay in seconds when the explosion should take place, or 0 to explode immediately</param>
        /// <param name="doDamage">Whether the explosion should deal damage. Default true</param>
        /// <param name="angles">Angles of the explosion entity</param>
        /// <param name="owner">Which entity owns this explosion</param>
        /// <param name="randomRange">If not 0, adds some random x and y distribution to the center point within a box that is 2 * this value in the x and y axes, centered on the given center point</param>
        public static void CreateExplosion(Vector center, int magnitude, float delay = 0, bool doDamage = true, Vector angles = default, BaseEntity owner = null, float randomRange = 0)
        {
            if (randomRange != 0)
            {
                center.x += EngineRandom.Float(-randomRange, randomRange);
                center.y += EngineRandom.Float(-randomRange, randomRange);
            }

            var explosion = Engine.EntityRegistry.CreateInstance<EnvExplosion>();

            explosion.Origin = center;
            explosion.Angles = angles;
            explosion.Owner = owner;

            explosion.Magnitude = magnitude;

            if (!doDamage)
            {
                explosion.SpawnFlags |= SF.NoDamage;
            }

            explosion.Spawn();

            if (delay == 0)
            {
                //TODO: pass world as both
                explosion.Use(null, null, UseType.Toggle, 0);
            }
            else
            {
                Debug.Assert(delay > 0);
                explosion.SetThink(explosion.SUB_CallUseToggle);
                explosion.SetNextThink(Engine.Globals.Time + delay);
            }
        }
    }
}
