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

using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;
using Server.Engine;
using Server.Game.Entities.Characters;
using Server.Game.Entities.MetaData;
using Server.Persistence;
using System;
using System.Diagnostics;

namespace Server.Game.Entities.Buttons
{
    /// <summary>
    /// <para>
    /// When a button is touched, it moves some distance in the direction of it's angle,
    /// triggers all of it's targets, waits some time, then returns to it's original position
    /// where it can be triggered again.
    /// </para>
    /// <para>
    /// "angle"		determines the opening direction
    /// "target"	all entities with a matching targetname will be used
    /// "speed"		override the default 40 speed
    /// "wait"		override the default 1 second wait(-1 = never return)
    /// "lip"		override the default 4 pixel lip remaining at end of move
    /// "health"	if set, the button must be killed instead of touched
    /// "sounds"
    /// 0) steam metal
    /// 1) wooden clunk
    /// 2) metallic click
    /// 3) in-out
    /// </para>
    /// </summary>
    [LinkEntityToClass("func_button")]
    public class BaseButton : BaseToggle
    {
        public static class SF
        {
            public const uint DontMove = 1;
            public const uint NotSolid = 1; //TODO: move to rotbutton

            /// <summary>
            /// button stays pushed until reactivated
            /// </summary>
            public const uint Toggle = 32;

            /// <summary>
            /// button sparks in OFF state
            /// </summary>
            public const uint SparkIfOff = 64;

            /// <summary>
            /// button only fires as a result of USE key
            /// </summary>
            public const uint TouchOnly = 256;
        }

        public enum ButtonCode
        {
            Nothing,
            Activate,
            Return
        }

        /// <summary>
        /// button stays pushed in until touched again?
        /// </summary>
        [Persist]
        private bool StayPushed;

        /// <summary>
        /// a rotating button?  default is a sliding button.
        /// </summary>
        [Persist]
        private bool Rotating;

        /// <summary>
        /// door lock sounds
        /// This is restored in Precache()
        /// </summary>
        private LockSound LockSound;

        /// <summary>
        /// ordinals from entity selection
        /// </summary>
        [KeyValue(KeyName = "locked_sound")]
        [Persist]
        public byte LockedSound;

        [KeyValue(KeyName = "locked_sentence")]
        [Persist]
        public byte LockedSentence;

        [KeyValue(KeyName = "unlocked_sound")]
        [Persist]
        public byte UnlockedSound;

        [KeyValue(KeyName = "unlocked_sentence")]
        [Persist]
        public byte UnlockedSentence;

        [KeyValue]
        [Persist]
        public int Sounds;

        private string NoisePress
        {
            get => pev.Noise;
            set => pev.Noise = value;
        }

        // Buttons that don't take damage can be IMPULSE used
        public override EntityCapabilities ObjectCaps()
        {
            return (base.ObjectCaps() & ~EntityCapabilities.AcrossTransition) | (TakeDamageState != TakeDamageState.No ? EntityCapabilities.None : EntityCapabilities.ImpulseUse);
        }

        public override void Precache()
        {
            if ((SpawnFlags & SF.SparkIfOff) != 0)// this button should spark in OFF state
            {
                Engine.Server.PrecacheSound("buttons/spark1.wav");
                Engine.Server.PrecacheSound("buttons/spark2.wav");
                Engine.Server.PrecacheSound("buttons/spark3.wav");
                Engine.Server.PrecacheSound("buttons/spark4.wav");
                Engine.Server.PrecacheSound("buttons/spark5.wav");
                Engine.Server.PrecacheSound("buttons/spark6.wav");
            }

            // get door button sounds, for doors which require buttons to open

            if (LockedSound != 0)
            {
                var sound = ButtonUtils.ButtonSound(LockedSound);
                Engine.Server.PrecacheSound(sound);
                LockSound.LockedSound = sound;
            }

            if (UnlockedSound != 0)
            {
                var sound = ButtonUtils.ButtonSound(UnlockedSound);
                Engine.Server.PrecacheSound(sound);
                LockSound.UnlockedSound = sound;
            }

            // get sentence group names, for doors which are directly 'touched' to open

            switch (LockedSentence)
            {
                case 1: LockSound.LockedSentence = "NA"; break; // access denied
                case 2: LockSound.LockedSentence = "ND"; break; // security lockout
                case 3: LockSound.LockedSentence = "NF"; break; // blast door
                case 4: LockSound.LockedSentence = "NFIRE"; break; // fire door
                case 5: LockSound.LockedSentence = "NCHEM"; break; // chemical door
                case 6: LockSound.LockedSentence = "NRAD"; break; // radiation door
                case 7: LockSound.LockedSentence = "NCON"; break; // gen containment
                case 8: LockSound.LockedSentence = "NH"; break; // maintenance door
                case 9: LockSound.LockedSentence = "NG"; break; // broken door

                default: LockSound.LockedSentence = null; break;
            }

            switch (UnlockedSentence)
            {
                case 1: LockSound.UnlockedSentence = "EA"; break; // access granted
                case 2: LockSound.UnlockedSentence = "ED"; break; // security door
                case 3: LockSound.UnlockedSentence = "EF"; break; // blast door
                case 4: LockSound.UnlockedSentence = "EFIRE"; break; // fire door
                case 5: LockSound.UnlockedSentence = "ECHEM"; break; // chemical door
                case 6: LockSound.UnlockedSentence = "ERAD"; break; // radiation door
                case 7: LockSound.UnlockedSentence = "ECON"; break; // gen containment
                case 8: LockSound.UnlockedSentence = "EH"; break; // maintenance door

                default: LockSound.UnlockedSentence = null; break;
            }
        }

