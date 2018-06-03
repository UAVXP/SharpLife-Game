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
using GoldSource.Server.Game.Engine;
using GoldSource.Server.Game.Game.Entities.Characters.NPCs;
using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Server.Game.Game.Entities.Weapons;
using GoldSource.Server.Game.Game.GlobalState;
using GoldSource.Server.Game.Persistence;
using GoldSource.Shared.Engine;
using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;
using GoldSource.Shared.Game;
using System;
using System.Collections.Generic;

namespace GoldSource.Server.Game.Game.Entities
{
    public class BaseEntity : IEntity
    {
        public delegate void ThinkFunc();
        public delegate void TouchFunc(BaseEntity other);
        public delegate void UseFunc(BaseEntity activator, BaseEntity caller, UseType useType, float flValue);
        public delegate void BlockedFunc(BaseEntity other);

        public EntVars pev;

        [Persist]
        public string ClassName
        {
            get => pev.ClassName;
            set => pev.ClassName = value ?? throw new ArgumentNullException(nameof(value));
        }

        [KeyValue]
        [Persist]
        public string Target
        {
            get => pev.Target;
            set => pev.Target = value;
        }

        [KeyValue]
        [Persist]
        public string TargetName
        {
            get => pev.TargetName;
            set => pev.TargetName = value;
        }

        [KeyValue]
        [Persist]
        public string Message
        {
            get => pev.Message;
            set => pev.Message = value;
        }

        [KeyValue]
        [Persist]
        public string NetName
        {
            get => pev.NetName;
            set => pev.NetName = value;
        }

        [KeyValue]
        [Persist]
        public string GlobalName
        {
            get => pev.GlobalName;
            set => pev.GlobalName = value;
        }

        [KeyValue]
        [Persist]
        public Vector Origin
        {
            get => pev.Origin;
            set => pev.Origin = value;
        }

        public void SetOrigin(in Vector origin)
        {
            Engine.Entities.SetOrigin(Edict(), origin);
        }

        [KeyValue]
        [Persist]
        public Vector Angles
        {
            get => pev.Angles;
            set => pev.Angles = value;
        }

        [KeyValue]
        [Persist]
        public Vector Velocity
        {
            get => pev.Velocity;
            set => pev.Velocity = value;
        }

        [KeyValue]
        [Persist]
        public Vector BaseVelocity
        {
            get => pev.BaseVelocity;
            set => pev.BaseVelocity = value;
        }

        [KeyValue(KeyName = "avelocity")]
        [Persist]
        public Vector AngularVelocity
        {
            get => pev.AngularVelocity;
            set => pev.AngularVelocity = value;
        }

        [KeyValue]
        [Persist]
        public Vector MoveDirection
        {
            get => pev.MoveDirection;
            set => pev.MoveDirection = value;
        }

        [KeyValue(KeyName = "v_angle")]
        [Persist]
        public Vector ViewAngle
        {
            get => pev.ViewAngle;
            set => pev.ViewAngle = value;
        }

        [KeyValue(KeyName = "view_ofs")]
        [Persist]
        public Vector ViewOffset
        {
            get => pev.ViewOffset;
            set => pev.ViewOffset = value;
        }

        [KeyValue]
        [Persist]
        public Vector PunchAngle
        {
            get => pev.PunchAngle;
            set => pev.PunchAngle = value;
        }

        [KeyValue]
        [Persist]
        public FixAngleMode FixAngle
        {
            get => pev.FixAngle;
            set => pev.FixAngle = value;
        }

        [KeyValue]
        [Persist]
        public Vector AbsMin
        {
            get => pev.AbsMin;
            set => pev.AbsMin = value;
        }

        [KeyValue]
        [Persist]
        public Vector AbsMax
        {
            get => pev.AbsMax;
            set => pev.AbsMax = value;
        }

        [KeyValue]
        [Persist]
        public Vector Mins
        {
            get => pev.Mins;
            set => pev.Mins = value;
        }

        [KeyValue]
        [Persist]
        public Vector Maxs
        {
            get => pev.Maxs;
            set => pev.Maxs = value;
        }

        [KeyValue]
        [Persist]
        public Vector Size
        {
            get => pev.Size;
            set => pev.Size = value;
        }

