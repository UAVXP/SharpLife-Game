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
using GoldSource.Server.Game.Game.Entities.Buttons;
using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Server.Game.Persistence;
using GoldSource.Server.Game.Utility;
using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;
using GoldSource.Shared.Game;
using System;
using System.Diagnostics;

namespace GoldSource.Server.Game.Game.Entities.Doors
{
    /// <summary>
    /// <para>if two doors touch, they are assumed to be connected and operate as a unit.</para>
    /// <para>TOGGLE causes the door to wait in both the start and end states for a trigger event.</para>
    /// <para>
    /// START_OPEN causes the door to move to its destination when spawned, and operate in reverse.
    /// It is used to temporarily or permanently close off an area when triggered (not usefull for
    /// touch or takedamage doors).
    /// </para>
    /// <para>
    /// "angle"         determines the opening direction
    /// "targetname"	if set, no touch field will be spawned and a remote button or trigger
    /// </para>
    /// <para>
    ///                 field activates the door.
    /// "health"        if set, door must be shot open
    /// "speed"         movement speed(100 default)
    /// "wait"          wait before returning(3 default, -1 = never return)
    /// "lip"           lip remaining at end of move(8 default)
    /// "dmg"           damage to inflict when blocked(2 default)
    /// "sounds"
    /// 0)      no sound
    /// 1)      stone
    /// 2)      base
    /// 3)      stone chain
    /// 4)      screechy metal
    /// </para>
    /// </summary>
    [LinkEntityToClass("func_door")]
    public class BaseDoor : BaseToggle
    {
        //skin is used for content type

        public static class SF
        {
            public const uint RotateY = 0;
            public const uint StartOpen = 1;
            public const uint RotateBackwards = 2;
            public const uint Passable = 8;
            public const uint OneWay = 16;
            public const uint NoAutoReturn = 32;
            public const uint RotateZ = 64;
            public const uint RotateX = 128;

            /// <summary>
            /// door must be opened by player's use button
            /// </summary>
            public const uint UseOnly = 256;

            /// <summary>
            /// Monster can't open
            /// </summary>
            public const uint NoMonsters = 512;
            public const uint Silent = 0x80000000;
        }

        /// <summary>
        /// some doors are medi-kit doors, they give players health
        /// </summary>
        [KeyValue]
        [Persist]
        public byte HealthValue;

        /// <summary>
        /// sound a door makes while moving
        /// </summary>
        [KeyValue]
        [Persist]
        public byte MoveSnd;

        /// <summary>
        /// sound a door makes when it stops
        /// </summary>
        [KeyValue]
        [Persist]
        public byte StopSnd;

        /// <summary>
        /// door lock sounds
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

        private string NoiseMoving
        {
            get => pev.Noise1;
            set => pev.Noise1 = value;
        }

        private string NoiseArrived
        {
            get => pev.Noise2;
            set => pev.Noise2 = value;
        }

        public override EntityCapabilities ObjectCaps()
        {
            var caps = base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;

            if ((SpawnFlags & SF.UseOnly) != 0)
            {
                caps |= EntityCapabilities.ImpulseUse;
            }

            return caps;
        }

        public override bool KeyValue(string key, string value)
        {
            if (key == "WaveHeight")
            {
                float.TryParse(value, out var result);
                Scale = result * (1.0f / 8.0f);
                return true;
            }

            return base.KeyValue(key, value);
        }

        public override void Precache()
        {
            // set the door's "in-motion" sound
            NoiseMoving = ButtonUtils.DoorMoveSound(MoveSnd);
            Engine.Server.PrecacheSound(NoiseMoving);

            // set the door's 'reached destination' stop sound
            NoiseArrived = ButtonUtils.DoorStopSound(StopSnd);
            Engine.Server.PrecacheSound(NoiseArrived);

            // get door button sounds, for doors which are directly 'touched' to open

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
            Precache();
            EntUtils.SetMovedir(this);

            if (Skin == 0)
            {//normal door
                if ((SpawnFlags & SF.Passable) != 0)
                {
                    Solid = Solid.Not;
                }
                else
                {
                    Solid = Solid.BSP;
                }
            }
            else
            {// special contents
                Solid = Solid.Not;
                SpawnFlags |= SF.Silent;  // water is silent for now
            }

            MoveType = MoveType.Push;
            SetOrigin(Origin);
            SetModel(ModelName);

            if (Speed == 0)
            {
                Speed = 100;
            }

            Position1 = Origin;
            // Subtract 2 from size because the engine expands bboxes by 1 in all directions making the size too big
            Position2 = Position1 + (MoveDirection * (Math.Abs(MoveDirection.x * (Size.x - 2)) + Math.Abs(MoveDirection.y * (Size.y - 2)) + Math.Abs(MoveDirection.z * (Size.z - 2)) - Lip));
            Debug.Assert(Position1 != Position2, "door start/end positions are equal");
            if ((SpawnFlags & SF.StartOpen) != 0)
            {   // swap pos1 and pos2, put door at pos2
                SetOrigin(Position2);
                Position2 = Position1;
                Position1 = Origin;
            }

            ToggleState = ToggleState.AtBottom;

            // if the door is flagged for USE button activation only, use null touch function
            if ((SpawnFlags & SF.UseOnly) != 0)
            {
                SetTouch(null);
            }
            else // touchable button
            {
                SetTouch(DoorTouch);
            }
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            Activator.Set(pActivator);
            // if not ready to be used, ignore "use" command.
            if (ToggleState == ToggleState.AtBottom || ((SpawnFlags & SF.NoAutoReturn) != 0 && ToggleState == ToggleState.AtTop))
            {
                DoorActivate();
            }
        }

