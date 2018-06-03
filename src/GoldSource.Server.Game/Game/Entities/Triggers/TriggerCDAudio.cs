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
    /// trigger_cdaudio - starts/stops cd audio tracks
    /// </summary>
    [LinkEntityToClass("trigger_cdaudio")]
    public class TriggerCDAudio : BaseTrigger
    {
        public override void Spawn()
        {
            InitTrigger();
        }

        /// <summary>
        /// <para>Changes tracks or stops CD when player touches</para>
        /// <para>!!!HACK - overloaded HEALTH to avoid adding new field</para>
        /// </summary>
        /// <param name="pOther"></param>
        public override void Touch(BaseEntity pOther)
        {
            if (!pOther.IsPlayer())
            {
                // only clients may trigger these events
                return;
            }

            PlayTrack();
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            PlayTrack();
        }

        private void PlayTrack()
        {
            EntUtils.PlayCDTrack((int)Health);

            SetTouch(null);
            EntUtils.Remove(this);
        }
    }
}
