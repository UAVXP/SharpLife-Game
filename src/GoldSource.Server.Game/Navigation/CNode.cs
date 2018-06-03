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
using GoldSource.Shared.Game;
using System;

namespace GoldSource.Server.Game.Navigation
{
    /// <summary>
    /// Instance of a node
    /// </summary>
    [Serializable]
    public class CNode
    {
        public Vector m_vecOrigin;
        //location of this node in space
        public Vector m_vecOriginPeek;
        //location of this node (LAND nodes are NODE_HEIGHT higher).
        public int[] m_Region = new int[3];
        //Which of 256 regions do each of the coordinate belong?
        public NodeType m_afNodeInfo;
        //bits that tell us more about this location
        public int m_cNumLinks;
        //how many links this node has
        public int m_iFirstLink;
        /*
        *	index of this node's first link in the link pool.
        *	Where to start looking in the compressed routing table (offset into m_pRouteInfo).
        *	(4 hull sizes -- smallest to largest + fly/swim), and secondly, door capability.
        */
        public int[,] m_pNextBestNode = new int[WorldConstants.NUM_HULLS, 2];
        /*
        *	Used in finding the shortest path. m_fClosestSoFar is -1 if not visited.
        *	Then it is the distance to the source. If another path uses this node
        *	and has a closer distance, then m_iPreviousNode is also updated.
        */
        public float m_flClosestSoFar;
        //Used in finding the shortest path.
        public int m_iPreviousNode;

        public short m_sHintType;
        //there is something interesting in the world at this node's position
        public short m_sHintActivity;
        //there is something interesting in the world at this node's position
        public float m_flHintYaw;
    }
}