        public void SetSize(in Vector mins, in Vector maxs)
        {
            Engine.Entities.SetSize(Edict(), mins, maxs);
        }

        [KeyValue]
        [Persist]
        public Solid Solid
        {
            get => pev.Solid;
            set => pev.Solid = value;
        }

        [KeyValue]
        [Persist]
        public MoveType MoveType
        {
            get => pev.MoveType;
            set => pev.MoveType = value;
        }

        [KeyValue(KeyName = "Model")]
        [Persist]
        public string ModelName
        {
            get => pev.ModelName;
            set => pev.ModelName = value;
        }

        public void SetModel(string modelName)
        {
            Engine.Entities.SetModel(Edict(), modelName);
        }

        [KeyValue]
        [Persist]
        public int ModelIndex
        {
            get => pev.ModelIndex;
            set => pev.ModelIndex = value;
        }

        [KeyValue(KeyName = "viewmodel")]
        [Persist]
        public string ViewModelName
        {
            get => pev.ViewModel;
            set => pev.ViewModel = value;
        }

        [KeyValue(KeyName = "weaponmodel")]
        [Persist]
        public string WeaponModelName
        {
            get => pev.WeaponModel;
            set => pev.WeaponModel = value;
        }

        [KeyValue]
        [Persist]
        public float Speed
        {
            get => pev.Speed;
            set => pev.Speed = value;
        }

        [KeyValue]
        [Persist]
        public uint SpawnFlags
        {
            get => pev.SpawnFlags;
            set => pev.SpawnFlags = value;
        }

        [KeyValue]
        [Persist]
        public EntFlags Flags
        {
            get => pev.Flags;
            set => pev.Flags = value;
        }

        [KeyValue]
        [Persist]
        public RenderMode RenderMode
        {
            get => pev.RenderMode;
            set => pev.RenderMode = value;
        }

        [KeyValue(KeyName = "renderamt")]
        [Persist]
        public float RenderAmount
        {
            get => pev.RenderAmount;
            set => pev.RenderAmount = value;
        }

        [KeyValue]
        [Persist]
        public Vector RenderColor
        {
            get => pev.RenderColor;
            set => pev.RenderColor = value;
        }

        [KeyValue(KeyName = "renderfx")]
        [Persist]
        public RenderEffect RenderEffect
        {
            get => pev.RenderEffect;
            set => pev.RenderEffect = value;
        }

        [KeyValue]
        [Persist]
        public EntityEffects Effects
        {
            get => pev.Effects;
            set => pev.Effects = value;
        }

        [KeyValue]
        [Persist]
        public int Body
        {
            get => pev.Body;
            set => pev.Body = value;
        }

        [KeyValue]
        [Persist]
        public int Skin
        {
            get => pev.Skin;
            set => pev.Skin = value;
        }

        [KeyValue]
        [Persist]
        public int Sequence
        {
            get => pev.Sequence;
            set => pev.Sequence = value;
        }

        [KeyValue]
        [Persist]
        public int GaitSequence
        {
            get => pev.GaitSequence;
            set => pev.GaitSequence = value;
        }

        [KeyValue]
        [Persist]
        public float Frame
        {
            get => pev.Frame;
            set => pev.Frame = value;
        }

        [KeyValue]
        [Persist]
        public float FrameRate
        {
            get => pev.FrameRate;
            set => pev.FrameRate = value;
        }

        public byte GetController(int index) => pev.GetController(index);

        public void SetController(int index, byte value) => pev.SetController(index, value);

        //TODO: need to persist arrays some other way
        [Persist]
        public List<byte> Controllers
        {
            get
            {
                return new List<byte>
                {
                    GetController(0),
                    GetController(1),
                    GetController(2),
                    GetController(3)
                };
            }

            set
            {
                for (var i = 0; i < value.Count; ++i)
                {
                    SetController(i, value[i]);
                }
            }
        }

        public byte GetBlending(int index) => pev.GetBlending(index);

        public void SetBlending(int index, byte value) => pev.SetBlending(index, value);

        //TODO: need to persist arrays some other way
        [Persist]
        public List<byte> Blendings
        {
            get
            {
                return new List<byte>
                {
                    GetBlending(0),
                    GetBlending(1),
                };
            }

            set
            {
                for (var i = 0; i < value.Count; ++i)
                {
                    SetBlending(i, value[i]);
                }
            }
        }

