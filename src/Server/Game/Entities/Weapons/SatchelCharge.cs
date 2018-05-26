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

using Server.Game.Entities.Characters;
using Server.Game.Entities.MetaData;

namespace Server.Game.Entities.Weapons
{
    [LinkEntityToClass("monster_satchelcharge")]
    public class SatchelCharge : BaseEntity
    {
        public void Deactivate()
        {

        }

        public static void DeactivateSatchels(BasePlayer pOwner)
        {
            for (BaseEntity entity = null; (entity = EntUtils.FindEntityByClassName(entity, "monster_satchel")) != null;)
            {
                if (entity is SatchelCharge satchel && satchel.Owner == pOwner)
                {
                    satchel.Deactivate();
                }
            }
        }
    }
}
