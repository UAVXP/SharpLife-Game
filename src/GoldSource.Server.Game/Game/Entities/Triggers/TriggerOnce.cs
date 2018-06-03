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

namespace GoldSource.Server.Game.Game.Entities.Triggers
{
    /// <summary>
    /// Variable sized trigger. Triggers once, then removes itself.  You must set the key "target" to the name of another object in the level that has a matching
    /// "targetname".  If "health" is set, the trigger must be killed to activate.
    /// If notouch is set, the trigger is only fired by other entities, not by touching.
    /// if "killtarget" is set, any objects that have a matching "target" will be removed when the trigger is fired.
    /// if "angle" is set, the trigger will only fire when someone is facing the direction of the angle.Use "360" for an angle of 0.
    /// sounds
    /// 1)      secret
    /// 2)      beep beep
    /// 3)      large switch
    /// 4)
    /// </summary>
    [LinkEntityToClass("trigger_once")]
    public class TriggerOnce : TriggerMultiple
    {
        public override void Spawn()
        {
            Wait = -1;

            base.Spawn();
        }
    }
}
