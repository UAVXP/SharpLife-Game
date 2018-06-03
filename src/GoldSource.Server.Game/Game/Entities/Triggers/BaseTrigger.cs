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

using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;

namespace GoldSource.Server.Game.Game.Entities.Triggers
{
    [LinkEntityToClass("trigger")]
    public class BaseTrigger : BaseToggle
    {
        public static class SF
        {
            /// <summary>
            /// monsters allowed to fire this trigger
            /// </summary>
            public const uint AllowMonsters = 1;

            /// <summary>
            /// players not allowed to fire this trigger
            /// </summary>
            public const uint NoClients = 2;

            /// <summary>
            /// only pushables can fire this trigger
            /// </summary>
            public const uint Pushables = 4;
        }

        private string Noise
        {
            get => pev.Noise;
            set => pev.Noise = value;
        }

        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;

        public override bool KeyValue(string key, string value)
        {
            if (key == "damage")
            {
                float.TryParse(value, out var result);
                Damage = result;
                return true;
            }
            else if (key == "count")
            {
                float.TryParse(value, out var result);
                TriggersLeft = (int)result;
                return true;
            }
            else if (key == "damagetype")
            {
                int.TryParse(value, out var result);
                DamageInflict = (DamageTypes)result;
                return true;
            }

            return base.KeyValue(key, value);
        }

        /// <summary>
        /// the trigger was just touched/killed/used
        /// self.enemy should be set to the activator so it can be held through a delay
        /// so wait for the delay time before firing
        /// </summary>
        /// <param name="pActivator"></param>
        protected void ActivateMultiTrigger(BaseEntity pActivator)
        {
            if (GetNextThink() > Engine.Globals.Time)
            {
                return;         // still waiting for reset time
            }

            if (!EntUtils.IsMasterTriggered(Master, pActivator))
                return;

            if (ClassName == "trigger_secret")
            {
                if (Enemy == null || !Enemy.IsPlayer())
                {
                    return;
                }

                ++Engine.Globals.FoundSecrets;
            }

            if (!string.IsNullOrEmpty(Noise))
            {
                EmitSound(SoundChannel.Voice, Noise);
            }

            // don't trigger again until reset
            // pev->takedamage = DAMAGE_NO;

            Activator.Set(pActivator);
            SUB_UseTargets(Activator, UseType.Toggle);

            if (!string.IsNullOrEmpty(Message) && pActivator.IsPlayer())
            {
                PlayerUtils.ShowMessage(Message, pActivator);
                //		CLIENT_PRINTF( ENT( pActivator->pev ), print_center, STRING(pev->message) );
            }

            if (Wait > 0)
            {
                SetThink(MultiWaitOver);
                SetNextThink(Engine.Globals.Time + Wait);
            }
            else
            {
                // we can't just remove (self) here, because this is a touch function
                // called while C code is looping through area links...
                SetTouch(null);
                SetNextThink(Engine.Globals.Time + 0.1f);
                SetThink(SUB_Remove);
            }
        }

        /// <summary>
        /// the wait time has passed, so set back up for another activation
        /// </summary>
        private void MultiWaitOver()
        {
            //	if (pev->max_health)
            //		{
            //		pev->health		= pev->max_health;
            //		pev->takedamage	= DAMAGE_YES;
            //		pev->solid		= SOLID_BBOX;
            //		}
            SetThink(null);
        }

        /// <summary>
        /// If this is the USE function for a trigger, its state will toggle every time it's fired
        /// </summary>
        /// <param name="pActivator"></param>
        /// <param name="pCaller"></param>
        /// <param name="useType"></param>
        /// <param name="value"></param>
        protected void ToggleUse(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            if (Solid == Solid.Not)
            {// if the trigger is off, turn it on
                Solid = Solid.Trigger;

                // Force retouch
                ++Engine.Globals.ForceRetouch;
            }
            else
            {// turn the trigger off
                Solid = Solid.Not;
            }

            SetOrigin(Origin);
        }

        protected void InitTrigger()
        {
            // trigger angles are used for one-way touches.  An angle of 0 is assumed
            // to mean no restrictions, so use a yaw of 360 instead.
            if (Angles != WorldConstants.g_vecZero)
            {
                EntUtils.SetMovedir(this);
            }

            Solid = Solid.Trigger;
            MoveType = MoveType.None;
            SetModel(ModelName);    // set size and link into world
            if (CVar.GetFloat("showtriggers") == 0)
            {
                Effects |= EntityEffects.NoDraw;
            }
        }
    }
}
