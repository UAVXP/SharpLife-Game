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
using Server.Persistence;

namespace Server.Game.Entities.Plats
{
    /// <summary>
    /// Trains are moving platforms that players can ride.
    /// The targets origin specifies the min point of the train at each corner.
    /// The train spawns at the first target it is pointing at.
    /// If the train is the target of a button or trigger, it will not begin moving until activated.
    /// Speed   default 100
    /// Damage     default	2
    /// sounds
    /// 1) ratchet metal
    /// </summary>
    [LinkEntityToClass("func_train")]
    public class FuncTrain : BasePlatTrain
    {
        public static new class SF
        {
            public const uint WaitRetrigger = 1;

            /// <summary>
            /// Train is initially moving
            /// </summary>
            public const uint StartOn = 4;

            /// <summary>
            /// Train is not solid -- used to make water trains
            /// </summary>
            public const uint Passable = 8;
        }

        [Persist]
        private EHandle<BaseEntity> CurrentTarget;

        [Persist]
        private bool Activated;

        public override void Spawn()
        {
            Precache();
            if (Speed == 0)
            {
                Speed = 100;
            }

            if (string.IsNullOrEmpty(Target))
            {
                Log.Alert(AlertType.Console, "FuncTrain with no target");
            }

            if (Damage == 0)
            {
                Damage = 2;
            }

            MoveType = MoveType.Push;

            if ((SpawnFlags & SF.Passable) != 0)
            {
                Solid = Solid.Not;
            }
            else
            {
                Solid = Solid.BSP;
            }

            SetModel(ModelName);
            SetSize(Mins, Maxs);
            SetOrigin(Origin);

            Activated = false;

            if (Volume == 0)
            {
                Volume = 0.85f;
            }
        }

        public override void Activate()
        {
            // Not yet active, so teleport to first target
            if (!Activated)
            {
                Activated = true;
                var target = EntUtils.FindEntityByTargetName(null, Target);

                //Added this null check because missing targets would cause crashes - Solokiller
                if (target != null)
                {
                    Target = target.Target;
                    CurrentTarget.Set(target);// keep track of this since path corners change our target for us.

                    SetOrigin(target.Origin - ((Mins + Maxs) * 0.5f));
                }

                if (string.IsNullOrEmpty(TargetName))
                {   // not triggered, so start immediately
                    SetNextThink(GetLastThink() + 0.1f);
                    SetThink(Next);
                }
                else
                {
                    SpawnFlags |= SF.WaitRetrigger;
                }
            }
        }

        public override void OverrideReset()
        {
            // Are we moving?
            if (Velocity != WorldConstants.g_vecZero && GetNextThink() != 0)
            {
                Target = Message;
                // now find our next target
                var pTarg = GetNextTarget();
                if (pTarg == null)
                {
                    SetNextThink(0);
                    Velocity = WorldConstants.g_vecZero;
                }
                else    // Keep moving for 0.1 secs, then find path_corner again and restart
                {
                    SetThink(Next);
                    SetNextThink(GetLastThink() + 0.1f);
                }
            }
        }

        public override void Blocked(BaseEntity pOther)
        {
            if (Engine.Globals.Time < ActivateFinished)
            {
                return;
            }

            ActivateFinished = Engine.Globals.Time + 0.5f;

            pOther.TakeDamage(this, this, Damage, DamageTypes.Crush);
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            if ((SpawnFlags & SF.WaitRetrigger) != 0)
            {
                // Move toward my target
                SpawnFlags &= ~SF.WaitRetrigger;
                Next();
            }
            else
            {
                SpawnFlags |= SF.WaitRetrigger;
                // Pop back to last target if it's available
                if (Enemy != null)
                {
                    Target = Enemy.TargetName;
                }

                SetNextThink(0);
                Velocity = WorldConstants.g_vecZero;

                if (!string.IsNullOrEmpty(NoiseArrived))
                {
                    EmitSound(SoundChannel.Voice, NoiseArrived, Volume);
                }
            }
        }