        [KeyValue(KeyName = "takedamage")]
        [Persist]
        public TakeDamageState TakeDamageState
        {
            get => pev.TakeDamage;
            set => pev.TakeDamage = value;
        }

        [KeyValue]
        [Persist]
        public float Health
        {
            get => pev.Health;
            set => pev.Health = value;
        }

        [KeyValue(KeyName = "max_health")]
        [Persist]
        public float MaxHealth
        {
            get => pev.MaxHealth;
            set => pev.MaxHealth = value;
        }

        [KeyValue]
        [Persist]
        public float ArmorValue
        {
            get => pev.ArmorValue;
            set => pev.ArmorValue = value;
        }

        [KeyValue]
        [Persist]
        public float ArmorType
        {
            get => pev.ArmorType;
            set => pev.ArmorType = value;
        }

        [KeyValue]
        [Persist]
        public int PlayerClass
        {
            get => pev.PlayerClass;
            set => pev.PlayerClass = value;
        }

        [KeyValue]
        [Persist]
        public float Scale
        {
            get => pev.Scale;
            set => pev.Scale = value;
        }

        [KeyValue]
        [Persist]
        public float Gravity
        {
            get => pev.Gravity;
            set => pev.Gravity = value;
        }

        [KeyValue]
        [Persist]
        public float Friction
        {
            get => pev.Friction;
            set => pev.Friction = value;
        }

        [KeyValue]
        [Persist]
        public int Impulse
        {
            get => pev.Impulse;
            set => pev.Impulse = value;
        }

        [KeyValue]
        [Persist]
        public int Button
        {
            get => pev.Button;
            set => pev.Button = value;
        }

        [KeyValue]
        [Persist]
        public int OldButtons
        {
            get => pev.OldButtons;
            set => pev.OldButtons = value;
        }

        [KeyValue]
        [Persist]
        public WaterLevel WaterLevel
        {
            get => pev.WaterLevel;
            set => pev.WaterLevel = value;
        }

        [KeyValue]
        [Persist]
        public Contents WaterType
        {
            get => pev.WaterType;
            set => pev.WaterType = value;
        }

        [KeyValue(KeyName = "dmg")]
        [Persist]
        public float Damage
        {
            get => pev.Damage;
            set => pev.Damage = value;
        }

        [KeyValue(KeyName = "dmgtime")]
        [Persist]
        public float DamageTime
        {
            get => pev.DamageTime;
            set => pev.DamageTime = value;
        }

        [KeyValue(KeyName = "pain_finished")]
        [Persist]
        public float PainFinished
        {
            get => pev.PainFinished;
            set => pev.PainFinished = value;
        }

        [KeyValue]
        [Persist]
        public int ColorMap
        {
            get => pev.ColorMap;
            set => pev.ColorMap = value;
        }

        [KeyValue]
        [Persist]
        public DeadFlag DeadFlag
        {
            get => pev.DeadFlag;
            set => pev.DeadFlag = value;
        }

        [KeyValue(KeyName = "animtime")]
        [Persist]
        public float AnimationTime
        {
            get => pev.AnimationTime;
            set => pev.AnimationTime = value;
        }

        public BaseEntity Chain
        {
            get => pev.Chain?.TryGetEntity();
            set => pev.Chain = value?.Edict();
        }

        [Persist]
        public BaseEntity DamageInflictor
        {
            get => pev.DamageInflictor?.TryGetEntity();
            set => pev.DamageInflictor = value?.Edict();
        }

        [Persist]
        public BaseEntity Enemy
        {
            get => pev.Enemy?.TryGetEntity();
            set => pev.Enemy = value?.Edict();
        }

        [Persist]
        public BaseEntity AimEntity
        {
            get => pev.AimEnt?.TryGetEntity();
            set => pev.AimEnt = value?.Edict();
        }

        [Persist]
        public BaseEntity Owner
        {
            get => pev.Owner?.TryGetEntity();
            set => pev.Owner = value?.Edict();
        }

