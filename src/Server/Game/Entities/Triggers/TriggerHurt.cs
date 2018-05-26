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

namespace Server.Game.Entities.Triggers
{
    [LinkEntityToClass("trigger_hurt")]
    public class TriggerHurt : BaseTrigger
    {
        new public static class SF
        {
            /// <summary>
            /// Only fire hurt target once
            /// </summary>
            public const uint TargetOnce = 1;

            /// <summary>
            /// spawnflag that makes trigger_push spawn turned OFF
            /// </summary>
            public const uint StartOff = 2;

            /// <summary>
            /// spawnflag that makes trigger_push spawn turned OFF
            /// </summary>
            public const uint NoClients = 8;

            /// <summary>
            /// trigger hurt will only fire its target if it is hurting a client
            /// </summary>
            public const uint ClientOnlyFire = 16;

            /// <summary>
            /// only clients may touch this trigger
            /// </summary>
            public const uint ClientOnlyTouch = 32;
        }

        private void HurtTouch(BaseEntity pOther)
        {
            if (pOther.TakeDamageState == TakeDamageState.No)
            {
                return;
            }

            if ((SpawnFlags & SF.ClientOnlyTouch) != 0 && !pOther.IsPlayer())
            {
                // this trigger is only allowed to touch clients, and this ain't a client.
                return;
            }

            if ((SpawnFlags & SF.NoClients) != 0 && pOther.IsPlayer())
            {
                return;
            }

            // HACKHACK -- In multiplayer, players touch this based on packet receipt.
            // So the players who send packets later aren't always hurt.  Keep track of
            // how much time has passed and whether or not you've touched that player
            if (Engine.GameRules.IsMultiplayer())
            {
                if (DamageTime > Engine.Globals.Time)
                {
                    if (Engine.Globals.Time != PainFinished)
                    {// too early to hurt again, and not same frame with a different entity
                        if (pOther.IsPlayer())
                        {
                            int playerMask = 1 << (pOther.EntIndex() - 1);

                            // If I've already touched this player (this time), then bail out
                            if ((Impulse & playerMask) != 0)
                                return;

                            // Mark this player as touched
                            // BUGBUG - There can be only 32 players!
                            Impulse |= playerMask;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else
                {
                    // New clock, "un-touch" all players
                    Impulse = 0;
                    if (pOther.IsPlayer())
                    {
                        int playerMask = 1 << (pOther.EntIndex() - 1);

                        // Mark this player as touched
                        // BUGBUG - There can be only 32 players!
                        Impulse |= playerMask;
                    }
                }
            }
            else    // Original code -- single player
            {
                if (DamageTime > Engine.Globals.Time && Engine.Globals.Time != PainFinished)
                {// too early to hurt again, and not same frame with a different entity
                    return;
                }
            }

            // If this is time_based damage (poison, radiation), override the pev->dmg with a 
            // default for the given damage type.  Monsters only take time-based damage
            // while touching the trigger.  Player continues taking damage for a while after
            // leaving the trigger

            var fldmg = Damage * 0.5f; // 0.5 seconds worth of damage, pev->dmg is damage/second

            // JAY: Cut this because it wasn't fully realized.  Damage is simpler now.
#if false
	switch (m_bitsDamageInflict)
	{
	default: break;
	case DMG_POISON:		fldmg = POISON_DAMAGE/4; break;
	case DMG_NERVEGAS:		fldmg = NERVEGAS_DAMAGE/4; break;
	case DMG_RADIATION:		fldmg = RADIATION_DAMAGE/4; break;
	case DMG_PARALYZE:		fldmg = PARALYZE_DAMAGE/4; break; // UNDONE: cut this? should slow movement to 50%
	case DMG_ACID:			fldmg = ACID_DAMAGE/4; break;
	case DMG_SLOWBURN:		fldmg = SLOWBURN_DAMAGE/4; break;
	case DMG_SLOWFREEZE:	fldmg = SLOWFREEZE_DAMAGE/4; break;
	}
#endif

            if (fldmg < 0)
                pOther.TakeHealth(-fldmg, DamageInflict);
            else
                pOther.TakeDamage(this, this, fldmg, DamageInflict);

            // Store pain time so we can get all of the other entities on this frame
            PainFinished = Engine.Globals.Time;

            // Apply damage every half second
            DamageTime = Engine.Globals.Time + 0.5f;// half second delay until this trigger can hurt toucher again

            if (!string.IsNullOrEmpty(Target))
            {
                // trigger has a target it wants to fire. 
                if ((SpawnFlags & SF.ClientOnlyFire) != 0)
                {
                    // if the toucher isn't a client, don't fire the target!
                    if (!pOther.IsPlayer())
                    {
                        return;
                    }
                }

                SUB_UseTargets(pOther, UseType.Toggle);
                if ((SpawnFlags & SF.TargetOnce) != 0)
                {
                    Target = null;
                }
            }
        }
    }
}
