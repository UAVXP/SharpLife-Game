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
using GoldSource.Server.Game.Game.Entities.Doors;
using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Server.Game.Persistence;
using System.Diagnostics;

namespace GoldSource.Server.Game.Game.Entities
{
    public abstract class BaseToggle : BaseAnimating
    {
        public ToggleState ToggleState;

        public float ActivateFinished;//like attack_finished, but for doors

        /// <summary>
        /// how far a door should slide or rotate
        /// </summary>
        [KeyValue(KeyName = "distance")]
        [Persist]
        public float MoveDistance;

        [KeyValue]
        [Persist]
        public float Wait;

        [KeyValue]
        [Persist]
        public float Lip;

        public float TWidth;// for plats
        public float TLength;// for plats

        public Vector Position1;
        public Vector Position2;
        public Vector Angle1;
        public Vector Angle2;

        public int TriggersLeft;        // trigger_counter only, # of activations remaining

        public float Height;

        public EHandle<BaseEntity> Activator;

        public delegate void MoveDone();

        public MoveDone CallWhenMoveDone;

        public void SetMoveDone(MoveDone func)
        {
            CallWhenMoveDone = func;
        }

        public Vector FinalDest;
        public Vector FinalAngle;

        public DamageTypes DamageInflict;   // DMG_ damage type that the door or tigger does

        /// <summary>
        /// If this button has a master switch, this is the targetname.
        /// A master switch must be of the multisource type. If all 
        /// of the switches in the multisource have been triggered, then
        /// the button will be allowed to operate. Otherwise, it will be
        /// deactivated.
        /// </summary>
        [KeyValue]
        [Persist]
        public string Master;

        public override ToggleState GetToggleState() => ToggleState;

        public override float GetDelay() => Wait;

        // common member functions

        /// <summary>
        /// calculate pev->velocity and pev->nextthink to reach vecDest from
        /// <see cref="Origin"/> traveling at flSpeed
        /// </summary>
        /// <param name="vecDest"></param>
        /// <param name="flSpeed"></param>
        public void LinearMove(in Vector vecDest, float flSpeed)
        {
            Debug.Assert(flSpeed != 0, "LinearMove:  no speed is defined!");
            //	Debug.Assert(CallWhenMoveDone != null, "LinearMove: no post-move function defined");

            FinalDest = vecDest;

            // Already there?
            if (vecDest == Origin)
            {
                LinearMoveDone();
                return;
            }

            // set destdelta to the vector needed to move
            var vecDestDelta = vecDest - Origin;

            // divide vector length by speed to get time to reach dest
            var flTravelTime = vecDestDelta.Length() / flSpeed;

            // set nextthink to trigger a call to LinearMoveDone when dest is reached
            SetNextThink(GetLastThink() + flTravelTime);
            SetThink(LinearMoveDone);

            // scale the destdelta vector by the time spent traveling to get velocity
            Velocity = vecDestDelta / flTravelTime;
        }

        /// <summary>
        /// After moving, set origin to exact final destination, call "move done" function
        /// </summary>
        public void LinearMoveDone()
        {
            var delta = FinalDest - Origin;
            var error = delta.Length();
            if (error > 0.03125)
            {
                LinearMove(FinalDest, 100);
                return;
            }

            SetOrigin(FinalDest);
            Velocity = WorldConstants.g_vecZero;
            SetNextThink(-1);

            CallWhenMoveDone?.Invoke();
        }

        /// <summary>
        /// calculate pev->velocity and pev->nextthink to reach vecDest from
        /// pev->origin traveling at flSpeed
        /// Just like LinearMove, but rotational.
        /// </summary>
        /// <param name="vecDestAngle"></param>
        /// <param name="flSpeed"></param>
        public void AngularMove(in Vector vecDestAngle, float flSpeed)
        {
            Debug.Assert(flSpeed != 0, "AngularMove:  no speed is defined!");
            //	Debug.Assert(CallWhenMoveDone != null, "AngularMove: no post-move function defined");

            FinalAngle = vecDestAngle;

            // Already there?
            if (vecDestAngle == Angles)
            {
                AngularMoveDone();
                return;
            }

            // set destdelta to the vector needed to move
            var vecDestDelta = vecDestAngle - Angles;

            // divide by speed to get time to reach dest
            float flTravelTime = vecDestDelta.Length() / flSpeed;

            // set nextthink to trigger a call to AngularMoveDone when dest is reached
            SetNextThink(GetLastThink() + flTravelTime);
            SetThink(AngularMoveDone);

            // scale the destdelta vector by the time spent traveling to get velocity
            AngularVelocity = vecDestDelta / flTravelTime;
        }

        /// <summary>
        /// After rotating, set angle to exact final angle, call "move done" function
        /// </summary>
        public void AngularMoveDone()
        {
            Angles = FinalAngle;
            AngularVelocity = WorldConstants.g_vecZero;
            SetNextThink(-1);

            CallWhenMoveDone?.Invoke();
        }

        public override bool IsLockedByMaster()
        {
            return !string.IsNullOrEmpty(Master) && !EntUtils.IsMasterTriggered(Master, Activator);
        }

        public static float AxisValue(int flags, in Vector angles)
        {
            if ((flags & BaseDoor.SF.RotateZ) != 0)
            {
                return angles.z;
            }

            if ((flags & BaseDoor.SF.RotateX) != 0)
            {
                return angles.x;
            }

            return angles.y;
        }

        public static void AxisDir(BaseEntity entity)
        {
            if ((entity.SpawnFlags & BaseDoor.SF.RotateZ) != 0)
            {
                entity.MoveDirection = new Vector(0, 0, 1);    // around z-axis
            }
            else if ((entity.SpawnFlags & BaseDoor.SF.RotateX) != 0)
            {
                entity.MoveDirection = new Vector(1, 0, 0);    // around x-axis
            }
            else
            {
                entity.MoveDirection = new Vector(0, 1, 0);    // around y-axis
            }
        }

        public static float AxisDelta(int flags, in Vector angle1, in Vector angle2)
        {
            if ((flags & BaseDoor.SF.RotateZ) != 0)
            {
                return angle1.z - angle2.z;
            }

            if ((flags & BaseDoor.SF.RotateX) != 0)
            {
                return angle1.x - angle2.x;
            }

            return angle1.y - angle2.y;
        }
    }
}
