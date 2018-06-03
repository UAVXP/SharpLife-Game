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
using GoldSource.Server.Game.Game.Entities.MetaData;

namespace GoldSource.Server.Game.Game.Entities.Triggers
{
    /// <summary>
    /// Acts as an intermediary for an action that takes multiple inputs.
    /// If nomessage is not set, it will print "1 more.. " etc when triggered and
    /// "sequence complete" when finished.After the counter has been triggered "cTriggersLeft"
    /// times (default 2), it will fire all of it's targets and remove itself.
    /// </summary>
    [LinkEntityToClass("trigger_counter")]
    public class TriggerCounter : BaseTrigger
    {
        public static new class SF
        {
            public const uint NoMessage = 1;
        }

        public override void Spawn()
        {
            // By making the flWait be -1, this counter-trigger will disappear after it's activated
            // (but of course it needs cTriggersLeft "uses" before that happens).
            Wait = -1;

            if (TriggersLeft == 0)
            {
                TriggersLeft = 2;
            }

            SetUse(CounterUse);
        }

        private void CounterUse(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            //TODO: bug when triggering a counter enough times where triggersleft will wrap around and become positive again
            --TriggersLeft;
            Activator.Set(pActivator);

            if (TriggersLeft < 0)
            {
                return;
            }

            var fTellActivator =
                pActivator?.IsPlayer() == true
                && (SpawnFlags & SF.NoMessage) == 0;
            if (TriggersLeft != 0)
            {
                if (fTellActivator)
                {
                    // UNDONE: I don't think we want these Quakesque messages
                    switch (TriggersLeft)
                    {
                        case 1: Log.Alert(AlertType.Console, "Only 1 more to go..."); break;
                        case 2: Log.Alert(AlertType.Console, "Only 2 more to go..."); break;
                        case 3: Log.Alert(AlertType.Console, "Only 3 more to go..."); break;
                        default: Log.Alert(AlertType.Console, "There are more to go..."); break;
                    }
                }
                return;
            }

            // !!!UNDONE: I don't think we want these Quakesque messages
            if (fTellActivator)
            {
                Log.Alert(AlertType.Console, "Sequence completed!");
            }

            ActivateMultiTrigger(Activator);
        }
    }
}
