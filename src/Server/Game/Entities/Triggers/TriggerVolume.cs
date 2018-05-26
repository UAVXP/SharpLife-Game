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
    /// Define space that travels across a level transition
    /// Derive from point entity so this doesn't move across levels
    /// </summary>
    [LinkEntityToClass("trigger_transition")]
    public class TriggerVolume : PointEntity
    {
        public override void Spawn()
        {
            Solid = Solid.Not;
            MoveType = MoveType.None;
            SetModel(ModelName);    // set size and link into world
            ModelName = null;
            ModelIndex = 0;
        }
    }
}
