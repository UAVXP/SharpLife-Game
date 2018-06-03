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
using GoldSource.Server.Engine.Entities;
using GoldSource.Server.Game.Game.Entities;
using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Server.Game.Game.GlobalState;
using GoldSource.Server.Game.Utility.KeyValues;
using GoldSource.Shared.Entities;
using System;
using System.Collections.Generic;

namespace GoldSource.Server.Game.Game.API.Implementations
{
    internal class Entities : IEntities
    {
        private IEntityDictionary EntityDictionary { get; }

        private IEntityRegistry EntityRegistry { get; }

        public Entities(IEntityDictionary entityDictionary, IEntityRegistry entityRegistry)
        {
            EntityDictionary = entityDictionary ?? throw new ArgumentNullException(nameof(entityDictionary));
            EntityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));
        }

        public void LoadEntities(string data)
        {
            {
                var keyvalues = KeyValuesParser.ParseAll(data);

                //For diagnostics: index of the entity in the entity data
                var index = 0;

                foreach (var block in keyvalues)
                {
                    //Better error handling than the engine: if an entity couldn't be created, log it and keep going
                    try
                    {
                        LoadEntity(block, index);
                    }
                    catch (ArgumentException e)
                    {
                        Log.Message($"A problem occurred while creating entity {index}:");
                        Log.Exception(e);
                    }

                    ++index;
                }
            }

            //At this point we'll have a lot of garbage, so clean up as much as possible
            GC.Collect();
        }

        private string GetClassName(List<KeyValuePair<string, string>> block, int index)
        {
            var name = block.Find(kv => kv.Key == "classname");

            if (name.Key == null)
            {
                //The engine only handles this error if there is a classname key that the game doesn't handle
                throw new ArgumentException($"No classname for entity {index}");
            }

            if (string.IsNullOrWhiteSpace(name.Value))
            {
                throw new ArgumentException($"Classname for entity {index} is invalid");
            }

            return name.Value;
        }

        private void LoadEntity(List<KeyValuePair<string, string>> block, int index)
        {
            var className = GetClassName(block, index);

            var info = EntityRegistry.FindEntityByMapName(className);

            if (info == null)
            {
                throw new ArgumentException($"No entity class of name {className} exists");
            }

            BaseEntity entity = null;

            //The world always has edict 0, but alloc calls can never get that edict directly.
            if (index == 0)
            {
                entity = EntityRegistry.CreateInstance(info, EntityDictionary.Allocate(0));
            }
            else
            {
                entity = EntityRegistry.CreateInstance(info);
            }

            if (entity == null)
            {
                throw new ArgumentException($"Couldn't create instance of entity {className}");
            }

            //The entity can remove itself in Spawn, so keep the edict here to properly free it in case of problems
            var edict = entity.Edict();

            try
            {
                foreach (var kv in block)
                {
                    var key = kv.Key;
                    var value = kv.Value;

                    //Don't do this multiple times
                    if (key != "classname")
                    {
                        //The engine does not allow values with the same content as the classname to be passed
                        //No reason to impose this restriction here

                        CheckKeyValue(entity, ref key, ref value);

                        if (!KeyValueUtils.TrySetKeyValue(entity, info, key, value))
                        {
                            entity.KeyValue(key, value);
                        }
                    }
                }

                Log.Message($"Spawning entity {entity.ClassName} ({entity.GetType().FullName})");
                Spawn(entity.Edict());

                //TODO: can check if the entity is a template and do stuff here
            }
            catch (Exception)
            {
                //On failure always free the edict
                //This will also free the entity instance if it has been assigned
                EntityDictionary.Free(edict);

                throw;
            }
        }

        /// <summary>
        /// Checks a keyvalue for anything that needs to be converted
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        private void CheckKeyValue(BaseEntity entity, ref string key, ref string value)
        {
            // anglehack is to allow QuakeEd to write single scalar angles
            // and allow them to be turned into vectors. (FIXME...)
            if (key == "angle")
            {
                float.TryParse(value, out var floatValue);

                if (floatValue >= 0)
                {
                    value = $"{entity.Angles.x} {floatValue} {entity.Angles.z}";
                }
                else
                {
                    if (Math.Floor(floatValue) == -1)
                    {
                        value = "-90 0 0";
                    }
                    else
                    {
                        value = "90 0 0";
                    }
                }

                key = "angles";
            }
        }

        public void OnFreeEntPrivateData(Edict pEnt)
        {
            if (pEnt.PrivateData != null)
            {
                //Inform of destruction
                pEnt.Entity().OnDestroy();
                //Mark the private data as freed, and set the managed instance to be garbage collected
                pEnt.PrivateData = null;
            }
        }

        public int Spawn(Edict pent)
        {
            var pEntity = pent.Entity();

            if (pEntity != null)
            {
                // Initialize these or entities who don't link to the world won't have anything in here
                pEntity.AbsMin = pEntity.Origin - new Vector(1, 1, 1);
                pEntity.AbsMax = pEntity.Origin + new Vector(1, 1, 1);

                pEntity.Spawn();

                // Try to get the pointer again, in case the spawn function deleted the entity.
                // UNDONE: Spawn() should really return a code to ask that the entity be deleted, but
                // that would touch too much code for me to do that right now.
                pEntity = pent.Entity();

                if (pEntity != null)
                {
                    if (!Engine.GameRules.IsAllowedToSpawn(pEntity))
                        return -1;  // return that this entity should be deleted
                    if (0 != (pEntity.Flags & EntFlags.KillMe))
                        return -1;
                }

                // Handle global stuff here
                if (pEntity != null && !string.IsNullOrEmpty(pEntity.GlobalName))
                {
                    var pGlobal = Globals.GlobalState.EntityFromTable(pEntity.GlobalName);
                    if (pGlobal != null)
                    {
                        // Already dead? delete
                        if (pGlobal.State == GlobalEState.Dead)
                            return -1;
                        else if (Engine.Globals.MapName != pGlobal.LevelName)
                            pEntity.MakeDormant();  // Hasn't been moved to this level yet, wait but stay alive
                                                    // In this level & not dead, continue on as normal
                    }
                    else
                    {
                        // Spawned entities default to 'On'
                        Globals.GlobalState.EntityAdd(pEntity.GlobalName, Engine.Globals.MapName, GlobalEState.On);
                        //				Log.Alert(AlertType.Console, $"Added global entity {pEntity.ClassName} ({pEntity.GlobalName})\n");
                    }
                }
            }

            return 0;
            //TODO: define return codes
        }

        public void Think(Edict pent)
        {
            var thinker = pent.Entity();

            if (thinker != null)
            {
                if ((thinker.Flags & EntFlags.Dormant) != 0)
                    Log.Alert(AlertType.Error, $"Dormant entity {thinker.ClassName} is thinking!!");

                thinker.Think();
            }
        }

        public void Use(Edict pentUsed, Edict pentOther)
        {
            var used = pentUsed.Entity();

            var other = pentOther.Entity();

            if (used != null && (used.Flags & EntFlags.KillMe) == 0)
            {
                used.Use(other, other, UseType.Toggle, 0);
            }
        }

        // HACKHACK -- this is a hack to keep the node graph entity from "touching" things (like triggers)
        // while it builds the graph
        public bool TouchDisabled { get; }

        public void Touch(Edict pentTouched, Edict pentOther)
        {
            if (TouchDisabled)
            {
                return;
            }

            var touched = pentTouched.Entity();

            var other = pentOther.Entity();

            if (touched != null && other != null && ((touched.Flags | other.Flags) & EntFlags.KillMe) == 0)
            {
                touched.Touch(other);
            }
        }

        public void Blocked(Edict pentBlocked, Edict pentOther)
        {
            var blocked = pentBlocked.Entity();

            var other = pentOther.Entity();

            blocked?.Blocked(other);
        }

        public void KeyValue(Edict pentKeyvalue, KeyValueData pkvd)
        {
            //Log.Message($"Entity {EntityDictionary.EntityIndex(pentKeyvalue)}/{EntityDictionary.Max} KeyValue ClassName=\"{pkvd.ClassName}\" Key=\"{pkvd.KeyName}\" Value=\"{pkvd.Value}\"");

            if (pkvd.KeyName == "classname")
            {
                if (pentKeyvalue.PrivateData == null)
                {
                    Log.Message($"Creating entity \"{pkvd.Value}\"");

                    //Create the entity instance
                    EntityRegistry.CreateInstance<BaseEntity>(pentKeyvalue);

                    Log.Message("Created entity");
                }
                else
                {
                    var ent = (BaseEntity)pentKeyvalue.PrivateData;

                    //This should never happen
                    if (pkvd.Value != ent.ClassName)
                    {
                        throw new InvalidOperationException($"Second occurence of classname keyvalue has different value (Expected: {ent.ClassName}, actual:{pkvd.Value})");
                    }

                    pkvd.Handled = true;
                    return;
                }
            }

            if (pentKeyvalue.PrivateData == null)
            {
                throw new InvalidOperationException($"Cannot set keyvalue \"{pkvd.KeyName}={pkvd.Value}\" on null entity of class {pkvd.ClassName}");
            }

            pkvd.Handled = false;

            var entity = (BaseEntity)pentKeyvalue.PrivateData;

            //TODO: uniformly handle keyvalue initialization

            switch (pkvd.KeyName)
            {
                case "classname":
                    {
                        entity.ClassName = pkvd.Value;
                        pkvd.Handled = true;
                        break;
                    }
            }
        }

        public void SetAbsBox(Edict pent)
        {
            var entity = pent.TryGetEntity();

            if (entity == null)
            {
                throw new InvalidOperationException($"Error: Entity \"{pent.Vars.ClassName}\" (index {EntUtils.EntIndex(pent)}) has no entity instance assigned for SetAbsBox call");
            }

            entity.SetObjectCollisionBox();
        }

        public bool ShouldCollide(Edict pentTouched, Edict pentOther)
        {
            return true;
        }
    }
}