        private void WaitAtNode()
        {
            var target = CurrentTarget.Entity;

            // Fire the pass target if there is one
            if (!string.IsNullOrEmpty(target.Message))
            {
                EntUtils.FireTargets(target.Message, this, this, UseType.Toggle, 0);
                if ((target.SpawnFlags & PathCorner.SF.FireOnce) != 0)
                {
                    target.Message = string.Empty;
                }
            }

            // need pointer to LAST target.
            if ((target.SpawnFlags & SF.WaitRetrigger) != 0 || (SpawnFlags & SF.WaitRetrigger) != 0)
            {
                SpawnFlags |= SF.WaitRetrigger;
                // clear the sound channel.
                if (!string.IsNullOrEmpty(NoiseMoving))
                {
                    StopSound(SoundChannel.Static, NoiseMoving);
                }

                if (!string.IsNullOrEmpty(NoiseArrived))
                {
                    EmitSound(SoundChannel.Voice, NoiseArrived, Volume);
                }

                SetNextThink(0);
                return;
            }

            // Log.Alert ( AlertType.Console, "%f\n", Wait );

            if (Wait != 0)
            {// -1 wait will wait forever!		
                SetNextThink(GetLastThink() + Wait);
                if (!string.IsNullOrEmpty(NoiseMoving))
                {
                    StopSound(SoundChannel.Static, NoiseMoving);
                }

                if (!string.IsNullOrEmpty(NoiseArrived))
                {
                    EmitSound(SoundChannel.Voice, NoiseArrived, Volume);
                }

                SetThink(Next);
            }
            else
            {
                Next();// do it RIGHT now!
            }
        }

        /// <summary>
        /// Train next - path corner needs to change to next target 
        /// </summary>
        private void Next()
        {
            // now find our next target
            var pTarg = GetNextTarget();

            if (pTarg == null)
            {
                if (!string.IsNullOrEmpty(NoiseMoving))
                {
                    StopSound(SoundChannel.Static, NoiseMoving);
                }
                // Play stop sound
                if (!string.IsNullOrEmpty(NoiseArrived))
                {
                    EmitSound(SoundChannel.Voice, NoiseArrived, Volume);
                }

                return;
            }

            // Save last target in case we need to find it again
            Message = Target;

            Target = pTarg.Target;
            Wait = pTarg.GetDelay();

            if (CurrentTarget)
            {
                var target = CurrentTarget.Entity;

                if (target.Speed != 0)
                {// don't copy Speed from target if it is 0 (uninitialized)
                    Speed = target.Speed;
                    Log.Alert(AlertType.AIConsole, $"Train {TargetName} Speed to {Speed:0000.00}%4.2f\n");
                }
            }
            CurrentTarget.Set(pTarg);// keep track of this since path corners change our target for us.

            Enemy = pTarg;//hack

            if ((pTarg.SpawnFlags & PathCorner.SF.Teleport) != 0)
            {
                // Path corner has indicated a teleport to the next corner.
                Effects |= EntityEffects.NoInterp;
                SetOrigin(pTarg.Origin - ((Mins + Maxs) * 0.5f));
                WaitAtNode(); // Get on with doing the next path corner.
            }
            else
            {
                // Normal linear move.

                // CHANGED this from SoundChannel.Voice to SoundChannel.Static around OEM beta time because trains should
                // use SoundChannel.Static for their movement sounds to prevent sound field problems.
                // this is not a hack or temporary fix, this is how things should be. (sjb).
                if (!string.IsNullOrEmpty(NoiseMoving))
                {
                    StopSound(SoundChannel.Static, NoiseMoving);
                }

                if (!string.IsNullOrEmpty(NoiseMoving))
                {
                    EmitSound(SoundChannel.Static, NoiseMoving, Volume);
                }

                Effects &= ~EntityEffects.NoInterp;
                CallWhenMoveDone = WaitAtNode;
                LinearMove(pTarg.Origin - ((Mins + Maxs) * 0.5f), Speed);
            }
        }
    }
}