        [Persist]
        public BaseEntity GroundEntity
        {
            get => pev.GroundEntity?.TryGetEntity();
            set => pev.GroundEntity = value?.Edict();
        }

        [Persist]
        public int GroupInfo
        {
            get => pev.GroupInfo;
            set => pev.GroupInfo = value;
        }

        [KeyValue]
        [Persist]
        public Vector StartPosition
        {
            get => pev.StartPosition;
            set => pev.StartPosition = value;
        }

        [KeyValue]
        [Persist]
        public Vector EndPosition
        {
            get => pev.EndPosition;
            set => pev.EndPosition = value;
        }

        [KeyValue]
        [Persist]
        public float StartTime
        {
            get => pev.StartTime;
            set => pev.StartTime = value;
        }

        [KeyValue]
        [Persist]
        public float ImpactTime
        {
            get => pev.ImpactTime;
            set => pev.ImpactTime = value;
        }

        /// <summary>
        /// path corner we are heading towards
        /// </summary>
        [Persist]
        public EHandle<BaseEntity> GoalEnt { get; set; }

        /// <summary>
        /// used for temporary link-list operations
        /// TODO: could just use List&lt;BaseEntity&gt; instead
        /// </summary>
        public BaseEntity Link { get; set; }

        //TODO: could use events for these
        [Persist]
        public ThinkFunc m_pfnThink { get; set; }

        [Persist]
        public TouchFunc m_pfnTouch { get; set; }

        [Persist]
        public UseFunc m_pfnUse { get; set; }

        [Persist]
        public BlockedFunc m_pfnBlocked { get; set; }

        //These are methods to allow for multiple think functions in the future

        public float GetNextThink()
        {
            return pev.NextThink;
        }

        public void SetNextThink(float time)
        {
            pev.NextThink = time;
        }

        /// <summary>
        /// Gets the last think time. Set by the engine for brush entities only (MoveType.Push)
        /// Should be used with brush entities when setting next think times (MoveType.Push); use GetLastThink() + delay
        /// For other movetypes, use Engine.Globals.Time + delay
        /// </summary>
        /// <returns></returns>
        public float GetLastThink()
        {
            return pev.LastTime;
        }

        public void SetThink(ThinkFunc func)
        {
            m_pfnThink = func;
        }

        public void SetTouch(TouchFunc func)
        {
            m_pfnTouch = func;
        }

        public void SetUse(UseFunc func)
        {
            m_pfnUse = func;
        }

        public void SetBlocked(BlockedFunc func)
        {
            m_pfnBlocked = func;
        }

        //We use this variables to store each ammo count.
        public int ammo_9mm;

        public int ammo_357;

        public int ammo_bolts;

        public int ammo_buckshot;

        public int ammo_rockets;

        public int ammo_uranium;

        public int ammo_hornets;

        public int ammo_argrens;
        //Special stuff for grenades and satchels.
        public float m_flStartThrow;

        public float m_flReleaseThrow;

        public int m_chargeReady;

        public int m_fInAttack;

        public int m_fireState;

        /// <summary>
        /// Called when the entity has just been created
        /// </summary>
        public virtual void OnCreate()
        {
        }

        /// <summary>
        /// Called when the entity is being destroyed, and should no longer be referenced
        /// </summary>
        public virtual void OnDestroy()
        {
        }

        /// <summary>
        /// Handle a keyvalue if it couldn't be found through metadata lookup
        /// </summary>
        /// <param name="key">Key name</param>
        /// <param name="value">Value to use</param>
        /// <returns>Whether the keyvalue was handled</returns>
        public virtual bool KeyValue(string key, string value)
        {
            return false;
        }

        /// <summary>
        /// Precaches resources
        /// </summary>
        public virtual void Precache()
        {
        }

        /// <summary>
        /// Spawns the entity
        /// </summary>
        public virtual void Spawn()
        {
        }

        /// <summary>
        /// Called once all entities have been spawned
        /// Activates entities, allows them to connect with eachother
        /// </summary>
        public virtual void Activate()
        {
        }

        public virtual bool Save(CSave save)
        {
            //TODO: implement
            return true;
        }

