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
using GoldSource.Server.Engine;
using GoldSource.Server.Engine.API;
using GoldSource.Server.Engine.Game.API;
using GoldSource.Server.Engine.Networking;
using GoldSource.Server.Game.Game.Entities;
using GoldSource.Server.Game.Game.Entities.Characters;
using GoldSource.Server.Game.Game.Entities.Weapons;
using GoldSource.Shared.Engine;
using GoldSource.Shared.Engine.Networking;
using GoldSource.Shared.Entities;
using GoldSource.Shared.Game;
using System;
using System.Collections.Generic;

namespace GoldSource.Server.Game.Game.API.Implementations
{
    public sealed class Networking : INetworking
    {
        private IEngineServer EngineServer { get; }

        private IGlobalVars Globals { get; }

        private IEngineModel EngineModel { get; }

        private IEntityDictionary EntityDictionary { get; }

        private ITrace Trace { get; }

        public Networking(IEngineServer engineServer, IGlobalVars globals, IEngineModel engineModel, IEntityDictionary entityDictionary, ITrace trace)
        {
            EngineServer = engineServer ?? throw new ArgumentNullException(nameof(engineServer));
            Globals = globals ?? throw new ArgumentNullException(nameof(globals));
            EngineModel = engineModel ?? throw new ArgumentNullException(nameof(engineModel));
            EntityDictionary = entityDictionary ?? throw new ArgumentNullException(nameof(entityDictionary));
            Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public void SetupVisibility(Edict pViewEntity, Edict pClient, out IntPtr pvs, out IntPtr pas)
        {
            var client = pClient.Entity();

            // Find the client's PVS
            var view = pViewEntity?.TryGetEntity() ?? client;

            if ((client.Flags & EntFlags.Proxy) != 0)
            {
                pvs = IntPtr.Zero;    // the spectator proxy sees
                pas = IntPtr.Zero;    // and hears everything
                return;
            }

            var org = view.Origin + view.ViewOffset;
            if ((view.Flags & EntFlags.Ducking) != 0)
            {
                org += (WorldConstants.HULL_MIN - WorldConstants.DUCK_HULL_MIN);
            }

            pvs = EngineServer.SetFatPVS(org);
            pas = EngineServer.SetFatPAS(org);
        }

        public void UpdateClientData(Edict ent, bool sendWeapons, ClientData cd)
        {
            if (ent == null || ent.PrivateData == null)
            {
                return;
            }

            var player = ent.TryGetEntity<BasePlayer>();

            BasePlayer originalPlayer = null;

            // if user is spectating different player in First person, override some vars
            if (player?.pev.UserInt1 == (int)ObserverMode.InEye && player.m_hObserverTarget)
            {
                originalPlayer = player;
                player = player.m_hObserverTarget;
            }

            cd.Flags = player.Flags;
            cd.Health = player.Health;

            cd.ViewModel = EngineModel.IndexOf(player.ViewModelName);

            cd.WaterLevel = player.WaterLevel;
            cd.WaterType = player.WaterType;
            cd.Weapons = player.pev.Weapons;

            // Vectors
            cd.Origin = player.Origin;
            cd.Velocity = player.Velocity;
            cd.ViewOffset = player.ViewOffset;
            cd.PunchAngle = player.PunchAngle;

            cd.InDuck = player.pev.InDuck;
            cd.TimeStepSound = player.pev.TimeStepSound;
            cd.DuckTime = player.pev.DuckTime;
            cd.SwimTime = player.pev.SwimTime;
            cd.WaterJumpTime = (int)player.pev.TeleportTime;

            cd.SetPhysInfo(EngineServer.GetUnmanagedClientPhysicsInfoBuffer(ent));

            cd.MaxSpeed = player.pev.MaxSpeed;
            cd.FieldOfView = player.pev.FieldOfView;
            cd.WeaponAnim = player.pev.WeaponAnim;

            cd.PushMSec = player.pev.PushMSec;

            //Spectator mode
            if (originalPlayer != null)
            {
                // don't use spec vars from chased player
                cd.UserInt1 = originalPlayer.pev.UserInt1;
                cd.UserInt2 = originalPlayer.pev.UserInt2;
            }
            else
            {
                cd.UserInt1 = player.pev.UserInt1;
                cd.UserInt2 = player.pev.UserInt2;
            }

#if CLIENT_WEAPONS
            if (sendWeapons && player != null)
            {
                cd.NextAttack = player.m_flNextAttack;
                cd.UserFloat2 = player.m_flNextAmmoBurn;
                cd.UserFloat3 = player.m_flAmmoStartCharge;

                cd.UserVector1 = new Vector(
                    player.ammo_9mm,
                    player.ammo_357,
                    player.ammo_argrens
                    );

                cd.AmmoNails = player.ammo_bolts;
                cd.AmmoShells = player.ammo_buckshot;
                cd.AmmoRockets = player.ammo_rockets;
                cd.AmmoCells = player.ammo_uranium;
                var vector2 = cd.UserVector2;
                vector2.x = player.ammo_hornets;
                cd.UserVector2 = vector2;

                if (player.m_pActiveItem != null)
                {
                    BasePlayerWeapon gun = player.m_pActiveItem as BasePlayerWeapon;

                    if (gun?.IsPredicted() == true)
                    {
                        //TODO:
#if false
                        ItemInfo II;
                        memset(&II, 0, sizeof(II));
                        gun.GetItemInfo(&II);

                        cd.m_iId = II.iId;

                        cd.vuser3.z = gun.m_iSecondaryAmmoType;
                        cd.vuser4.x = gun.m_iPrimaryAmmoType;
                        cd.vuser4.y = player.m_rgAmmo[gun.m_iPrimaryAmmoType];
                        cd.vuser4.z = player.m_rgAmmo[gun.m_iSecondaryAmmoType];

                        if (player.m_pActiveItem.m_iId == WEAPON_RPG)
                        {
                            cd.vuser2.y = ((Rpg)player.m_pActiveItem).m_fSpotActive;
                            cd.vuser2.z = ((Rpg)player.m_pActiveItem).m_cActiveRockets;
                        }
#endif
                    }
                }
            }
#endif
        }

        public bool AddToFullPack(EntityState state, int e, Edict ent, Edict host, HostFlags hostFlags, bool isPlayer, IntPtr pSet)
        {
            //Never add entities that aren't in use
            if (ent.Free)
            {
                return false;
            }

            var entity = ent.Entity();
            var hostEntity = host.Entity();

            // don't send if flagged for NODRAW and it's not the host getting the message
            if ((entity.Effects & EntityEffects.NoDraw) != 0
                 && (entity != hostEntity))
            {
                return false;
            }

            // Ignore ents without valid / visible models
            if (entity.ModelIndex == 0 || string.IsNullOrEmpty(entity.ModelName))
            {
                return false;
            }

            // Don't send spectators to other players
            if ((entity.Flags & EntFlags.Spectator) != 0 && (entity != hostEntity))
            {
                return false;
            }

            // Ignore if not the host and not touching a PVS/PAS leaf
            // If pSet is NULL, then the test will always succeed and the entity will be added to the update
            if (entity != hostEntity)
            {
                if (!EngineServer.CheckVisibility(ent, pSet))
                {
                    return false;
                }
            }

            // Don't send entity to local client if the client says it's predicting the entity itself.
            if ((entity.Flags & EntFlags.SkipLocalHost) != 0)
            {
                if ((hostFlags & HostFlags.SkipLocalEnts) != 0 && (entity.Owner == hostEntity))
                {
                    return false;
                }
            }

            if (hostEntity.GroupInfo != 0)
            {
                Trace.PushGroupTrace(hostEntity.GroupInfo, GroupOp.And);

                try
                {
                    Trace.GetGroupTrace(out var currentMask, out var currentOp);

                    // Should always be set, of course
                    if (entity.GroupInfo != 0)
                    {
                        if (currentOp == GroupOp.And)
                        {
                            if ((entity.GroupInfo & hostEntity.GroupInfo) == 0)
                            {
                                return false;
                            }
                        }
                        else if (currentOp == GroupOp.NAnd)
                        {
                            if ((entity.GroupInfo & hostEntity.GroupInfo) != 0)
                            {
                                return false;
                            }
                        }
                    }
                }
                finally
                {
                    //There is a bug in the SDK that can cause the last group trace to remain if it failed the tests above
                    Trace.PopGroupTrace();
                }
            }

            //This is done by the wrapper since there's no memset in managed code
            //memset(state, 0, sizeof( * state) );

            // Assign index so we can track this entity from frame to frame and
            //  delta from it.
            state.Number = e;
            state.EntityType = EntityType.Normal;

            // Flag custom entities.
            if ((entity.Flags & EntFlags.CustomEntity) != 0)
            {
                state.EntityType = EntityType.Beam;
            }

            // 
            // Copy state data
            //

            // Round animtime to nearest millisecond
            state.AnimTime = (float)((int)(1000.0 * entity.AnimationTime) / 1000.0);

            state.Origin = entity.Origin;
            state.Angles = entity.Angles;
            state.Mins = entity.Mins;
            state.Maxs = entity.Maxs;

            state.EndPos = entity.EndPosition;
            state.StartPos = entity.StartPosition;

            state.ImpactTime = entity.ImpactTime;
            state.StartTime = entity.StartTime;

            state.ModelIndex = entity.ModelIndex;

            state.Frame = entity.Frame;

            state.Skin = (short)entity.Skin;
            state.Effects = entity.Effects;

            // This non-player entity is being moved by the game .dll and not the physics simulation system
            //  make sure that we interpolate it's position on the client if it moves
            if (!isPlayer
                 && entity.AnimationTime != 0
                 && entity.Velocity[0] == 0
                 && entity.Velocity[1] == 0
                 && entity.Velocity[2] == 0)
            {
                state.EFlags |= EntityStateFlags.SLerp;
            }

            state.Scale = entity.Scale;
            state.Solid = entity.Solid;
            state.ColorMap = entity.ColorMap;

            state.MoveType = entity.MoveType;
            state.Sequence = entity.Sequence;
            state.FrameRate = entity.FrameRate;
            state.Body = entity.Body;

            for (var i = 0; i < 4; ++i)
            {
                state.SetController(i, entity.GetController(i));
            }

            for (var i = 0; i < 2; ++i)
            {
                state.SetBlending(i, entity.GetBlending(i));
            }

            state.RenderMode = entity.RenderMode;
            state.RenderAmount = (int)entity.RenderAmount;
            state.RenderEffect = entity.RenderEffect;

            state.RenderColor = new Color24
            {
                r = (byte)entity.RenderColor.x,
                g = (byte)entity.RenderColor.y,
                b = (byte)entity.RenderColor.z
            };

            state.AimEnt = 0;
            if (entity.AimEntity != null)
            {
                state.AimEnt = entity.AimEntity.EntIndex();
            }

            state.Owner = 0;
            if (entity.Owner != null)
            {
                var owner = entity.Owner.EntIndex();

                // Only care if owned by a player
                if (owner >= 1 && owner <= Globals.MaxClients)
                {
                    state.Owner = owner;
                }
            }

            // HACK:  Somewhat...
            // Class is overridden for non-players to signify a breakable glass object ( sort of a class? )
            if (!isPlayer)
            {
                state.PlayerClass = entity.PlayerClass;
            }

            // Special stuff for players only
            if (isPlayer)
            {
                state.BaseVelocity = entity.BaseVelocity;

                state.WeaponModel = EngineModel.IndexOf(entity.WeaponModelName);
                state.GaitSequence = entity.GaitSequence;
                state.Spectator = (entity.Flags & EntFlags.Spectator) != 0;
                state.Friction = entity.Friction;

                state.Gravity = entity.Gravity;
                //		state.Team			= entity.Team;
                //		
                state.UseHull = (entity.Flags & EntFlags.Ducking) != 0 ? 1 : 0;
                state.Health = (int)entity.Health;
            }

            return true;
        }

        public void CreateBaseline(bool isPlayer, int eindex, EntityState baseline, Edict entity, int playermodelindex, in Vector playerMins, in Vector playerMaxs)
        {
            //TODO: the engine calls this before players have connected so there is no entity for them
            baseline.Origin = entity.Vars.Origin;
            baseline.Angles = entity.Vars.Angles;
            baseline.Frame = entity.Vars.Frame;
            baseline.Skin = (short)entity.Vars.Skin;

            // render information
            baseline.RenderMode = entity.Vars.RenderMode;
            baseline.RenderAmount = (int)entity.Vars.RenderAmount;
            baseline.RenderColor = new Color24
            {
                r = (byte)entity.Vars.RenderColor.x,
                g = (byte)entity.Vars.RenderColor.y,
                b = (byte)entity.Vars.RenderColor.z
            };
            baseline.RenderEffect = entity.Vars.RenderEffect;

            if (isPlayer)
            {
                baseline.Mins = playerMins;
                baseline.Maxs = playerMaxs;

                baseline.ColorMap = eindex;
                baseline.ModelIndex = playermodelindex;
                baseline.Friction = 1.0f;
                baseline.MoveType = MoveType.Walk;

                baseline.Scale = entity.Vars.Scale;
                baseline.Solid = Solid.SlideBox;
                baseline.FrameRate = 1.0f;
                baseline.Gravity = 1.0f;
            }
            else
            {
                baseline.Mins = entity.Vars.Mins;
                baseline.Maxs = entity.Vars.Maxs;

                baseline.ColorMap = 0;
                baseline.ModelIndex = entity.Vars.ModelIndex;//SV_ModelIndex(source.ModelName);
                baseline.MoveType = entity.Vars.MoveType;

                baseline.Scale = entity.Vars.Scale;
                baseline.Solid = entity.Vars.Solid;
                baseline.FrameRate = entity.Vars.FrameRate;
                baseline.Gravity = entity.Vars.Gravity;
            }
        }

        public void RegisterEncoders()
        {
            //TODO
        }

        public bool GetWeaponData(Edict player, IReadOnlyList<WeaponData> info)
        {
            //TODO:
            return false;
        }

        public void CmdStart(Edict player, UserCmd cmd, uint randomSeed)
        {
            var pl = player.TryGetEntity<BasePlayer>();

            if (pl == null)
            {
                return;
            }

            if (pl.pev.GroupInfo != 0)
            {
                Trace.PushGroupTrace(pl.pev.GroupInfo, GroupOp.And);
            }

            pl.RandomSeed = randomSeed;
        }

        public void CmdEnd(Edict player)
        {
            var pl = player.TryGetEntity<BasePlayer>();

            if (pl == null)
            {
                return;
            }

            if (pl.pev.GroupInfo != 0)
            {
                Trace.PopGroupTrace();
            }
        }

        public void CreateInstancedBaselines()
        {
            //Nothing
        }

        public bool InconsistentFile(Edict player, string fileName, out string disconnectMessage)
        {
            // Server doesn't care?
            if (CVar.GetFloat("mp_consistency") != 1)
            {
                disconnectMessage = string.Empty;
                return false;
            }

            // Default behavior is to kick the player
            disconnectMessage = $"Server is enforcing file consistency for {fileName}\n";

            // Kick now with specified disconnect message.
            return true;
        }

        public bool AllowLagCompensation()
        {
            return true;
        }
    }
}
