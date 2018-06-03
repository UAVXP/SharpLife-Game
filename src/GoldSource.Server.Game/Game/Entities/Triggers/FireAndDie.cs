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

namespace Server.Game.Entities.Triggers
{
    /// <summary>
    /// Fires a target after level transition and then dies
    /// </summary>
    [LinkEntityToClass("fireanddie")]
    public class FireAndDie : BaseDelay
    {
        /// <summary>
        /// Always go across transitions
        /// </summary>
        /// <returns></returns>
        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() | EntityCapabilities.ForceTransition;

        public override void Precache()
        {
            // This gets called on restore
            SetNextThink(Engine.Globals.Time + Delay);
        }

        public override void Spawn()
        {
            // Don't call Precache() - it should be called on restore
        }

        public override void Think()
        {
            SUB_UseTargets(this, UseType.Toggle, 0);
            EntUtils.Remove(this);
        }
    }
}