        public virtual bool Restore(CRestore restore)
        {
            //TODO: implement

            if (ModelIndex != 0 && !string.IsNullOrEmpty(ModelName))
            {
                var mins = Mins;   // Set model is about to destroy these
                var maxs = Maxs;

                Engine.Server.PrecacheModel(ModelName);
                SetModel(ModelName);
                SetSize(mins, maxs);  // Reset them
            }

            return true;
        }

        public virtual EntityCapabilities ObjectCaps()
        {
            return EntityCapabilities.AcrossTransition;
        }

        public virtual void SetObjectCollisionBox()
        {
            if ((Solid == Solid.BSP)
                 && (0 != Angles.x || 0 != Angles.y || 0 != Angles.z))
            {   // expand for rotation
                float max = 0;
                for (var i = 0; i < 3; ++i)
                {
                    var v = Math.Abs(Mins[i]);
                    if (v > max)
                    {
                        max = v;
                    }

                    v = Math.Abs(Maxs[i]);
                    if (v > max)
                    {
                        max = v;
                    }
                }
                AbsMin = Origin - new Vector(max);
                AbsMax = Origin + new Vector(max);
            }
            else
            {
                AbsMin = Origin + Mins;
                AbsMax = Origin + Maxs;
            }
            AbsMin -= new Vector(1);
            AbsMax += new Vector(1);
        }

        public virtual EntityClass Classify()
        {
            return EntityClass.None;
        }

        public virtual void DeathNotice(BaseEntity child)
        {
        }

        public virtual void TraceAttack(BaseEntity attacker, float flDamage, Vector vecDir, ref TraceResult ptr, DamageTypes bitsDamageType)
        {
            Vector vecOrigin = ptr.EndPos - (vecDir * 4);

            if (TakeDamageState.No != TakeDamageState)
            {
                Globals.MultiDamage.AddMultiDamage(attacker, this, flDamage, bitsDamageType);

                var blood = GetBloodColor();

                if (blood != BloodColor.DontBleed)
                {
                    EntUtils.SpawnBlood(vecOrigin, blood, flDamage);// a little surface blood.
                    TraceBleed(flDamage, vecDir, ref ptr, bitsDamageType);
                }
            }
        }

        public virtual int TakeDamage(BaseEntity inflictor, BaseEntity attacker, float flDamage, DamageTypes bitsDamageType)
        {
            if (TakeDamageState.No == TakeDamageState)
            {
                return 0;
            }

            // UNDONE: some entity types may be immune or resistant to some bitsDamageType
            Vector vecTemp;

            // if Attacker == Inflictor, the attack was a melee or other instant-hit attack.
            // (that is, no actual entity projectile was involved in the attack so use the shooter's origin). 
            if (ReferenceEquals(attacker, inflictor))
            {
                vecTemp = inflictor.Origin - (EntUtils.BrushModelOrigin(this));
            }
            else
            // an actual missile was involved.
            {
                vecTemp = inflictor.Origin - (EntUtils.BrushModelOrigin(this));
            }

            // this global is still used for glass and other non-monster killables, along with decals.
            Globals.g_vecAttackDir = vecTemp.Normalize();

            // save damage based on the target's armor level

            // figure momentum add (don't let hurt brushes or other triggers move player)
            if (inflictor != null && (MoveType == MoveType.Walk || MoveType == MoveType.Step) && (attacker.Solid != Solid.Trigger))
            {
                var vecDir = (Origin - ((inflictor.AbsMin + inflictor.AbsMax) * 0.5)).Normalize();

                var flForce = (flDamage * ((32 * 32 * 72.0f) / (Size.x * Size.y * Size.z)) * 5);

                if (flForce > 1000.0)
                {
                    flForce = 1000.0f;
                }

                Velocity += vecDir * flForce;
            }

            // do the damage
            Health -= flDamage;
            if (Health <= 0)
            {
                Killed(attacker, GibAction.Normal);
                return 0;
            }

            return 1;
        }

        public virtual bool TakeHealth(float flHealth, DamageTypes bitsDamageType)
        {
            if (TakeDamageState.No == TakeDamageState)
            {
                return false;
            }

            // heal
            if (Health >= MaxHealth)
            {
                return false;
            }

            Health += flHealth;

            if (Health > MaxHealth)
            {
                Health = MaxHealth;
            }

            return true;
        }

