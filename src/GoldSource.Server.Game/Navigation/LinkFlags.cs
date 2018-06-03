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

using System;

namespace GoldSource.Server.Game.Navigation
{
    [Flags]
    public enum LinkFlags
    {
        None        = 0,

        PointHull	= 1 << 0, // headcrab box can fit through this connection
        HumanHull	= 1 << 1, // player box can fit through this connection
        LargeHull	= 1 << 2, // big box can fit through this connection
        HeadHull	= 1 << 3, // a flying big box can fit through this connection
        Disabled	= 1 << 4, // link is not valid when the set
    }
}
