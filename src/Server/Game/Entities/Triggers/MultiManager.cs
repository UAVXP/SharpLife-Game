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

using Server.Engine;
using Server.Game.Entities.MetaData;
using Server.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Entities.Triggers
{
    /// <summary>
    /// The Multimanager Entity - when fired, will fire up to 16 targets at specified times.
    /// </summary>
    [LinkEntityToClass("multi_manager")]
    public class MultiManager : BaseToggle
    {
        public static class SF
        {
            /// <summary>
            /// this is a clone for a threaded execution
            /// </summary>
            public const uint Clone = 0x80000000;

            /// <summary>
            /// create clones when triggered
            /// </summary>
            public const uint Thread = 0x00000001;
        }

        private sealed class MultiTarget : IComparable<MultiTarget>
        {
            /// <summary>
            /// Name of the target to trigger
            /// </summary>
            [Persist]
            public string Name;

            /// <summary>
            /// delay (in seconds) from time of manager fire to target fire
            /// </summary>
            [Persist]
            public float Delay;

            public int CompareTo(MultiTarget other)
            {
                //Sort according to delay
                return Comparer<float>.Default.Compare(Delay, other.Delay);
            }
        }

        /// <summary>
        /// Current target
        /// </summary>
        [Persist]
        private int Index;

        /// <summary>
        /// Time we started firing
        /// </summary>
        [Persist]
        new private float StartTime;

        [Persist]
        private List<MultiTarget> Targets = new List<MultiTarget>();

        public bool IsClone => (SpawnFlags & SF.Clone) != 0;

        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;

        public override bool KeyValue(string key, string value)
        {
            // UNDONE: Maybe this should do something like this:
            // if (!base.KeyValue(key, value))
            // ... etc.

            if (key == "wait")
            {
                float.TryParse(value, out Wait);
                return true;
            }

            // add this field to the target list
            // this assumes that additional fields are targetnames and their values are delay values.
            //Hammer appends #<number> to the end of keys with the same name to differentiate, remove that
            EntUtils.StripToken(key, out var target);

            float.TryParse(value, out var delay);

            Targets.Add(new MultiTarget { Name = key, Delay = delay });
            return true;
        }

        public override void Spawn()
        {
            Solid = Solid.Not;
            SetUse(ManagerUse);
            SetThink(ManagerThink);

            // Sort targets
            Targets.Sort();
        }

        public override bool HasTarget(string targetname)
        {
            return Targets.Any(t => t.Name == targetname);
        }

        /// <summary>
        /// Designers were using this to fire targets that may or may not exist -- 
        /// so I changed it to use the standard target fire code, made it a little simpler.
        /// </summary>
        private void ManagerThink()
        {
            var time = Engine.Globals.Time - StartTime;
            while (Index < Targets.Count && Targets[Index].Delay <= time)
            {
                EntUtils.FireTargets(Targets[Index].Name, Activator, this, UseType.Toggle, 0);
                ++Index;
            }

            if (Index >= Targets.Count)// have we fired all targets?
            {
                SetThink(null);
                if (IsClone)
                {
                    EntUtils.Remove(this);
                    return;
                }
                SetUse(ManagerUse);// allow manager re-use 
            }
            else
            {
                SetNextThink(StartTime + Targets[Index].Delay);
            }
        }

        /// <summary>
        /// The USE function builds the time table and starts the entity thinking.
        /// </summary>
        /// <param name="pActivator"></param>
        /// <param name="pCaller"></param>
        /// <param name="useType"></param>
        /// <param name="value"></param>
        private void ManagerUse(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            // In multiplayer games, clone the MM and execute in the clone (like a thread)
            // to allow multiple players to trigger the same multimanager
            if (ShouldClone())
            {
                var clone = Clone();
                clone.ManagerUse(pActivator, pCaller, useType, value);
                return;
            }

            Activator.Set(pActivator);
            Index = 0;
            StartTime = Engine.Globals.Time;

            SetUse(null);// disable use until all targets have fired

            SetThink(ManagerThink);
            SetNextThink(Engine.Globals.Time);
        }

        private bool ShouldClone()
        {
            if (IsClone)
            {
                return false;
            }

            return (SpawnFlags & SF.Thread) != 0;
        }

        private MultiManager Clone()
        {
            var multi = Engine.EntityRegistry.CreateInstance<MultiManager>();

            //The original version copied its entvars members by memcpy
            //Since we don't have that many that the clone actually needs, we'll just copy them manually
            multi.Origin = Origin;
            multi.Solid = Solid;
            multi.SpawnFlags = SpawnFlags;

            multi.SpawnFlags |= SF.Clone;

            //The target list is immutable, but save restore code will restore these as separate lists
            //So do a deep clone

            multi.Targets = new List<MultiTarget>(Targets.Count);

            foreach (var target in Targets)
            {
                multi.Targets.Add(new MultiTarget { Name = target.Name, Delay = target.Delay });
            }

            return multi;
        }

#if _DEBUG
	void ManagerReport();
#endif
    }
}