        public virtual void Killed(BaseEntity attacker, GibAction gibAction)
        {
            TakeDamageState = TakeDamageState.No;
            DeadFlag = DeadFlag.Dead;
            EntUtils.Remove(this);
        }

        public virtual BloodColor GetBloodColor()
        {
            return BloodColor.DontBleed;
        }

        public virtual void TraceBleed(float flDamage, Vector vecDir, ref TraceResult ptr, DamageTypes bitsDamageType)
        {
            if (GetBloodColor() == BloodColor.DontBleed)
                return;

            if (flDamage == 0)
                return;

            if (0 == (bitsDamageType & (DamageTypes.Crush | DamageTypes.Bullet | DamageTypes.Slash | DamageTypes.Blast | DamageTypes.Club | DamageTypes.Mortar)))
                return;

            // make blood decal on the wall! 
            float flNoise;
            int cCount;

            /*
                if ( !IsAlive() )
                {
                    // dealing with a dead monster. 
                    if ( pev->max_health <= 0 )
                    {
                        // no blood decal for a monster that has already decalled its limit.
                        return; 
                    }
                    else
                    {
                        pev->max_health--;
                    }
                }
            */

            if (flDamage < 10)
            {
                flNoise = 0.1f;
                cCount = 1;
            }
            else if (flDamage < 25)
            {
                flNoise = 0.2f;
                cCount = 2;
            }
            else
            {
                flNoise = 0.3f;
                cCount = 4;
            }

            Vector vecTraceDir;

            for (var i = 0; i < cCount; ++i)
            {
                vecTraceDir = vecDir * -1;// trace in the opposite direction the shot came from (the direction the shot is going)

                vecTraceDir.x += EngineRandom.Float(-flNoise, flNoise);
                vecTraceDir.y += EngineRandom.Float(-flNoise, flNoise);
                vecTraceDir.z += EngineRandom.Float(-flNoise, flNoise);

                Trace.Line(ptr.EndPos, ptr.EndPos + (vecTraceDir * -172), TraceFlags.IgnoreMonsters, Edict(), out TraceResult Bloodtr);

                if (Bloodtr.Fraction != 1.0)
                {
                    EntUtils.BloodDecalTrace(Bloodtr, GetBloodColor());
                }
            }
        }

        public virtual bool IsTriggered(BaseEntity pActivator)
        {
            return true;
        }

        //TODO: replace with e as BaseMonster
        public virtual BaseMonster MyMonsterPointer()
        {
            return null;
        }

        //TODO: replace with e as SquadMonster
        public virtual SquadMonster MySquadMonsterPointer()
        {
            return null;
        }

        //TODO: move to subclass
        public virtual ToggleState GetToggleState()
        {
            return ToggleState.AtTop;
        }

        //TODO: move to interface or BasePlayer?
        public virtual void AddPoints(int score, bool bAllowNegativeScore)
        {
        }

        public virtual void AddPointsToTeam(int score, bool bAllowNegativeScore)
        {
        }

        //TODO: move to BasePlayer
        public virtual bool AddPlayerItem(BasePlayerItem pItem)
        {
            return false;
        }

        public virtual bool RemovePlayerItem(BasePlayerItem pItem)
        {
            return false;
        }

        public virtual int GiveAmmo(int iAmount, char szName, int iMax)
        {
            return -1;
        }

        public virtual float GetDelay()
        {
            return 0;
        }

        public virtual bool IsMoving()
        {
            return Velocity != WorldConstants.g_vecZero;
        }

        public virtual void OverrideReset()
        {
        }

        public virtual Decal DamageDecal(DamageTypes bitsDamageType)
        {
            if (RenderMode == RenderMode.TransAlpha)
            {
                return Decal.Invalid;
            }

            if (RenderMode != RenderMode.Normal)
            {
                return Decal.BProof1;
            }

            return Decal.Gunshot1 + EngineRandom.Long(0, 4);
        }

        //TODO: move to subclass
        public virtual void SetToggleState(ToggleState state)
        {
        }

        public virtual void StartSneaking()
        {
        }

        public virtual void StopSneaking()
        {
        }

        public virtual bool OnControls(BaseEntity entity)
        {
            return false;
        }

        public virtual bool IsSneaking()
        {
            return false;
        }

