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
using GoldSource.Server.Game.Game.GlobalState;
using GoldSource.Server.Game.Utility;
using GoldSource.Shared.Engine;
using GoldSource.Shared.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GoldSource.Server.Game.Game.Entities
{
    public static class EntUtils
    {
        public static IEntityDictionary EntityDictionary { get; private set; }

        private static Dictionary<string, PropertyInfo> EntityProperties { get; set; }

        /// <summary>
        /// Initializes the utility code
        /// </summary>
        /// <param name="entityDictionary"></param>
        public static void Initialize(IEntityDictionary entityDictionary)
        {
            EntityDictionary = entityDictionary ?? throw new ArgumentNullException(nameof(entityDictionary));

            //Cache properties for lookup
            //Case insensitive
            EntityProperties = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in typeof(BaseEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.PropertyType == typeof(string))
                {
                    EntityProperties.Add(prop.Name, prop);
                }
            }
        }

        public static BaseEntity EntityByIndex(int index)
        {
            //TODO: could throw here
            if (index < 0 || index >= EntityDictionary.Max)
            {
                return null;
            }

            var edict = EntityDictionary.EdictByIndex(index);

            return edict.TryGetEntity();
        }

        /// <summary>
        /// Gets the first entity that exists at or after the given index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static BaseEntity FirstEntityAtIndex(int index)
        {
            for (; index < EntityDictionary.Max; ++index)
            {
                var entity = EntityByIndex(index);

                if (entity != null)
                {
                    return entity;
                }
            }

            return null;
        }

        /// <summary>
        /// Accumulates entities into a list from a function that returns entities in order
        /// </summary>
        /// <param name="nextFunc">Function that takes the entity to start searching after, returns the next entity or null if there are no more entities</param>
        /// <returns></returns>
        public static List<BaseEntity> AccumulateEntities(Func<BaseEntity, BaseEntity> nextFunc)
        {
            var list = new List<BaseEntity>();

            for (BaseEntity entity = null; (entity = nextFunc(entity)) != null;)
            {
                list.Add(entity);
            }

            return list;
        }

        public static BaseEntity FindEntityByString(BaseEntity entStartAfter, string key, string value)
        {
            for (var index = entStartAfter != null ? EntityDictionary.EntityIndex(entStartAfter.Edict()) + 1 : 0; index < EntityDictionary.Max;)
            {
                var entity = FirstEntityAtIndex(index);

                if (entity == null)
                {
                    return null;
                }

                if (EntityProperties.TryGetValue(key, out var prop))
                {
                    var propValue = prop.GetValue(entity);

                    if (value == (string)propValue)
                    {
                        return entity;
                    }
                }

                index = EntityDictionary.EntityIndex(entity.Edict()) + 1;
            }

            return null;
        }

        public static BaseEntity FindEntityByClassName(BaseEntity entStartAfter, string name)
        {
            return FindEntityByString(entStartAfter, "classname", name);
        }

        public static BaseEntity FindEntityByTargetName(BaseEntity entStartAfter, string name)
        {
            return FindEntityByString(entStartAfter, "targetname", name);
        }

        public static BaseEntity FindEntityByTarget(BaseEntity entStartAfter, string name)
        {
            return FindEntityByString(entStartAfter, "target", name);
        }

        public static BaseEntity FindEntityInSphere(BaseEntity entStartAfter, in Vector org, float rad)
        {
            float radiusSquared = rad * rad;

            for (var index = entStartAfter != null ? EntityDictionary.EntityIndex(entStartAfter.Edict()) + 1 : 0; index < EntityDictionary.Max;)
            {
                var entity = FirstEntityAtIndex(index);

                if (entity == null)
                {
                    return null;
                }

                // Now X
                var delta = org.x - ((entity.AbsMin.x + entity.AbsMax.x) * 0.5f);
                delta *= delta;

                if (delta <= radiusSquared)
                {
                    var distance = delta;

                    // Now Y
                    delta = org.y - ((entity.AbsMin.y + entity.AbsMax.y) * 0.5f);
                    delta *= delta;

                    distance += delta;
                    if (distance <= radiusSquared)
                    {
                        // Now Z
                        delta = org.z - ((entity.AbsMin.z + entity.AbsMax.z) * 0.5f);
                        delta *= delta;

                        distance += delta;
                        if (distance <= radiusSquared)
                        {
                            return entity;
                        }
                    }
                }

                index = EntityDictionary.EntityIndex(entity.Edict()) + 1;
            }

            return null;
        }

        public static BaseEntity FindEntityGeneric(string szName, Vector vecSrc, float flRadius)
        {
            BaseEntity pEntity = FindEntityByTargetName(null, szName);
            if (pEntity != null)
            {
                return pEntity;
            }

            var flMaxDist2 = flRadius * flRadius;
            for (BaseEntity pSearch = null; (pSearch = FindEntityByClassName(pSearch, szName)) != null;)
            {
                var flDist2 = (pSearch.Origin - vecSrc).Length();
                flDist2 *= flDist2;
                if (flMaxDist2 > flDist2)
                {
                    pEntity = pSearch;
                    flMaxDist2 = flDist2;
                }
            }
            return pEntity;
        }

        public static BaseEntity FindGlobalEntity(string className, string globalName)
        {
            var entity = FindEntityByString(null, "globalname", globalName);

            if (entity != null && entity.ClassName != className)
            {
                Log.Alert(AlertType.Console, $"Global entity found {globalName}, wrong class {entity.ClassName}");
                entity = null;
            }

            return entity;
        }

        public static BaseEntity FindEntityForward(BaseEntity me)
        {
            MathUtils.MakeVectors(me.ViewAngle);
            Trace.Line(me.Origin + me.ViewOffset, me.Origin + me.ViewOffset + (Engine.Globals.ForwardVector * 8192), TraceFlags.None, me.Edict(), out var tr);

            return (tr.Fraction != 1.0) ? tr.Hit.TryGetEntity() : null;
        }

        public static BaseEntity EntitiesInPVS(BaseEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return Engine.Server.EntitiesInPVS(entity.Edict())?.Entity();
        }

        public static int EntIndex(Edict pEdict)
        {
            return Engine.EntityDictionary.EntityIndex(pEdict);
        }

        public static BaseEntity IndexEnt(int edictNum)
        {
            var edict = Engine.EntityDictionary.EdictByIndex(edictNum);

            return edict?.TryGetEntity();
        }

        public static T IndexEnt<T>(int edictNum)
            where T : BaseEntity
        {
            var edict = Engine.EntityDictionary.EdictByIndex(edictNum);

            return edict?.TryGetEntity<T>();
        }

        public static void Remove(BaseEntity pEntity)
        {
            if (pEntity == null)
            {
                return;
            }

            pEntity.UpdateOnRemove();
            pEntity.Flags |= EntFlags.KillMe;
            pEntity.TargetName = string.Empty;
        }

        //TODO: duplicate of Spawn in Entities
        public static int DispatchSpawn(Edict pent)
        {
            var pEntity = pent?.TryGetEntity();

            if (pEntity != null)
            {
                // Initialize these or entities who don't link to the world won't have anything in here
                pEntity.AbsMin = pEntity.Origin - new Vector(1, 1, 1);
                pEntity.AbsMax = pEntity.Origin + new Vector(1, 1, 1);

                pEntity.Spawn();

                // Try to get the pointer again, in case the spawn function deleted the entity.
                // UNDONE: Spawn() should really return a code to ask that the entity be deleted, but
                // that would touch too much code for me to do that right now.
                pEntity = pent.TryGetEntity();

                if (pEntity != null)
                {
                    if (!Engine.GameRules.IsAllowedToSpawn(pEntity))
                    {
                        return -1;  // return that this entity should be deleted
                    }

                    if (0 != (pEntity.Flags & EntFlags.KillMe))
                    {
                        return -1;
                    }
                }

                // Handle global stuff here
                if (pEntity != null && !string.IsNullOrEmpty(pEntity.GlobalName))
                {
                    var pGlobal = Globals.GlobalState.EntityFromTable(pEntity.GlobalName);
                    if (pGlobal != null)
                    {
                        // Already dead? delete
                        if (pGlobal.State == GlobalEState.Dead)
                        {
                            return -1;
                        }
                        else if (Engine.Globals.MapName != pGlobal.LevelName)
                        {
                            pEntity.MakeDormant();  // Hasn't been moved to this level yet, wait but stay alive
                        }
                        // In this level & not dead, continue on as normal
                    }
                    else
                    {
                        // Spawned entities default to 'On'
                        Globals.GlobalState.EntityAdd(pEntity.GlobalName, Engine.Globals.MapName, GlobalEState.On);
                        //				Log.Alert( AlertType.Console, $"Added global entity {pEntity.ClassName} ({pEntity.GlobalName})\n");
                    }
                }
            }

            return 0;
        }

        public static bool ShouldShowBlood(BloodColor color)
        {
            if (color != BloodColor.DontBleed)
            {
                if (color == BloodColor.Red)
                {
                    if (CVar.GetFloat("violence_hblood") != 0)
                    {
                        return true;
                    }
                }
                else
                {
                    if (CVar.GetFloat("violence_ablood") != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Vector RandomBloodVector()
        {
            return new Vector(
                EngineRandom.Float(-1, 1),
                EngineRandom.Float(-1, 1),
                EngineRandom.Float(0, 1)
                );
        }

        public static void BloodDrips(Vector origin, Vector direction, BloodColor color, int amount)
        {
            if (!ShouldShowBlood(color))
            {
                return;
            }

            if (color == BloodColor.DontBleed || amount == 0)
            {
                return;
            }

            if (Globals.Language == Language.German && color == BloodColor.Red)
            {
                color = 0;
            }

            if (Engine.GameRules.IsMultiplayer())
            {
                // scale up blood effect in multiplayer for better visibility
                amount *= 2;
            }

            if (amount > 255)
            {
                amount = 255;
            }

            var message = NetMessage.Begin(MsgDest.PVS, ServerCommand.TempEntity, origin);
            message.WriteByte((int)TempEntityMsg.BloodStream);
            message.WriteCoord(origin.x);                              // pos
            message.WriteCoord(origin.y);
            message.WriteCoord(origin.z);
            message.WriteShort(Globals.g_sModelIndexBloodSpray);              // initial sprite model
            message.WriteShort(Globals.g_sModelIndexBloodDrop);               // droplet sprite models
            message.WriteByte((int)color);                             // color index into host_basepal
            message.WriteByte(Math.Min(Math.Max(3, amount / 10), 16));       // size
            message.End();
        }

        public static void SpawnBlood(Vector vecSpot, BloodColor bloodColor, float flDamage)
        {
            BloodDrips(vecSpot, Globals.g_vecAttackDir, bloodColor, (int)flDamage);
        }

        public static void BloodDecalTrace(in TraceResult trace, BloodColor bloodColor)
        {
            if (ShouldShowBlood(bloodColor))
            {
                if (bloodColor == BloodColor.Red)
                {
                    DecalTrace(trace, Decal.Blood1 + EngineRandom.Long(0, 5));
                }
                else
                {
                    DecalTrace(trace, Decal.YBlood1 + EngineRandom.Long(0, 5));
                }
            }
        }

        public static void DecalTrace(in TraceResult trace, Decal decalNumber)
        {
            if (decalNumber < 0)
                return;

            var index = Globals.gDecals[(int)decalNumber].index;

            if (index < 0)
                return;

            if (trace.Fraction == 1.0)
                return;

            short entityIndex;

            // Only decal BSP models
            if (trace.Hit != null)
            {
                var pEntity = BaseEntity.Instance(trace.Hit);
                if (pEntity?.IsBSPModel() == false)
                {
                    return;
                }

                entityIndex = (short)pEntity.EntIndex();
            }
            else
            {
                entityIndex = 0;
            }

            var teMessage = TempEntityMsg.Decal;
            if (entityIndex != 0)
            {
                if (index > 255)
                {
                    teMessage = TempEntityMsg.DecalHigh;
                    index -= 256;
                }
            }
            else
            {
                teMessage = TempEntityMsg.WorldDecal;
                if (index > 255)
                {
                    teMessage = TempEntityMsg.WorldDecalHigh;
                    index -= 256;
                }
            }

            var message = NetMessage.Begin(MsgDest.Broadcast, ServerCommand.TempEntity);
            message.WriteByte((int)teMessage);
            message.WriteCoord(trace.EndPos.x);
            message.WriteCoord(trace.EndPos.y);
            message.WriteCoord(trace.EndPos.z);
            message.WriteByte(index);
            if (0 != entityIndex)
                message.WriteShort(entityIndex);
            message.End();
        }

        public static Vector BrushModelOrigin(BaseEntity bModel)
        {
            return bModel.AbsMin + (bModel.Size * 0.5f);
        }

        public static void FireTargets(string targetName, BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                return;
            }

            Log.Alert(AlertType.AIConsole, $"Firing: ({targetName})\n");

            for (BaseEntity target = null; (target = FindEntityByTargetName(target, targetName)) != null;)
            {
                if (target != null && (target.Flags & EntFlags.KillMe) == 0)  // Don't use dying ents
                {
                    Log.Alert(AlertType.AIConsole, $"Found: {target.ClassName}, firing ({targetName})\n");
                    target.Use(pActivator, pCaller, useType, value);
                }
            }
        }

        public static bool IsPointEntity(BaseEntity pEnt)
        {
            if (pEnt.ModelIndex == 0)
            {
                return true;
            }

            if (pEnt.ClassName == "info_target" || pEnt.ClassName == "info_landmark" || pEnt.ClassName == "path_corner")
            {
                return true;
            }

            return false;
        }

        public static bool IsValidEntity(BaseEntity entity)
        {
            return entity != null && (entity.Flags & EntFlags.KillMe) == 0;
        }

        public static bool IsMasterTriggered(string sMaster, BaseEntity pActivator)
        {
            if (!string.IsNullOrEmpty(sMaster))
            {
                var master = FindEntityByTargetName(null, sMaster);

                if (master != null && (master.ObjectCaps() & EntityCapabilities.Master) != 0)
                {
                    return master.IsTriggered(pActivator);
                }

                Log.Alert(AlertType.Console, "Master was null or not a master!");
            }

            // if this isn't a master entity, just say yes.
            return true;
        }

        public static void SetMovedir(BaseEntity entity)
        {
            if (entity.Angles == new Vector(0, -1, 0))
            {
                entity.MoveDirection = new Vector(0, 0, 1);
            }
            else if (entity.Angles == new Vector(0, -2, 0))
            {
                entity.MoveDirection = new Vector(0, 0, -1);
            }
            else
            {
                MathUtils.MakeVectors(entity.Angles);
                entity.MoveDirection = Engine.Globals.ForwardVector;
            }

            entity.Angles = WorldConstants.g_vecZero;
        }

        public static void StripToken(string key, out string dest)
        {
            var index = key.IndexOf('#');

            dest = key.Substring(0, -1 != index ? index : key.Length);
        }

        public static bool IsFacing(BaseEntity entity, in Vector reference)
        {
            var vecDir = reference - entity.Origin;
            vecDir.z = 0;
            vecDir = vecDir.Normalize();
            var angle = entity.ViewAngle;
            angle.x = 0;
            MathUtils.MakeVectorsPrivate(angle, out var forward, out var _, out var _);
            // He's facing me, he meant it
            // +/- 15 degrees or so
            return forward.DotProduct(vecDir) > 0.96;
        }

        public static Vector CheckSplatToss(BaseEntity entity, in Vector vecSpot1, in Vector vecSpot2, float maxHeight)
        {
            var gravity = Globals.g_psv_gravity.Float;

            // calculate the midpoint and apex of the 'triangle'
            // halfway point between Spot1 and Spot2
            var vecMidPoint = vecSpot1 + ((vecSpot2 - vecSpot1) * 0.5f);
            Trace.Line(vecMidPoint, vecMidPoint + new Vector(0, 0, maxHeight), TraceFlags.IgnoreMonsters, entity.Edict(), out var tr);
            // highest point
            var vecApex = tr.EndPos;

            Trace.Line(vecSpot1, vecApex, TraceFlags.None, entity.Edict(), out tr);
            if (tr.Fraction != 1.0)
            {
                // fail!
                return WorldConstants.g_vecZero;
            }

            // Don't worry about actually hitting the target, this won't hurt us!

            // How high should the grenade travel (subtract 15 so the grenade doesn't hit the ceiling)?
            var height = (vecApex.z - vecSpot1.z) - 15;
            // How fast does the grenade need to travel to reach that height given gravity?
            var speed = (float)Math.Sqrt(2 * gravity * height);

            // How much time does it take to get there?
            var time = speed / gravity;
            var vecGrenadeVel = (vecSpot2 - vecSpot1);
            vecGrenadeVel.z = 0;
            var distance = vecGrenadeVel.Length();

            // Travel half the distance to the target in that time (apex is at the midpoint)
            vecGrenadeVel *= (0.5 / time);
            // Speed to offset gravity at the desired height
            vecGrenadeVel.z = speed;

            return vecGrenadeVel;
        }

        public static Vector CheckToss(BaseEntity entity, Vector vecSpot1, Vector vecSpot2, float flGravityAdj)
        {
            var flGravity = Globals.g_psv_gravity.Float * flGravityAdj;

            if (vecSpot2.z - vecSpot1.z > 500)
            {
                // to high, fail
                return WorldConstants.g_vecZero;
            }

            MathUtils.MakeVectors(entity.Angles);

            // toss a little bit to the left or right, not right down on the enemy's bean (head). 
            vecSpot2 += Engine.Globals.RightVector * (EngineRandom.Float(-8, 8) + EngineRandom.Float(-16, 16));
            vecSpot2 += Engine.Globals.ForwardVector * (EngineRandom.Float(-8, 8) + EngineRandom.Float(-16, 16));

            // calculate the midpoint and apex of the 'triangle'
            // UNDONE: normalize any Z position differences between spot1 and spot2 so that triangle is always RIGHT

            // How much time does it take to get there?

            // halfway point between Spot1 and Spot2
            // get a rough idea of how high it can be thrown
            var vecMidPoint = vecSpot1 + ((vecSpot2 - vecSpot1) * 0.5f);
            Trace.Line(vecMidPoint, vecMidPoint + new Vector(0, 0, 500), TraceFlags.IgnoreMonsters, entity.Edict(), out var tr);
            vecMidPoint = tr.EndPos;
            // (subtract 15 so the grenade doesn't hit the ceiling)
            vecMidPoint.z -= 15;

            if (vecMidPoint.z < vecSpot1.z || vecMidPoint.z < vecSpot2.z)
            {
                // to not enough space, fail
                return WorldConstants.g_vecZero;
            }

            // How high should the grenade travel to reach the apex
            var distance1 = (vecMidPoint.z - vecSpot1.z);
            var distance2 = (vecMidPoint.z - vecSpot2.z);

            // How long will it take for the grenade to travel this distance
            var time1 = (float)Math.Sqrt(distance1 / (0.5 * flGravity));
            var time2 = (float)Math.Sqrt(distance2 / (0.5 * flGravity));

            if (time1 < 0.1)
            {
                // too close
                return WorldConstants.g_vecZero;
            }

            // how hard to throw sideways to get there in time.
            var vecGrenadeVel = (vecSpot2 - vecSpot1) / (time1 + time2);
            // how hard upwards to reach the apex at the right time.
            vecGrenadeVel.z = flGravity * time1;

            // highest point
            // find the apex
            var vecApex = vecSpot1 + (vecGrenadeVel * time1);
            vecApex.z = vecMidPoint.z;

            Trace.Line(vecSpot1, vecApex, TraceFlags.None, entity.Edict(), out tr);
            if (tr.Fraction != 1.0)
            {
                // fail!
                return WorldConstants.g_vecZero;
            }

            // UNDONE: either ignore monsters or change it to not care if we hit our enemy
            Trace.Line(vecSpot2, vecApex, TraceFlags.IgnoreMonsters, entity.Edict(), out tr);
            if (tr.Fraction != 1.0)
            {
                // fail!
                return WorldConstants.g_vecZero;
            }

            return vecGrenadeVel;
        }

        public static Vector CheckThrow(BaseEntity entity, Vector vecSpot1, Vector vecSpot2, float flSpeed, float flGravityAdj)
        {
            var flGravity = Globals.g_psv_gravity.Float * flGravityAdj;

            var vecGrenadeVel = (vecSpot2 - vecSpot1);

            // throw at a constant time
            var time = vecGrenadeVel.Length() / flSpeed;
            vecGrenadeVel *= (1.0 / time);

            // adjust upward toss to compensate for gravity loss
            vecGrenadeVel.z += flGravity * time * 0.5f;

            var vecApex = vecSpot1 + ((vecSpot2 - vecSpot1) * 0.5);
            vecApex.z += 0.5f * flGravity * (time * 0.5f) * (time * 0.5f);

            Trace.Line(vecSpot1, vecApex, TraceFlags.None, entity.Edict(), out var tr);
            if (tr.Fraction != 1.0)
            {
                // fail!
                return WorldConstants.g_vecZero;
            }

            Trace.Line(vecSpot2, vecApex, TraceFlags.IgnoreMonsters, entity.Edict(), out tr);
            if (tr.Fraction != 1.0)
            {
                // fail!
                return WorldConstants.g_vecZero;
            }

            return vecGrenadeVel;
        }

        public static Vector VelocityForDamage(float flDamage)
        {
            var vec = new Vector(EngineRandom.Float(-100, 100), EngineRandom.Float(-100, 100), EngineRandom.Float(200, 300));

            if (flDamage > -50)
            {
                vec *= 0.7;
            }
            else if (flDamage > -200)
            {
                vec *= 2;
            }
            else
            {
                vec *= 10;
            }

            return vec;
        }

        public static bool EntIsVisible(BaseEntity entity, BaseEntity target)
        {
            var vecSpot1 = entity.Origin + entity.ViewOffset;
            var vecSpot2 = target.Origin + target.ViewOffset;

            Trace.Line(vecSpot1, vecSpot2, TraceFlags.IgnoreMonsters, entity.Edict(), out var tr);

            if (tr.InOpen && tr.InWater)
            {
                return false;                   // sight line crossed contents
            }

            return tr.Fraction == 1;
        }

        public static bool BoxVisible(BaseEntity looker, BaseEntity target, out Vector vecTargetOrigin, float flSize)
        {
            // don't look through water
            if ((looker.WaterLevel != WaterLevel.Head && target.WaterLevel == WaterLevel.Head)
                || (looker.WaterLevel == WaterLevel.Head && target.WaterLevel == WaterLevel.Dry))
            {
                vecTargetOrigin = new Vector();
                return false;
            }

            var vecLookerOrigin = looker.Origin + looker.ViewOffset;//look through the monster's 'eyes'
            for (var i = 0; i < 5; ++i)
            {
                var vecTarget = target.Origin;
                vecTarget.x += EngineRandom.Float(target.Mins.x + flSize, target.Maxs.x - flSize);
                vecTarget.y += EngineRandom.Float(target.Mins.y + flSize, target.Maxs.y - flSize);
                vecTarget.z += EngineRandom.Float(target.Mins.z + flSize, target.Maxs.z - flSize);

                Trace.Line(vecLookerOrigin, vecTarget, TraceFlags.IgnoreMonsters | TraceFlags.IgnoreGlass, looker.Edict(), out var tr);

                if (tr.Fraction == 1.0)
                {
                    vecTargetOrigin = vecTarget;
                    return true;// line of sight is valid.
                }
            }

            vecTargetOrigin = new Vector();

            return false;// Line of sight is not established
        }

        public static Contents PointContents(in Vector vec)
        {
            return Engine.Server.PointContents(vec);
        }

        public static void RadiusDamage(Vector src, BaseEntity inflictor, BaseEntity attacker, float damage, float radius, EntityClass classIgnore, DamageTypes damageTypes)
        {
            var falloff = (0 != radius) ? damage / radius : 1.0f;

            var inWater = PointContents(src) == Contents.Water;

            ++src.z;// in case grenade is lying on the ground

            if (attacker == null)
            {
                attacker = inflictor;
            }

            // iterate on all entities in the vicinity.
            for (BaseEntity entity = null; (entity = FindEntityInSphere(entity, src, radius)) != null;)
            {
                if (entity.TakeDamageState != TakeDamageState.No)
                {
                    // UNDONE: this should check a damage mask, not an ignore
                    if (classIgnore != EntityClass.None && entity.Classify() == classIgnore)
                    {// houndeyes don't hurt other houndeyes with their attack
                        continue;
                    }

                    // blast's don't tavel into or out of water
                    if (inWater && entity.WaterLevel == WaterLevel.Dry)
                    {
                        continue;
                    }

                    if (!inWater && entity.WaterLevel == WaterLevel.Head)
                    {
                        continue;
                    }

                    var spot = entity.BodyTarget(src);

                    Trace.Line(src, spot, TraceFlags.None, inflictor.Edict(), out var tr);

                    if (tr.Fraction == 1.0 || tr.Hit == entity.Edict())
                    {// the explosion can 'see' this entity, so hurt them!
                        if (tr.StartSolid)
                        {
                            // if we're stuck inside them, fixup the position and distance
                            tr.EndPos = src;
                            tr.Fraction = 0.0f;
                        }

                        // decrease damage for an ent that's farther from the bomb.
                        var adjustedDamage = (src - tr.EndPos).Length() * falloff;
                        adjustedDamage = Math.Max(0, damage - adjustedDamage);

                        // Log.Alert(AlertType.Console, $"hit {entity.ClassName}\n");
                        if (tr.Fraction != 1.0)
                        {
                            Globals.MultiDamage.Clear();
                            entity.TraceAttack(inflictor, adjustedDamage, (tr.EndPos - src).Normalize(), ref tr, damageTypes);
                            Globals.MultiDamage.ApplyMultiDamage(inflictor, attacker);
                        }
                        else
                        {
                            entity.TakeDamage(inflictor, attacker, adjustedDamage, damageTypes);
                        }
                    }
                }
            }
        }

        public static bool TeamsMatch(string teamName1, string teamName2)
        {
            // Everyone matches unless it's teamplay
            if (!Engine.GameRules.IsTeamplay())
            {
                return true;
            }

            // Both on a team?
            if (teamName1.Length > 0 && teamName2.Length > 0)
            {
                if (teamName1 == teamName2)   // Same Team?
                {
                    return true;
                }
            }

            return false;
        }

        public static float CalculateWaterLevel(in Vector position, float minz, float maxz)
        {
            var midUp = position;
            midUp.z = minz;

            if (PointContents(midUp) != Contents.Water)
            {
                return minz;
            }

            midUp.z = maxz;
            if (PointContents(midUp) == Contents.Water)
            {
                return maxz;
            }

            float diff = maxz - minz;
            while (diff > 1.0f)
            {
                midUp.z = minz + (diff / 2.0f);
                if (PointContents(midUp) == Contents.Water)
                {
                    minz = midUp.z;
                }
                else
                {
                    maxz = midUp.z;
                }
                diff = maxz - minz;
            }

            return midUp.z;
        }

        public static void PrecacheOther(string szClassname)
        {
            var entity = Engine.EntityRegistry.CreateInstance(szClassname);

            if (entity == null)
            {
                Log.Alert(AlertType.Console, $"null Ent {szClassname} in UTIL_PrecacheOther\n");
                return;
            }

            entity.Precache();
            //TODO: use a cleaner way
            Engine.EntityDictionary.Free(entity.Edict());
        }

        public static int EntitiesInBox(IList<BaseEntity> list, int listMax, in Vector mins, in Vector maxs, EntFlags flagMask)
        {
            int count = 0;

            for (int i = 1; i < Engine.Globals.MaxEntities; ++i)
            {
                var entity = IndexEnt(i);

                if (entity == null)    // Not in use
                {
                    continue;
                }

                if (flagMask != EntFlags.None && (entity.Flags & flagMask) == 0)   // Does it meet the criteria?
                {
                    continue;
                }

                if (mins.x > entity.AbsMax.x
                    || mins.y > entity.AbsMax.y
                     || mins.z > entity.AbsMax.z
                     || maxs.x < entity.AbsMin.x
                     || maxs.y < entity.AbsMin.y
                     || maxs.z < entity.AbsMin.z)
                {
                    continue;
                }

                list.Add(entity);
                ++count;

                if (count >= listMax)
                {
                    return count;
                }
            }

            return count;
        }

        public static int MonstersInSphere(IList<BaseEntity> list, int listMax, in Vector center, float radius)
        {
            int count = 0;
            var radiusSquared = radius * radius;

            for (int i = 1; i < Engine.Globals.MaxEntities; ++i)
            {
                var entity = IndexEnt(i);

                if (entity == null)    // Not in use
                {
                    continue;
                }

                if ((entity.Flags & (EntFlags.Client | EntFlags.Monster)) == 0)   // Not a client/monster ?
                {
                    continue;
                }

                // Use origin for X & Y since they are centered for all monsters
                // Now X
                var delta = center.x - entity.Origin.x;//(pEdict.v.absmin.x + pEdict.v.absmax.x)*0.5f;
                delta *= delta;

                if (delta > radiusSquared)
                {
                    continue;
                }

                var distance = delta;

                // Now Y
                delta = center.y - entity.Origin.y;//(pEdict.v.absmin.y + pEdict.v.absmax.y)*0.5f;
                delta *= delta;

                distance += delta;
                if (distance > radiusSquared)
                {
                    continue;
                }

                // Now Z
                delta = center.z - ((entity.AbsMin.z + entity.AbsMax.z) * 0.5f);
                delta *= delta;

                distance += delta;
                if (distance > radiusSquared)
                {
                    continue;
                }

                list.Add(entity);
                ++count;

                if (count >= listMax)
                {
                    return count;
                }
            }

            return count;
        }

        public static void DecalGunshot(in TraceResult trace, Bullet bulletType)
        {
            // Is the entity valid
            if (!IsValidEntity(trace.Hit.TryGetEntity()))
            {
                return;
            }

            var entity = trace.Hit.TryGetEntity();

            if (entity.Solid == Solid.BSP || entity.MoveType == MoveType.PushStep)
            {
                // Decal the wall with a gunshot
                switch (bulletType)
                {
                    case Bullet.Player9MM:
                    case Bullet.Monster9MM:
                    case Bullet.PlayerMP5:
                    case Bullet.MonsterMP5:
                    case Bullet.PlayerBuckShot:
                    case Bullet.Player357:
                    default:
                        // smoke and decal
                        TempEntity.GunshotDecalTrace(trace, DamageDecal(entity, DamageTypes.Bullet));
                        break;
                    case Bullet.Monster12MM:
                        // smoke and decal
                        TempEntity.GunshotDecalTrace(trace, DamageDecal(entity, DamageTypes.Bullet));
                        break;
                    case Bullet.PlayerCrowbar:
                        // wall decal
                        DecalTrace(trace, DamageDecal(entity, DamageTypes.Club));
                        break;
                }
            }
        }

        public static Decal DamageDecal(BaseEntity entity, DamageTypes damageType)
        {
            if (entity == null)
            {
                return Decal.Gunshot1 + EngineRandom.Long(0, 4);
            }

            return entity.DamageDecal(damageType);
        }

        public static void ParticleEffect(in Vector vecOrigin, in Vector vecDirection, uint ulColor, uint ulCount)
        {
            Engine.Server.ParticleEffect(vecOrigin, vecDirection, ulColor, ulCount);
        }

        public static void MoveToOrigin(Edict entity, in Vector goal, float dist, MoveToOrigin moveType)
        {
            Engine.Server.MoveToOrigin(entity, goal, dist, moveType);
        }

        public static bool IsSoundEvent(AnimationEventId eventNumber)
        {
            return eventNumber == AnimationEventId.ScriptSound || eventNumber == AnimationEventId.ScriptSoundVoice;
        }

        public static void PlayCDTrack(int track)
        {
            // manually find the single player. 
            //TODO: adjust so all players get this
            var pClient = IndexEnt(1);

            // Can't play if the client is not connected!
            if (pClient == null)
            {
                return;
            }

            if (track < -1 || track > 30)
            {
                Log.Alert(AlertType.Console, $"TriggerCDAudio - Track {track} out of range\n");
                return;
            }

            if (track == -1)
            {
                CVar.EngineCVar.ClientCommand(pClient.Edict(), "cd stop\n");
            }
            else
            {
                CVar.EngineCVar.ClientCommand(pClient.Edict(), $"cd play {track:3}\n");
            }
        }
    }
}
