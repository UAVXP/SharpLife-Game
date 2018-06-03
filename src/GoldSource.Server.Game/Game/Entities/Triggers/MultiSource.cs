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

using GoldSource.Server.Engine;
using GoldSource.Shared.Entities;
using Server.Game.Entities.MetaData;
using Server.Game.GlobalState;
using Server.Persistence;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.Entities.Triggers
{
    [LinkEntityToClass("multisource")]
    public class MultiSource : PointEntity
    {
        public static class SF
        {
            public const uint Init = 1;
        }

        private sealed class MultiEntity
        {
            [Persist]
            public EHandle<BaseEntity> Entity;

            [Persist]
            public bool Triggered;

            public MultiEntity(BaseEntity entity)
            {
                Entity.Set(entity);
                Triggered = false;
            }
        }

        [Persist]
        private List<MultiEntity> Entities = new List<MultiEntity>();

        [KeyValue]
        [Persist]
        public string GlobalState;

        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() | EntityCapabilities.Master;

        public override void Spawn()
        {
            // set up think for later registration

            Solid = Solid.Not;
            MoveType = MoveType.None;
            SetNextThink(Engine.Globals.Time + 0.1f);
            SpawnFlags |= SF.Init;   // Until it's initialized
            SetThink(Register);
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            // Find the entity in our list
            //This fixes a bug where entities that trigger this that weren't listed could still count
            var entity = Entities.Find(e => e.Entity == pCaller);

            if (entity == null)
            {
                // if we didn't find it, report error and leave
                Log.Alert(AlertType.Console, $"MultiSrc:Used by non member {pCaller.ClassName}.\n");
                return;
            }

            // CONSIDER: a Use input to the multisource always toggles.  Could check useType for ON/OFF/TOGGLE

            entity.Triggered = !entity.Triggered;

            if (IsTriggered(pActivator))
            {
                Log.Alert(AlertType.AIConsole, $"Multisource {TargetName} enabled ({Entities.Count} inputs)\n");

                var targetUseType = UseType.Toggle;
                if (GlobalState != null)
                    targetUseType = UseType.On;

                //TODO: should not pass null
                SUB_UseTargets(null, targetUseType, 0);
            }
        }

        public override bool IsTriggered(BaseEntity pActivator)
        {
            // Is everything triggered?

            // Still initializing?
            if ((SpawnFlags & SF.Init) != 0)
            {
                return false;
            }

            var triggered = Entities.All(e => e.Triggered);

            if (triggered)
            {
                if (GlobalState == null || Globals.GlobalState.EntityGetState(GlobalState) == GlobalEState.On)
                {
                    return true;
                }
            }

            return false;
        }

        private void Register()
        {
            SetThink(SUB_DoNothing);

            // search for all entities which target this multisource (TargetName)
            for (BaseEntity entity = null; (entity = EntUtils.FindEntityByString(entity, "target", TargetName)) != null;)
            {
                Entities.Add(new MultiEntity(entity));
            }

            for (BaseEntity entity = null; (entity = EntUtils.FindEntityByString(entity, "classname", "multi_manager")) != null;)
            {
                if (entity.HasTarget(TargetName))
                {
                    Entities.Add(new MultiEntity(entity));
                }
            }

            SpawnFlags &= ~SF.Init;
        }
    }
}