        public override void Spawn()
        {
            //----------------------------------------------------
            //determine sounds for buttons
            //a sound of 0 should not make a sound
            //----------------------------------------------------
            NoisePress = ButtonUtils.ButtonSound(Sounds);
            Engine.Server.PrecacheSound(NoisePress);

            Precache();

            if ((SpawnFlags & SF.SparkIfOff) != 0)// this button should spark in OFF state
            {
                SetThink(ButtonSpark);
                SetNextThink(Engine.Globals.Time + 0.5f);// no hurry, make sure everything else spawns
            }

            EntUtils.SetMovedir(this);

            MoveType = MoveType.Push;
            Solid = Solid.BSP;
            SetModel(ModelName);

            if (Speed == 0)
            {
                Speed = 40;
            }

            if (Health > 0)
            {
                TakeDamageState = TakeDamageState.Yes;
            }

            if (Wait == 0)
            {
                Wait = 1;
            }

            if (Lip == 0)
            {
                Lip = 4;
            }

            ToggleState = ToggleState.AtBottom;
            Position1 = Origin;
            // Subtract 2 from size because the engine expands bboxes by 1 in all directions making the size too big
            Position2 = Position1
                + (MoveDirection
                * (Math.Abs(MoveDirection.x * (Size.x - 2))
                + Math.Abs(MoveDirection.y * (Size.y - 2))
                + Math.Abs(MoveDirection.z * (Size.z - 2)) - Lip));

            // Is this a non-moving button?
            if (((Position2 - Position1).Length() < 1) || (SpawnFlags & SF.DontMove) != 0)
            {
                Position2 = Position1;
            }

            StayPushed = Wait == -1;
            Rotating = false;

            // if the button is flagged for USE button activation only, take away it's touch function and add a use function

            if ((SpawnFlags & SF.TouchOnly) != 0) // touchable button
            {
                SetTouch(ButtonTouch);
            }
            else
            {
                SetTouch(null);
                SetUse(ButtonUse);
            }
        }

        /// <summary>
        /// Starts the button moving "in/up"
        /// </summary>
        private void ButtonActivate()
        {
            EmitSound(SoundChannel.Voice, NoisePress);

            if (!EntUtils.IsMasterTriggered(Master, Activator))
            {
                // button is locked, play locked sound
                ButtonUtils.PlayLockSounds(this, ref LockSound, true, true);
                return;
            }
            else
            {
                // button is unlocked, play unlocked sound
                ButtonUtils.PlayLockSounds(this, ref LockSound, false, true);
            }

            Debug.Assert(ToggleState == ToggleState.AtBottom);
            ToggleState = ToggleState.GoingUp;

            SetMoveDone(TriggerAndWait);
            if (!Rotating)
            {
                LinearMove(Position2, Speed);
            }
            else
            {
                AngularMove(Angle2, Speed);
            }
        }

        /// <summary>
        /// Touching a button simply "activates" it
        /// </summary>
        /// <param name="pOther"></param>
        public void ButtonTouch(BaseEntity pOther)
        {
            // Ignore touches by anything but players
            if (!(pOther is BasePlayer))
            {
                return;
            }

            Activator.Set(pOther);

            var code = ButtonResponseToTouch();

            if (code == ButtonCode.Nothing)
            {
                return;
            }

            if (!EntUtils.IsMasterTriggered(Master, pOther))
            {
                // play button locked sound
                ButtonUtils.PlayLockSounds(this, ref LockSound, true, true);
                return;
            }

            // Temporarily disable the touch function, until movement is finished.
            SetTouch(null);

            if (code == ButtonCode.Return)
            {
                EmitSound(SoundChannel.Voice, NoisePress);
                SUB_UseTargets(Activator, UseType.Toggle);
                ButtonReturn();
            }
            else    // code == ButtonCode.Activate
            {
                ButtonActivate();
            }
        }

        private void ButtonSpark()
        {
            SetThink(ButtonSpark);
            SetNextThink(Engine.Globals.Time + (0.1f + EngineRandom.Float(0, 1.5f)));// spark again at random interval

            ButtonUtils.DoSpark(this, Mins);
        }

