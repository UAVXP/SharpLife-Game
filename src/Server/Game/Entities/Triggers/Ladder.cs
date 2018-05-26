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

using Server.Engine;
using Server.Game.Entities.MetaData;

namespace Server.Game.Entities.Triggers
{
    /// <summary>
    /// makes an area vertically negotiable
    /// </summary>
    [LinkEntityToClass("func_ladder")]
    public class Ladder : BaseTrigger
    {
        public override void Precache()
        {
            // Do all of this in here because we need to 'convert' old saved games
            Solid = Solid.Not;
            Skin = (int)Contents.Ladder;
            if (CVar.GetFloat("showtriggers") == 0)
            {
                RenderMode = RenderMode.TransTexture;
                RenderAmount = 0;
            }
            Effects &= ~EntityEffects.NoDraw;
        }

        public override void Spawn()
        {
            Precache();

            SetModel(ModelName);    // set size and link into world
            MoveType = MoveType.Push;
        }
    }
}