        public virtual bool IsAlive()
        {
            return (DeadFlag == DeadFlag.No) && Health > 0;
        }

        public virtual bool IsBSPModel()
        {
            return Solid == Solid.BSP || MoveType == MoveType.PushStep;
        }

        public virtual bool ReflectGauss()
        {
            return IsBSPModel() && TakeDamageState.No == TakeDamageState;
        }

        public virtual bool HasTarget(string targetname)
        {
            return targetname == TargetName;
        }

        public virtual bool IsInWorld()
        {
            // position 
            if (Origin.x >= WorldConstants.MAX_COORDINATE) return false;
            if (Origin.y >= WorldConstants.MAX_COORDINATE) return false;
            if (Origin.z >= WorldConstants.MAX_COORDINATE) return false;
            if (Origin.x <= -WorldConstants.MAX_COORDINATE) return false;
            if (Origin.y <= -WorldConstants.MAX_COORDINATE) return false;
            if (Origin.z <= -WorldConstants.MAX_COORDINATE) return false;
            // speed
            if (Velocity.x >= WorldConstants.MAX_SPEED) return false;
            if (Velocity.y >= WorldConstants.MAX_SPEED) return false;
            if (Velocity.z >= WorldConstants.MAX_SPEED) return false;
            if (Velocity.x <= -WorldConstants.MAX_SPEED) return false;
            if (Velocity.y <= -WorldConstants.MAX_SPEED) return false;
            if (Velocity.z <= -WorldConstants.MAX_SPEED) return false;

            return true;
        }

        public virtual bool IsPlayer()
        {
            return false;
        }

        public virtual bool IsNetClient()
        {
            return false;
        }

        public virtual string TeamID()
        {
            return "";
        }

        public virtual BaseEntity GetNextTarget()
        {
            if (string.IsNullOrEmpty(Target))
            {
                return null;
            }

            return EntUtils.FindEntityByTargetName(null, Target);
        }

        public virtual void Think()
        {
            m_pfnThink?.Invoke();
        }

        public virtual void Touch(BaseEntity pOther)
        {
            m_pfnTouch?.Invoke(pOther);
        }

        public virtual void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            m_pfnUse?.Invoke(pActivator, pCaller, useType, value);
        }

        public virtual void Blocked(BaseEntity pOther)
        {
            m_pfnBlocked?.Invoke(pOther);
        }

        /// <summary>
        /// Made this virtual
        /// </summary>
        public virtual void UpdateOnRemove()
        {
            if (0 != (Flags & EntFlags.Graphed))
            {
                // this entity was a LinkEnt in the world node graph, so we must remove it from
                // the graph since we are removing it from the world.
                for (var i = 0; i < Globals.WorldGraph.m_cLinks; ++i)
                {
                    if (Globals.WorldGraph.m_pLinkPool[i].LinkEntity == this)
                    {
                        // if this link has a link ent which is the same ent that is removing itself, remove it!
                        Globals.WorldGraph.m_pLinkPool[i].LinkEntity = null;
                    }
                }
            }

            if (!string.IsNullOrEmpty(GlobalName))
            {
                Globals.GlobalState.EntitySetState(GlobalName, GlobalEState.Dead);
            }
        }

        public void SUB_Remove()
        {
            UpdateOnRemove();
            if (Health > 0)
            {
                // this situation can screw up monsters who can't tell their entity pointers are invalid.
                Health = 0;
                Log.Alert(AlertType.AIConsole, "SUB_Remove called on entity with health > 0\n");
            }

            EntUtils.Remove(this);
        }

        /// <summary>
        /// Convenient way to explicitly do nothing (passed to functions that require a method)
        /// </summary>
        public void SUB_DoNothing()
        {
        }

        public void SUB_StartFadeOut()
        {

        }

        public void SUB_FadeOut()
        {

        }

        public void SUB_CallUseToggle()
        {
            Use(this, this, UseType.Toggle, 0);
        }

        public bool ShouldToggle(UseType useType, bool currentState)
        {
            if (useType != UseType.Toggle && useType != UseType.Set)
            {
                if ((currentState && useType == UseType.On) || (!currentState && useType == UseType.Off))
                {
                    return false;
                }
            }
            return true;
        }

