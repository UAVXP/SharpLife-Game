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
using Server.Game.Entities.MetaData;
using Server.Persistence;

namespace Server.Game.Entities
{
    [LinkEntityToClass("DelayedUse")]
    public class BaseDelay : BaseEntity
    {
        [KeyValue]
        [Persist]
        public float Delay { get; set; }

        [KeyValue]
        [Persist]
        public string KillTarget { get; set; }

        // common member functions
        public override void SUB_UseTargets(BaseEntity pActivator, UseType useType, float value = 0)
        {
            //
            // exit immediatly if we don't have a target or kill target
            //
            if (string.IsNullOrEmpty(Target) && string.IsNullOrEmpty(KillTarget))
            {
                return;
            }

            //
            // check for a delay
            //
            if (Delay != 0)
            {
                // create a temp object to fire at a later time
                var pTemp = Engine.EntityRegistry.CreateInstance<BaseDelay>();

                pTemp.SetNextThink(Engine.Globals.Time + Delay);

                pTemp.SetThink(pTemp.DelayThink);

                // Save the useType
                pTemp.Button = (int)useType;
                pTemp.KillTarget = KillTarget;
                pTemp.Delay = 0; // prevent "recursion"
                pTemp.Target = Target;

                // HACKHACK
                // This wasn't in the release build of Half-Life.  We should have moved m_hActivator into this class
                // but changing member variable hierarchy would break save/restore without some ugly code.
                // This code is not as ugly as that code
                if (pActivator?.IsPlayer() == true)       // If a player activates, then save it
                {
                    pTemp.Owner = pActivator;
                }
                else
                {
                    pTemp.Owner = null;
                }

                return;
            }

            //
            // kill the killtargets
            //

            if (!string.IsNullOrEmpty(KillTarget))
            {
                Log.Alert(AlertType.AIConsole, $"KillTarget: {KillTarget}\n");

                for (BaseEntity entity = null; (entity = EntUtils.FindEntityByTargetName(entity, KillTarget)) != null;)
                {
                    //Remove after logging, not before
                    Log.Alert(AlertType.AIConsole, $"killing {entity.ClassName}\n");
                    EntUtils.Remove(entity);
                }
            }

            //
            // fire targets
            //
            if (!string.IsNullOrEmpty(Target))
            {
                EntUtils.FireTargets(Target, pActivator, this, useType, value);
            }
        }

        private void DelayThink()
        {
            // A player activated this on delay
            var activator = Owner;

            // The use type is cached (and stashed) in pev->button
            SUB_UseTargets(activator, (UseType)Button, 0);
            EntUtils.Remove(this);
        }
    }
}
