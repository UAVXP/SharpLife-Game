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
using GoldSource.Shared.Entities;
using System.Diagnostics;

namespace GoldSource.Server.Game.Game.Entities.Triggers
{
    /// <summary>
    /// Variable sized repeatable trigger.  Must be targeted at one or more entities.
    /// If "health" is set, the trigger must be killed to activate each time.
    /// If "delay" is set, the trigger waits some time after activating before firing.
    /// "wait" : Seconds between triggerings. (.2 default)
    /// If notouch is set, the trigger is only fired by other entities, not by touching.
    /// NOTOUCH has been obsoleted by trigger_relay!
    /// sounds
    /// 1)      secret
    /// 2)      beep beep
    /// 3)      large switch
    /// 4)
    /// NEW
    /// if a trigger has a NETNAME, that NETNAME will become the TARGET of the triggered object.
    /// </summary>
    [LinkEntityToClass("trigger_multiple")]
    public class TriggerMultiple : BaseTrigger
    {
        public override void Spawn()
        {
            if (Wait == 0)
                Wait = 0.2f;

            InitTrigger();

            Debug.Assert(Health == 0, "trigger_multiple with health");
            //	UTIL_SetOrigin(pev, pev->origin);
            //	SET_MODEL( ENT(pev), STRING(pev->model) );
            //	if (pev->health > 0)
            //		{
            //		if (FBitSet(pev->spawnflags, SPAWNFLAG_NOTOUCH))
            //			ALERT(at_error, "trigger_multiple spawn: health and notouch don't make sense");
            //		pev->max_health = pev->health;
            //UNDONE: where to get pfnDie from?
            //		pev->pfnDie = multi_killed;
            //		pev->takedamage = DAMAGE_YES;
            //		pev->solid = SOLID_BBOX;
            //		UTIL_SetOrigin(pev, pev->origin);  // make sure it links into the world
            //		}
            //	else
            {
                SetTouch(MultiTouch);
            }
        }

        private void MultiTouch(BaseEntity pOther)
        {
            // Only touch clients, monsters, or pushables (depending on flags)
            if (((pOther.Flags & EntFlags.Client) != 0 && (SpawnFlags & SF.NoClients) == 0)
                 || ((pOther.Flags & EntFlags.Monster) != 0 && (SpawnFlags & SF.AllowMonsters) != 0)
                 || ((SpawnFlags & SF.Pushables) != 0 && pOther.ClassName == "func_pushable"))
            {
#if false
        // if the trigger has an angles field, check player's facing direction
        if (MoveDirection != WorldConstants.g_vecZero)
        {
            MathUtils.MakeVectors(pOther.Angles);
            if (Engine.Globals.ForwardVector.DotProduct(MoveDirection) < 0)
            {
                return;         // not facing the right way
            }
        }
#endif

                ActivateMultiTrigger(pOther);
            }
        }
    }
}