        /// <summary>
        /// Button has reached the "in/up" position.  Activate its "targets", and pause before "popping out"
        /// </summary>
        private void TriggerAndWait()
        {
            Debug.Assert(ToggleState == ToggleState.GoingUp);

            if (!EntUtils.IsMasterTriggered(Master, Activator))
            {
                return;
            }

            ToggleState = ToggleState.AtTop;

            // If button automatically comes back out, start it moving out.
            // Else re-instate touch method
            if (StayPushed || (SpawnFlags & SF.Toggle) != 0)
            {
                if ((SpawnFlags & SF.TouchOnly) == 0) // this button only works if USED, not touched!
                {
                    // ALL buttons are now use only
                    SetTouch(null);
                }
                else
                {
                    SetTouch(ButtonTouch);
                }
            }
            else
            {
                SetNextThink(GetLastThink() + Wait);
                SetThink(ButtonReturn);
            }

            Frame = 1;         // use alternate textures

            SUB_UseTargets(Activator, UseType.Toggle);
        }

        /// <summary>
        /// Starts the button moving "out/down"
        /// </summary>
        private void ButtonReturn()
        {
            Debug.Assert(ToggleState == ToggleState.AtTop);
            ToggleState = ToggleState.GoingDown;

            SetMoveDone(ButtonBackHome);
            if (!Rotating)
            {
                LinearMove(Position1, Speed);
            }
            else
            {
                AngularMove(Angle1, Speed);
            }

            Frame = 0;			// use normal textures
        }

        /// <summary>
        /// Button has returned to start state.  Quiesce it
        /// </summary>
        private void ButtonBackHome()
        {
            Debug.Assert(ToggleState == ToggleState.GoingDown);
            ToggleState = ToggleState.AtBottom;

            if ((SpawnFlags & SF.Toggle) != 0)
            {
                //EMIT_SOUND(ENT(pev), CHAN_VOICE, (char*)STRING(pev->noise), 1, ATTN_NORM);

                SUB_UseTargets(Activator, UseType.Toggle);
            }

            if (!string.IsNullOrEmpty(Target))
            {
                for (BaseEntity entity = null; (entity = EntUtils.FindEntityByTargetName(entity, Target)) != null;)
                {
                    if (entity.ClassName != "multisource")
                    {
                        continue;
                    }

                    entity.Use(Activator, this, UseType.Toggle, 0);
                }
            }

            // Re-instate touch method, movement cycle is complete.
            if ((SpawnFlags & SF.TouchOnly) == 0) // this button only works if USED, not touched!
            {
                // All buttons are now use only	
                SetTouch(null);
            }
            else
            {
                SetTouch(ButtonTouch);
            }

            // reset think for a sparking button
            if ((SpawnFlags & SF.SparkIfOff) != 0)
            {
                SetThink(ButtonSpark);
                SetNextThink(Engine.Globals.Time + 0.5f);// no hurry.
            }
        }

        /// <summary>
        /// Button's Use function
        /// </summary>
        /// <param name="pActivator"></param>
        /// <param name="pCaller"></param>
        /// <param name="useType"></param>
        /// <param name="value"></param>
        private void ButtonUse(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            // Ignore touches if button is moving, or pushed-in and waiting to auto-come-out.
            // UNDONE: Should this use ButtonResponseToTouch() too?
            if (ToggleState == ToggleState.GoingUp || ToggleState == ToggleState.GoingDown)
            {
                return;
            }

            Activator.Set(pActivator);

            if (ToggleState == ToggleState.AtTop)
            {
                if (!StayPushed && (SpawnFlags & SF.Toggle) != 0)
                {
                    EmitSound(SoundChannel.Voice, NoisePress);

                    //SUB_UseTargets( m_eoActivator );
                    ButtonReturn();
                }
            }
            else
            {
                ButtonActivate();
            }
        }

        public override int TakeDamage(BaseEntity inflictor, BaseEntity attacker, float flDamage, DamageTypes bitsDamageType)
        {
            var code = ButtonResponseToTouch();

            if (code == ButtonCode.Nothing)
            {
                return 0;
            }

            // Temporarily disable the touch function, until movement is finished.
            SetTouch(null);

            Activator.Set(attacker);
            if (!Activator)
            {
                return 0;
            }

            if (code == ButtonCode.Return)
            {
                EmitSound(SoundChannel.Voice, NoisePress);

                // Toggle buttons fire when they get back to their "home" position
                if ((SpawnFlags & SF.Toggle) == 0)
                {
                    SUB_UseTargets(Activator, UseType.Toggle, 0);
                }

                ButtonReturn();
            }
            else // code == ButtonCode.Activate
            {
                ButtonActivate();
            }

            return 0;
        }

        public ButtonCode ButtonResponseToTouch()
        {
            // Ignore touches if button is moving, or pushed-in and waiting to auto-come-out.
            if (ToggleState == ToggleState.GoingUp
                || ToggleState == ToggleState.GoingDown
                || (ToggleState == ToggleState.AtTop && !StayPushed && (SpawnFlags & SF.Toggle) == 0))
            {
                return ButtonCode.Nothing;
            }

            if (ToggleState == ToggleState.AtTop)
            {
                if ((SpawnFlags & SF.Toggle) != 0 && !StayPushed)
                {
                    return ButtonCode.Return;
                }
            }
            else
            {
                return ButtonCode.Activate;
            }

            return ButtonCode.Nothing;
        }
    }
}
