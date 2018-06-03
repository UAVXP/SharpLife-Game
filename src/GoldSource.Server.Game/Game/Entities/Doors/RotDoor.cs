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
using System.Diagnostics;

namespace GoldSource.Server.Game.Game.Entities.Doors
{
    /// <summary>
    /// <para>
    /// if two doors touch, they are assumed to be connected and operate as  
    /// a unit.
    /// </para>
    /// <para>
    /// Toggle causes the door to wait in both the start and end states for  
    /// a trigger event.
    /// </para>
    /// <para>
    /// StartOpen causes the door to move to its destination when spawned,
    /// and operate in reverse.It is used to temporarily or permanently
    /// close off an area when triggered (not usefull for touch or
    /// takedamage doors).
    /// </para>
    /// <para>
    /// You need to have an origin brush as part of this entity.The
    /// center of that brush will be
    /// the point around which it is rotated.It will rotate around the Z
    /// axis by default.  You can
    /// check either the X_AXIS or Y_AXIS box to change that.
    /// </para>
    /// <para>
    /// "distance" is how many degrees the door will be rotated.
    /// "speed" determines how fast the door moves; default value is 100.
    /// </para>
    /// <para>REVERSE will cause the door to rotate in the opposite direction.</para>
    /// <para>
    /// "angle"		determines the opening direction
    /// "targetname" if set, no touch field will be spawned and a remote
    /// button or trigger field activates the door.
    /// "health"	if set, door must be shot open
    /// "speed"		movement speed (100 default)
    /// "wait"		wait before returning (3 default, -1 = never return)
    /// "dmg"		damage to inflict when blocked (2 default)
    /// "sounds"
    /// 0)	no sound
    /// 1)	stone
    /// 2)	base
    /// 3)	stone chain
    /// 4)	screechy metal
    /// </para>
    /// </summary>
    [LinkEntityToClass("func_door_rotating")]
    public class RotDoor : BaseDoor
    {
        public override void Spawn()
        {
            Precache();
            // set the axis of rotation
            AxisDir(this);

            // check for clockwise rotation
            if ((SpawnFlags & SF.RotateBackwards) != 0)
            {
                MoveDirection *= -1;
            }

            //m_flWait			= 2; who the hell did this? (sjb)
            Angle1 = Angles;
            Angle2 = Angles + (MoveDirection * MoveDistance);

            Debug.Assert(Angle1 != Angle2, "rotating door start/end positions are equal");

            if ((SpawnFlags & SF.Passable) != 0)
            {
                Solid = Solid.Not;
            }
            else
            {
                Solid = Solid.BSP;
            }

            MoveType = MoveType.Push;
            SetOrigin(Origin);
            SetModel(ModelName);

            if (Speed == 0)
            {
                Speed = 100;
            }

            // StartOpen is to allow an entity to be lighted in the closed position
            // but spawn in the open position
            if ((SpawnFlags & SF.StartOpen) != 0)
            {   // swap pos1 and pos2, put door at pos2, invert movement direction
                Angles = Angle2;
                Vector vecSav = Angle1;
                Angle2 = Angle1;
                Angle1 = vecSav;
                MoveDirection *= -1;
            }

            ToggleState = ToggleState.AtBottom;

            if ((SpawnFlags & SF.UseOnly) != 0)
            {
                SetTouch(null);
            }
            else // touchable button
            {
                SetTouch(DoorTouch);
            }
        }

        public override void SetToggleState(ToggleState state)
        {
            if (state == ToggleState.AtTop)
            {
                Angles = Angle2;
            }
            else
            {
                Angles = Angle1;
            }

            SetOrigin(Origin);
        }
    }
}
