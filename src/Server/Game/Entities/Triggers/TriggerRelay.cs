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
using Server.Persistence;

namespace Server.Game.Entities.Triggers
{
    [LinkEntityToClass("trigger_relay")]
    public class TriggerRelay : BaseDelay
    {
        public static class SF
        {
            public const uint FireOnce = 0x0001;
        }

        [Persist]
        private UseType TriggerType;

        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;

        public override bool KeyValue(string key, string value)
        {
            if (key == "triggerstate")
            {
                int.TryParse(value, out var type);

                switch (type)
                {
                    case 0:
                        TriggerType = UseType.Off;
                        break;
                    case 2:
                        TriggerType = UseType.Toggle;
                        break;
                    default:
                        TriggerType = UseType.On;
                        break;
                }
                return true;
            }

            return base.KeyValue(key, value);
        }

        public override void Spawn()
        {
            //Nothing
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            SUB_UseTargets(this, TriggerType);

            if ((SpawnFlags & SF.FireOnce) != 0)
            {
                EntUtils.Remove(this);
            }
        }
    }
}
