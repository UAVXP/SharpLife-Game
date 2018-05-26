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

using Server.Game.Entities.MetaData;
using Server.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using Server.Engine;
using System.Diagnostics;
using System.Linq;

namespace Server.Game.Entities.Triggers
{
    /// <summary>
    /// When the player touches this, he gets sent to the map listed in the "map" variable.  Unless the NO_INTERMISSION flag is set, the view will go to the info_intermission spot and display stats
    /// </summary>
    [LinkEntityToClass("trigger_changelevel")]
    public class ChangeLevel : BaseTrigger
    {
        new public static class SF
        {
            public const uint UseOnly = 0x0002;
        }

        /// <summary>
        /// Next map
        /// </summary>
        [KeyValue(KeyName = "map")]
        [Persist]
        public string MapName;

        /// <summary>
        /// Landmark on next map
        /// </summary>
        [KeyValue(KeyName = "landmark")]
        [Persist]
        public string LandmarkName;

        [KeyValue]
        [Persist]
        public string ChangeTarget;

        [KeyValue(KeyName = "changedelay")]
        [Persist]
        public float ChangeTargetDelay;

        public override void Spawn()
        {
            if (string.IsNullOrEmpty(MapName))
            {
                Log.Alert(AlertType.Console, "a trigger_changelevel doesn't have a map\n");
            }
            else if (Encoding.UTF8.GetByteCount(MapName) > Framework.MaxMapNameLength)
            {
                Log.Alert(AlertType.Error, $"Map name '{MapName}' too long ({Framework.MaxMapNameLength} bytes)\n");
            }

            if (string.IsNullOrEmpty(LandmarkName))
            {
                Log.Alert(AlertType.Console, $"trigger_changelevel to {MapName} doesn't have a landmark\n");
            }
            else if (Encoding.UTF8.GetByteCount(LandmarkName) > Framework.MaxMapNameLength)
            {
                Log.Alert(AlertType.Error, $"Landmark name '{LandmarkName}' too long ({Framework.MaxMapNameLength} bytes)\n");
            }

            if (!string.IsNullOrEmpty(TargetName))
            {
                SetUse(UseChangeLevel);
            }

            InitTrigger();

            if ((SpawnFlags & SF.UseOnly) == 0)
            {
                SetTouch(TouchChangeLevel);
            }

            //	Log.Alert(AlertType.Console, $"TRANSITION: {MapName} ({LandmarkName})\n");
        }

        /// <summary>
        /// allows level transitions to be triggered by buttons, etc.
        /// </summary>
        /// <param name="pActivator"></param>
        /// <param name="pCaller"></param>
        /// <param name="useType"></param>
        /// <param name="value"></param>
        private void UseChangeLevel(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            ChangeLevelNow(pActivator);
        }

        private void TouchChangeLevel(BaseEntity pOther)
        {
            if (!pOther.IsPlayer())
            {
                return;
            }

            ChangeLevelNow(pOther);
        }

        private void ChangeLevelNow(BaseEntity pActivator)
        {
            //The original version stored off the arguments for ChangeLevel because it referenced the strings directly
            //The current engine build queues up a changelevel server command, and no longer requires the strings to stick around
            //So we don't have to do that anymore

            Debug.Assert(!string.IsNullOrEmpty(MapName));

            // Don't work in deathmatch
            if (Engine.GameRules.IsDeathmatch())
            {
                return;
            }

            // Some people are firing these multiple times in a frame, disable
            if (Engine.Globals.Time == DamageTime)
            {
                return;
            }

            DamageTime = Engine.Globals.Time;

            //TODO: needs revisiting for multiplayer changelevel - Solokiller
            var pPlayer = EntUtils.IndexEnt(1);
            if (!InTransitionVolume(pPlayer, LandmarkName))
            {
                Log.Alert(AlertType.AIConsole, $"Player isn't in the transition volume {LandmarkName}, aborting\n");
                return;
            }

            // Create an entity to fire the changetarget
            if (!string.IsNullOrEmpty(ChangeTarget))
            {
                var fireAndDie = Engine.EntityRegistry.CreateInstance<FireAndDie>();

                if (fireAndDie != null)
                {
                    // Set target and delay
                    fireAndDie.Target = ChangeTarget;
                    fireAndDie.Delay = ChangeTargetDelay;
                    fireAndDie.Origin = pPlayer.Origin;
                    // Call spawn
                    EntUtils.DispatchSpawn(fireAndDie.Edict());
                }
            }

            Activator.Set(pActivator);
            SUB_UseTargets(pActivator, UseType.Toggle, 0);

            // look for a landmark entity
            var landmark = FindLandmark(LandmarkName);
            if (landmark != null)
            {
                Engine.Globals.LandmarkOffset = landmark.Origin;
            }

            var landmarkName = landmark != null ? LandmarkName : string.Empty;

            //	Log.Alert(AlertType.Console, $"Level touches {ChangeList(levels, 16)} levels\n");
            Log.Alert(AlertType.Console, $"CHANGE LEVEL: {MapName} {landmarkName}\n");
            Engine.Server.ChangeLevel(MapName, LandmarkName);
        }

