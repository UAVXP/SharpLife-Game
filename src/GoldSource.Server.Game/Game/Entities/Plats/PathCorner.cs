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
using GoldSource.Server.Game.Persistence;
using System.Diagnostics;

namespace GoldSource.Server.Game.Game.Entities.Plats
{
    [LinkEntityToClass("path_corner")]
    public class PathCorner : PointEntity
    {
        public static class SF
        {
            public const uint WaitForTrigger = 0x001;
            public const uint Teleport = 0x002;
            public const uint FireOnce = 0x004;
        }

        [KeyValue]
        [Persist]
        public float Wait;

        public override void Spawn()
        {
            Debug.Assert(!string.IsNullOrEmpty(TargetName), "path_corner without a targetname");
        }

        public override float GetDelay() => Wait;
    }
}