        public override void Blocked(BaseEntity pOther)
        {
            // Hurt the blocker a little.
            if (Damage != 0)
            {
                pOther.TakeDamage(this, this, Damage, DamageTypes.Crush);
            }

            // if a door has a negative wait, it would never come back if blocked,
            // so let it just squash the object to death real fast

            if (Wait >= 0)
            {
                if (ToggleState == ToggleState.GoingDown)
                {
                    DoorGoUp();
                }
                else
                {
                    DoorGoDown();
                }
            }

            // Block all door pieces with the same targetname here.
            if (!string.IsNullOrEmpty(TargetName))
            {
                for (BaseEntity entity = null; (entity = EntUtils.FindEntityByTargetName(entity, TargetName)) != null;)
                {
                    if (entity != this)
                    {
                        //TODO: use entity is BaseDoor door, then do as described below - Solokiller
                        if (entity.ClassName == "func_door" || entity.ClassName == "func_door_rotating")
                        {
                            var door = entity as BaseDoor;
                            //TODO: should let door handle fixup instead - Solokiller

                            if (door.Wait >= 0)
                            {
                                //TODO: This avelocity check is probably wrong - Solokiller
                                if (door.Velocity == Velocity && door.AngularVelocity == Velocity)
                                {
                                    // this is the most hacked, evil, bastardized thing I've ever seen. kjb
                                    if (door.ClassName == "func_door")
                                    {// set origin to realign normal doors
                                        door.Origin = Origin;
                                        door.Velocity = WorldConstants.g_vecZero;// stop!
                                    }
                                    else
                                    {// set angles to realign rotating doors
                                        door.Angles = Angles;
                                        door.AngularVelocity = WorldConstants.g_vecZero;
                                    }
                                }

                                if ((SpawnFlags & SF.Silent) == 0)
                                    StopSound(SoundChannel.Static, NoiseMoving);

                                if (door.ToggleState == ToggleState.GoingDown)
                                {
                                    door.DoorGoUp();
                                }
                                else
                                {
                                    door.DoorGoDown();
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void SetToggleState(ToggleState state)
        {
            if (state == ToggleState.AtTop)
            {
                SetOrigin(Position2);
            }
            else
            {
                SetOrigin(Position1);
            }
        }

        /// <summary>
        /// used to selectivly override defaults
        /// Doors not tied to anything (e.g. button, another door) can be touched, to make them activate.
        /// </summary>
        /// <param name="pOther"></param>
        protected void DoorTouch(BaseEntity pOther)
        {
            // Ignore touches by anything but players
            if (!pOther.IsPlayer())
            {
                return;
            }

            // If door has master, and it's not ready to trigger, 
            // play 'locked' sound

            if (!string.IsNullOrEmpty(Master) && !EntUtils.IsMasterTriggered(Master, pOther))
            {
                ButtonUtils.PlayLockSounds(this, ref LockSound, true, false);
            }

            // If door is somebody's target, then touching does nothing.
            // You have to activate the owner (e.g. button).

            if (!string.IsNullOrEmpty(TargetName))
            {
                // play locked sound
                ButtonUtils.PlayLockSounds(this, ref LockSound, true, false);
                return;
            }

            Activator.Set(pOther);// remember who activated the door

            if (DoorActivate())
            {
                SetTouch(null); // Temporarily disable the touch function, until movement is finished.
            }
        }

        // local functions
        private bool DoorActivate()
        {
            if (!EntUtils.IsMasterTriggered(Master, Activator))
            {
                return false;
            }

            if ((SpawnFlags & SF.NoAutoReturn) != 0 && ToggleState == ToggleState.AtTop)
            {// door should close
                DoorGoDown();
            }
            else
            {// door should open
                if (Activator && Activator.Entity.IsPlayer())
                {// give health if player opened the door (medikit)
                 // VARS( m_eoActivator ).health += m_bHealthValue;

                    Activator.Entity.TakeHealth(HealthValue, DamageTypes.Generic);
                }

                // play door unlock sounds
                ButtonUtils.PlayLockSounds(this, ref LockSound, false, false);

                DoorGoUp();
            }

            return true;
        }

        /// <summary>
        /// Starts the door going to its "up" position (simply ToggleData.vecPosition2).
        /// </summary>
        private void DoorGoUp()
        {
            // It could be going-down, if blocked.
            Debug.Assert(ToggleState == ToggleState.AtBottom || ToggleState == ToggleState.GoingDown);

            // emit door moving and stop sounds on CHAN_STATIC so that the multicast doesn't
            // filter them out and leave a client stuck with looping door sounds!
            if ((SpawnFlags & SF.Silent) == 0)
            {
                if (ToggleState != ToggleState.GoingUp && ToggleState != ToggleState.GoingDown)
                    EmitSound(SoundChannel.Static, NoiseMoving);
            }

            ToggleState = ToggleState.GoingUp;

            SetMoveDone(DoorHitTop);
            if (ClassName == "func_door_rotating")        // !!! BUGBUG Triggered doors don't work with this yet
            {
                float sign = 1.0f;

                if (Activator)
                {
                    var activator = Activator.Entity;

                    if ((SpawnFlags & SF.OneWay) == 0 && MoveDirection.y != 0)        // Y axis rotation, move away from the player
                    {
                        Vector vec = activator.Origin - Origin;
                        Vector angles = activator.Angles;
                        angles.x = 0;
                        angles.z = 0;
                        ServerMathUtils.MakeVectors(angles);
                        //			Vector vnext = (pevToucher.origin + (pevToucher.velocity * 10)) - Origin;
                        ServerMathUtils.MakeVectors(activator.Angles);
                        Vector vnext = (activator.Origin + (Engine.Globals.ForwardVector * 10)) - Origin;
                        if (((vec.x * vnext.y) - (vec.y * vnext.x)) < 0)
                        {
                            sign = -1.0f;
                        }
                    }
                }
                AngularMove(Angle2 * sign, Speed);
            }
            else
            {
                LinearMove(Position2, Speed);
            }
        }

        /// <summary>
        /// Starts the door going to its "down" position (simply ToggleData.vecPosition1).
        /// </summary>
        private void DoorGoDown()
        {
            if ((SpawnFlags & SF.Silent) == 0)
            {
                if (ToggleState != ToggleState.GoingUp && ToggleState != ToggleState.GoingDown)
                {
                    EmitSound(SoundChannel.Static, NoiseMoving);
                }
            }

#if DOOR_ASSERT
            Debug.Assert(ToggleState == ToggleState.AtTop);
#endif // DOOR_ASSERT
            ToggleState = ToggleState.GoingDown;

            SetMoveDone(DoorHitBottom);
            if (ClassName == "func_door_rotating")//rotating door
            {
                AngularMove(Angle1, Speed);
            }
            else
            {
                LinearMove(Position1, Speed);
            }
        }

        /// <summary>
        /// The door has reached the "up" position.  Either go back down, or wait for another activation.
        /// </summary>
        private void DoorHitTop()
        {
            if ((SpawnFlags & SF.Silent) == 0)
            {
                StopSound(SoundChannel.Static, NoiseMoving);
                EmitSound(SoundChannel.Static, NoiseArrived);
            }

            Debug.Assert(ToggleState == ToggleState.GoingUp);
            ToggleState = ToggleState.AtTop;

            // toggle-doors don't come down automatically, they wait for refire.
            if ((SpawnFlags & SF.NoAutoReturn) != 0)
            {
                // Re-instate touch method, movement is complete
                if ((SpawnFlags & SF.UseOnly) == 0)
                {
                    SetTouch(DoorTouch);
                }
            }
            else
            {
                // In flWait seconds, DoorGoDown will fire, unless wait is -1, then door stays open
                SetNextThink(GetLastThink() + Wait);
                SetThink(DoorGoDown);

                if (Wait == -1)
                {
                    SetNextThink(-1);
                }
            }

            // Fire the close target (if startopen is set, then "top" is closed) - netname is the close target
            if (!string.IsNullOrEmpty(NetName) && (SpawnFlags & SF.StartOpen) != 0)
            {
                EntUtils.FireTargets(NetName, Activator, this, UseType.Toggle, 0);
            }

            SUB_UseTargets(Activator, UseType.Toggle); // this isn't finished
        }

        /// <summary>
        /// The door has reached the "down" position.  Back to quiescence.
        /// </summary>
        private void DoorHitBottom()
        {
            if ((SpawnFlags & SF.Silent) == 0)
            {
                StopSound(SoundChannel.Static, NoiseMoving);
                EmitSound(SoundChannel.Static, NoiseArrived);
            }

            Debug.Assert(ToggleState == ToggleState.GoingDown);
            ToggleState = ToggleState.AtBottom;

            // Re-instate touch method, cycle is complete
            if ((SpawnFlags & SF.UseOnly) != 0)
            {// use only door
                SetTouch(null);
            }
            else // touchable door
            {
                SetTouch(DoorTouch);
            }

            SUB_UseTargets(Activator, UseType.Toggle); // this isn't finished

            // Fire the close target (if startopen is set, then "top" is closed) - netname is the close target
            if (!string.IsNullOrEmpty(NetName) && (SpawnFlags & SF.StartOpen) == 0)
            {
                EntUtils.FireTargets(NetName, Activator, this, UseType.Toggle, 0);
            }
        }
    }
}
