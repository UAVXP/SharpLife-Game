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
using GoldSource.Shared.Engine;
using GoldSource.Shared.Engine.PlayerPhysics;
using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;
using Server.Game.Materials;
using Server.Game.Sound;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.API.Implementations
{
    //TODO: shared with client
    public sealed class PlayerPhysics : IPlayerPhysics
    {
        private MaterialsSystem Materials { get; }

        private PlayerMove PlayerMove { get; set; }

        private IEnginePhysics EnginePhysics { get; set; }

        private const int NumStuckVectors = 54;

        /// <summary>
        /// Don't check again too quickly
        /// </summary>
        private const float CheckStuckMinTime = 0.05f;

        private const int MaxStepSearchIterations = 10;

        private const float PlayerDuckingMultiplier = 0.333f;
        private const float TimeToDuck = 0.4f;

        private const int StuckMoveUp = 1;

        private const float StopEpsilon = 0.1f;

        private const int MaxClipPlanes = 5;

        private const int WaterJumpHeight = 8;

        private static readonly Vector[] StuckTable;

        /// <summary>
        /// Table mapping current contents to their current vector
        /// Indexable as contents - Contents.Current0
        /// </summary>
        private static readonly Vector[] CurrentTable =
        {
            new Vector(1, 0, 0), new Vector(0, 1, 0), new Vector(-1, 0, 0),
            new Vector(0, -1, 0), new Vector(0, 0, 1), new Vector(0, 0, -1)
        };

        /// <summary>
        /// Last time we did a full stuck test
        /// </summary>
        private readonly float[,] StuckCheckTime = new float[Framework.MaxClients, 2];

        private readonly int[,] StuckLast = new int[Framework.MaxClients, 2];

        private bool OnLadder;

        private int SkipStep;

        //TODO: figure out how to have this in the material system
        //TODO: the impact sounds are currently temporary, should match the original though
        private static readonly MaterialType LadderMaterialType = new MaterialType(
            MaterialTypeCode.Concrete,
            0.65f, 0.65f, 0.65f, 0.65f,
            600, 600,
            new List<string>
            {
                "player/pl_step1.wav",
                "player/pl_step2.wav"
            },
            new List<MaterialType.MovementSound>
            {
                new MaterialType.MovementSound("player/pl_ladder1.wav", false),
                new MaterialType.MovementSound("player/pl_ladder3.wav", false),
                new MaterialType.MovementSound("player/pl_ladder2.wav", true),
                new MaterialType.MovementSound("player/pl_ladder4.wav", true),
            }
            );

        private static readonly MaterialType WadeMaterialType = new MaterialType(
            MaterialTypeCode.Slosh,
            0.5f, 0.5f, 0.2f, 0.5f,
            400, 300,
            new List<string>
            {
                "player/pl_step1.wav",
                "player/pl_step2.wav"
            },
            new List<MaterialType.MovementSound>
            {
                new MaterialType.MovementSound("player/pl_wade1.wav", false),
                new MaterialType.MovementSound("player/pl_wade2.wav", false),
                new MaterialType.MovementSound("player/pl_wade3.wav", true),
                new MaterialType.MovementSound("player/pl_wade4.wav", true),
            }
            )
        {
            SkipStepInterval = 4
        };

        private static readonly MaterialType SloshMaterialType = new MaterialType(
            MaterialTypeCode.Slosh,
            0.5f, 0.5f, 0.2f, 0.5f,
            400, 300,
            new List<string>
            {
                "player/pl_step1.wav",
                "player/pl_step2.wav"
            },
            new List<MaterialType.MovementSound>
            {
                new MaterialType.MovementSound("player/pl_slosh1.wav", false),
                new MaterialType.MovementSound("player/pl_slosh3.wav", false),
                new MaterialType.MovementSound("player/pl_slosh2.wav", true),
                new MaterialType.MovementSound("player/pl_slosh4.wav", true),
            }
            );

        static PlayerPhysics()
        {
            //Initialize the stuck table
            StuckTable = new Vector[NumStuckVectors];

            float x, y, z;

            var idx = 0;
            // Little Moves.
            x = y = 0;
            // Z moves
            for (z = -0.125f; z <= 0.125f; z += 0.125f)
            {
                StuckTable[idx][0] = x;
                StuckTable[idx][1] = y;
                StuckTable[idx][2] = z;
                ++idx;
            }
            x = z = 0;
            // Y moves
            for (y = -0.125f; y <= 0.125f; y += 0.125f)
            {
                StuckTable[idx][0] = x;
                StuckTable[idx][1] = y;
                StuckTable[idx][2] = z;
                ++idx;
            }
            y = z = 0;
            // X moves
            for (x = -0.125f; x <= 0.125f; x += 0.125f)
            {
                StuckTable[idx][0] = x;
                StuckTable[idx][1] = y;
                StuckTable[idx][2] = z;
                ++idx;
            }

            // Remaining multi axis nudges.
            for (x = -0.125f; x <= 0.125f; x += 0.250f)
            {
                for (y = -0.125f; y <= 0.125f; y += 0.250f)
                {
                    for (z = -0.125f; z <= 0.125f; z += 0.250f)
                    {
                        StuckTable[idx][0] = x;
                        StuckTable[idx][1] = y;
                        StuckTable[idx][2] = z;
                        ++idx;
                    }
                }
            }

            // Big Moves.
            x = y = 0;
            var zi = new Vector(0.0f, 1.0f, 6.0f);

            for (var i = 0; i < 3; ++i)
            {
                // Z moves
                z = zi[i];
                StuckTable[idx][0] = x;
                StuckTable[idx][1] = y;
                StuckTable[idx][2] = z;
                idx++;
            }

            x = z = 0;

            // Y moves
            for (y = -2.0f; y <= 2.0f; y += 2.0f)
            {
                StuckTable[idx][0] = x;
                StuckTable[idx][1] = y;
                StuckTable[idx][2] = z;
                idx++;
            }
            y = z = 0;
            // X moves
            for (x = -2.0f; x <= 2.0f; x += 2.0f)
            {
                StuckTable[idx][0] = x;
                StuckTable[idx][1] = y;
                StuckTable[idx][2] = z;
                idx++;
            }

            // Remaining multi axis nudges.
            for (var i = 0; i < 3; ++i)
            {
                z = zi[i];

                for (x = -2.0f; x <= 2.0f; x += 2.0f)
                {
                    for (y = -2.0f; y <= 2.0f; y += 2.0f)
                    {
                        StuckTable[idx][0] = x;
                        StuckTable[idx][1] = y;
                        StuckTable[idx][2] = z;
                        idx++;
                    }
                }
            }
        }

        public PlayerPhysics(MaterialsSystem materials)
        {
            Materials = materials ?? throw new ArgumentNullException(nameof(materials));
        }

        public void Move(PlayerMove ppmove, IEnginePhysics enginePhysics)
        {
            if (PlayerMove == null)
            {
                throw new InvalidOperationException("Cannot run player physics before it is initialized");
            }

            if (EnginePhysics == null)
            {
                EnginePhysics = enginePhysics ?? throw new ArgumentNullException(nameof(enginePhysics));
            }

            RunPlayerMove();

            if (PlayerMove.OnGround != -1)
            {
                PlayerMove.Flags |= EntFlags.OnGround;
            }
            else
            {
                PlayerMove.Flags &= ~EntFlags.OnGround;
            }

            // In single player, reset friction after each movement to FrictionModifier Triggers work still.
            if (!PlayerMove.IsMultiplayer && (PlayerMove.MoveType == MoveType.Walk))
            {
                PlayerMove.Friction = 1.0f;
            }
        }

        public void Init(PlayerMove ppmove)
        {
            if (PlayerMove != null)
            {
                throw new InvalidOperationException("Cannot initialize player physics multiple times");
            }

            PlayerMove = ppmove ?? throw new ArgumentNullException(nameof(ppmove));
        }

        public byte FindTextureType(string name)
        {
            return (byte)Materials.Find(name);
        }

        public bool GetHullBounds(PMHull hullnumber, out Vector mins, out Vector maxs)
        {
            switch (hullnumber)
            {
                case PMHull.Standing:
                    {
                        mins = WorldConstants.HULL_MIN;
                        maxs = WorldConstants.HULL_MAX;
                        return true;
                    }

                case PMHull.Crouched:
                    {
                        mins = WorldConstants.DUCK_HULL_MIN;
                        maxs = WorldConstants.DUCK_HULL_MAX;
                        return true;
                    }

                case PMHull.Point:
                    {
                        mins = new Vector(0, 0, 0);
                        maxs = new Vector(0, 0, 0);
                        return true;
                    }

                default:
                    {
                        mins = new Vector();
                        maxs = new Vector();
                        return false;
                    }
            }
        }

        private void RunPlayerMove()
        {
            // Adjust speeds etc.
            CheckParameters();

            // Assume we don't touch anything
            PlayerMove.NumTouch = 0;

            // # of msec to apply movement
            PlayerMove.FrameTime = PlayerMove.Cmd.MSec * 0.001f;

            ReduceTimers();

            {
                // Convert view angles to vectors
                MathUtils.AngleVectors(PlayerMove.Angles, out var forward, out var right, out var up);
                PlayerMove.Forward = forward;
                PlayerMove.Right = right;
                PlayerMove.Up = up;
            }

            // ShowClipBox();

            // Special handling for spectator and observers. (iuser1 is set if the player's in observer mode)
            if (PlayerMove.IsSpectator || PlayerMove.UserInt1 > 0)
            {
                SpectatorMove();
                CategorizePosition();
                return;
            }

            // Always try and unstick us unless we are in NOCLIP mode
            if (PlayerMove.MoveType != MoveType.Noclip && PlayerMove.MoveType != MoveType.None)
            {
                if (CheckStuck())
                {
                    return;  // Can't move, we're stuck
                }
            }

            // Now that we are "unstuck", see where we are ( waterlevel and type, PlayerMove.onground ).
            CategorizePosition();

            // Store off the starting water level
            PlayerMove.OldWaterLevel = PlayerMove.WaterLevel;

            // If we are not on ground, store off how fast we are moving down
            if (PlayerMove.OnGround == -1)
            {
                PlayerMove.FallVelocity = -PlayerMove.Velocity[2];
            }

            PhysEnt ladder = null;

            OnLadder = false;
            // Don't run ladder code if dead or on a train
            if (!PlayerMove.Dead && (PlayerMove.Flags & EntFlags.OnTrain) == 0)
            {
                ladder = CheckForLadder();
                if (ladder != null)
                {
                    OnLadder = true;
                }
            }

            UpdateStepSound();

            Duck();

            // Don't run ladder code if dead or on a train
            if (!PlayerMove.Dead && (PlayerMove.Flags & EntFlags.OnTrain) == 0)
            {
                if (ladder != null)
                {
                    LadderMove(ladder);
                }
                else if (PlayerMove.MoveType != MoveType.Walk
                          && PlayerMove.MoveType != MoveType.Noclip)
                {
                    // Clear ladder stuff unless player is noclipping
                    //  it will be set immediately again next frame if necessary
                    PlayerMove.MoveType = MoveType.Walk;
                }
            }

#if !_TFC
            // Slow down, I'm pulling it! (a box maybe) but only when I'm standing on ground
            if ((PlayerMove.OnGround != -1) && (PlayerMove.Cmd.Buttons & InputKeys.Use) != 0)
            {
                PlayerMove.Velocity *= 0.3f;
            }
#endif

            // Handle movement
            switch (PlayerMove.MoveType)
            {
                default:
                    EnginePhysics.Con_DPrintf($"Bogus pmove player movetype {PlayerMove.MoveType} on ({PlayerMove.IsServer}) false=cl true=sv\n");
                    break;

                case MoveType.None:
                    break;

                case MoveType.Noclip:
                    NoClip();
                    break;

                case MoveType.Toss:
                case MoveType.Bounce:
                    Physics_Toss();
                    break;

                case MoveType.Fly:

                    CheckWater();

                    // Was jump button pressed?
                    // If so, set velocity to 270 away from ladder.  This is currently wrong.
                    // Also, set MOVE_TYPE to walk, too.
                    if ((PlayerMove.Cmd.Buttons & InputKeys.Jump) != 0)
                    {
                        if (ladder == null)
                        {
                            Jump();
                        }
                    }
                    else
                    {
                        PlayerMove.OldButtons &= ~InputKeys.Jump;
                    }

                    // Perform the move accounting for any base velocity.
                    PlayerMove.Velocity += PlayerMove.BaseVelocity;
                    FlyMove();
                    PlayerMove.Velocity -= PlayerMove.BaseVelocity;
                    break;

                case MoveType.Walk:
                    if (!InWater())
                    {
                        AddCorrectGravity();
                    }

                    // If we are leaping out of the water, just update the counters.
                    if (PlayerMove.WaterJumpTime != 0)
                    {
                        WaterJump();
                        FlyMove();

                        // Make sure waterlevel is set correctly
                        CheckWater();
                        return;
                    }

                    // If we are swimming in the water, see if we are nudging against a place we can jump up out
                    //  of, and, if so, start out jump.  Otherwise, if we are not moving up, then reset jump timer to 0
                    if (PlayerMove.WaterLevel >= WaterLevel.Waist)
                    {
                        if (PlayerMove.WaterLevel == WaterLevel.Waist)
                        {
                            CheckWaterJump();
                        }

                        // If we are falling again, then we must not trying to jump out of water any more.
                        if (PlayerMove.Velocity[2] < 0 && PlayerMove.WaterJumpTime != 0)
                        {
                            PlayerMove.WaterJumpTime = 0;
                        }

                        // Was jump button pressed?
                        if ((PlayerMove.Cmd.Buttons & InputKeys.Jump) != 0)
                        {
                            Jump();
                        }
                        else
                        {
                            PlayerMove.OldButtons &= ~InputKeys.Jump;
                        }

                        // Perform regular water movement
                        WaterMove();

                        PlayerMove.Velocity -= PlayerMove.BaseVelocity;

                        // Get a final position
                        CategorizePosition();
                    }
                    else

                    // Not underwater
                    {
                        // Was jump button pressed?
                        if ((PlayerMove.Cmd.Buttons & InputKeys.Jump) != 0)
                        {
                            if (ladder == null)
                            {
                                Jump();
                            }
                        }
                        else
                        {
                            PlayerMove.OldButtons &= ~InputKeys.Jump;
                        }

                        // Fricion is handled before we add in any base velocity. That way, if we are on a conveyor, 
                        //  we don't slow when standing still, relative to the conveyor.
                        if (PlayerMove.OnGround != -1)
                        {
                            var velocity = PlayerMove.Velocity;
                            velocity[2] = 0;
                            PlayerMove.Velocity = velocity;
                            Friction();
                        }

                        // Make sure velocity is valid.
                        CheckVelocity();

                        // Are we on ground now
                        if (PlayerMove.OnGround != -1)
                        {
                            WalkMove();
                        }
                        else
                        {
                            AirMove();  // Take into account movement when in air.
                        }

                        // Set final flags.
                        CategorizePosition();

                        // Now pull the base velocity back out.
                        // Base velocity is set if you are on a moving object, like
                        //  a conveyor (or maybe another monster?)
                        PlayerMove.Velocity -= PlayerMove.BaseVelocity;

                        // Make sure velocity is valid.
                        CheckVelocity();

                        // Add any remaining gravitational component.
                        if (!InWater())
                        {
                            FixupGravityVelocity();
                        }

                        // If we are on ground, no downward velocity.
                        if (PlayerMove.OnGround != -1)
                        {
                            var velocity = PlayerMove.Velocity;
                            velocity[2] = 0;
                            PlayerMove.Velocity = velocity;
                            Friction();
                        }

                        // See if we landed on the ground with enough force to play
                        //  a landing sound.
                        CheckFalling();
                    }

                    // Did we enter or leave the water?
                    PlayWaterSounds();
                    break;
            }
        }

        private void CheckParameters()
        {
            float spd = (PlayerMove.Cmd.ForwardMove * PlayerMove.Cmd.ForwardMove)
                  + (PlayerMove.Cmd.SideMove * PlayerMove.Cmd.SideMove)
                  + (PlayerMove.Cmd.UpMove * PlayerMove.Cmd.UpMove);
            spd = (float)Math.Sqrt(spd);

            var maxspeed = PlayerMove.ClientMaxSpeed; //atof( PlayerMove.PM_Info_ValueForKey( PlayerMove.physinfo, "maxspd" ) );
            if (maxspeed != 0.0)
            {
                PlayerMove.MaxSpeed = Math.Min(maxspeed, PlayerMove.MaxSpeed);
            }

            if ((spd != 0.0) && (spd > PlayerMove.MaxSpeed))
            {
                float fRatio = PlayerMove.MaxSpeed / spd;
                PlayerMove.Cmd.ForwardMove *= fRatio;
                PlayerMove.Cmd.SideMove *= fRatio;
                PlayerMove.Cmd.UpMove *= fRatio;
            }

            if ((PlayerMove.Flags & EntFlags.Frozen) != 0
                 || (PlayerMove.Flags & EntFlags.OnTrain) != 0
                 || PlayerMove.Dead)
            {
                PlayerMove.Cmd.ForwardMove = 0;
                PlayerMove.Cmd.SideMove = 0;
                PlayerMove.Cmd.UpMove = 0;
            }

            PlayerMove.PunchAngle = DropPunchAngle(PlayerMove.PunchAngle);

            Vector newAngles;

            // Take angles from command.
            if (!PlayerMove.Dead)
            {
                var v_angle = PlayerMove.Cmd.ViewAngles;
                v_angle += PlayerMove.PunchAngle;

                // Set up view angles.
                newAngles = new Vector(
                    v_angle[MathUtils.PITCH],
                    v_angle[MathUtils.YAW],
                    CalcRoll(v_angle, PlayerMove.Velocity, PlayerMove.MoveVars.RollAngle, PlayerMove.MoveVars.RollSpeed) * 4
                    );
            }
            else
            {
                newAngles = PlayerMove.OldAngles;
            }

            // Set dead player view_offset
            if (PlayerMove.Dead)
            {
                var viewOffset = PlayerMove.ViewOffset;
                viewOffset[2] = WorldConstants.DeadViewHeight;
                PlayerMove.ViewOffset = viewOffset;
            }

            // Adjust client view angles to match values used on server.
            if (newAngles[MathUtils.YAW] > 180.0f)
            {
                newAngles[MathUtils.YAW] -= 360.0f;
            }

            PlayerMove.Angles = newAngles;
        }

        private Vector DropPunchAngle(Vector punchangle)
        {
            var len = punchangle.Length();
            punchangle = punchangle.Normalize();

            len -= (float)((10.0 + (len * 0.5)) * PlayerMove.FrameTime);
            len = Math.Max(len, 0.0f);

            return punchangle * len;
        }

        private static float CalcRoll(in Vector angles, in Vector velocity, float rollangle, float rollspeed)
        {
            MathUtils.AngleVectors(angles, out var forward, out var right, out var up);

            var side = velocity.DotProduct(right);

            var sign = side < 0 ? -1 : 1;

            side = Math.Abs(side);

            var value = rollangle;

            if (side < rollspeed)
            {
                side = side * value / rollspeed;
            }
            else
            {
                side = value;
            }

            return side * sign;
        }

        private void ReduceTimers()
        {
            if (PlayerMove.TimeStepSound > 0)
            {
                PlayerMove.TimeStepSound = Math.Max(0, PlayerMove.TimeStepSound - PlayerMove.Cmd.MSec);
            }
            if (PlayerMove.DuckTime > 0)
            {
                PlayerMove.DuckTime = Math.Max(0, PlayerMove.DuckTime - PlayerMove.Cmd.MSec);
            }
            if (PlayerMove.SwimTime > 0)
            {
                PlayerMove.SwimTime = Math.Max(0, PlayerMove.SwimTime - PlayerMove.Cmd.MSec);
            }
        }

        private void SpectatorMove()
        {
            //float   accel;
            // this routine keeps track of the spectators psoition
            // there a two different main move types : track player or moce freely (OBS_ROAMING)
            // doesn't need excate track position, only to generate PVS, so just copy
            // targets position and real view position is calculated on client (saves server CPU)

            if (PlayerMove.UserInt1 == (int)ObserverMode.Roaming)
            {
#if CLIENT_DLL
                //TODO:
                // jump only in roaming mode
                if (JumpSpectator)
                {
                    PlayerMove.Origin = vJumpOrigin;
                    PlayerMove.Angles = vJumpAngles;
                    PlayerMove.Velocity = WorldConstants.g_vecZero;
                    JumpSpectator = false;
                    return;
                }
#endif
                // Move around in normal spectator method

                var speed = PlayerMove.Velocity.Length();
                if (speed < 1)
                {
                    PlayerMove.Velocity = WorldConstants.g_vecZero;
                }
                else
                {
                    var drop = 0.0f;

                    var friction = PlayerMove.MoveVars.Friction * 1.5f; // extra friction
                    var control = speed < PlayerMove.MoveVars.StopSpeed ? PlayerMove.MoveVars.StopSpeed : speed;
                    drop += control * friction * PlayerMove.FrameTime;

                    // scale the velocity
                    var newspeed = speed - drop;
                    if (newspeed < 0)
                    {
                        newspeed = 0;
                    }

                    newspeed /= speed;

                    PlayerMove.Velocity *= newspeed;
                }

                // accelerate
                var fmove = PlayerMove.Cmd.ForwardMove;
                var smove = PlayerMove.Cmd.SideMove;

                PlayerMove.Forward = PlayerMove.Forward.Normalize();
                PlayerMove.Right = PlayerMove.Right.Normalize();

                var wishvel = (PlayerMove.Forward * fmove) + (PlayerMove.Right * smove);

                wishvel[2] += PlayerMove.Cmd.UpMove;

                var wishdir = wishvel;

                var wishspeed = wishdir.Length();
                wishdir = wishdir.Normalize();

                //
                // clamp to server defined max speed
                //
                if (wishspeed > PlayerMove.MoveVars.SpectatorMaxSpeed)
                {
                    wishvel *= (PlayerMove.MoveVars.SpectatorMaxSpeed / wishspeed);
                    wishspeed = PlayerMove.MoveVars.SpectatorMaxSpeed;
                }

                var currentspeed = PlayerMove.Velocity.DotProduct(wishdir);
                var addspeed = wishspeed - currentspeed;
                if (addspeed <= 0)
                {
                    return;
                }

                var accelspeed = PlayerMove.MoveVars.Accelerate * PlayerMove.FrameTime * wishspeed;
                if (accelspeed > addspeed)
                {
                    accelspeed = addspeed;
                }

                PlayerMove.Velocity += accelspeed * wishdir;

                // move
                PlayerMove.Origin += PlayerMove.FrameTime * PlayerMove.Velocity;
            }
            else
            {
                // all other modes just track some kind of target, so spectator PVS = target PVS

                // no valid target ?
                if (PlayerMove.UserInt2 <= 0)
                {
                    return;
                }

                int target;

                // Find the client this player's targeting
                for (target = 0; target < PlayerMove.NumPhysEnt; ++target)
                {
                    if (PlayerMove.PhysEnts[target].Info == PlayerMove.UserInt2)
                    {
                        break;
                    }
                }

                if (target == PlayerMove.NumPhysEnt)
                {
                    return;
                }

                // use targets position as own origin for PVS
                PlayerMove.Angles = PlayerMove.PhysEnts[target].Angles;
                PlayerMove.Origin = PlayerMove.PhysEnts[target].Origin;

                // no velocity
                PlayerMove.Velocity = WorldConstants.g_vecZero;
            }
        }

        private void CategorizePosition()
        {
            // if the player hull point one unit down is solid, the player
            // is on ground

            // see if standing on something solid	

            // Doing this before we move may introduce a potential latency in water detection, but
            // doing it after can get us stuck on the bottom in water if the amount we move up
            // is less than the 1 pixel 'threshold' we're about to snap to.	Also, we'll call
            // this several times per frame, so we really need to avoid sticking to the bottom of
            // water on each call, and the converse case will correct itself if called twice.
            CheckWater();

            var point = PlayerMove.Origin;
            point.z -= 2;

            if (PlayerMove.Velocity[2] > 180)   // Shooting up really fast.  Definitely not on ground.
            {
                PlayerMove.OnGround = -1;
            }
            else
            {
                // Try and move down.
                var tr = EnginePhysics.PlayerTrace(PlayerMove.Origin, point, PMTraceFlags.None, -1);
                // If we hit a steep plane, we are not on ground
                if (tr.Plane.Normal[2] < 0.7)
                {
                    PlayerMove.OnGround = -1;   // too steep
                }
                else
                {
                    PlayerMove.OnGround = tr.Ent;  // Otherwise, point to index of ent under us.
                }

                // If we are on something...
                if (PlayerMove.OnGround != -1)
                {
                    // Then we are not in water jump sequence
                    PlayerMove.WaterJumpTime = 0;
                    // If we could make the move, drop us down that 1 pixel
                    if (PlayerMove.WaterLevel < WaterLevel.Waist && !tr.StartSolid && !tr.AllSolid)
                    {
                        PlayerMove.Origin = tr.EndPos;
                    }
                }

                // Standing on an entity other than the world
                if (tr.Ent > 0)   // So signal that we are touching something.
                {
                    AddToTouched(tr, PlayerMove.Velocity);
                }
            }
        }

        /// <summary>
        /// Sets PlayerMove.waterlevel and PlayerMove.watertype values
        /// </summary>
        /// <returns></returns>
        private bool CheckWater()
        {
            // Pick a spot just above the players feet.
            var point = PlayerMove.Origin + new Vector(
                (PlayerMove.GetPlayerMins((int)PlayerMove.UseHull)[0] + PlayerMove.GetPlayerMaxs((int)PlayerMove.UseHull)[0]) * 0.5f,
                (PlayerMove.GetPlayerMins((int)PlayerMove.UseHull)[1] + PlayerMove.GetPlayerMaxs((int)PlayerMove.UseHull)[1]) * 0.5f,
                PlayerMove.GetPlayerMins((int)PlayerMove.UseHull)[2] + 1
                );

            // Assume that we are not in water at all.
            PlayerMove.WaterLevel = WaterLevel.Dry;
            PlayerMove.WaterType = Contents.Empty;

            // Grab point contents.
            var cont = EnginePhysics.PointContents(point, out var truecont);
            // Are we under water? (not solid and not empty?)
            if (cont <= Contents.Water && cont > Contents.Translucent)
            {
                // Set water type
                PlayerMove.WaterType = cont;

                // We are at least at level one
                PlayerMove.WaterLevel = WaterLevel.Feet;

                var height = (PlayerMove.GetPlayerMins((int)PlayerMove.UseHull)[2] + PlayerMove.GetPlayerMaxs((int)PlayerMove.UseHull)[2]);
                var heightover2 = height * 0.5f;

                // Now check a point that is at the player hull midpoint.
                point[2] = PlayerMove.Origin[2] + heightover2;
                cont = EnginePhysics.PointContents(point, out var _);
                // If that point is also under water...
                if (cont <= Contents.Water && cont > Contents.Translucent)
                {
                    // Set a higher water level.
                    PlayerMove.WaterLevel = WaterLevel.Waist;

                    // Now check the eye position.  (view_ofs is relative to the origin)
                    point[2] = PlayerMove.Origin[2] + PlayerMove.ViewOffset[2];

                    cont = EnginePhysics.PointContents(point, out var _);
                    if (cont <= Contents.Water && cont > Contents.Translucent)
                    {
                        PlayerMove.WaterLevel = WaterLevel.Head;  // In over our eyes
                    }
                }

                // Adjust velocity based on water current, if any.
                if ((truecont <= Contents.Current0)
                     && (truecont >= Contents.CurrentDown))
                {
                    // The deeper we are, the stronger the current.
                    PlayerMove.BaseVelocity += (50.0f * (int)PlayerMove.WaterLevel) * CurrentTable[Contents.Current0 - truecont];
                }
            }

            return PlayerMove.WaterLevel > WaterLevel.Feet;
        }

        /// <summary>
        /// Add's the trace result to touch list, if contact is not already in list
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="impactvelocity"></param>
        /// <returns></returns>
        private bool AddToTouched(PMTrace tr, in Vector impactvelocity)
        {
            int i;

            for (i = 0; i < PlayerMove.NumTouch; ++i)
            {
                if (PlayerMove.TouchIndex[i].Ent == tr.Ent)
                {
                    break;
                }
            }

            if (i != PlayerMove.NumTouch)  // Already in list.
            {
                return false;
            }

            tr.DeltaVelocity = impactvelocity;

            if (PlayerMove.NumTouch >= PlayerMove.TouchIndex.Count)
            {
                EnginePhysics.Con_DPrintf("Too many entities were touched!\n");
            }

            //Must assign here to ensure engine instance is updated
            //Engine accesses this to run impacts, which handles entity think & touch
            PlayerMove.TouchIndex[PlayerMove.NumTouch++].Assign(tr);
            return true;
        }

        /// <summary>
        /// If PlayerMove.origin is in a solid position,
        /// try nudging slightly on all axis to
        /// allow for the cut precision of the net coordinates
        /// </summary>
        /// <returns></returns>
        private bool CheckStuck()
        {
            int i;

            // If position is okay, exit
            var hitent = EnginePhysics.TestPlayerPosition(PlayerMove.Origin, out var traceresult);
            if (hitent == -1)
            {
                ResetStuckOffsets(PlayerMove.PlayerIndex, PlayerMove.IsServer);
                return false;
            }

            var basePosition = PlayerMove.Origin;

            // 
            // Deal with precision error in network.
            // 
            if (!PlayerMove.IsServer)
            {
                // World or BSP model
                if ((hitent == 0)
                     || (PlayerMove.PhysEnts[hitent].Model != IntPtr.Zero))
                {
                    int nReps = 0;
                    ResetStuckOffsets(PlayerMove.PlayerIndex, PlayerMove.IsServer);
                    do
                    {
                        i = GetRandomStuckOffsets(PlayerMove.PlayerIndex, PlayerMove.IsServer, out var offset);

                        var test = basePosition + offset;
                        if (EnginePhysics.TestPlayerPosition(test, out traceresult) == -1)
                        {
                            ResetStuckOffsets(PlayerMove.PlayerIndex, PlayerMove.IsServer);

                            PlayerMove.Origin = test;
                            return false;
                        }
                        ++nReps;
                    } while (nReps < NumStuckVectors);
                }
            }

            // Only an issue on the client.
            //TODO: this index is the inverse what's used in the rest of this code. Is this broken?
            var idx = (PlayerMove.IsServer) ? 0 : 1;

            var fTime = (float)EnginePhysics.Sys_FloatTime();
            // Too soon?
            if (StuckCheckTime[PlayerMove.PlayerIndex, idx] >= (fTime - CheckStuckMinTime))
            {
                return true;
            }
            StuckCheckTime[PlayerMove.PlayerIndex, idx] = fTime;

            EnginePhysics.StuckTouch(hitent, traceresult);

            {
                i = GetRandomStuckOffsets(PlayerMove.PlayerIndex, PlayerMove.IsServer, out var offset);

                var test = basePosition + offset;
                if ((hitent = EnginePhysics.TestPlayerPosition(test, out var _)) == -1)
                {
                    //Con_DPrintf("Nudged\n");

                    ResetStuckOffsets(PlayerMove.PlayerIndex, PlayerMove.IsServer);

                    if (i >= (NumStuckVectors / 2))
                    {
                        PlayerMove.Origin = test;
                    }

                    return false;
                }
            }

            // If player is flailing while stuck in another player ( should never happen ), then see
            //  if we can't "unstick" them forceably.
            if ((PlayerMove.Cmd.Buttons & (InputKeys.Jump | InputKeys.Duck | InputKeys.Attack)) != 0 && (PlayerMove.PhysEnts[hitent].PlayerIndex != 0))
            {
                const float xystep = 8.0f;
                const float zstep = 18.0f;
                const float xyminmax = xystep;
                const float zminmax = 4 * zstep;

                for (var z = 0.0f; z <= zminmax; z += zstep)
                {
                    for (var x = -xyminmax; x <= xyminmax; x += xystep)
                    {
                        for (var y = -xyminmax; y <= xyminmax; y += xystep)
                        {
                            var test = basePosition;
                            test[0] += x;
                            test[1] += y;
                            test[2] += z;

                            if (EnginePhysics.TestPlayerPosition(test, out var _) == -1)
                            {
                                PlayerMove.Origin = test;
                                return false;
                            }
                        }
                    }
                }
            }

            //PlayerMove.Origin = base;

            return true;
        }

        /// <summary>
        /// When a player is stuck, it's costly to try and unstick them
        /// Grab a test offset for the player based on a passed in index
        /// </summary>
        /// <param name="nIndex"></param>
        /// <param name="server"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private int GetRandomStuckOffsets(int nIndex, bool server, out Vector offset)
        {
            // Last time we did a full
            var idx = StuckLast[nIndex, server ? 1 : 0]++;

            offset = StuckTable[idx % NumStuckVectors];

            return idx % NumStuckVectors;
        }

        private void ResetStuckOffsets(int nIndex, bool server)
        {
            StuckLast[nIndex, server ? 1 : 0] = 0;
        }

        private PhysEnt CheckForLadder()
        {
            for (var i = 0; i < PlayerMove.NumMoveEnt; ++i)
            {
                var pe = PlayerMove.MoveEnts[i];

                if (pe.Model != IntPtr.Zero && EnginePhysics.GetModelType(pe.Model) == ModelType.Brush && pe.Skin == (int)Contents.Ladder)
                {
                    var hull = EnginePhysics.HullForBsp(pe, out var test);
                    var num = hull.FirstClipNode;

                    // Offset the test point appropriately for this hull.
                    test = PlayerMove.Origin - test;

                    // Test the player's hull for intersection with this model
                    if (EnginePhysics.HullPointContents(hull, num, test) == Contents.Empty)
                    {
                        continue;
                    }

                    return pe;
                }
            }

            return null;
        }

        private void UpdateStepSound()
        {
            if (PlayerMove.TimeStepSound > 0)
            {
                return;
            }

            if ((PlayerMove.Flags & EntFlags.Frozen) != 0)
            {
                return;
            }

            CategorizeTextureType();

            var speed = PlayerMove.Velocity.Length();

            // determine if we are on a ladder
            var fLadder = (PlayerMove.MoveType == MoveType.Fly);// IsOnLadder();

            float velrun;
            float velwalk;
            float flduck;

            // UNDONE: need defined numbers for run, walk, crouch, crouch run velocities!!!!	
            if ((PlayerMove.Flags & EntFlags.Ducking) != 0 || fLadder)
            {
                velwalk = 60;       // These constants should be based on cl_movespeedkey * cl_forwardspeed somehow
                velrun = 80;        // UNDONE: Move walking to server
                flduck = 100;
            }
            else
            {
                velwalk = 120;
                velrun = 210;
                flduck = 0;
            }

            // If we're on a ladder or on the ground, and we're moving fast enough,
            //  play step sound.  Also, if PlayerMove.TimeStepSound is zero, get the new
            //  sound right away - we just started moving in new level.
            if ((fLadder || (PlayerMove.OnGround != -1))
                && (PlayerMove.Velocity.Length() > 0.0f)
                && (speed >= velwalk || PlayerMove.TimeStepSound == 0))
            {
                var fWalking = speed < velrun;

                var center = PlayerMove.Origin;
                var knee = PlayerMove.Origin;
                var feet = PlayerMove.Origin;

                var height = PlayerMove.GetPlayerMaxs((int)PlayerMove.UseHull)[2] - PlayerMove.GetPlayerMins((int)PlayerMove.UseHull)[2];

                knee[2] = (float)(PlayerMove.Origin[2] - (0.3 * height));
                feet[2] = (float)(PlayerMove.Origin[2] - (0.5 * height));

                MaterialType materialType = null;

                // find out what we're stepping in or on...
                if (fLadder)
                {
                    materialType = LadderMaterialType;
                }
                else if (EnginePhysics.PointContents(knee, out var _) == Contents.Water)
                {
                    materialType = WadeMaterialType;
                }
                else if (EnginePhysics.PointContents(feet, out var _) == Contents.Water)
                {
                    materialType = SloshMaterialType;
                }
                else
                {
                    // find texture under player, if different from current texture, 
                    // get material type
                    materialType = Materials.GetMaterialType(PlayerMove.TextureName);
                }

                var fvol = materialType != null ? (fWalking ? materialType.WalkVolume : materialType.RunVolume) : 0;
                PlayerMove.TimeStepSound = materialType != null ? (fWalking ? materialType.WalkTimeDelay : materialType.RunTimeDelay) : 0;

                PlayerMove.TimeStepSound += (int)flduck; // slower step time if ducking

                // play the sound
                // 35% volume if ducking
                if ((PlayerMove.Flags & EntFlags.Ducking) != 0)
                {
                    fvol *= 0.35f;
                }

                if (materialType != null)
                {
                    PlayStepSound(materialType, fvol);
                }
            }
        }

        /// <summary>
        /// Determine texture info for the texture we are standing on
        /// </summary>
        /// <param name=""></param>
        private void CategorizeTextureType()
        {
            var start = PlayerMove.Origin;
            var end = PlayerMove.Origin;

            // Straight down
            end[2] -= 64;

            // Fill in default values, just in case.
            PlayerMove.TextureName = string.Empty;
            PlayerMove.TextureType = (byte)MaterialsSystem.DefaultMaterialType;

            var pTextureName = EnginePhysics.TraceTexture(PlayerMove.OnGround, start, end);
            if (pTextureName == null)
            {
                return;
            }

            pTextureName = SoundUtils.GetTextureBaseName(pTextureName);

            PlayerMove.TextureName = pTextureName;

            // get texture type
            PlayerMove.TextureType = FindTextureType(PlayerMove.TextureName);
        }

        private void PlayStepSound(MaterialType materialType, float fvol)
        {
            PlayerMove.StepLeft = !PlayerMove.StepLeft;

            //No sounds to play
            if (materialType.MovementSounds.Count == 0)
            {
                return;
            }

            //If there are no sounds to choose from, just exit now
            if (PlayerMove.StepLeft ? !materialType.HasLeftMovementSounds : !materialType.HasRightMovementSounds)
            {
                return;
            }

            if (!PlayerMove.RunFuncs)
            {
                return;
            }

            var irand = EnginePhysics.RandomLong(0, 1) + ((PlayerMove.StepLeft ? 1 : 0) * 2);

            // FIXME mp_footsteps needs to be a movevar
            if (PlayerMove.IsMultiplayer && !PlayerMove.MoveVars.Footsteps)
            {
                return;
            }

            var hvel = PlayerMove.Velocity;
            hvel[2] = 0.0f;

            if (PlayerMove.IsMultiplayer && (!OnLadder && hvel.Length() <= 220))
                return;

            var skipStepState = SkipStep;

            //Avoid wrapping to negative values
            SkipStep = Math.Max(0, SkipStep + 1);

            //TODO: maybe keep this as per-material data to match original?
            if (materialType.SkipStepInterval != 0 && (skipStepState % materialType.SkipStepInterval) == 0)
            {
                return;
            }

            // irand - 0,1 for right foot, 2,3 for left foot
            // used to alternate left and right foot
            // FIXME, move to player state
            MaterialType.MovementSound sound = null;

            //Find a sound to play for the current step
            for (var i = 0; i < MaxStepSearchIterations; ++i)
            {
                var index = EnginePhysics.RandomLong(0, materialType.MovementSounds.Count - 1);
                var potentialSound = materialType.MovementSounds[index];

                if (potentialSound.IsLeft == PlayerMove.StepLeft)
                {
                    sound = potentialSound;
                    break;
                }
            }

            if (sound == null)
            {
                //Pick the first found one
                sound = materialType.MovementSounds.First(s => s.IsLeft == PlayerMove.StepLeft);
            }

            //TODO: allow for more complex logic to decide sound to play
            EnginePhysics.PlaySound(SoundChannel.Body, sound.Sound, fvol, Attenuation.Normal, SoundFlags.None, Pitch.Normal);
        }

        private void Duck()
        {
            var buttonsChanged = (PlayerMove.OldButtons ^ PlayerMove.Cmd.Buttons);  // These buttons have changed this frame
            var nButtonPressed = buttonsChanged & PlayerMove.Cmd.Buttons;       // The changed ones still down are "pressed"

            var duckchange = (buttonsChanged & InputKeys.Duck) != 0;
            var duckpressed = (nButtonPressed & InputKeys.Duck) != 0;

            if ((PlayerMove.Cmd.Buttons & InputKeys.Duck) != 0)
            {
                PlayerMove.OldButtons |= InputKeys.Duck;
            }
            else
            {
                PlayerMove.OldButtons &= ~InputKeys.Duck;
            }

            // Prevent ducking if the iuser3 variable is set
            if (PlayerMove.UserInt3 != 0 || PlayerMove.Dead)
            {
                // Try to unduck
                if ((PlayerMove.Flags & EntFlags.Ducking) != 0)
                {
                    UnDuck();
                }
                return;
            }

            if ((PlayerMove.Flags & EntFlags.Ducking) != 0)
            {
                PlayerMove.Cmd.ForwardMove *= PlayerDuckingMultiplier;
                PlayerMove.Cmd.SideMove *= PlayerDuckingMultiplier;
                PlayerMove.Cmd.UpMove *= PlayerDuckingMultiplier;
            }

            if ((PlayerMove.Cmd.Buttons & InputKeys.Duck) != 0 || (PlayerMove.InDuck) || (PlayerMove.Flags & EntFlags.Ducking) != 0)
            {
                if ((PlayerMove.Cmd.Buttons & InputKeys.Duck) != 0)
                {
                    if ((nButtonPressed & InputKeys.Duck) != 0 && (PlayerMove.Flags & EntFlags.Ducking) == 0)
                    {
                        // Use 1 second so super long jump will work
                        PlayerMove.DuckTime = 1000;
                        PlayerMove.InDuck = true;
                    }

                    var time = (float)Math.Max(0.0, 1.0 - (PlayerMove.DuckTime / 1000.0));

                    if (PlayerMove.InDuck)
                    {
                        // Finish ducking immediately if duck time is over or not on ground
                        if (PlayerMove.DuckTime / 1000.0 <= (1.0 - TimeToDuck)
                             || (PlayerMove.OnGround == -1))
                        {
                            PlayerMove.UseHull = PMHull.Crouched;

                            var viewOffset = PlayerMove.ViewOffset;
                            viewOffset[2] = WorldConstants.DuckViewHeight;
                            PlayerMove.ViewOffset = viewOffset;

                            PlayerMove.Flags |= EntFlags.Ducking;
                            PlayerMove.InDuck = false;

                            // HACKHACK - Fudge for collision bug - no time to fix this properly
                            if (PlayerMove.OnGround != -1)
                            {
                                PlayerMove.Origin -= PlayerMove.GetPlayerMins((int)PMHull.Crouched) - PlayerMove.GetPlayerMins((int)PMHull.Standing);

                                // See if we are stuck?
                                FixPlayerCrouchStuck(StuckMoveUp);

                                // Recatagorize position since ducking can change origin
                                CategorizePosition();
                            }
                        }
                        else
                        {
                            var fMore = (WorldConstants.DUCK_HULL_MIN[2] - WorldConstants.HULL_MIN[2]);

                            // Calc parametric time
                            var duckFraction = SplineFraction(time, 1.0f / TimeToDuck);

                            var viewOffset = PlayerMove.ViewOffset;
                            viewOffset[2] = ((WorldConstants.DuckViewHeight - fMore) * duckFraction) + (WorldConstants.ViewHeight * (1 - duckFraction));
                            PlayerMove.ViewOffset = viewOffset;
                        }
                    }
                }
                else
                {
                    // Try to unduck
                    UnDuck();
                }
            }
        }

        private void UnDuck()
        {
            var newOrigin = PlayerMove.Origin;

            if (PlayerMove.OnGround != -1)
            {
                newOrigin += PlayerMove.GetPlayerMins((int)PMHull.Crouched) - PlayerMove.GetPlayerMins((int)PMHull.Standing);
            }

            var trace = EnginePhysics.PlayerTrace(newOrigin, newOrigin, PMTraceFlags.None, -1);

            if (!trace.StartSolid)
            {
                PlayerMove.UseHull = PMHull.Standing;

                // Oh, no, changing hulls stuck us into something, try unsticking downward first.
                trace = EnginePhysics.PlayerTrace(newOrigin, newOrigin, PMTraceFlags.None, -1);
                if (trace.StartSolid)
                {
                    // See if we are stuck?  If so, stay ducked with the duck hull until we have a clear spot
                    //Con_Printf( "unstick got stuck\n" );
                    PlayerMove.UseHull = PMHull.Crouched;
                    return;
                }

                PlayerMove.Flags &= ~EntFlags.Ducking;
                PlayerMove.InDuck = false;

                var viewOffset = PlayerMove.ViewOffset;
                viewOffset[2] = WorldConstants.ViewHeight;
                PlayerMove.ViewOffset = viewOffset;

                PlayerMove.DuckTime = 0;

                PlayerMove.Origin = newOrigin;

                // Recatagorize position since ducking can change origin
                CategorizePosition();
            }
        }

        /// <summary>
        /// Use for ease-in, ease-out style interpolation (accel/decel)
        /// Used by ducking code
        /// </summary>
        /// <param name="value"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        private float SplineFraction(float value, float scale)
        {
            value = scale * value;
            var valueSquared = value * value;

            // Nice little ease-in, ease-out spline-like curve
            return (3 * valueSquared) - (2 * valueSquared * value);
        }

        private void FixPlayerCrouchStuck(int direction)
        {
            var hitent = EnginePhysics.TestPlayerPosition(PlayerMove.Origin, out var _);
            if (hitent == -1)
                return;

            var test = PlayerMove.Origin;

            for (var i = 0; i < 36; ++i)
            {
                test[2] += direction;
                hitent = EnginePhysics.TestPlayerPosition(test, out var _);
                if (hitent == -1)
                {
                    PlayerMove.Origin = test;
                    return;
                }
            }

            // Failed
        }

        private void LadderMove(PhysEnt ladder)
        {
            if (PlayerMove.MoveType == MoveType.Noclip)
            {
                return;
            }

#if _TFC
            // this is how TFC freezes players, so we don't want them climbing ladders
            if ( PlayerMove.MaxSpeed <= 1.0 )
                return;
#endif

            EnginePhysics.GetModelBounds(ladder.Model, out var modelmins, out var modelmaxs);

            var ladderCenter = (modelmins + modelmaxs) * 0.5f;

            PlayerMove.MoveType = MoveType.Fly;

            // On ladder, convert movement to be relative to the ladder

            var floor = PlayerMove.Origin;
            floor[2] += PlayerMove.GetPlayerMins((int)PlayerMove.UseHull)[2] - 1;

            var onFloor = EnginePhysics.PointContents(floor, out var _) == Contents.Solid;

            PlayerMove.Gravity = 0;
            EnginePhysics.TraceModel(ladder, PlayerMove.Origin, ladderCenter, out var trace);
            if (trace.Fraction != 1.0)
            {
                float forward = 0, right = 0;

                // they shouldn't be able to move faster than their maxspeed
                var flSpeed = Math.Min(WorldConstants.MaxClimbSpeed, PlayerMove.MaxSpeed);

                MathUtils.AngleVectors(PlayerMove.Angles, out var vpn, out var v_right, out var _);

                if ((PlayerMove.Flags & EntFlags.Ducking) != 0)
                {
                    flSpeed *= PlayerDuckingMultiplier;
                }

                if ((PlayerMove.Cmd.Buttons & InputKeys.Back) != 0)
                {
                    forward -= flSpeed;
                }
                if ((PlayerMove.Cmd.Buttons & InputKeys.Forward) != 0)
                {
                    forward += flSpeed;
                }
                if ((PlayerMove.Cmd.Buttons & InputKeys.MoveLeft) != 0)
                {
                    right -= flSpeed;
                }
                if ((PlayerMove.Cmd.Buttons & InputKeys.MoveRight) != 0)
                {
                    right += flSpeed;
                }
                if ((PlayerMove.Cmd.Buttons & InputKeys.Jump) != 0)
                {
                    PlayerMove.MoveType = MoveType.Walk;
                    PlayerMove.Velocity = trace.Plane.Normal * 270;
                }
                else
                {
                    if (forward != 0 || right != 0)
                    {
                        //ALERT(at_console, "pev %.2f %.2f %.2f - ",
                        //	pev->velocity.x, pev->velocity.y, pev->velocity.z);
                        // Calculate player's intended velocity
                        //Vector velocity = (forward * gpGlobals->v_forward) + (right * gpGlobals->v_right);
                        var velocity = vpn * forward;
                        velocity += right * v_right;

                        // Perpendicular in the ladder plane
                        //					Vector perp = CrossProduct( Vector(0,0,1), trace.vecPlaneNormal );
                        //					perp = perp.Normalize();
                        var tmp = new Vector(0, 0, 1);
                        var perp = tmp.CrossProduct(trace.Plane.Normal);
                        perp = perp.Normalize();

                        // decompose velocity into ladder plane
                        var normal = velocity.DotProduct(trace.Plane.Normal);
                        // This is the velocity into the face of the ladder
                        var cross = trace.Plane.Normal * normal;

                        // This is the player's additional velocity
                        var lateral = velocity - cross;

                        // This turns the velocity into the face of the ladder into velocity that
                        // is roughly vertically perpendicular to the face of the ladder.
                        // NOTE: It IS possible to face up and move down or face down and move up
                        // because the velocity is a sum of the directional velocity and the converted
                        // velocity through the face of the ladder -- by design.
                        tmp = trace.Plane.Normal.CrossProduct(perp);
                        PlayerMove.Velocity = lateral + ((-normal) * tmp);
                        if (onFloor && normal > 0)  // On ground moving away from the ladder
                        {
                            PlayerMove.Velocity += WorldConstants.MaxClimbSpeed * trace.Plane.Normal;
                        }
                        //pev->velocity = lateral - (CrossProduct( trace.vecPlaneNormal, perp ) * normal);
                    }
                    else
                    {
                        PlayerMove.Velocity.Clear();
                    }
                }
            }
        }

        private void NoClip()
        {
            //	float		currentspeed, addspeed, accelspeed;

            // Copy movement amounts
            var fmove = PlayerMove.Cmd.ForwardMove;
            var smove = PlayerMove.Cmd.SideMove;

            PlayerMove.Forward = PlayerMove.Forward.Normalize();
            PlayerMove.Right = PlayerMove.Right.Normalize();

            // Determine x and y parts of velocity
            var wishvel = (PlayerMove.Forward * fmove) + (PlayerMove.Right * smove);

            wishvel[2] += PlayerMove.Cmd.UpMove;

            PlayerMove.Origin += PlayerMove.FrameTime * wishvel;

            // Zero out the velocity so that we don't accumulate a huge downward velocity from
            //  gravity, etc.
            PlayerMove.Velocity = WorldConstants.g_vecZero;
        }

        /// <summary>
        /// Dead player flying through air., e.g.
        /// </summary>
        private void Physics_Toss()
        {
            CheckWater();

            if (PlayerMove.Velocity[2] > 0)
                PlayerMove.OnGround = -1;

            // If on ground and not moving, return.
            if (PlayerMove.OnGround != -1)
            {
                if (PlayerMove.BaseVelocity == WorldConstants.g_vecZero
                    && PlayerMove.Velocity == WorldConstants.g_vecZero)
                {
                    return;
                }
            }

            CheckVelocity();

            // add gravity
            if (PlayerMove.MoveType != MoveType.Fly
                 && PlayerMove.MoveType != MoveType.BounceMissile
                 && PlayerMove.MoveType != MoveType.FlyMissile)
            {
                AddGravity();
            }

            // move origin
            // Base velocity is not properly accounted for since this entity will move again after the bounce without
            // taking it into account
            PlayerMove.Velocity += PlayerMove.BaseVelocity;

            CheckVelocity();
            var move = PlayerMove.Velocity * PlayerMove.FrameTime;
            PlayerMove.Velocity -= PlayerMove.BaseVelocity;

            var trace = PushEntity(move);    // Should this clear basevelocity

            CheckVelocity();

            if (trace.AllSolid)
            {
                // entity is trapped in another solid
                PlayerMove.OnGround = trace.Ent;
                PlayerMove.Velocity = WorldConstants.g_vecZero;
                return;
            }

            if (trace.Fraction == 1)
            {
                CheckWater();
                return;
            }

            float backoff;

            if (PlayerMove.MoveType == MoveType.Bounce)
            {
                backoff = 2.0f - PlayerMove.Friction;
            }
            else if (PlayerMove.MoveType == MoveType.BounceMissile)
            {
                backoff = 2.0f;
            }
            else
            {
                backoff = 1;
            }

            ClipVelocity(PlayerMove.Velocity, trace.Plane.Normal, out var clippedVelocity, backoff);
            PlayerMove.Velocity = clippedVelocity;

            // stop if on ground
            if (trace.Plane.Normal[2] > 0.7)
            {
                var baseOffset = WorldConstants.g_vecZero;

                if (PlayerMove.Velocity[2] < PlayerMove.MoveVars.Gravity * PlayerMove.FrameTime)
                {
                    // we're rolling on the ground, add static friction.
                    PlayerMove.OnGround = trace.Ent;
                    var velocity = PlayerMove.Velocity;
                    velocity[2] = 0;
                    PlayerMove.Velocity = velocity;
                }

                var vel = PlayerMove.Velocity.DotProduct(PlayerMove.Velocity);

                // Con_DPrintf("%f %f: %.0f %.0f %.0f\n", vel, trace.fraction, ent->velocity[0], ent->velocity[1], ent->velocity[2] );

                if (vel < (30 * 30) || (PlayerMove.MoveType != MoveType.Bounce && PlayerMove.MoveType != MoveType.BounceMissile))
                {
                    PlayerMove.OnGround = trace.Ent;
                    PlayerMove.Velocity = WorldConstants.g_vecZero;
                }
                else
                {
                    move = PlayerMove.Velocity * ((1.9 - trace.Fraction) * PlayerMove.FrameTime * 0.9);
                    trace = PushEntity(move);
                }

                PlayerMove.Velocity -= baseOffset;
            }

            // check for in water
            CheckWater();
        }

        /// <summary>
        /// See if the player has a bogus velocity value
        /// </summary>
        private void CheckVelocity()
        {
            //
            // bound velocity
            //
            var velocity = PlayerMove.Velocity;
            var origin = PlayerMove.Origin;

            for (var i = 0; i < 3; ++i)
            {
                // See if it's bogus.
                if (float.IsNaN(velocity[i]))
                {
                    EnginePhysics.Con_Printf($"PM  Got a NaN velocity {i}\n");
                    velocity[i] = 0;
                }
                if (float.IsNaN(origin[i]))
                {
                    EnginePhysics.Con_Printf($"PM  Got a NaN origin on {i}\n");
                    origin[i] = 0;
                }

                // Bound it.
                if (velocity[i] > PlayerMove.MoveVars.MaxVelocity)
                {
                    EnginePhysics.Con_DPrintf($"PM  Got a velocity too high on {i}\n");
                    velocity[i] = PlayerMove.MoveVars.MaxVelocity;
                }
                else if (velocity[i] < -PlayerMove.MoveVars.MaxVelocity)
                {
                    EnginePhysics.Con_DPrintf($"PM  Got a velocity too low on {i}\n");
                    velocity[i] = -PlayerMove.MoveVars.MaxVelocity;
                }
            }

            PlayerMove.Velocity = velocity;
            PlayerMove.Origin = origin;
        }

        private void AddGravity()
        {
            var ent_gravity = (PlayerMove.Gravity != 0) ? PlayerMove.Gravity : 1.0f;

            // Add gravity incorrectly
            var velocity = PlayerMove.Velocity;
            velocity[2] -= (ent_gravity * PlayerMove.MoveVars.Gravity * PlayerMove.FrameTime);
            velocity[2] += PlayerMove.BaseVelocity[2] * PlayerMove.FrameTime;
            PlayerMove.Velocity = velocity;

            var baseVelocity = PlayerMove.BaseVelocity;
            baseVelocity[2] = 0;
            PlayerMove.BaseVelocity = baseVelocity;

            CheckVelocity();
        }

        /// <summary>
        /// Does not change the entities velocity at all
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        private PMTrace PushEntity(in Vector push)
        {
            var end = PlayerMove.Origin + push;

            var trace = EnginePhysics.PlayerTrace(PlayerMove.Origin, end, PMTraceFlags.None, -1);

            PlayerMove.Origin = trace.EndPos;

            // So we can run impact function afterwards.
            if (trace.Fraction < 1.0
                && !trace.AllSolid)
            {
                AddToTouched(trace, PlayerMove.Velocity);
            }

            return trace;
        }

        /// <summary>
        /// Slide off of the impacting object
        /// returns the blocked flags:
        /// 0x01 == floor
        /// 0x02 == step / wall
        /// </summary>
        /// <param name="input"></param>
        /// <param name="normal"></param>
        /// <param name="output"></param>
        /// <param name="overbounce"></param>
        /// <returns></returns>
        private int ClipVelocity(in Vector input, in Vector normal, out Vector output, float overbounce)
        {
            var angle = normal[2];

            var blocked = 0x00;            // Assume unblocked.
            if (angle > 0)      // If the plane that is blocking us has a positive z component, then assume it's a floor.
                blocked |= 0x01;        // 
            if (angle == 0)         // If the plane has no Z, it is vertical (wall/step)
                blocked |= 0x02;        // 

            // Determine how far along plane to slide based on incoming direction.
            // Scale by overbounce factor.
            var backoff = input.DotProduct(normal) * overbounce;

            output = new Vector();

            for (var i = 0; i < 3; ++i)
            {
                var change = normal[i] * backoff;
                output[i] = input[i] - change;
                // If out velocity is too small, zero it out.
                if (output[i] > -StopEpsilon && output[i] < StopEpsilon)
                {
                    output[i] = 0;
                }
            }

            // Return blocking flags.
            return blocked;
        }

        private void Jump()
        {
            if (PlayerMove.Dead)
            {
                PlayerMove.OldButtons |= InputKeys.Jump;   // don't jump again until released
                return;
            }

            int.TryParse(EnginePhysics.Info_ValueForKey(PlayerMove.PhysInfo, "tfc"), out var tfcValue);

            var tfc = tfcValue == 1;

            // Spy that's feigning death cannot jump
            if (tfc
                && ((int)PlayerMove.DeadFlag == ((int)DeadFlag.DiscardBody + 1)))
            {
                return;
            }

            // See if we are waterjumping.  If so, decrement count and return.
            if (PlayerMove.WaterJumpTime != 0)
            {
                PlayerMove.WaterJumpTime = Math.Max(0, PlayerMove.WaterJumpTime - PlayerMove.Cmd.MSec);
                return;
            }

            // If we are in the water most of the way...
            if (PlayerMove.WaterLevel >= WaterLevel.Waist)
            {   // swimming, not jumping
                PlayerMove.OnGround = -1;

                var velocity = PlayerMove.Velocity;

                if (PlayerMove.WaterType == Contents.Water)    // We move up a certain amount
                {
                    velocity[2] = 100;
                }
                else if (PlayerMove.WaterType == Contents.Slime)
                {
                    velocity[2] = 80;
                }
                else  // LAVA
                {
                    velocity[2] = 50;
                }

                PlayerMove.Velocity = velocity;

                // play swiming sound
                if (PlayerMove.SwimTime <= 0)
                {
                    // Don't play sound again for 1 second
                    PlayerMove.SwimTime = 1000;
                    //TODO: use material type?
                    switch (EnginePhysics.RandomLong(0, 3))
                    {
                        case 0:
                            EnginePhysics.PlaySound(SoundChannel.Body, "player/pl_wade1.wav", 1, Attenuation.Normal, 0, Pitch.Normal);
                            break;
                        case 1:
                            EnginePhysics.PlaySound(SoundChannel.Body, "player/pl_wade2.wav", 1, Attenuation.Normal, 0, Pitch.Normal);
                            break;
                        case 2:
                            EnginePhysics.PlaySound(SoundChannel.Body, "player/pl_wade3.wav", 1, Attenuation.Normal, 0, Pitch.Normal);
                            break;
                        case 3:
                            EnginePhysics.PlaySound(SoundChannel.Body, "player/pl_wade4.wav", 1, Attenuation.Normal, 0, Pitch.Normal);
                            break;
                    }
                }

                return;
            }

            // No more effect
            if (PlayerMove.OnGround == -1)
            {
                // Flag that we jumped.
                // HACK HACK HACK
                // Remove this when the game .dll no longer does physics code!!!!
                PlayerMove.OldButtons |= InputKeys.Jump;   // don't jump again until released
                return;     // in air, so no effect
            }

            if ((PlayerMove.OldButtons & InputKeys.Jump) != 0)
            {
                return;     // don't pogo stick
            }

            // In the air now.
            PlayerMove.OnGround = -1;

            PreventMegaBunnyJumping();

            if (tfc)
            {
                EnginePhysics.PlaySound(SoundChannel.Body, "player/plyrjmp8.wav", 0.5f, Attenuation.Normal, 0, Pitch.Normal);
            }
            else
            {
                var materialType = Materials.GetMaterialType(PlayerMove.TextureName);
                if (materialType != null)
                {
                    PlayStepSound(materialType, 1.0f);
                }
            }

            // See if user can super long jump?
            int.TryParse(EnginePhysics.Info_ValueForKey(PlayerMove.PhysInfo, "slj"), out var superjumpValue);

            var cansuperjump = superjumpValue == 1;

            {
                var velocity = PlayerMove.Velocity;

                // Acclerate upward
                // If we are ducking...
                if (PlayerMove.InDuck || (PlayerMove.Flags & EntFlags.Ducking) != 0)
                {
                    // Adjust for super long jump module
                    // UNDONE -- note this should be based on forward angles, not current velocity.
                    if (cansuperjump
                        && (PlayerMove.Cmd.Buttons & InputKeys.Duck) != 0
                        && (PlayerMove.DuckTime > 0)
                        && PlayerMove.Velocity.Length() > 50)
                    {
                        var punchAngle = PlayerMove.PunchAngle;
                        punchAngle[0] = -5;
                        PlayerMove.PunchAngle = punchAngle;

                        for (var i = 0; i < 2; ++i)
                        {
                            velocity[i] = PlayerMove.Forward[i] * WorldConstants.PlayerLongJumpSpeed * 1.6f;
                        }

                        velocity[2] = (float)Math.Sqrt(2 * 800 * 56.0);
                    }
                    else
                    {
                        velocity[2] = (float)Math.Sqrt(2 * 800 * 45.0);
                    }
                }
                else
                {
                    velocity[2] = (float)Math.Sqrt(2 * 800 * 45.0);
                }

                PlayerMove.Velocity = velocity;
            }

            // Decay it for simulation
            FixupGravityVelocity();

            // Flag that we jumped.
            PlayerMove.OldButtons |= InputKeys.Jump;   // don't jump again until released
        }

        /// <summary>
        /// Corrects bunny jumping ( where player initiates a bunny jump before other
        ///  movement logic runs, thus making onground == -1 thus making Friction get skipped and
        ///  running AirMove, which doesn't crop velocity to maxspeed like the ground / other
        ///  movement logic does.
        /// </summary>
        private void PreventMegaBunnyJumping()
        {
            // Speed at which bunny jumping is limited
            var maxscaledspeed = WorldConstants.BunnyJumpMaxSpeedFactor * PlayerMove.MaxSpeed;

            // Don't divide by zero
            if (maxscaledspeed <= 0.0f)
            {
                return;
            }

            // Current player speed
            var spd = PlayerMove.Velocity.Length();

            if (spd <= maxscaledspeed)
            {
                return;
            }

            // If we have to crop, apply this cropping fraction to velocity
            var fraction = (maxscaledspeed / spd) * 0.65; //Returns the modifier for the velocity

            PlayerMove.Velocity *= fraction; //Crop it down!.
        }

        private void FixupGravityVelocity()
        {
            if (PlayerMove.WaterJumpTime != 0)
            {
                return;
            }

            var ent_gravity = (PlayerMove.Gravity != 0) ? PlayerMove.Gravity : 1.0f;

            // Get the correct velocity for the end of the dt
            var velocity = PlayerMove.Velocity;
            velocity[2] -= (ent_gravity * PlayerMove.MoveVars.Gravity * PlayerMove.FrameTime * 0.5f);
            PlayerMove.Velocity = velocity;

            CheckVelocity();
        }

        /// <summary>
        /// The basic solid body movement clip that slides along multiple planes
        /// </summary>
        /// <returns></returns>
        private int FlyMove()
        {
            var planes = new Vector[MaxClipPlanes];

            const int numbumps = 4;           // Bump up to four times

            var blocked = 0;           // Assume not blocked
            var numplanes = 0;           //  and not sliding along any planes
            var original_velocity = PlayerMove.Velocity;  // Store original velocity
            var primal_velocity = PlayerMove.Velocity;

            var allFraction = 0.0f;
            var time_left = PlayerMove.FrameTime;   // Total time for this movement operation.

            for (var bumpcount = 0; bumpcount < numbumps; ++bumpcount)
            {
                if (PlayerMove.Velocity[0] == 0 && PlayerMove.Velocity[1] == 0 && PlayerMove.Velocity[2] == 0)
                {
                    break;
                }

                // Assume we can move all the way from the current origin to the
                //  end point.
                var end = PlayerMove.Origin + (time_left * PlayerMove.Velocity);

                // See if we can make it from origin to end point.
                var trace = EnginePhysics.PlayerTrace(PlayerMove.Origin, end, PMTraceFlags.None, -1);

                allFraction += trace.Fraction;
                // If we started in a solid object, or we were in solid space
                //  the whole way, zero out our velocity and return that we
                //  are blocked by floor and wall.
                if (trace.AllSolid)
                {   // entity is trapped in another solid
                    PlayerMove.Velocity = WorldConstants.g_vecZero;
                    //Con_DPrintf("Trapped 4\n");
                    return 4;
                }

                // If we moved some portion of the total distance, then
                //  copy the end position into the PlayerMove.origin and 
                //  zero the plane counter.
                if (trace.Fraction > 0)
                {   // actually covered some distance
                    PlayerMove.Origin = trace.EndPos;
                    original_velocity = PlayerMove.Velocity;
                    numplanes = 0;
                }

                // If we covered the entire distance, we are done
                //  and can return.
                if (trace.Fraction == 1)
                    break;      // moved the entire distance

                //if (!trace.ent)
                //	Sys_Error ("PM_PlayerTrace: !trace.ent");

                // Save entity that blocked us (since fraction was < 1.0)
                //  for contact
                // Add it if it's not already in the list!!!
                AddToTouched(trace, PlayerMove.Velocity);

                // If the plane we hit has a high z component in the normal, then
                //  it's probably a floor
                if (trace.Plane.Normal[2] > 0.7)
                {
                    blocked |= 1;       // floor
                }
                // If the plane has a zero z component in the normal, then it's a 
                //  step or wall
                if (trace.Plane.Normal[2] == 0)
                {
                    blocked |= 2;       // step / wall
                                        //Con_DPrintf("Blocked by %i\n", trace.ent);
                }

                // Reduce amount of PlayerMove.frametime left by total time left * fraction
                //  that we covered.
                time_left -= time_left * trace.Fraction;

                // Did we run out of planes to clip against?
                if (numplanes >= MaxClipPlanes)
                {   // this shouldn't really happen
                    //  Stop our movement if so.
                    PlayerMove.Velocity = WorldConstants.g_vecZero;
                    //Con_DPrintf("Too many planes 4\n");

                    break;
                }

                // Set up next clipping plane
                planes[numplanes] = trace.Plane.Normal;
                ++numplanes;
                //

                // modify original_velocity so it parallels all of the clip planes
                //
                if (PlayerMove.MoveType == MoveType.Walk
                    && ((PlayerMove.OnGround == -1) || (PlayerMove.Friction != 1)))    // relfect player velocity
                {
                    var new_velocity = new Vector();

                    for (var i = 0; i < numplanes; ++i)
                    {
                        if (planes[i][2] > 0.7)
                        {// floor or slope
                            ClipVelocity(original_velocity, planes[i], out new_velocity, 1);
                            original_velocity = new_velocity;
                        }
                        else
                        {
                            ClipVelocity(original_velocity, planes[i], out new_velocity, 1.0f + (PlayerMove.MoveVars.Bounce * (1 - PlayerMove.Friction)));
                        }
                    }

                    PlayerMove.Velocity = new_velocity;
                    original_velocity = new_velocity;
                }
                else
                {
                    int i;

                    for (i = 0; i < numplanes; ++i)
                    {
                        ClipVelocity(
                            original_velocity,
                            planes[i],
                            out var clippedVelocity,
                            1);
                        PlayerMove.Velocity = clippedVelocity;

                        int j;

                        for (j = 0; j < numplanes; ++j)
                        {
                            if (j != i)
                            {
                                // Are we now moving against this plane?
                                if (PlayerMove.Velocity.DotProduct(planes[j]) < 0)
                                {
                                    break;  // not ok
                                }
                            }
                        }

                        if (j == numplanes)  // Didn't have to clip, so we're ok
                        {
                            break;
                        }
                    }

                    // Did we go all the way through plane set
                    if (i != numplanes)
                    {   // go along this plane
                        // PlayerMove.velocity is set in clipping call, no need to set again.
                    }
                    else
                    {   // go along the crease
                        if (numplanes != 2)
                        {
                            //Con_Printf ("clip velocity, numplanes == %i\n",numplanes);
                            PlayerMove.Velocity = WorldConstants.g_vecZero;
                            //Con_DPrintf("Trapped 4\n");

                            break;
                        }
                        var dir = planes[0].CrossProduct(planes[1]);
                        var d = dir.DotProduct(PlayerMove.Velocity);
                        PlayerMove.Velocity = dir * d;
                    }

                    //
                    // if original velocity is against the original velocity, stop dead
                    // to avoid tiny occilations in sloping corners
                    //
                    if (PlayerMove.Velocity.DotProduct(primal_velocity) <= 0)
                    {
                        //Con_DPrintf("Back\n");
                        PlayerMove.Velocity = WorldConstants.g_vecZero;
                        break;
                    }
                }
            }

            if (allFraction == 0)
            {
                PlayerMove.Velocity = WorldConstants.g_vecZero;
                //Con_DPrintf( "Don't stick\n" );
            }

            return blocked;
        }

        private bool InWater()
        {
            return PlayerMove.WaterLevel > WaterLevel.Feet;
        }

        private void AddCorrectGravity()
        {
            float ent_gravity;

            if (PlayerMove.WaterJumpTime != 0)
            {
                return;
            }

            if (PlayerMove.Gravity != 0)
                ent_gravity = PlayerMove.Gravity;
            else
                ent_gravity = 1.0f;

            // Add gravity so they'll be in the correct position during movement
            // yes, this 0.5 looks wrong, but it's not.  
            var velocity = PlayerMove.Velocity;
            velocity[2] -= (ent_gravity * PlayerMove.MoveVars.Gravity * 0.5f * PlayerMove.FrameTime);
            velocity[2] += PlayerMove.BaseVelocity[2] * PlayerMove.FrameTime;
            PlayerMove.Velocity = velocity;

            var baseVelocity = PlayerMove.BaseVelocity;
            baseVelocity[2] = 0;
            PlayerMove.BaseVelocity = baseVelocity;

            CheckVelocity();
        }

        private void WaterJump()
        {
            if (PlayerMove.WaterJumpTime > 10000)
            {
                PlayerMove.WaterJumpTime = 10000;
            }

            if (PlayerMove.WaterJumpTime == 0)
            {
                return;
            }

            PlayerMove.WaterJumpTime -= PlayerMove.Cmd.MSec;
            if (PlayerMove.WaterJumpTime < 0
                 || PlayerMove.WaterLevel == WaterLevel.Dry)
            {
                PlayerMove.WaterJumpTime = 0;
                PlayerMove.Flags &= ~EntFlags.WaterJump;
            }

            var velocity = PlayerMove.Velocity;
            velocity[0] = PlayerMove.MoveDirection[0];
            velocity[1] = PlayerMove.MoveDirection[1];
            PlayerMove.Velocity = velocity;
        }

        private void CheckWaterJump()
        {
            // Already water jumping.
            if (PlayerMove.WaterJumpTime != 0)
            {
                return;
            }

            // Don't hop out if we just jumped in
            if (PlayerMove.Velocity[2] < -180)
            {
                return; // only hop out if we are moving up
            }

            // See if we are backing up
            var flatvelocity = new Vector(
                PlayerMove.Velocity[0],
                PlayerMove.Velocity[1],
                0
                );

            // Must be moving
            var curspeed = flatvelocity.Length();
            flatvelocity = flatvelocity.Normalize();

            // see if near an edge
            var flatforward = new Vector(
                PlayerMove.Forward[0],
                PlayerMove.Forward[1],
                0
                );

            flatforward = flatforward.Normalize();

            // Are we backing into water from steps or something?  If so, don't pop forward
            if (curspeed != 0.0 && (flatvelocity.DotProduct(flatforward) < 0.0))
            {
                return;
            }

            var vecStart = PlayerMove.Origin;
            vecStart[2] += WaterJumpHeight;

            var vecEnd = vecStart + (24 * flatforward);

            // Trace, this trace should use the point sized collision hull
            var savehull = PlayerMove.UseHull;
            PlayerMove.UseHull = PMHull.Point;
            var tr = EnginePhysics.PlayerTrace(vecStart, vecEnd, PMTraceFlags.None, -1);
            if (tr.Fraction < 1.0 && Math.Abs(tr.Plane.Normal[2]) < 0.1f)  // Facing a near vertical wall?
            {
                vecStart[2] += PlayerMove.GetPlayerMaxs((int)savehull)[2] - WaterJumpHeight;
                vecEnd = vecStart + (24 * flatforward);
                PlayerMove.MoveDirection = -50 * tr.Plane.Normal;

                tr = EnginePhysics.PlayerTrace(vecStart, vecEnd, PMTraceFlags.None, -1);
                if (tr.Fraction == 1.0)
                {
                    PlayerMove.WaterJumpTime = 2000;

                    var velocity = PlayerMove.Velocity;
                    velocity[2] = 225;
                    PlayerMove.Velocity = velocity;

                    PlayerMove.OldButtons |= InputKeys.Jump;
                    PlayerMove.Flags |= EntFlags.WaterJump;
                }
            }

            // Reset the collision hull
            PlayerMove.UseHull = savehull;
        }

        private void WaterMove()
        {
            //
            // user intentions
            //
            var wishvel = (PlayerMove.Forward * PlayerMove.Cmd.ForwardMove) + (PlayerMove.Right * PlayerMove.Cmd.SideMove);

            // Sinking after no other movement occurs
            if (PlayerMove.Cmd.ForwardMove == 0 && PlayerMove.Cmd.SideMove == 0 && PlayerMove.Cmd.UpMove == 0)
            {
                wishvel[2] -= 60;       // drift towards bottom
            }
            else  // Go straight up by upmove amount.
            {
                wishvel[2] += PlayerMove.Cmd.UpMove;
            }

            // Copy it over and determine speed
            var wishdir = wishvel;
            var wishspeed = wishdir.Length();
            wishdir = wishdir.Normalize();

            // Cap speed.
            if (wishspeed > PlayerMove.MaxSpeed)
            {
                wishvel *= PlayerMove.MaxSpeed / wishspeed;
                wishspeed = PlayerMove.MaxSpeed;
            }
            // Slow us down a bit.
            wishspeed *= 0.8f;

            PlayerMove.Velocity += PlayerMove.BaseVelocity;
            // Water friction
            var temp = PlayerMove.Velocity;
            var speed = temp.Length();
            temp = temp.Normalize();

            float newspeed;

            if (speed != 0)
            {
                newspeed = Math.Max(0, speed - (PlayerMove.FrameTime * speed * PlayerMove.MoveVars.Friction * PlayerMove.Friction));

                PlayerMove.Velocity *= newspeed / speed;
            }
            else
            {
                newspeed = 0;
            }

            //
            // water acceleration
            //
            if (wishspeed < 0.1f)
            {
                return;
            }

            var addspeed = wishspeed - newspeed;
            if (addspeed > 0)
            {
                wishvel = wishvel.Normalize();
                var accelspeed = PlayerMove.MoveVars.Accelerate * wishspeed * PlayerMove.FrameTime * PlayerMove.Friction;
                if (accelspeed > addspeed)
                    accelspeed = addspeed;

                PlayerMove.Velocity += accelspeed * wishvel;
            }

            // Now move
            // assume it is a stair or a slope, so press down from stepheight above
            var dest = PlayerMove.Origin + (PlayerMove.FrameTime * PlayerMove.Velocity);
            var start = dest;
            start[2] += PlayerMove.MoveVars.StepSize + 1;

            var trace = EnginePhysics.PlayerTrace(start, dest, PMTraceFlags.None, -1);

            if (!trace.StartSolid && !trace.AllSolid)   // FIXME: check steep slope?
            {   // walked up the step, so just keep result and exit
                PlayerMove.Origin = trace.EndPos;
                return;
            }

            // Try moving straight along out normal path.
            FlyMove();
        }

        /// <summary>
        /// Handles both ground friction and water friction
        /// </summary>
        /// <param name=""></param>
        private void Friction()
        {
            // If we are in water jump cycle, don't apply friction
            if (PlayerMove.WaterJumpTime != 0)
            {
                return;
            }

            // Get velocity
            // Calculate speed
            var speed = PlayerMove.Velocity.Length();

            // If too slow, return
            if (speed < 0.1f)
            {
                return;
            }

            var drop = 0.0f;

            // apply ground friction
            if (PlayerMove.OnGround != -1)  // On an entity that is the ground
            {
                var start = new Vector(
                    PlayerMove.Origin[0] + (PlayerMove.Velocity[0] / speed * 16),
                    PlayerMove.Origin[1] + (PlayerMove.Velocity[1] / speed * 16),
                    PlayerMove.Origin[2] + PlayerMove.GetPlayerMins((int)PlayerMove.UseHull)[2]
                    );

                var stop = start;
                stop[2] -= 34;

                var trace = EnginePhysics.PlayerTrace(start, stop, PMTraceFlags.None, -1);

                var friction = (trace.Fraction == 1.0) ? PlayerMove.MoveVars.Friction * PlayerMove.MoveVars.EdgeFriction : PlayerMove.MoveVars.Friction;

                // Grab friction value.
                //friction = PlayerMove.movevars->friction;      

                friction *= PlayerMove.Friction;  // player friction?

                // Bleed off some speed, but if we have less than the bleed
                //  threshhold, bleed the theshold amount.
                var control = (speed < PlayerMove.MoveVars.StopSpeed) ? PlayerMove.MoveVars.StopSpeed : speed;
                // Add the amount to t'he drop amount.
                drop += control * friction * PlayerMove.FrameTime;
            }

            // apply water friction
            //	if (PlayerMove.waterlevel)
            //		drop += speed * PlayerMove.movevars->waterfriction * waterlevel * PlayerMove.frametime;

            // scale the velocity
            var newspeed = speed - drop;
            if (newspeed < 0)
            {
                newspeed = 0;
            }

            // Determine proportion of old speed we are using.
            newspeed /= speed;

            // Adjust velocity according to proportion.
            PlayerMove.Velocity *= newspeed;
        }

        /// <summary>
        /// Only used by players.  Moves along the ground when player is a MoveType.Walk
        /// </summary>
        private void WalkMove()
        {
            // Copy movement amounts
            var fmove = PlayerMove.Cmd.ForwardMove;
            var smove = PlayerMove.Cmd.SideMove;

            {
                // Zero out z components of movement vectors
                var forward = PlayerMove.Forward;
                var right = PlayerMove.Right;
                forward[2] = 0;
                right[2] = 0;

                //Normalize remainder of vectors.
                PlayerMove.Forward = forward.Normalize();
                PlayerMove.Right = right.Normalize();
            }

            // Determine x and y parts of velocity
            // Zero out z part of velocity
            var wishvel = new Vector(
                (PlayerMove.Forward[0] * fmove) + (PlayerMove.Right[0] * smove),
                (PlayerMove.Forward[1] * fmove) + (PlayerMove.Right[1] * smove),
                0
                );

            var wishdir = wishvel;
            var wishspeed = wishdir.Length(); // Determine maginitude of speed of move
            wishdir = wishdir.Normalize();

            //
            // Clamp to server defined max speed
            //
            if (wishspeed > PlayerMove.MaxSpeed)
            {
                wishvel *= PlayerMove.MaxSpeed / wishspeed;
                wishspeed = PlayerMove.MaxSpeed;
            }

            // Set pmove velocity
            {
                var velocity = PlayerMove.Velocity;
                velocity[2] = 0;
                PlayerMove.Velocity = velocity;

                Accelerate(wishdir, wishspeed, PlayerMove.MoveVars.Accelerate);

                velocity = PlayerMove.Velocity;
                velocity[2] = 0;
                PlayerMove.Velocity = velocity;
            }

            // Add in any base velocity to the current velocity.
            PlayerMove.Velocity += PlayerMove.BaseVelocity;

            var spd = PlayerMove.Velocity.Length();

            if (spd < 1.0f)
            {
                PlayerMove.Velocity = WorldConstants.g_vecZero;
                return;
            }

            // If we are not moving, do nothing
            //if (!PlayerMove.velocity[0] && !PlayerMove.velocity[1] && !PlayerMove.velocity[2])
            //	return;

            var oldonground = PlayerMove.OnGround;

            // first try just moving to the destination	
            var dest = new Vector(
                PlayerMove.Origin[0] + (PlayerMove.Velocity[0] * PlayerMove.FrameTime),
                PlayerMove.Origin[1] + (PlayerMove.Velocity[1] * PlayerMove.FrameTime),
                PlayerMove.Origin[2]
            );

            // first try moving directly to the next spot
            var start = dest;
            var trace = EnginePhysics.PlayerTrace(PlayerMove.Origin, dest, PMTraceFlags.None, -1);
            // If we made it all the way, then copy trace end
            //  as new player position.
            if (trace.Fraction == 1)
            {
                PlayerMove.Origin = trace.EndPos;
                return;
            }

            if (oldonground == -1 &&   // Don't walk up stairs if not on ground.
                PlayerMove.WaterLevel == WaterLevel.Dry)
            {
                return;
            }

            if (PlayerMove.WaterJumpTime != 0)         // If we are jumping out of water, don't do anything more.
            {
                return;
            }

            // Try sliding forward both on ground and up 16 pixels
            //  take the move that goes farthest
            var original = PlayerMove.Origin;       // Save out original pos &
            var originalvel = PlayerMove.Velocity;  //  velocity.

            // Slide move
            var clip = FlyMove();

            // Copy the results out
            var down = PlayerMove.Origin;
            var downvel = PlayerMove.Velocity;

            // Reset original values.
            PlayerMove.Origin = original;
            PlayerMove.Velocity = originalvel;

            // Start out up one stair height
            dest = PlayerMove.Origin;
            dest[2] += PlayerMove.MoveVars.StepSize;

            trace = EnginePhysics.PlayerTrace(PlayerMove.Origin, dest, PMTraceFlags.None, -1);
            // If we started okay and made it part of the way at least,
            //  copy the results to the movement start position and then
            //  run another move try.
            if (!trace.StartSolid && !trace.AllSolid)
            {
                PlayerMove.Origin = trace.EndPos;
            }

            // slide move the rest of the way.
            clip = FlyMove();

            // Now try going back down from the end point
            //  press down the stepheight
            dest = PlayerMove.Origin;
            dest[2] -= PlayerMove.MoveVars.StepSize;

            trace = EnginePhysics.PlayerTrace(PlayerMove.Origin, dest, PMTraceFlags.None, -1);

            // If we are not on the ground any more then
            //  use the original movement attempt
            var useDown = false;

            if (trace.Plane.Normal[2] < 0.7)
            {
                useDown = true;
            }
            else
            {
                // If the trace ended up in empty space, copy the end
                //  over to the origin.
                if (!trace.StartSolid && !trace.AllSolid)
                {
                    PlayerMove.Origin = trace.EndPos;
                }
                // Copy this origion to up.
                PlayerMove.Up = PlayerMove.Origin;

                // decide which one went farther
                var downdist = ((down[0] - original[0]) * (down[0] - original[0]))
                         + ((down[1] - original[1]) * (down[1] - original[1]));
                var updist = ((PlayerMove.Up[0] - original[0]) * (PlayerMove.Up[0] - original[0]))
                         + ((PlayerMove.Up[1] - original[1]) * (PlayerMove.Up[1] - original[1]));

                useDown = downdist > updist;
            }

            if (useDown)
            {
                PlayerMove.Origin = down;
                PlayerMove.Velocity = downvel;
            }
            else // copy z value from slide move
            {
                var velocity = PlayerMove.Velocity;
                velocity[2] = downvel[2];
                PlayerMove.Velocity = velocity;
            }
        }

        private void Accelerate(in Vector wishdir, float wishspeed, float accel)
        {
            // Dead player's don't accelerate
            if (PlayerMove.Dead)
            {
                return;
            }

            // If waterjumping, don't accelerate
            if (PlayerMove.WaterJumpTime != 0)
            {
                return;
            }

            // See if we are changing direction a bit
            var currentspeed = PlayerMove.Velocity.DotProduct(wishdir);

            // Reduce wishspeed by the amount of veer.
            var addspeed = wishspeed - currentspeed;

            // If not going to add any speed, done.
            if (addspeed <= 0)
            {
                return;
            }

            // Determine amount of accleration.
            // Cap at addspeed
            var accelspeed = Math.Min(addspeed, accel * PlayerMove.FrameTime * wishspeed * PlayerMove.Friction);

            // Adjust velocity.
            PlayerMove.Velocity += accelspeed * wishdir;
        }

        private void AirMove()
        {
            // Copy movement amounts
            var fmove = PlayerMove.Cmd.ForwardMove;
            var smove = PlayerMove.Cmd.SideMove;

            {
                // Zero out z components of movement vectors
                var forward = PlayerMove.Forward;
                var right = PlayerMove.Right;
                forward[2] = 0;
                right[2] = 0;

                // Renormalize
                PlayerMove.Forward = forward.Normalize();
                PlayerMove.Right = right.Normalize();
            }

            // Determine x and y parts of velocity
            // Zero out z part of velocity
            var wishvel = new Vector(
                (PlayerMove.Forward[0] * fmove) + (PlayerMove.Right[0] * smove),
                (PlayerMove.Forward[1] * fmove) + (PlayerMove.Right[1] * smove),
                0
                );

            var wishdir = wishvel;
            var wishspeed = wishdir.Length(); // Determine maginitude of speed of move
            wishdir = wishdir.Normalize();

            // Clamp to server defined max speed
            if (wishspeed > PlayerMove.MaxSpeed)
            {
                wishvel *= PlayerMove.MaxSpeed / wishspeed;
                wishspeed = PlayerMove.MaxSpeed;
            }

            AirAccelerate(wishdir, wishspeed, PlayerMove.MoveVars.AirAccelerate);

            // Add in any base velocity to the current velocity.
            PlayerMove.Velocity += PlayerMove.BaseVelocity;

            FlyMove();
        }

        private void AirAccelerate(in Vector wishdir, float wishspeed, float accel)
        {
            if (PlayerMove.Dead)
            {
                return;
            }

            if (PlayerMove.WaterJumpTime != 0)
            {
                return;
            }

            // Cap speed
            //wishspd = VectorNormalize (PlayerMove.wishveloc);

            var wishspd = Math.Min(30, wishspeed);

            // Determine veer amount
            var currentspeed = PlayerMove.Velocity.DotProduct(wishdir);
            // See how much to add
            var addspeed = wishspd - currentspeed;
            // If not adding any, done.
            if (addspeed <= 0)
            {
                return;
            }
            // Determine acceleration speed after acceleration

            var accelspeed = accel * wishspeed * PlayerMove.FrameTime * PlayerMove.Friction;
            // Cap it
            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            // Adjust pmove vel.
            PlayerMove.Velocity += accelspeed * wishdir;
        }

        private void CheckFalling()
        {
            if (PlayerMove.OnGround != -1
                 && !PlayerMove.Dead
                 && PlayerMove.FallVelocity >= WorldConstants.PlayerFallPunchTreshold)
            {
                var fvol = 0.5f;

                if (PlayerMove.WaterLevel <= WaterLevel.Dry)
                {
                    if (PlayerMove.FallVelocity > WorldConstants.PlayerMaxSafeFallSpeed)
                    {
                        // NOTE:  In the original game dll , there were no breaks after these cases, causing the first one to 
                        // cascade into the second
                        //switch ( RandomLong(0,1) )
                        //{
                        //case 0:
                        //EnginePhysics.PlaySound(SoundChannel.Voice, "player/pl_fallpain2.wav", 1, Attenuation.Normal, SoundFlags.None, Pitch.Normal);
                        //break;
                        //case 1:
                        EnginePhysics.PlaySound(SoundChannel.Voice, "player/pl_fallpain3.wav", 1, Attenuation.Normal, SoundFlags.None, Pitch.Normal);
                        //	break;
                        //}
                        fvol = 1.0f;
                    }
                    else if (PlayerMove.FallVelocity > WorldConstants.PlayerMaxSafeFallSpeed / 2)
                    {
                        int.TryParse(EnginePhysics.Info_ValueForKey(PlayerMove.PhysInfo, "tfc"), out var tfcValue);
                        var tfc = tfcValue == 1;

                        if (tfc)
                        {
                            EnginePhysics.PlaySound(SoundChannel.Voice, "player/pl_fallpain3.wav", 1, Attenuation.Normal, SoundFlags.None, Pitch.Normal);
                        }

                        fvol = 0.85f;
                    }
                    else if (PlayerMove.FallVelocity < WorldConstants.PlayerMinBounceSpeed)
                    {
                        fvol = 0;
                    }
                }

                if (fvol > 0.0)
                {
                    // Play landing step right away
                    PlayerMove.TimeStepSound = 0;

                    UpdateStepSound();

                    // play step sound for current texture
                    var materialType = Materials.GetMaterialType(PlayerMove.TextureName);
                    if (materialType != null)
                    {
                        PlayStepSound(materialType, fvol);
                    }

                    // Knock the screen around a little bit, temporary effect
                    var punchAngle = PlayerMove.PunchAngle;

                    punchAngle[2] = PlayerMove.FallVelocity * 0.013f;   // punch z axis
                    punchAngle[0] = Math.Min(8, punchAngle[0]);

                    PlayerMove.PunchAngle = punchAngle;
                }
            }

            if (PlayerMove.OnGround != -1)
            {
                PlayerMove.FallVelocity = 0;
            }
        }

        private void PlayWaterSounds()
        {
            // Did we enter or leave water?
            if ((PlayerMove.OldWaterLevel == WaterLevel.Dry && PlayerMove.WaterLevel != WaterLevel.Dry)
                  || (PlayerMove.OldWaterLevel != WaterLevel.Dry && PlayerMove.WaterLevel == WaterLevel.Dry))
            {
                switch (EnginePhysics.RandomLong(0, 3))
                {
                    case 0:
                        EnginePhysics.PlaySound(SoundChannel.Body, "player/pl_wade1.wav", 1, Attenuation.Normal, SoundFlags.None, Pitch.Normal);
                        break;
                    case 1:
                        EnginePhysics.PlaySound(SoundChannel.Body, "player/pl_wade2.wav", 1, Attenuation.Normal, SoundFlags.None, Pitch.Normal);
                        break;
                    case 2:
                        EnginePhysics.PlaySound(SoundChannel.Body, "player/pl_wade3.wav", 1, Attenuation.Normal, SoundFlags.None, Pitch.Normal);
                        break;
                    case 3:
                        EnginePhysics.PlaySound(SoundChannel.Body, "player/pl_wade4.wav", 1, Attenuation.Normal, SoundFlags.None, Pitch.Normal);
                        break;
                }
            }
        }
    }
}
