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

using GoldSource.Shared.Entities;
using Server.Game.Entities.MetaData;

namespace Server.Game.Entities.Triggers
{
    [LinkEntityToClass("trigger_monsterjump")]
    public class TriggerMonsterJump : BaseTrigger
    {
        public override void Spawn()
        {
            EntUtils.SetMovedir(this);

            InitTrigger();

            SetNextThink(0);
            Speed = 200;
            Height = 150;

            if (!string.IsNullOrEmpty(TargetName))
            {
                // if targeted, spawn turned off
                Solid = Solid.Not;
                SetOrigin(Origin); // Unlink from trigger list
                SetUse(ToggleUse);
            }
        }

        public override void Touch(BaseEntity pOther)
        {
            if ((pOther.Flags & EntFlags.Monster) == 0)
            {
                // touched by a non-monster.
                return;
            }

            var origin = pOther.Origin;
            ++origin.z;
            pOther.Origin = origin;

            if ((pOther.Flags & EntFlags.OnGround) != 0)
            {
                // clear the onground so physics don't bitch
                pOther.Flags &= ~EntFlags.OnGround;
            }

            // toss the monster!
            var velocity = MoveDirection * Speed;
            velocity.z += Height;
            pOther.Velocity = Velocity;
            SetNextThink(Engine.Globals.Time);
        }

        public override void Think()
        {
            Solid = Solid.Not;// kill the trigger for now !!!UNDONE
            SetOrigin(Origin); // Unlink from trigger list
            SetThink(null);
        }
    }
}