        //TODO
        public void FireBullets(uint cShots, Vector vecSrc, Vector vecDirShooting, Vector vecSpread, float flDistance, int iBulletType, int iTracerFreq = 4, int iDamage = 0, BaseEntity attacker = null)
        {

        }

        public Vector FireBulletsPlayer(uint cShots, Vector vecSrc, Vector vecDirShooting, Vector vecSpread, float flDistance, int iBulletType, int iTracerFreq = 4, int iDamage = 0, BaseEntity attacker = null, int shared_rand = 0)
        {
            return WorldConstants.g_vecZero;
        }

        public virtual BaseEntity Respawn()
        {
            return null;
        }

        public virtual void SUB_UseTargets(BaseEntity pActivator, UseType useType, float value)
        {
            //
            // fire targets
            //
            EntUtils.FireTargets(Target, pActivator, this, useType, value);
        }

        //Do the bounding boxes of these two intersect?
        public bool Intersects(BaseEntity other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return other.AbsMin.x <= AbsMax.x
                 && other.AbsMin.y <= AbsMax.y
                 && other.AbsMin.z <= AbsMax.z
                 && other.AbsMax.x >= AbsMin.x
                 && other.AbsMax.y >= AbsMin.y
                 && other.AbsMax.z >= AbsMin.z;
        }

        public void MakeDormant()
        {
            Flags |= EntFlags.Dormant;

            // Don't touch
            Solid = Solid.Not;
            // Don't move
            MoveType = MoveType.None;
            // Don't draw
            Effects |= EntityEffects.NoDraw;
            // Don't think
            SetNextThink(0);
            // Relink
            SetOrigin(Origin);
        }

        public bool IsDormant()
        {
            return (Flags & EntFlags.Dormant) != 0;
        }

        public virtual bool IsLockedByMaster()
        {
            return false;
        }

        public static BaseEntity Instance(Edict pent)
        {
            if (pent == null)
            {
                return World.WorldInstance;
            }

            return pent.TryGetEntity();
        }

        public BaseMonster GetMonsterPointer(Edict pentMonster)
        {
            BaseEntity pEntity = Instance(pentMonster);
            return pEntity?.MyMonsterPointer();
        }

        public virtual void UpdateOwner()
        {
        }

        public static BaseEntity Create(string szName, Vector vecOrigin, Vector vecAngles, BaseEntity owner = null)
        {
            var entity = Engine.EntityRegistry.CreateInstance(szName);

            if (entity == null)
            {
                Log.Alert(AlertType.Console, $"NULL Ent in Create {szName}!");
                return null;
            }

            entity.Owner = owner;
            entity.Origin = vecOrigin;
            entity.Angles = vecAngles;

            //TODO: handle elsewhere, handle removal of entity in spawn
            EntUtils.DispatchSpawn(entity.Edict());

            return entity;
        }

        public virtual bool BecomeProne()
        {
            return false;
        }

        public Edict Edict()
        {
            return pev.ContainingEntity;
        }

        public int EntIndex()
        {
            return EntUtils.EntIndex(Edict());
        }

        public virtual Vector Center()
        {
            return (AbsMax + AbsMin) * 0.5;
        }

        public virtual Vector EyePosition()
        {
            return Origin + ViewOffset;
        }

        public virtual Vector EarPosition()
        {
            return Origin + ViewOffset;
        }

        public virtual Vector BodyTarget(Vector posSrc)
        {
            return Center();
        }

        public virtual int Illumination()
        {
            return Engine.Entities.GetIllumination(Edict());
        }

        public virtual bool FVisible(BaseEntity pEntity)
        {
            return false;
        }

        public virtual bool FVisible(Vector vecOrigin)
        {
            return false;
        }

        public void EmitSound(SoundChannel channel, string sample, float volume = Volume.Normal, float attenuation = Attenuation.Normal, SoundFlags flags = SoundFlags.None, int pitch = Pitch.Normal)
        {
            Engine.Sound.EmitSound(Edict(), channel, sample, volume, attenuation, flags, pitch);
        }

        public void StopSound(SoundChannel channel, string sample)
        {
            Engine.Sound.StopSound(Edict(), channel, sample);
        }
    }
}