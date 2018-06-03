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
using GoldSource.Shared.Game;

namespace GoldSource.Server.Game.Game.Entities.Triggers
{
    [LinkEntityToClass("trigger_teleport")]
    public class TriggerTeleport : BaseTrigger
    {
        public override void Spawn()
        {
            InitTrigger();

            SetTouch(TeleportTouch);
        }

        private void TeleportTouch(BaseEntity pOther)
        {
            // Only teleport monsters or clients
            if ((pOther.Flags & (EntFlags.Client | EntFlags.Monster)) == 0)
            {
                return;
            }

            if (!EntUtils.IsMasterTriggered(Master, pOther))
            {
                return;
            }

            if ((SpawnFlags & SF.AllowMonsters) == 0)
            {// no monsters allowed!
                if ((pOther.Flags & EntFlags.Monster) != 0)
                {
                    return;
                }
            }

            if ((SpawnFlags & SF.NoClients) != 0)
            {// no clients allowed
                if (pOther.IsPlayer())
                {
                    return;
                }
            }

            var target = EntUtils.FindEntityByTargetName(null, Target);

            if (target == null)
            {
                return;
            }

            var tmp = target.Origin;

            if (pOther.IsPlayer())
            {
                tmp.z -= pOther.Mins.z;// make origin adjustments in case the teleportee is a player. (origin in center, not at feet)
            }

            tmp.z++;

            pOther.Flags &= ~EntFlags.OnGround;

            pOther.SetOrigin(tmp);

            pOther.Angles = target.Angles;

            if (pOther.IsPlayer())
            {
                pOther.ViewAngle = target.ViewAngle;
            }

            pOther.FixAngle = FixAngleMode.ForceViewAngles;
            pOther.Velocity = pOther.BaseVelocity = WorldConstants.g_vecZero;
        }
    }
}
