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
using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;
using Server.Game.Entities.MetaData;
using System.Diagnostics;

namespace Server.Game.Entities.Plats
{
    /// <summary>
    /// <para>Plats are always drawn in the extended position, so they will light correctly.</para>
    /// <para>
    /// If the plat is the target of another trigger or button, it will start out disabled in
    /// the extended position until it is trigger, when it will lower and become a normal plat.
    /// </para>
    /// <para>
    /// If the "height" key is set, that will determine the amount the plat moves, instead of
    /// being implicitly determined by the model's height.
    /// </para>
    /// <para>
    /// Set "sounds" to one of the following:
    /// 1) base fast
    /// 2) chain slow
    /// </para>
    /// </summary>
    [LinkEntityToClass("func_plat")]
    public class FuncPlat : BasePlatTrain
    {
        public override void Precache()
        {
            base.Precache();
            //PRECACHE_SOUND("plats/platmove1.wav");
            //PRECACHE_SOUND("plats/platstop1.wav");
            if (!IsTogglePlat())
            {
                Engine.EntityRegistry.CreateInstance<PlatTrigger>().SpawnInsideTrigger(this);      // the "start moving" trigger
            }
        }

        public override void Spawn()
        {
            Setup();

            Precache();

            // If this platform is the target of some button, it starts at the TOP position,
            // and is brought down by that button.  Otherwise, it starts at BOTTOM.
            if (!string.IsNullOrEmpty(TargetName))
            {
                SetOrigin(Position1);
                ToggleState = ToggleState.AtTop;
                SetUse(PlatUse);
            }
            else
            {
                SetOrigin(Position2);
                ToggleState = ToggleState.AtBottom;
            }
        }

        private void Setup()
        {
            //pev->noiseMovement = MAKE_STRING("plats/platmove1.wav");
            //pev->noiseStopMoving = MAKE_STRING("plats/platstop1.wav");

            if (TLength == 0)
            {
                TLength = 80;
            }

            if (TWidth == 0)
            {
                TWidth = 10;
            }

            Angles = WorldConstants.g_vecZero;

            Solid = Solid.BSP;
            MoveType = MoveType.Push;

            SetOrigin(Origin);       // set size and link into world
            SetSize(Mins, Maxs);
            SetModel(ModelName);

            // vecPosition1 is the top position, vecPosition2 is the bottom
            Position1 = Origin;
            Position2 = Origin;

            if (Height != 0)
            {
                Position2.z = Origin.z - Height;
            }
            else
            {
                Position2.z = Origin.z - Size.z + 8;
            }

            if (Speed == 0)
            {
                Speed = 150;
            }

            if (Volume == 0)
            {
                Volume = 0.85f;
            }
        }

        public override void Blocked(BaseEntity pOther)
        {
            Log.Alert(AlertType.AIConsole, $"{ClassName} Blocked by {pOther.ClassName}\n");
            // Hurt the blocker a little
            pOther.TakeDamage(this, this, 1, DamageTypes.Crush);

            if (!string.IsNullOrEmpty(NoiseMoving))
            {
                StopSound(SoundChannel.Static, NoiseMoving);
            }

            // Send the platform back where it came from
            Debug.Assert(ToggleState == ToggleState.GoingUp || ToggleState == ToggleState.GoingDown);

            if (ToggleState == ToggleState.GoingUp)
            {
                GoDown();
            }
            else if (ToggleState == ToggleState.GoingDown)
            {
                GoUp();
            }
        }

        /// <summary>
        /// Used by SUB_UseTargets, when a platform is the target of a button
        /// Start bringing platform down
        /// </summary>
        /// <param name="pActivator"></param>
        /// <param name="pCaller"></param>
        /// <param name="useType"></param>
        /// <param name="value"></param>
        private void PlatUse(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            if (IsTogglePlat())
            {
                // Top is off, bottom is on
                var on = ToggleState == ToggleState.AtBottom;

                if (!ShouldToggle(useType, on))
                {
                    return;
                }

                if (ToggleState == ToggleState.AtTop)
                {
                    GoDown();
                }
                else if (ToggleState == ToggleState.AtBottom)
                {
                    GoUp();
                }
            }
            else
            {
                SetUse(null);

                if (ToggleState == ToggleState.AtTop)
                {
                    GoDown();
                }
            }
        }

        /// <summary>
        /// Platform is at bottom, now starts moving up
        /// </summary>
        public virtual void GoUp()
        {
            if (!string.IsNullOrEmpty(NoiseMoving))
            {
                EmitSound(SoundChannel.Static, NoiseMoving, Volume);
            }

            Debug.Assert(ToggleState == ToggleState.AtBottom || ToggleState == ToggleState.GoingDown);
            ToggleState = ToggleState.GoingDown;
            SetMoveDone(HitTop);
            LinearMove(Position1, Speed);
        }

        /// <summary>
        /// Platform is at top, now starts moving down
        /// </summary>
        public virtual void GoDown()
        {
            if (!string.IsNullOrEmpty(NoiseMoving))
            {
                EmitSound(SoundChannel.Static, NoiseMoving, Volume);
            }

            Debug.Assert(ToggleState == ToggleState.AtTop || ToggleState == ToggleState.GoingUp);
            ToggleState = ToggleState.GoingDown;
            SetMoveDone(HitBottom);
            LinearMove(Position2, Speed);
        }

        /// <summary>
        /// Platform has hit top.  Pauses, then starts back down again
        /// </summary>
        public virtual void HitTop()
        {
            if (!string.IsNullOrEmpty(NoiseMoving))
            {
                StopSound(SoundChannel.Static, NoiseMoving);
            }

            if (!string.IsNullOrEmpty(NoiseArrived))
            {
                EmitSound(SoundChannel.Weapon, NoiseArrived, Volume);
            }

            Debug.Assert(ToggleState == ToggleState.GoingUp);
            ToggleState = ToggleState.AtTop;

            if (!IsTogglePlat())
            {
                // After a delay, the platform will automatically start going down again.
                SetThink(GoDown);
                SetNextThink(GetLastThink() + 3);
            }
        }

        /// <summary>
        /// Platform has hit bottom.  Stops and waits forever
        /// </summary>
        public virtual void HitBottom()
        {
            if (!string.IsNullOrEmpty(NoiseMoving))
            {
                StopSound(SoundChannel.Static, NoiseMoving);
            }

            if (!string.IsNullOrEmpty(NoiseArrived))
            {
                EmitSound(SoundChannel.Weapon, NoiseArrived, Volume);
            }

            Debug.Assert(ToggleState == ToggleState.GoingDown);
            ToggleState = ToggleState.AtBottom;
        }
    }
}
