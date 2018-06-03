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

using GoldSource.Mathlib;
using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Shared.Entities;

namespace GoldSource.Server.Game.Game.Entities.Plats
{
    /// <summary>
    /// UNDONE: Need to save this!!! It needs class & linkage
    /// </summary>
    [LinkEntityToClass("plat_trigger")]
    public class PlatTrigger : BaseEntity
    {
        private FuncPlat Platform;

        public override EntityCapabilities ObjectCaps() { return (base.ObjectCaps() & ~EntityCapabilities.AcrossTransition) | EntityCapabilities.DontSave; }

        /// <summary>
        /// When the platform's trigger field is touched, the platform ???
        /// </summary>
        /// <param name="pOther"></param>
        public override void Touch(BaseEntity pOther)
        {
            // Ignore touches by non-players
            if (!pOther.IsPlayer())
            {
                return;
            }

            // Ignore touches by corpses
            if (!pOther.IsAlive() || Platform == null)
            {
                return;
            }

            // Make linked platform go up/down.
            if (Platform.ToggleState == ToggleState.AtBottom)
            {
                Platform.GoUp();
            }
            else if (Platform.ToggleState == ToggleState.AtTop)
            {
                Platform.SetNextThink(Platform.GetLastThink() + 1);// delay going down
            }
        }

        /// <summary>
        /// Create a trigger entity for a platform
        /// </summary>
        /// <param name="platform"></param>
        public void SpawnInsideTrigger(FuncPlat platform)
        {
            Platform = platform;
            // Create trigger entity, "point" it at the owning platform, give it a touch method
            Solid = Solid.Trigger;
            MoveType = MoveType.None;
            Origin = Platform.Origin;

            // Establish the trigger field's size
            var vecTMin = Platform.Mins + new Vector(25, 25, 0);
            var vecTMax = Platform.Maxs + new Vector(25, 25, 8);
            vecTMin.z = vecTMax.z - (Platform.Position1.z - Platform.Position2.z + 8);

            if (Platform.Size.x <= 50)
            {
                vecTMin.x = (Platform.Mins.x + Platform.Maxs.x) / 2;
                vecTMax.x = vecTMin.x + 1;
            }
            if (Platform.Size.y <= 50)
            {
                vecTMin.y = (Platform.Mins.y + Platform.Maxs.y) / 2;
                vecTMax.y = vecTMin.y + 1;
            }

            SetSize(vecTMin, vecTMax);
        }
    }
}
