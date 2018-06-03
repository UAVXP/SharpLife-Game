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

namespace GoldSource.Server.Game.Game.Entities.BrushModels
{
    /// <summary>
    /// This is just a solid wall if not inhibited
    /// </summary>
    [LinkEntityToClass("func_wall")]
    public class FuncWall : BaseEntity
    {
        public override void Spawn()
        {
            Angles = WorldConstants.g_vecZero;
            MoveType = MoveType.Push;  // so it doesn't get pushed by anything
            Solid = Solid.BSP;
            SetModel(ModelName);

            // If it can't move/go away, it's really part of the world
            Flags |= EntFlags.WorldBrush;
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            //Toggle texture
            if (ShouldToggle(useType, ((int)Frame) != 0))
            {
                Frame = 1 - Frame;
            }
        }

        // Bmodels don't go across transitions
        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;
    }
}