        private static BaseEntity FindLandmark(string landmarkName)
        {
            for (BaseEntity landmark = null; (landmark = EntUtils.FindEntityByTargetName(landmark, landmarkName)) != null;)
            {
                // Found the landmark
                if (landmark.ClassName == "info_landmark")
                {
                    return landmark;
                }
            }

            Log.Alert(AlertType.Error, $"Can't find landmark {landmarkName}\n");
            return null;
        }

        /// <summary>
        /// This has grown into a complicated beast
        /// Can we make this more elegant?
        /// This builds the list of all transitions on this level and which entities are in their PVS's and can / should
        /// be moved across.
        /// </summary>
        /// <returns></returns>
        private static IList<Transition> ChangeList()
        {
            var transitions = new List<Transition>();

            // Find all of the possible level changes on this BSP
            for (ChangeLevel changeLevel = null; (changeLevel = (ChangeLevel)EntUtils.FindEntityByClassName(changeLevel, "trigger_changelevel")) != null;)
            {
                // Find the corresponding landmark
                var landmark = FindLandmark(changeLevel.LandmarkName);

                if (landmark != null)
                {
                    // Build a list of unique transitions
                    if (AddTransitionToList(transitions, changeLevel.MapName, changeLevel.LandmarkName, landmark))
                    {
                    }
                }
            }

            //TODO: implement when persistence system is implemented
#if false
            if (transitions.Count > 0 && Engine.Globals.SaveData != IntPtr.Zero && ((SAVERESTOREDATA*)gpGlobals->pSaveData)->pTable)
            {
                CSave saveHelper((SAVERESTOREDATA*) gpGlobals->pSaveData );

                for (var i = 0; i < transitions.Count; ++i)
                {
                    var transition = transitions[i];

                    // Follow the linked list of entities in the PVS of the transition landmark
                    // Build a list of valid entities in this linked list (we're going to use entity.Chain again)
                    for (var entity = EntUtils.EntitiesInPVS(transition.Landmark); entity != null; entity = entity.Chain)
                    {
                        //Log.Alert(AlertType.Console, $"Trying {entity.ClassName}\n");
                        var caps = entity.ObjectCaps();
                        if ((caps & EntityCapabilities.DontSave) == 0)
                        {
                            var flags = EntTableFlags.None;

                            // If this entity can be moved or is global, mark it
                            if ((caps & EntityCapabilities.AcrossTransition) != 0)
                            {
                                flags |= EntTableFlags.Moveable;
                            }

                            if (!string.IsNullOrEmpty(entity.GlobalName) && !entity.IsDormant())
                            {
                                flags |= EntTableFlags.Global;
                            }

                            if (flags != 0)
                            {
                                // Check to make sure the entity isn't screened out by a trigger_transition
                                if (InTransitionVolume(entity, transition.LandmarkName))
                                {
                                    // Mark entity table with 1<<i
                                    var index = saveHelper.EntityIndex(entity);
                                    // Flag it with the level number
                                    saveHelper.EntityFlagsSet(index, flags | (EntTableFlags)(1 << i));
                                }
                                //else
                                //	Log.Alert(AlertType.Console, $"Screened out {pEntList[j].ClassName}\n");
                            }
                            //else
                            //	Log.Alert(AlertType.Console, $"Failed {pEntity.ClassName}\n");
                        }
                        //else
                        //	Log.Alert(AlertType.Console, $"DON'T SAVE {pEntity.ClassName}\n");
                    }
                }
            }
#endif

            return transitions;
        }

        /// <summary>
        /// Add a transition to the list, but ignore duplicates 
        /// (a designer may have placed multiple trigger_changelevels with the same landmark)
        /// </summary>
        /// <param name="transitions"></param>
        /// <param name="mapName"></param>
        /// <param name="landmarkName"></param>
        /// <param name="landmark"></param>
        /// <returns></returns>
        private static bool AddTransitionToList(IList<Transition> transitions, string mapName, string landmarkName, BaseEntity landmark)
        {
            if (mapName == null || landmarkName == null || landmark == null)
            {
                return false;
            }

            if (transitions.Any(m => m.Landmark == landmark && m.Name == mapName))
            {
                return false;
            }

            transitions.Add(new Transition { Name = mapName, LandmarkName = landmarkName, Landmark = landmark, LandmarkOrigin = landmark.Origin });

            return true;
        }

	    private static bool InTransitionVolume(BaseEntity pEntity, string volumeName)
        {
            if ((pEntity.ObjectCaps() & EntityCapabilities.ForceTransition) != 0)
            {
                return true;
            }

            // If you're following another entity, follow it through the transition (weapons follow the player)
            if (pEntity.MoveType == MoveType.Follow && pEntity.AimEntity != null)
            {
                pEntity = pEntity.AimEntity;
            }

            var inVolume = true;   // Unless we find a trigger_transition, everything is in the volume

            for (BaseEntity volume = null; (volume = EntUtils.FindEntityByTargetName(volume, volumeName)) != null;)
            {
                if (volume.ClassName == "trigger_transition")
                {
                    if (volume.Intersects(pEntity))   // It touches one, it's in the volume
                    {
                        return true;
                    }
                    else
                    {
                        inVolume = false;   // Found a trigger_transition, but I don't intersect it -- if I don't find another, don't go!
                    }
                }
            }

            return inVolume;
        }
    }
}
