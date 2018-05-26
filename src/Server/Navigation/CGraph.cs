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

using Force.Crc32;
using Server.Engine;
using Server.Game.Entities;
using Server.Game.Entities.Characters.NPCs;
using Server.Game.Entities.Doors;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace Server.Game.Navigation
{
    [Serializable]
    public class CGraph
    {
        /// <summary>
        /// Increment this whever graph/node/link classes change, to obsolesce older disk files
        /// </summary>
        public const int GRAPH_VERSION = 16;

        private const short ENTRY_STATE_EMPTY = -1;

        private const int CACHE_SIZE = 128;
        private const int NUM_RANGES = 256;

        private const int UNNUMBERED_NODE = -1;

        // to help eliminate node clutter by level designers, this is used to cap how many other nodes
        // any given node is allowed to 'see' in the first stage of graph creation "LinkVisibleNodes()".
        private const int MAX_NODE_INITIAL_LINKS = 128;

        private const int MAX_NODES = 1024;

        private const int NODE_HEIGHT = 8;	// how high to lift nodes off the ground after we drop them all (make stair/ramp mapping easier)

        //the graph has two flags, and should not be accessed unless both flags are true!
        public bool m_fGraphPresent; //is the graph in memory?
        public bool m_fGraphPointersSet; //are the entity pointers for the graph all set?
        public bool m_fRoutingComplete; // are the optimal routes computed, yet?

        public CNode[] m_pNodes; // pointer to the memory block that contains all node info
        public CLink[] m_pLinkPool; // big list of all node connections
        public List<char> m_pRouteInfo; //compressed routing information the nodes use.

        public int m_cNodes; //total number of nodes
        public uint m_cLinks; //total number of links

        public DIST_INFO[] m_di; //This is m_cNodes long, but the entries don't correspond to CNode entries.

        public Vector[] m_RangeStart = new Vector[NUM_RANGES];
        public Vector[] m_RangeEnd = new Vector[NUM_RANGES];

        public float m_flShortest;

        public int m_iNearest;

        public int m_minX;
        public int m_minY;
        public int m_minZ;

        public int m_maxX;
        public int m_maxY;
        public int m_maxZ;

        public int m_minBoxX;
        public int m_minBoxY;
        public int m_minBoxZ;

        public int m_maxBoxX;
        public int m_maxBoxY;
        public int m_maxBoxZ;

        public int m_CheckedCounter;

        //The range of nodes.
        public float[] m_RegionMin = new float[3];
        public float[] m_RegionMax = new float[3];

        [NonSerialized]
        public CACHE_ENTRY[] m_Cache;

        public uint[] m_HashPrimes;

        public short[] m_pHashLinks;

        public uint m_nHashLinks;
        /*
        *	kinda sleazy. In order to allow variety in active idles for monster groups in a room with more than one node, 
        *	we keep track of the last node we searched from and store it here. Subsequent searches by other monsters will pick
        *	up where the last search stopped.
        */
        public int m_iLastActiveIdleSearch;

        //another such system used to track the search for cover nodes, helps greatly with two monsters trying to get to the same node.
        public int m_iLastCoverSearch;

        //functions to create the graph

        /// <summary>
        /// <para>
        /// The first, most basic function of node graph creation,
        /// this connects every node to every other node that it can see.
        /// Expects a pointer to an empty connection pool and a stream to write progress to.
        /// Returns the total number of initial links.
        /// </para>
        /// <para>If there's a problem with this process, the index of the offending node will be written to badNode</para>
        /// </summary>
        /// <param name="linkPool"></param>
        /// <param name="writer"></param>
        /// <param name="badNode"></param>
        /// <returns></returns>
        public int LinkVisibleNodes(CLink[] linkPool, StreamWriter writer, out int badNode)
        {
            // !!!BUGBUG - this function returns 0 if there is a problem in the middle of connecting the graph
            // it also returns 0 if none of the nodes in a level can see each other. piBadNode is ALWAYS read
            // by BuildNodeGraph() if this function returns a 0, so make sure that it doesn't get some random
            // number back.
            badNode = 0;

            if (m_cNodes <= 0)
            {
                Log.Alert(AlertType.AIConsole, "No Nodes!\n");
                return 0;
            }

            // if the file pointer is bad, don't blow up, just don't write the
            // file.
            if (writer == null)
            {
                Log.Alert(AlertType.AIConsole, "**LinkVisibleNodes:\ncan't write to file.");
            }
            else
            {
                writer.WriteLine("----------------------------------------------------------------------------");
                writer.WriteLine("LinkVisibleNodes - Initial Connections");
                writer.WriteLine("----------------------------------------------------------------------------");
            }

            var cTotalLinks = 0;// start with no connections

            // to keep track of the maximum number of initial links any node had so far.
            // this lets us keep an eye on MAX_NODE_INITIAL_LINKS to ensure that we are
            // being generous enough.
            var cMaxInitialLinks = 0;

            for (var i = 0; i < m_cNodes; ++i)
            {
                var cLinksThisNode = 0;// reset this count for each node.

                writer?.WriteLine($"Node #{i:4}:");
                writer?.WriteLine();

                for (var z = 0; z < MAX_NODE_INITIAL_LINKS; ++z)
                {// clear out the important fields in the link pool for this node
                    linkPool[cTotalLinks + z].m_iSrcNode = i;// so each link knows which node it originates from
                    linkPool[cTotalLinks + z].m_iDestNode = 0;
                    linkPool[cTotalLinks + z].LinkEntity = null;
                }

                m_pNodes[i].m_iFirstLink = cTotalLinks;

                // now build a list of every other node that this node can see
                for (var j = 0; j < m_cNodes; ++j)
                {
                    if (j == i)
                    {// don't connect to self!
                        continue;
                    }

#if false
			
			        if ( (m_pNodes[ i ].m_afNodeInfo & bits_NODE_WATER) != (m_pNodes[ j ].m_afNodeInfo & bits_NODE_WATER) )
			        {
				        // don't connect water nodes to air nodes or land nodes. It just wouldn't be prudent at this juncture.
				        continue;
			        }
#else
                    if ((m_pNodes[i].m_afNodeInfo & NodeType.Group_Realm) != (m_pNodes[j].m_afNodeInfo & NodeType.Group_Realm))
                    {
                        // don't connect air nodes to water nodes to land nodes. It just wouldn't be prudent at this juncture.
                        continue;
                    }
#endif

                    BaseEntity pTraceEnt = null;

                    Trace.Line(m_pNodes[i].m_vecOrigin,
                                     m_pNodes[j].m_vecOrigin,
                                     TraceFlags.IgnoreMonsters,
                                     PlayerUtils.BodyQueueHead.Edict(),//!!!HACKHACK no real ent to supply here, using a global we don't care about
                                     out var tr);

                    if (tr.StartSolid)
                    {
                        continue;
                    }

                    if (tr.Fraction != 1.0)
                    {
                        // trace hit a brush ent, trace backwards to make sure that this ent is the only thing in the way.
                        pTraceEnt = tr.Hit.Entity();// store the ent that the trace hit, for comparison

                        Trace.Line(m_pNodes[j].m_vecOrigin,
                                         m_pNodes[i].m_vecOrigin,
                                         TraceFlags.IgnoreMonsters,
                                         PlayerUtils.BodyQueueHead.Edict(),//!!!HACKHACK no real ent to supply here, using a global we don't care about
                                         out tr);

                        var hitEnt = tr.Hit.TryGetEntity();

                        // there is a solid_bsp ent in the way of these two nodes, so we must record several things about in order to keep
                        // track of it in the pathfinding code, as well as through save and restore of the node graph. ANY data that is manipulated 
                        // as part of the process of adding a LINKENT to a connection here must also be done in CGraph::SetGraphPointers, where reloaded
                        // graphs are prepared for use.
                        if (hitEnt == pTraceEnt && hitEnt.ClassName != "worldspawn")
                        {
                            // get a pointer
                            linkPool[cTotalLinks].LinkEntity = hitEnt;

                            // record the modelname, so that we can save/load node trees
                            linkPool[cTotalLinks].m_szLinkEntModelname = hitEnt.ModelName;

                            // set the flag for this ent that indicates that it is attached to the world graph
                            // if this ent is removed from the world, it must also be removed from the connections
                            // that it formerly blocked.
                            if (0 == (hitEnt.Flags & EntFlags.Graphed))
                            {
                                hitEnt.Flags |= EntFlags.Graphed;
                            }
                        }
                        else
                        {// even if the ent wasn't there, these nodes couldn't be connected. Skip.
                            continue;
                        }
                    }

                    if (writer != null)
                    {
                        writer.Write($"{j:4}");

                        if (linkPool[cTotalLinks].LinkEntity != null)
                        {
                            // record info about the ent in the way, if any.
                            writer.Write($"  Entity on connection: {pTraceEnt.ClassName}, name: {pTraceEnt.TargetName}  Model: {pTraceEnt.ModelName}");
                        }

                        writer.WriteLine();
                    }

                    linkPool[cTotalLinks].m_iDestNode = j;
                    cLinksThisNode++;
                    cTotalLinks++;

                    // If we hit this, either a level designer is placing too many nodes in the same area, or 
                    // we need to allow for a larger initial link pool.
                    if (cLinksThisNode == MAX_NODE_INITIAL_LINKS)
                    {
                        Log.Alert(AlertType.AIConsole, "**LinkVisibleNodes:\nNode %d has NodeLinks > MAX_NODE_INITIAL_LINKS", i);
                        writer?.WriteLine($"** NODE {i} HAS NodeLinks > MAX_NODE_INITIAL_LINKS **");
                        badNode = i;
                        return 0;
                    }
                    else if (cTotalLinks > MAX_NODE_INITIAL_LINKS * m_cNodes)
                    {// this is paranoia
                        Log.Alert(AlertType.AIConsole, "**LinkVisibleNodes:\nTotalLinks > MAX_NODE_INITIAL_LINKS * NUMNODES");
                        badNode = i;
                        return 0;
                    }

                    if (cLinksThisNode == 0)
                    {
                        writer?.WriteLine("**NO INITIAL LINKS**");
                    }

                    // record the connection info in the link pool
                    m_pNodes[i].m_cNumLinks = cLinksThisNode;

                    // keep track of the most initial links ANY node had, so we can figure out
                    // if we have a large enough default link pool
                    if (cLinksThisNode > cMaxInitialLinks)
                    {
                        cMaxInitialLinks = cLinksThisNode;
                    }
                }

                writer?.WriteLine("----------------------------------------------------------------------------");
            }

            if (writer != null)
            {
                writer.WriteLine();
                writer.WriteLine($"{cTotalLinks:4} Total Initial Connections - {cMaxInitialLinks:4} Maximum connections for a single node.");
                writer.WriteLine("----------------------------------------------------------------------------");
                writer.WriteLine();
                writer.WriteLine();
            }

            return cTotalLinks;
        }

        public int RejectInlineLinks(CLink[] pLinkPool, StreamWriter writer)
        {
            bool fRestartLoop;// have to restart the J loop if we eliminate a link.

            CNode pSrcNode;
            CNode pCheckNode;// the node we are testing for (one of pSrcNode's connections)
            CNode pTestNode;// the node we are checking against ( also one of pSrcNode's connections)

            float flDistToTestNode, flDistToCheckNode;

            Vector2D vec2DirToTestNode, vec2DirToCheckNode;

            if (writer != null)
            {
                writer.WriteLine("----------------------------------------------------------------------------");
                writer.WriteLine("InLine Rejection:");
                writer.WriteLine("----------------------------------------------------------------------------");
            }

            var cRejectedLinks = 0;

            for (var i = 0; i < m_cNodes; ++i)
            {
                pSrcNode = m_pNodes[i];

                writer?.WriteLine($"Node {i:3}:");

                for (var j = 0; j < pSrcNode.m_cNumLinks; ++j)
                {
                    pCheckNode = m_pNodes[pLinkPool[pSrcNode.m_iFirstLink + j].m_iDestNode];

                    vec2DirToCheckNode = (pCheckNode.m_vecOrigin - pSrcNode.m_vecOrigin).Make2D();
                    flDistToCheckNode = vec2DirToCheckNode.Length();
                    vec2DirToCheckNode = vec2DirToCheckNode.Normalize();

                    pLinkPool[pSrcNode.m_iFirstLink + j].m_flWeight = flDistToCheckNode;

                    fRestartLoop = false;
                    for (var k = 0; k < pSrcNode.m_cNumLinks && !fRestartLoop; ++k)
                    {
                        if (k == j)
                        {// don't check against same node
                            continue;
                        }

                        pTestNode = m_pNodes[pLinkPool[pSrcNode.m_iFirstLink + k].m_iDestNode];

                        vec2DirToTestNode = (pTestNode.m_vecOrigin - pSrcNode.m_vecOrigin).Make2D();

                        flDistToTestNode = vec2DirToTestNode.Length();
                        vec2DirToTestNode = vec2DirToTestNode.Normalize();

                        if (vec2DirToCheckNode.DotProduct(vec2DirToTestNode) >= 0.998)
                        {
                            // there's a chance that TestNode intersects the line to CheckNode. If so, we should disconnect the link to CheckNode. 
                            if (flDistToTestNode < flDistToCheckNode)
                            {
                                writer?.WriteLine($"REJECTED NODE {pLinkPool[pSrcNode.m_iFirstLink + j].m_iDestNode:3} through Node {pLinkPool[pSrcNode.m_iFirstLink + k].m_iDestNode:3}, Dot = {vec2DirToCheckNode.DotProduct(vec2DirToTestNode):8}\n");

                                pLinkPool[pSrcNode.m_iFirstLink + j] = pLinkPool[pSrcNode.m_iFirstLink + (pSrcNode.m_cNumLinks - 1)];
                                --pSrcNode.m_cNumLinks;
                                --j;

                                ++cRejectedLinks;// keeping track of how many links are cut, so that we can return that value.

                                fRestartLoop = true;
                            }
                        }
                    }
                }

                if (writer != null)
                {
                    writer.WriteLine("----------------------------------------------------------------------------\n");
                    writer.WriteLine();
                }
            }

            return cRejectedLinks;
        }

        public int FindShortestPath(int[] piPath, int iStart, int iDest, Hull hull, NPCCapabilities afCapMask)
        {
            int iVisitNode;
            int iCurrentNode;
            int iNumPathNodes;

            if (!m_fGraphPresent || !m_fGraphPointersSet)
            {// protect us in the case that the node graph isn't available or built
                Log.Alert(AlertType.AIConsole, "Graph not ready!\n");
                return 0;
            }

            if (iStart < 0 || iStart > m_cNodes)
            {// The start node is bad?
                Log.Alert(AlertType.AIConsole, "Can't build a path, iStart is %d!\n", iStart);
                return 0;
            }

            if (iStart == iDest)
            {
                piPath[0] = iStart;
                piPath[1] = iDest;
                return 2;
            }

            // Is routing information present.
            //
            if (m_fRoutingComplete)
            {
                int iCap = CapIndex(afCapMask);

                iNumPathNodes = 0;
                piPath[iNumPathNodes++] = iStart;
                iCurrentNode = iStart;
                int iNext;

                //ALERT(at_aiconsole, "GOAL: %d to %d\n", iStart, iDest);

                // Until we arrive at the destination
                //
                while (iCurrentNode != iDest)
                {
                    iNext = NextNodeInRoute(iCurrentNode, iDest, hull, iCap);
                    if (iCurrentNode == iNext)
                    {
                        //ALERT(at_aiconsole, "SVD: Can't get there from here..\n");
                        return 0;
                    }
                    if (iNumPathNodes >= NavConstants.MAX_PATH_SIZE)
                    {
                        //ALERT(at_aiconsole, "SVD: Don't return the entire path.\n");
                        break;
                    }
                    piPath[iNumPathNodes++] = iNext;
                    iCurrentNode = iNext;
                }
                //ALERT( at_aiconsole, "SVD: Path with %d nodes.\n", iNumPathNodes);
            }
            else
            {
                var queue = new CQueuePriority();

                LinkFlags hullMask = (LinkFlags)(1 << (int)hull);

                // Mark all the nodes as unvisited.
                //
                for (var i = 0; i < m_cNodes; ++i)
                {
                    m_pNodes[i].m_flClosestSoFar = -1.0f;
                }

                m_pNodes[iStart].m_flClosestSoFar = 0.0f;
                m_pNodes[iStart].m_iPreviousNode = iStart;// tag this as the origin node
                queue.Insert(iStart, 0.0f);// insert start node 

                while (!queue.Empty)
                {
                    // now pull a node out of the queue
                    iCurrentNode = queue.Remove(out var flCurrentDistance);

                    // For straight-line weights, the following Shortcut works. For arbitrary weights,
                    // it doesn't.
                    //
                    if (iCurrentNode == iDest) break;

                    var pCurrentNode = m_pNodes[iCurrentNode];

                    for (var i = 0; i < pCurrentNode.m_cNumLinks; ++i)
                    {
                        // run through all of this node's neighbors
                        iVisitNode = INodeLink(iCurrentNode, i);
                        if ((m_pLinkPool[m_pNodes[iCurrentNode].m_iFirstLink + i].m_afLinkInfo & hullMask) != hullMask)
                        {// monster is too large to walk this connection
                         //ALERT ( at_aiconsole, "fat ass %d/%d\n",m_pLinkPool[ m_pNodes[ iCurrentNode ].m_iFirstLink + i ].m_afLinkInfo, iMonsterHull );
                            continue;
                        }
                        // check the connection from the current node to the node we're about to mark visited and push into the queue				
                        if (m_pLinkPool[m_pNodes[iCurrentNode].m_iFirstLink + i].LinkEntity != null)
                        {
                            // there's a brush ent in the way! Don't mark this node or put it into the queue unless the monster can negotiate it
                            if (!HandleLinkEnt(iCurrentNode, m_pLinkPool[m_pNodes[iCurrentNode].m_iFirstLink + i].LinkEntity, afCapMask, NodeQuery.Static))
                            {// monster should not try to go this way.
                                continue;
                            }
                        }
                        float flOurDistance = flCurrentDistance + m_pLinkPool[m_pNodes[iCurrentNode].m_iFirstLink + i].m_flWeight;
                        if (m_pNodes[iVisitNode].m_flClosestSoFar < -0.5
                           || flOurDistance < m_pNodes[iVisitNode].m_flClosestSoFar - 0.001)
                        {
                            m_pNodes[iVisitNode].m_flClosestSoFar = flOurDistance;
                            m_pNodes[iVisitNode].m_iPreviousNode = iCurrentNode;

                            queue.Insert(iVisitNode, flOurDistance);
                        }
                    }
                }
                if (m_pNodes[iDest].m_flClosestSoFar < -0.5)
                {// Destination is unreachable, no path found.
                    return 0;
                }

                // the queue is not empty

                // now we must walk backwards through the m_iPreviousNode field, and count how many connections there are in the path
                iCurrentNode = iDest;
                iNumPathNodes = 1;// count the dest

                while (iCurrentNode != iStart)
                {
                    iNumPathNodes++;
                    iCurrentNode = m_pNodes[iCurrentNode].m_iPreviousNode;
                }

                iCurrentNode = iDest;
                for (var i = iNumPathNodes - 1; i >= 0; --i)
                {
                    piPath[i] = iCurrentNode;
                    iCurrentNode = m_pNodes[iCurrentNode].m_iPreviousNode;
                }
            }

#if false

	if (m_fRoutingComplete)
	{
		// This will draw the entire path that was generated for the monster.

		for ( int i = 0 ; i < iNumPathNodes - 1 ; i++ )
		{
			MESSAGE_BEGIN( MSG_BROADCAST, SVC_TEMPENTITY );
				WRITE_BYTE( TE_SHOWLINE);
				
				WRITE_COORD( m_pNodes[ piPath[ i ] ].m_vecOrigin.x );
				WRITE_COORD( m_pNodes[ piPath[ i ] ].m_vecOrigin.y );
				WRITE_COORD( m_pNodes[ piPath[ i ] ].m_vecOrigin.z + NODE_HEIGHT );

				WRITE_COORD( m_pNodes[ piPath[ i + 1 ] ].m_vecOrigin.x );
				WRITE_COORD( m_pNodes[ piPath[ i + 1 ] ].m_vecOrigin.y );
				WRITE_COORD( m_pNodes[ piPath[ i + 1 ] ].m_vecOrigin.z + NODE_HEIGHT );
			MESSAGE_END();
		}
	}

#endif
#if false // MAZE map
	MESSAGE_BEGIN( MSG_BROADCAST, SVC_TEMPENTITY );
		WRITE_BYTE( TE_SHOWLINE);
		
		WRITE_COORD( m_pNodes[ 4 ].m_vecOrigin.x );
		WRITE_COORD( m_pNodes[ 4 ].m_vecOrigin.y );
		WRITE_COORD( m_pNodes[ 4 ].m_vecOrigin.z + NODE_HEIGHT );

		WRITE_COORD( m_pNodes[ 9 ].m_vecOrigin.x );
		WRITE_COORD( m_pNodes[ 9 ].m_vecOrigin.y );
		WRITE_COORD( m_pNodes[ 9 ].m_vecOrigin.z + NODE_HEIGHT );
	MESSAGE_END();
#endif

            return iNumPathNodes;
        }

        public int FindNearestNode(Vector vecOrigin, BaseEntity pEntity)
        {
            return FindNearestNode(vecOrigin, GetNodeType(pEntity));
        }

        public int FindNearestNode(Vector vecOrigin, NodeType afNodeTypes)
        {
            if (!m_fGraphPresent || !m_fGraphPointersSet)
            {// protect us in the case that the node graph isn't available
                Log.Alert(AlertType.AIConsole, "Graph not ready!\n");
                return -1;
            }

            // Check with the cache
            //
            var hash = Crc32Algorithm.Compute(BitConverterUtils.GetBytes(vecOrigin));
            if (m_Cache[hash].v == vecOrigin)
            {
                //ALERT(at_aiconsole, "Cache Hit.\n");
                return m_Cache[hash].n;
            }
            else
            {
                //ALERT(at_aiconsole, "Cache Miss.\n");
            }

            // Mark all points as unchecked.
            //
            m_CheckedCounter++;
            if (m_CheckedCounter == 0)
            {
                for (int i = 0; i < m_cNodes; i++)
                {
                    m_di[i].m_CheckedEvent = 0;
                }
                m_CheckedCounter++;
            }

            m_iNearest = -1;
            m_flShortest = 999999.0f; // just a big number.

            // If we can find a visible point, then let CalcBounds set the limits, but if
            // we have no visible point at all to start with, then don't restrict the limits.
            //
#if true
            m_minX = 0; m_maxX = 255;
            m_minY = 0; m_maxY = 255;
            m_minZ = 0; m_maxZ = 255;
            m_minBoxX = 0; m_maxBoxX = 255;
            m_minBoxY = 0; m_maxBoxY = 255;
            m_minBoxZ = 0; m_maxBoxZ = 255;
#else
            m_minBoxX = CALC_RANGE(vecOrigin.x - flDist, m_RegionMin[0], m_RegionMax[0]);
            m_maxBoxX = CALC_RANGE(vecOrigin.x + flDist, m_RegionMin[0], m_RegionMax[0]);
            m_minBoxY = CALC_RANGE(vecOrigin.y - flDist, m_RegionMin[1], m_RegionMax[1]);
            m_maxBoxY = CALC_RANGE(vecOrigin.y + flDist, m_RegionMin[1], m_RegionMax[1]);
            m_minBoxZ = CALC_RANGE(vecOrigin.z - flDist, m_RegionMin[2], m_RegionMax[2]);
            m_maxBoxZ = CALC_RANGE(vecOrigin.z + flDist, m_RegionMin[2], m_RegionMax[2])
            CalcBounds(m_minX, m_maxX, CALC_RANGE(vecOrigin.x, m_RegionMin[0], m_RegionMax[0]), m_pNodes[m_iNearest].m_Region[0]);
            CalcBounds(m_minY, m_maxY, CALC_RANGE(vecOrigin.y, m_RegionMin[1], m_RegionMax[1]), m_pNodes[m_iNearest].m_Region[1]);
            CalcBounds(m_minZ, m_maxZ, CALC_RANGE(vecOrigin.z, m_RegionMin[2], m_RegionMax[2]), m_pNodes[m_iNearest].m_Region[2]);
#endif

            int halfX = (m_minX + m_maxX) / 2;
            int halfY = (m_minY + m_maxY) / 2;
            int halfZ = (m_minZ + m_maxZ) / 2;

            for (var i = halfX; i >= m_minX; i--)
            {
                for (var j = (int)m_RangeStart[i][0]; j <= m_RangeEnd[i][0]; j++)
                {
                    if (0 == (m_pNodes[m_di[j].m_SortedBy[0]].m_afNodeInfo & afNodeTypes)) continue;

                    int rgY = m_pNodes[m_di[j].m_SortedBy[0]].m_Region[1];
                    if (rgY > m_maxBoxY) break;
                    if (rgY < m_minBoxY) continue;

                    int rgZ = m_pNodes[m_di[j].m_SortedBy[0]].m_Region[2];
                    if (rgZ < m_minBoxZ) continue;
                    if (rgZ > m_maxBoxZ) continue;
                    CheckNode(vecOrigin, m_di[j].m_SortedBy[0]);
                }
            }

            for (var i = Math.Max(m_minY, halfY + 1); i <= m_maxY; i++)
            {
                for (var j = (int)m_RangeStart[i][1]; j <= m_RangeEnd[i][1]; j++)
                {
                    if (0 == (m_pNodes[m_di[j].m_SortedBy[1]].m_afNodeInfo & afNodeTypes)) continue;

                    int rgZ = m_pNodes[m_di[j].m_SortedBy[1]].m_Region[2];
                    if (rgZ > m_maxBoxZ) break;
                    if (rgZ < m_minBoxZ) continue;
                    int rgX = m_pNodes[m_di[j].m_SortedBy[1]].m_Region[0];
                    if (rgX < m_minBoxX) continue;
                    if (rgX > m_maxBoxX) continue;
                    CheckNode(vecOrigin, m_di[j].m_SortedBy[1]);
                }
            }

            for (var i = Math.Min(m_maxZ, halfZ); i >= m_minZ; i--)
            {
                for (var j = (int)m_RangeStart[i][2]; j <= m_RangeEnd[i][2]; j++)
                {
                    if (0 == (m_pNodes[m_di[j].m_SortedBy[2]].m_afNodeInfo & afNodeTypes)) continue;

                    int rgX = m_pNodes[m_di[j].m_SortedBy[2]].m_Region[0];
                    if (rgX > m_maxBoxX) break;
                    if (rgX < m_minBoxX) continue;
                    int rgY = m_pNodes[m_di[j].m_SortedBy[2]].m_Region[1];
                    if (rgY < m_minBoxY) continue;
                    if (rgY > m_maxBoxY) continue;
                    CheckNode(vecOrigin, m_di[j].m_SortedBy[2]);
                }
            }

            for (var i = Math.Max(m_minX, halfX + 1); i <= m_maxX; i++)
            {
                for (var j = (int)m_RangeStart[i][0]; j <= m_RangeEnd[i][0]; j++)
                {
                    if (0 == (m_pNodes[m_di[j].m_SortedBy[0]].m_afNodeInfo & afNodeTypes)) continue;

                    int rgY = m_pNodes[m_di[j].m_SortedBy[0]].m_Region[1];
                    if (rgY > m_maxBoxY) break;
                    if (rgY < m_minBoxY) continue;

                    int rgZ = m_pNodes[m_di[j].m_SortedBy[0]].m_Region[2];
                    if (rgZ < m_minBoxZ) continue;
                    if (rgZ > m_maxBoxZ) continue;
                    CheckNode(vecOrigin, m_di[j].m_SortedBy[0]);
                }
            }

            for (var i = Math.Min(m_maxY, halfY); i >= m_minY; i--)
            {
                for (var j = (int)m_RangeStart[i][1]; j <= m_RangeEnd[i][1]; j++)
                {
                    if (0 == (m_pNodes[m_di[j].m_SortedBy[1]].m_afNodeInfo & afNodeTypes)) continue;

                    int rgZ = m_pNodes[m_di[j].m_SortedBy[1]].m_Region[2];
                    if (rgZ > m_maxBoxZ) break;
                    if (rgZ < m_minBoxZ) continue;
                    int rgX = m_pNodes[m_di[j].m_SortedBy[1]].m_Region[0];
                    if (rgX < m_minBoxX) continue;
                    if (rgX > m_maxBoxX) continue;
                    CheckNode(vecOrigin, m_di[j].m_SortedBy[1]);
                }
            }

            for (var i = Math.Max(m_minZ, halfZ + 1); i <= m_maxZ; i++)
            {
                for (var j = (int)m_RangeStart[i][2]; j <= m_RangeEnd[i][2]; j++)
                {
                    if (0 == (m_pNodes[m_di[j].m_SortedBy[2]].m_afNodeInfo & afNodeTypes)) continue;

                    int rgX = m_pNodes[m_di[j].m_SortedBy[2]].m_Region[0];
                    if (rgX > m_maxBoxX) break;
                    if (rgX < m_minBoxX) continue;
                    int rgY = m_pNodes[m_di[j].m_SortedBy[2]].m_Region[1];
                    if (rgY < m_minBoxY) continue;
                    if (rgY > m_maxBoxY) continue;
                    CheckNode(vecOrigin, m_di[j].m_SortedBy[2]);
                }
            }

#if false
	        // Verify our answers.
	        //
	        int iNearestCheck = -1;
	        m_flShortest = 8192;// find nodes within this radius

	        for (var i = 0 ; i < m_cNodes ; ++i)
	        {
		        float flDist = ( vecOrigin - m_pNodes[ i ].m_vecOriginPeek ).Length();

		        if ( flDist < m_flShortest )
		        {
			        // make sure that vecOrigin can trace to this node!
			        UTIL_TraceLine ( vecOrigin, m_pNodes[ i ].m_vecOriginPeek, ignore_monsters, 0, &tr );

			        if ( tr.flFraction == 1.0 )
			        {
				        iNearestCheck = i;
				        m_flShortest = flDist;
			        }
		        }
	        }

	        if (iNearestCheck != m_iNearest)
	        {
		        ALERT( at_aiconsole, "NOT closest %d(%f,%f,%f) %d(%f,%f,%f).\n",
			        iNearestCheck,
			        m_pNodes[iNearestCheck].m_vecOriginPeek.x,
			        m_pNodes[iNearestCheck].m_vecOriginPeek.y,
			        m_pNodes[iNearestCheck].m_vecOriginPeek.z,
			        m_iNearest,
			        (m_iNearest == -1?0.0:m_pNodes[m_iNearest].m_vecOriginPeek.x),
			        (m_iNearest == -1?0.0:m_pNodes[m_iNearest].m_vecOriginPeek.y),
			        (m_iNearest == -1?0.0:m_pNodes[m_iNearest].m_vecOriginPeek.z));
	        }
	        if (m_iNearest == -1)
	        {
		        ALERT(at_aiconsole, "All that work for nothing.\n");
	        }
#endif
            m_Cache[hash].v = vecOrigin;
            m_Cache[hash].n = (short)m_iNearest;
            return m_iNearest;
        }

        //int		FindNearestLink ( const Vector &vecTestPoint, int *piNearestLink, bool *pfAlongLine );
        public float PathLength(int iStart, int iDest, Hull hull, NPCCapabilities afCapMask)
        {
            float distance = 0;

            int iMaxLoop = m_cNodes;

            var iCap = CapIndex(afCapMask);

            int iNext;

            for (var iCurrentNode = iStart; iCurrentNode != iDest; iCurrentNode = iNext)
            {
                if (iMaxLoop-- <= 0)
                {
                    Log.Alert(AlertType.Console, "Route Failure\n");
                    return 0;
                }

                iNext = NextNodeInRoute(iCurrentNode, iDest, hull, iCap);
                if (iCurrentNode == iNext)
                {
                    //ALERT(at_aiconsole, "SVD: Can't get there from here..\n");
                    return 0;
                }

                HashSearch((short)iCurrentNode, (short)iNext, out var iLink);
                if (iLink < 0)
                {
                    Log.Alert(AlertType.Console, "HashLinks is broken from %d to %d.\n", iCurrentNode, iDest);
                    return 0;
                }
                var link = Link(iLink);
                distance += link.m_flWeight;
            }

            return distance;
        }

        public int NextNodeInRoute(int iCurrentNode, int iDest, Hull hull, int iCap)
        {
            int iNext = iCurrentNode;
            int nCount = iDest + 1;
            var nodeIndex = m_pNodes[iCurrentNode].m_pNextBestNode[(int)hull, iCap];

            // Until we decode the next best node
            //
            while (nCount > 0)
            {
                char ch = m_pRouteInfo[nodeIndex++];
                //ALERT(at_aiconsole, "C(%d)", ch);
                if (ch < 0)
                {
                    // Sequence phrase
                    //
                    ch = (char)-ch;
                    if (nCount <= ch)
                    {
                        iNext = iDest;
                        nCount = 0;
                        //ALERT(at_aiconsole, "SEQ: iNext/iDest=%d\n", iNext);
                    }
                    else
                    {
                        //ALERT(at_aiconsole, "SEQ: nCount + ch (%d + %d)\n", nCount, ch);
                        nCount -= ch;
                    }
                }
                else
                {
                    //ALERT(at_aiconsole, "C(%d)", *pRoute);

                    // Repeat phrase
                    //
                    if (nCount <= ch + 1)
                    {
                        iNext = iCurrentNode + m_pRouteInfo[nodeIndex];
                        if (iNext >= m_cNodes) iNext -= m_cNodes;
                        else if (iNext < 0) iNext += m_cNodes;
                        nCount = 0;
                        //ALERT(at_aiconsole, "REP: iNext=%d\n", iNext);
                    }
                    else
                    {
                        //ALERT(at_aiconsole, "REP: nCount - ch+1 (%d - %d+1)\n", nCount, ch);
                        nCount = nCount - ch - 1;
                    }
                    ++nodeIndex;
                }
            }

            return iNext;
        }

        /// <summary>
        /// A static query means we're asking about the possiblity of handling this entity at ANY time
        /// A dynamic query means we're asking about it RIGHT NOW.  So we should query the current state
        /// </summary>
        /// <param name="iNode"></param>
        /// <param name="linkEnt"></param>
        /// <param name="afCapMask"></param>
        /// <param name="queryType"></param>
        /// <returns></returns>
        public bool HandleLinkEnt(int iNode, BaseEntity linkEnt, NPCCapabilities afCapMask, NodeQuery queryType)
        {
            if (!m_fGraphPresent || !m_fGraphPointersSet)
            {
                // protect us in the case that the node graph isn't available
                Log.Alert(AlertType.AIConsole, "Graph not ready!\n");
                return false;
            }

            if (linkEnt == null)
            {
                Log.Alert(AlertType.AIConsole, "dead path ent!\n");
                return true;
            }

            // func_door
            if (linkEnt.ClassName == "func_door" || linkEnt.ClassName == "func_door_rotating")
            {
                // ent is a door.
                if (0 != (linkEnt.SpawnFlags & BaseDoor.SF.UseOnly))
                {
                    // door is use only.
                    if (0 != (afCapMask & NPCCapabilities.OpenDoors))
                    {
                        // let monster right through if he can open doors
                        return true;
                    }
                    else
                    {
                        // monster should try for it if the door is open and looks as if it will stay that way
                        return linkEnt.GetToggleState() == ToggleState.AtTop && 0 != (linkEnt.SpawnFlags & BaseDoor.SF.NoAutoReturn);
                    }
                }
                else
                {
                    // door must be opened with a button or trigger field.
                    // monster should try for it if the door is open and looks as if it will stay that way
                    if (linkEnt.GetToggleState() == ToggleState.AtTop && 0 != (linkEnt.SpawnFlags & BaseDoor.SF.NoAutoReturn))
                    {
                        return true;
                    }
                    if (0 != (afCapMask & NPCCapabilities.OpenDoors))
                    {
                        if (0 == (linkEnt.SpawnFlags & BaseDoor.SF.NoMonsters) || queryType == NodeQuery.Static)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
            // func_breakable	
            else if (linkEnt.ClassName == "func_breakable" && queryType == NodeQuery.Static)
            {
                return true;
            }
            else
            {
                Log.Alert(AlertType.AIConsole, $"Unhandled Ent in Path {linkEnt.ClassName}\n");
                return false;
            }
        }

        /// <summary>
        /// <para>
        /// Sometimes the ent that blocks a path is a usable door,
        /// in which case the monster just needs to face the door and fire it.
        /// In other cases, the monster needs to operate a button or lever to get the door to open.
        /// This function will return a pointer to the button if the monster needs to hit a button to open the door,
        /// or returns a pointer to the door if the monster  need only use the door.
        /// </para>
        /// <para>pNode is the node the monster will be standing on when it will need to stop and trigger the ent.</para>
        /// </summary>
        /// <param name="pLink"></param>
        /// <param name="pNode"></param>
        /// <returns></returns>
        public BaseEntity LinkEntForLink(CLink pLink, CNode pNode)
        {
            var linkEnt = pLink.LinkEntity;
            if (linkEnt == null)
            {
                return null;
            }

            BaseEntity search = null;// start search at the top of the ent list.

            if (linkEnt.ClassName == "func_door" || linkEnt.ClassName == "func_door_rotating")
            {
                ///!!!UNDONE - check for TOGGLE or STAY open doors here. If a door is in the way, and is 
                // TOGGLE or STAY OPEN, even monsters that can't open doors can go that way.

                if (0 != (linkEnt.SpawnFlags & BaseDoor.SF.UseOnly))
                {// door is use only, so the door is all the monster has to worry about
                    return linkEnt;
                }

                while (true)
                {
                    var trigger = EntUtils.FindEntityByTarget(search, linkEnt.TargetName);// find the button or trigger

                    if (trigger == null)
                    {
                        // no trigger found
                        // right now this is a problem among auto-open doors, or any door that opens through the use 
                        // of a trigger brush. Trigger brushes have no models, and don't show up in searches. Just allow
                        // monsters to open these sorts of doors for now. 
                        return linkEnt;
                    }

                    search = trigger;

                    if (trigger.ClassName == "func_button" || trigger.ClassName == "func_rot_button")
                    {
                        // only buttons are handled right now. 
                        // trace from the node to the trigger, make sure it's one we can see from the node.
                        // !!!HACKHACK Use bodyqueue here cause there are no ents we really wish to ignore!
                        Trace.Line(pNode.m_vecOrigin, EntUtils.BrushModelOrigin(trigger), TraceFlags.IgnoreMonsters, PlayerUtils.BodyQueueHead.Edict(), out var tr);

                        var hit = tr.Hit.TryGetEntity();

                        if (hit == trigger)
                        {
                            // good to go!
                            return hit;
                        }
                    }
                }
            }
            else
            {
                Log.Alert(AlertType.AIConsole, $"Unsupported PathEnt:\n'{linkEnt.ClassName}'\n");
                return null;
            }
        }

        public void ShowNodeConnections(int iNode)
        {
            if (!m_fGraphPresent || !m_fGraphPointersSet)
            {// protect us in the case that the node graph isn't available or built
                Log.Alert(AlertType.AIConsole, "Graph not ready!\n");
                return;
            }

            if (iNode < 0)
            {
                Log.Alert(AlertType.AIConsole, "Can't show connections for node %d\n", iNode);
                return;
            }

            var pNode = m_pNodes[iNode];

            EntUtils.ParticleEffect(pNode.m_vecOrigin, WorldConstants.g_vecZero, 255, 20);// show node position

            if (pNode.m_cNumLinks <= 0)
            {// no connections!
                Log.Alert(AlertType.AIConsole, "**No Connections!\n");
            }

            for (var i = 0; i < pNode.m_cNumLinks; ++i)
            {
                var pLinkNode = Node(NodeLink(iNode, i).m_iDestNode);
                var vecSpot = pLinkNode.m_vecOrigin;

                var message = NetMessage.Begin(MsgDest.Broadcast, ServerCommand.TempEntity);
                message.WriteByte((int)TempEntityMsg.ShowLine);

                message.WriteCoord(m_pNodes[iNode].m_vecOrigin.x);
                message.WriteCoord(m_pNodes[iNode].m_vecOrigin.y);
                message.WriteCoord(m_pNodes[iNode].m_vecOrigin.z + NODE_HEIGHT);

                message.WriteCoord(vecSpot.x);
                message.WriteCoord(vecSpot.y);
                message.WriteCoord(vecSpot.z + NODE_HEIGHT);
                message.End();
            }
        }

        public void InitGraph()
        {
            // Make the graph unavailable
            //
            m_fGraphPresent = false;
            m_fGraphPointersSet = false;
            m_fRoutingComplete = false;

            // Free the link pool
            //
            m_pLinkPool = null;

            // Free the node info
            //
            m_pNodes = null;

            m_di = null;

            // Free the routing info.
            //
            m_pRouteInfo = null;
            m_pHashLinks = null;

            // Zero node and link counts
            //
            m_cNodes = 0;
            m_cLinks = 0;

            m_iLastActiveIdleSearch = 0;
            m_iLastCoverSearch = 0;
        }

        public bool AllocNodes()
        {
            //TODO: obsolete
            //  malloc all of the nodes
            m_pNodes = new CNode[MAX_NODES];

            // could not malloc space for all the nodes!
            if (m_pNodes == null)
            {
                Log.Alert(AlertType.AIConsole, $"**ERROR**\nCouldn't malloc {m_cNodes} nodes!\n");
                return false;
            }

            return true;
        }

        /// <summary>
        /// <para>
        /// this function checks the date of the BSP file that was just loaded and the date of the associated .NOD file.
        /// If the NOD file is not present, or is older than the BSP file, we rebuild it.
        /// </para>
        /// <para>
        /// returns false if the .NOD file doesn't qualify and needs
        /// to be rebuilt.
        /// </para>
        /// <para>
        /// !!!BUGBUG - the file times we get back are 20 hours ahead!
        /// since this happens consistantly, we can still correctly 
        /// determine which of the 2 files is newer. This needs fixed,
        /// though. ( I now suspect that we are getting GMT back from
        /// these functions and must compensate for local time ) (sjb)
        /// </para>
        /// </summary>
        /// <param name="mapName"></param>
        /// <returns></returns>
        public bool CheckNODFile(string mapName)
        {
            var bspFileName = Engine.FileSystem.GetAbsolutePath($"maps/{mapName}.bsp");
            var graphFileName = Engine.FileSystem.GetAbsolutePath($"maps/graphs/{mapName}.nod");

            try
            {
                var bspTime = File.GetLastWriteTimeUtc(bspFileName);
                var graphTime = File.GetLastWriteTimeUtc(graphFileName);

                if (bspTime > graphTime)
                {
                    // BSP file is newer.
                    Log.Alert(AlertType.AIConsole, ".NOD File will be updated\n\n");
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        public static CGraph FLoadGraph(string mapName)
        {
            // make sure directories have been made
            var fileName = Engine.FileSystem.GetAbsolutePath($"maps/graphs/{mapName}.nod");

            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            try
            {
                using (var file = File.OpenRead(fileName))
                {
                    using (var reader = new BinaryReader(file))
                    {
                        var version = reader.ReadInt32();

                        if (version != GRAPH_VERSION)
                        {
                            // This file was written by a different build of the dll!
                            //
                            Log.Alert(AlertType.AIConsole, $"**ERROR** Graph version is {version}, expected {GRAPH_VERSION}\n");
                            return null;
                        }

                        var formatter = new BinaryFormatter();

                        // Read the graph class
                        //
                        var graph = (CGraph)formatter.Deserialize(file);

                        return graph;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Alert(AlertType.AIConsole, $"An error occured while loading the node graph:\n{e.Message}\n");
            }

            return null;
        }

        public bool FSaveGraph(string mapName)
        {
            if (!m_fGraphPresent || !m_fGraphPointersSet)
            {// protect us in the case that the node graph isn't available or built
                Log.Alert(AlertType.AIConsole, "Graph not ready!\n");
                return false;
            }

            // make sure directories have been made
            var fileName = Engine.FileSystem.GetAbsolutePath($"maps/graphs/{mapName}.nod");

            Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            try
            {
                using (var file = File.OpenWrite(fileName))
                {
                    Log.Alert(AlertType.AIConsole, $"Created: {fileName}\n");

                    using (var writer = new BinaryWriter(file))
                    {
                        // write the version
                        writer.Write(GRAPH_VERSION);

                        var formatter = new BinaryFormatter();

                        // write the CGraph class
                        // This will serialize all members as well
                        formatter.Serialize(writer.BaseStream, this);

                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // couldn't create
                Log.Alert(AlertType.AIConsole, $"Couldn't Create: {fileName}\n");
                return false;
            }
        }

        public bool FSetGraphPointers()
        {
            for (var i = 0; i < m_cLinks; ++i)
            {
                // go through all of the links
                if (m_pLinkPool[i].LinkEntity != null)
                {
                    // when graphs are saved, any valid pointers are will be non-zero, signifying that we should
                    // reset those pointers upon reloading. Any pointers that were NULL when the graph was saved
                    // will be NULL when reloaded, and will ignored by this function.

                    var name = m_pLinkPool[i].m_szLinkEntModelname;

                    var linkEnt = EntUtils.FindEntityByString(null, "model", name);

                    if (linkEnt == null)
                    {
                        // the ent isn't around anymore? Either there is a major problem, or it was removed from the world
                        // ( like a func_breakable that's been destroyed or something ). Make sure that LinkEnt is null.
                        Log.Alert(AlertType.AIConsole, $"**Could not find model {name}\n");
                        m_pLinkPool[i].LinkEntity = null;
                    }
                    else
                    {
                        m_pLinkPool[i].LinkEntity = linkEnt;

                        if (0 == (m_pLinkPool[i].LinkEntity.Flags & EntFlags.Graphed))
                        {
                            m_pLinkPool[i].LinkEntity.Flags |= EntFlags.Graphed;
                        }
                    }
                }
            }

            // the pointers are now set.
            m_fGraphPointersSet = true;
            return true;
        }

        private static void CalcBounds(ref int Lower, ref int Upper, int Goal, int Best)
        {
            int Temp = (2 * Goal) - Best;
            if (Best > Goal)
            {
                Lower = Math.Max(0, Temp);
                Upper = Best;
            }
            else
            {
                Upper = Math.Min(255, Temp);
                Lower = Best;
            }
        }

        // Convert from [-8192,8192] to [0, 255]
        //
        private static int CALC_RANGE(float x, float lower, float upper)
        {
            return NUM_RANGES * (((int)x) - ((int)lower)) / (((int)upper) - ((int)lower) + 1);
        }

        private static void UpdateRange(ref int minValue, ref int maxValue, int Goal, int Best)
        {
            int Lower = 0, Upper = 0;
            CalcBounds(ref Lower, ref Upper, Goal, Best);
            if (Upper < maxValue) maxValue = Upper;
            if (minValue < Lower) minValue = Lower;
        }

        public void CheckNode(Vector vecOrigin, int iNode)
        {
            // Have we already seen this point before?.
            //
            if (m_di[iNode].m_CheckedEvent == m_CheckedCounter) return;
            m_di[iNode].m_CheckedEvent = m_CheckedCounter;

            float flDist = (vecOrigin - m_pNodes[iNode].m_vecOriginPeek).Length();

            if (flDist < m_flShortest)
            {
                // make sure that vecOrigin can trace to this node!
                Trace.Line(vecOrigin, m_pNodes[iNode].m_vecOriginPeek, TraceFlags.IgnoreMonsters, null, out var tr);

                if (tr.Fraction == 1.0)
                {
                    m_iNearest = iNode;
                    m_flShortest = flDist;

                    UpdateRange(ref m_minX, ref m_maxX, CALC_RANGE(vecOrigin.x, m_RegionMin[0], m_RegionMax[0]), m_pNodes[iNode].m_Region[0]);
                    UpdateRange(ref m_minY, ref m_maxY, CALC_RANGE(vecOrigin.y, m_RegionMin[1], m_RegionMax[1]), m_pNodes[iNode].m_Region[1]);
                    UpdateRange(ref m_minZ, ref m_maxZ, CALC_RANGE(vecOrigin.z, m_RegionMin[2], m_RegionMax[2]), m_pNodes[iNode].m_Region[2]);

                    // From maxCircle, calculate maximum bounds box. All points must be
                    // simultaneously inside all bounds of the box.
                    //
                    m_minBoxX = CALC_RANGE(vecOrigin.x - flDist, m_RegionMin[0], m_RegionMax[0]);
                    m_maxBoxX = CALC_RANGE(vecOrigin.x + flDist, m_RegionMin[0], m_RegionMax[0]);
                    m_minBoxY = CALC_RANGE(vecOrigin.y - flDist, m_RegionMin[1], m_RegionMax[1]);
                    m_maxBoxY = CALC_RANGE(vecOrigin.y + flDist, m_RegionMin[1], m_RegionMax[1]);
                    m_minBoxZ = CALC_RANGE(vecOrigin.z - flDist, m_RegionMin[2], m_RegionMax[2]);
                    m_maxBoxZ = CALC_RANGE(vecOrigin.z + flDist, m_RegionMin[2], m_RegionMax[2]);
                }
            }
        }

        public void BuildRegionTables()
        {
            if (m_di != null)
            {
                m_di = null;
            }

            // Go ahead and setup for range searching the nodes for FindNearestNodes
            //
            m_di = new DIST_INFO[m_cNodes];

            // Calculate regions for all the nodes.
            //
            //
            for (var i = 0; i < 3; ++i)
            {
                m_RegionMin[i] = 999999999.0f; // just a big number out there;
                m_RegionMax[i] = -999999999.0f; // just a big number out there;
            }

            for (var i = 0; i < m_cNodes; ++i)
            {
                if (m_pNodes[i].m_vecOrigin.x < m_RegionMin[0])
                    m_RegionMin[0] = m_pNodes[i].m_vecOrigin.x;
                if (m_pNodes[i].m_vecOrigin.y < m_RegionMin[1])
                    m_RegionMin[1] = m_pNodes[i].m_vecOrigin.y;
                if (m_pNodes[i].m_vecOrigin.z < m_RegionMin[2])
                    m_RegionMin[2] = m_pNodes[i].m_vecOrigin.z;

                if (m_pNodes[i].m_vecOrigin.x > m_RegionMax[0])
                    m_RegionMax[0] = m_pNodes[i].m_vecOrigin.x;
                if (m_pNodes[i].m_vecOrigin.y > m_RegionMax[1])
                    m_RegionMax[1] = m_pNodes[i].m_vecOrigin.y;
                if (m_pNodes[i].m_vecOrigin.z > m_RegionMax[2])
                    m_RegionMax[2] = m_pNodes[i].m_vecOrigin.z;
            }
            for (var i = 0; i < m_cNodes; ++i)
            {
                m_pNodes[i].m_Region[0] = CALC_RANGE(m_pNodes[i].m_vecOrigin.x, m_RegionMin[0], m_RegionMax[0]);
                m_pNodes[i].m_Region[1] = CALC_RANGE(m_pNodes[i].m_vecOrigin.y, m_RegionMin[1], m_RegionMax[1]);
                m_pNodes[i].m_Region[2] = CALC_RANGE(m_pNodes[i].m_vecOrigin.z, m_RegionMin[2], m_RegionMax[2]);
            }

            for (var i = 0; i < 3; ++i)
            {
                for (var j = 0; j < NUM_RANGES; ++j)
                {
                    m_RangeStart[j][i] = 255;
                    m_RangeEnd[j][i] = 0;
                }
                for (var j = 0; j < m_cNodes; ++j)
                {
                    m_di[j].m_SortedBy[i] = j;
                }

                for (var j = 0; j < m_cNodes - 1; ++j)
                {
                    int jNode = m_di[j].m_SortedBy[i];
                    int jCodeX = m_pNodes[jNode].m_Region[0];
                    int jCodeY = m_pNodes[jNode].m_Region[1];
                    int jCodeZ = m_pNodes[jNode].m_Region[2];
                    int jCode = 0;
                    switch (i)
                    {
                        case 0:
                            jCode = (jCodeX << 16) + (jCodeY << 8) + jCodeZ;
                            break;
                        case 1:
                            jCode = (jCodeY << 16) + (jCodeZ << 8) + jCodeX;
                            break;
                        case 2:
                            jCode = (jCodeZ << 16) + (jCodeX << 8) + jCodeY;
                            break;
                    }

                    for (int k = j + 1; k < m_cNodes; k++)
                    {
                        int kNode = m_di[k].m_SortedBy[i];
                        int kCodeX = m_pNodes[kNode].m_Region[0];
                        int kCodeY = m_pNodes[kNode].m_Region[1];
                        int kCodeZ = m_pNodes[kNode].m_Region[2];
                        int kCode = 0;
                        switch (i)
                        {
                            case 0:
                                kCode = (kCodeX << 16) + (kCodeY << 8) + kCodeZ;
                                break;
                            case 1:
                                kCode = (kCodeY << 16) + (kCodeZ << 8) + kCodeX;
                                break;
                            case 2:
                                kCode = (kCodeZ << 16) + (kCodeX << 8) + kCodeY;
                                break;
                        }

                        if (kCode < jCode)
                        {
                            // Swap j and k entries.
                            //
                            int Tmp = m_di[j].m_SortedBy[i];
                            m_di[j].m_SortedBy[i] = m_di[k].m_SortedBy[i];
                            m_di[k].m_SortedBy[i] = Tmp;
                        }
                    }
                }
            }

            // Generate lookup tables.
            //
            for (var i = 0; i < m_cNodes; ++i)
            {
                int CodeX = m_pNodes[m_di[i].m_SortedBy[0]].m_Region[0];
                int CodeY = m_pNodes[m_di[i].m_SortedBy[1]].m_Region[1];
                int CodeZ = m_pNodes[m_di[i].m_SortedBy[2]].m_Region[2];

                if (i < m_RangeStart[CodeX][0])
                {
                    m_RangeStart[CodeX][0] = i;
                }
                if (i < m_RangeStart[CodeY][1])
                {
                    m_RangeStart[CodeY][1] = i;
                }
                if (i < m_RangeStart[CodeZ][2])
                {
                    m_RangeStart[CodeZ][2] = i;
                }
                if (m_RangeEnd[0][CodeX] < i)
                {
                    m_RangeEnd[0][CodeX] = i;
                }
                if (m_RangeEnd[1][CodeY] < i)
                {
                    m_RangeEnd[1][CodeY] = i;
                }
                if (m_RangeEnd[2][CodeZ] < i)
                {
                    m_RangeEnd[2][CodeZ] = i;
                }
            }

            // Initialize the cache.
            //
            m_Cache = new CACHE_ENTRY[CACHE_SIZE];
        }

        private int FROM_TO(int x, int y)
        {
            return (x * m_cNodes) + y;
        }

        public void ComputeStaticRoutingTables()
        {
            m_pRouteInfo = new List<char>();

            int nRoutes = m_cNodes * m_cNodes;

            var Routes = new short[nRoutes];

            var pMyPath = new int[m_cNodes];
            var BestNextNodes = new ushort[m_cNodes];
            var pRoute = new List<char>(m_cNodes * 2);

            int nTotalCompressedSize = 0;
            for (int iHull = 0; iHull < WorldConstants.NUM_HULLS; ++iHull)
            {
                for (int iCap = 0; iCap < 2; ++iCap)
                {
                    var iCapMask = NPCCapabilities.None;
                    switch (iCap)
                    {
                        case 0:
                            iCapMask = NPCCapabilities.None;
                            break;

                        case 1:
                            iCapMask = NPCCapabilities.DoorsGroup;
                            break;
                    }

                    // Initialize Routing table to uncalculated.
                    //
                    int iFrom;
                    for (iFrom = 0; iFrom < m_cNodes; iFrom++)
                    {
                        for (int iTo = 0; iTo < m_cNodes; iTo++)
                        {
                            Routes[FROM_TO(iFrom, iTo)] = -1;
                        }
                    }

                    for (iFrom = 0; iFrom < m_cNodes; iFrom++)
                    {
                        for (int iTo = m_cNodes - 1; iTo >= 0; iTo--)
                        {
                            if (Routes[FROM_TO(iFrom, iTo)] != -1) continue;

                            int cPathSize = FindShortestPath(pMyPath, iFrom, iTo, (Hull)iHull, iCapMask);

                            // Use the computed path to update the routing table.
                            //
                            if (cPathSize > 1)
                            {
                                for (int iNode = 0; iNode < cPathSize - 1; iNode++)
                                {
                                    int iStart = pMyPath[iNode];
                                    int iNext = pMyPath[iNode + 1];
                                    for (int iNode1 = iNode + 1; iNode1 < cPathSize; iNode1++)
                                    {
                                        int iEnd = pMyPath[iNode1];
                                        Routes[FROM_TO(iStart, iEnd)] = (short)iNext;
                                    }
                                }
#if false
							// Well, at first glance, this should work, but actually it's safer
							// to be told explictly that you can take a series of node in a
							// particular direction. Some links don't appear to have links in
							// the opposite direction.
							//
							for (iNode = cPathSize-1; iNode >= 1; iNode--)
							{
								int iStart = pMyPath[iNode];
								int iNext  = pMyPath[iNode-1];
								for (int iNode1 = iNode-1; iNode1 >= 0; iNode1--)
								{
									int iEnd = pMyPath[iNode1];
									Routes[FROM_TO(iStart, iEnd)] = iNext;
								}
							}
#endif
                            }
                            else
                            {
                                Routes[FROM_TO(iFrom, iTo)] = (short)iFrom;
                                Routes[FROM_TO(iTo, iFrom)] = (short)iTo;
                            }
                        }
                    }

                    for (iFrom = 0; iFrom < m_cNodes; iFrom++)
                    {
                        for (int iTo = 0; iTo < m_cNodes; iTo++)
                        {
                            BestNextNodes[iTo] = (ushort)Routes[FROM_TO(iFrom, iTo)];
                        }

                        // Compress this node's routing table.
                        //
                        int iLastNode = 9999999; // just really big.
                        int cSequence = 0;
                        int cRepeats = 0;
                        int CompressedSize = 0;

                        for (int i = 0; i < m_cNodes; i++)
                        {
                            var CanRepeat = ((BestNextNodes[i] == iLastNode) && cRepeats < 127);
                            var CanSequence = (BestNextNodes[i] == i && cSequence < 128);

                            if (0 != cRepeats)
                            {
                                if (CanRepeat)
                                {
                                    cRepeats++;
                                }
                                else
                                {
                                    // Emit the repeat phrase.
                                    //
                                    CompressedSize += 2; // (count-1, iLastNode-i)
                                    pRoute.Add((char)(cRepeats - 1));
                                    int a = iLastNode - iFrom;
                                    int b = iLastNode - iFrom + m_cNodes;
                                    int c = iLastNode - iFrom - m_cNodes;
                                    if (-128 <= a && a <= 127)
                                    {
                                        pRoute.Add((char)a);
                                    }
                                    else if (-128 <= b && b <= 127)
                                    {
                                        pRoute.Add((char)b);
                                    }
                                    else if (-128 <= c && c <= 127)
                                    {
                                        pRoute.Add((char)c);
                                    }
                                    else
                                    {
                                        Log.Alert(AlertType.AIConsole, "Nodes need sorting (%d,%d)!\n", iLastNode, iFrom);
                                    }
                                    cRepeats = 0;

                                    if (CanSequence)
                                    {
                                        // Start a sequence.
                                        //
                                        cSequence++;
                                    }
                                    else
                                    {
                                        // Start another repeat.
                                        //
                                        cRepeats++;
                                    }
                                }
                            }
                            else if (0 != cSequence)
                            {
                                if (CanSequence)
                                {
                                    cSequence++;
                                }
                                else
                                {
                                    // It may be advantageous to combine
                                    // a single-entry sequence phrase with the
                                    // next repeat phrase.
                                    //
                                    if (cSequence == 1 && CanRepeat)
                                    {
                                        // Combine with repeat phrase.
                                        //
                                        cRepeats = 2;
                                        cSequence = 0;
                                    }
                                    else
                                    {
                                        // Emit the sequence phrase.
                                        //
                                        ++CompressedSize; // (-count)
                                        pRoute.Add((char)-cSequence);
                                        cSequence = 0;

                                        // Start a repeat sequence.
                                        //
                                        cRepeats++;
                                    }
                                }
                            }
                            else
                            {
                                if (CanSequence)
                                {
                                    // Start a sequence phrase.
                                    //
                                    cSequence++;
                                }
                                else
                                {
                                    // Start a repeat sequence.
                                    //
                                    cRepeats++;
                                }
                            }
                            iLastNode = BestNextNodes[i];
                        }
                        if (0 != cRepeats)
                        {
                            // Emit the repeat phrase.
                            //
                            CompressedSize += 2;
                            pRoute.Add((char)(cRepeats - 1));
#if false
						iLastNode = iFrom + *pRoute;
						if (iLastNode >= m_cNodes) iLastNode -= m_cNodes;
						else if (iLastNode < 0) iLastNode += m_cNodes;
#endif
                            int a = iLastNode - iFrom;
                            int b = iLastNode - iFrom + m_cNodes;
                            int c = iLastNode - iFrom - m_cNodes;
                            if (-128 <= a && a <= 127)
                            {
                                pRoute.Add((char)a);
                            }
                            else if (-128 <= b && b <= 127)
                            {
                                pRoute.Add((char)b);
                            }
                            else if (-128 <= c && c <= 127)
                            {
                                pRoute.Add((char)c);
                            }
                            else
                            {
                                Log.Alert(AlertType.AIConsole, "Nodes need sorting (%d,%d)!\n", iLastNode, iFrom);
                            }
                        }
                        if (0 != cSequence)
                        {
                            // Emit the Sequence phrase.
                            //
                            ++CompressedSize;
                            pRoute.Add((char)-cSequence);
                        }

                        // Go find a place to store this thing and point to it.
                        //
                        int nRoute = pRoute.Count;
                        if (m_pRouteInfo != null)
                        {
                            int i;
                            for (i = 0; i < m_pRouteInfo.Count - nRoute; ++i)
                            {
                                if (m_pRouteInfo.SequenceEqual(i, pRoute, 0, nRoute))
                                {
                                    break;
                                }
                            }
                            if (i < m_pRouteInfo.Count - nRoute)
                            {
                                m_pNodes[iFrom].m_pNextBestNode[iHull, iCap] = i;
                            }
                            else
                            {
                                var index = m_pRouteInfo.Count;
                                m_pRouteInfo.AddRange(pRoute);
                                m_pNodes[iFrom].m_pNextBestNode[iHull, iCap] = index;
                                nTotalCompressedSize += CompressedSize;
                            }
                        }
                        else
                        {
                            m_pRouteInfo.AddRange(pRoute);
                            m_pNodes[iFrom].m_pNextBestNode[iHull, iCap] = 0;
                            nTotalCompressedSize += CompressedSize;
                        }
                    }
                }
            }
            Log.Alert(AlertType.AIConsole, "Size of Routes = %d\n", nTotalCompressedSize);

#if false
	TestRoutingTables();
#endif
            m_fRoutingComplete = true;
        }

        /// <summary>
        /// Test those routing tables. Doesn't really work, yet
        /// </summary>
        public void TestRoutingTables()
        {
            var pMyPath = new int[m_cNodes];
            var pMyPath2 = new int[m_cNodes];

            for (var iHull = 0; iHull < WorldConstants.NUM_HULLS; ++iHull)
            {
                for (int iCap = 0; iCap < 2; ++iCap)
                {
                    NPCCapabilities iCapMask = NPCCapabilities.None;
                    switch (iCap)
                    {
                        case 0:
                            iCapMask = NPCCapabilities.None;
                            break;

                        case 1:
                            iCapMask = NPCCapabilities.DoorsGroup;
                            break;
                    }

                    for (int iFrom = 0; iFrom < m_cNodes; ++iFrom)
                    {
                        for (int iTo = 0; iTo < m_cNodes; ++iTo)
                        {
                            m_fRoutingComplete = false;
                            int cPathSize1 = FindShortestPath(pMyPath, iFrom, iTo, (Hull)iHull, iCapMask);
                            m_fRoutingComplete = true;
                            int cPathSize2 = FindShortestPath(pMyPath2, iFrom, iTo, (Hull)iHull, iCapMask);

                            // Unless we can look at the entire path, we can verify that it's correct.
                            //
                            if (cPathSize2 == NavConstants.MAX_PATH_SIZE) continue;

                            // Compare distances.
                            //
#if true
                            float flDistance1 = 0.0f;

                            for (var i = 0; i < cPathSize1 - 1; ++i)
                            {
                                // Find the link from pMyPath[i] to pMyPath[i+1]
                                //
                                if (pMyPath[i] == pMyPath[i + 1]) continue;
                                int iVisitNode;
                                bool bFound = false;
                                for (int iLink = 0; iLink < m_pNodes[pMyPath[i]].m_cNumLinks; iLink++)
                                {
                                    iVisitNode = INodeLink(pMyPath[i], iLink);
                                    if (iVisitNode == pMyPath[i + 1])
                                    {
                                        flDistance1 += m_pLinkPool[m_pNodes[pMyPath[i]].m_iFirstLink + iLink].m_flWeight;
                                        bFound = true;
                                        break;
                                    }
                                }
                                if (!bFound)
                                {
                                    Log.Alert(AlertType.AIConsole, "No link.\n");
                                }
                            }

                            float flDistance2 = 0.0f;
                            for (var i = 0; i < cPathSize2 - 1; i++)
                            {
                                // Find the link from pMyPath2[i] to pMyPath2[i+1]
                                //
                                if (pMyPath2[i] == pMyPath2[i + 1]) continue;
                                int iVisitNode;
                                bool bFound = false;
                                for (var iLink = 0; iLink < m_pNodes[pMyPath2[i]].m_cNumLinks; ++iLink)
                                {
                                    iVisitNode = INodeLink(pMyPath2[i], iLink);
                                    if (iVisitNode == pMyPath2[i + 1])
                                    {
                                        flDistance2 += m_pLinkPool[m_pNodes[pMyPath2[i]].m_iFirstLink + iLink].m_flWeight;
                                        bFound = true;
                                        break;
                                    }
                                }
                                if (!bFound)
                                {
                                    Log.Alert(AlertType.AIConsole, "No link.\n");
                                }
                            }
                            if (Math.Abs(flDistance1 - flDistance2) > 0.10)
                            {
#else
                            if (cPathSize1 != cPathSize2 || memcmp(pMyPath, pMyPath2, sizeof(int) * cPathSize1) != 0)
                            {
#endif
                                Log.Alert(AlertType.AIConsole, "Routing is inconsistent!!!\n");
                                Log.Alert(AlertType.AIConsole, $"({iFrom} to {iTo} |{iHull }/{iCap})1:");
                                for (int i = 0; i < cPathSize1; ++i)
                                {
                                    Log.Alert(AlertType.AIConsole, $"{pMyPath[i]} ");
                                }
                                Log.Alert(AlertType.AIConsole, $"\n({iFrom} to {iTo} |{iHull }/{iCap})2:");
                                for (var i = 0; i < cPathSize2; ++i)
                                {
                                    Log.Alert(AlertType.AIConsole, $"{pMyPath2[i]} ");
                                }
                                Log.Alert(AlertType.AIConsole, "\n");
                                m_fRoutingComplete = false;
                                cPathSize1 = FindShortestPath(pMyPath, iFrom, iTo, (Hull)iHull, iCapMask);
                                m_fRoutingComplete = true;
                                cPathSize2 = FindShortestPath(pMyPath2, iFrom, iTo, (Hull)iHull, iCapMask);
                                return;
                            }
                        }
                    }
                }
            }
        }

        private static uint CRC32Node(short srcNode, short destNode)
        {
            var dwHash = Crc32Algorithm.Compute(BitConverter.GetBytes(srcNode));
            return Crc32Algorithm.Append(dwHash, BitConverter.GetBytes(destNode));
        }

        public void HashInsert(short srcNode, short destNode, short iKey)
        {
            var dwHash = CRC32Node(srcNode, destNode);

            var di = m_HashPrimes[dwHash & 15];
            var i = (dwHash >> 4) % m_nHashLinks;
            while (m_pHashLinks[i] != ENTRY_STATE_EMPTY)
            {
                i += di;
                if (i >= m_nHashLinks) i -= m_nHashLinks;
            }
            m_pHashLinks[i] = iKey;
        }

        public void HashSearch(short srcNode, short destNode, out int iKey)
        {
            var dwHash = CRC32Node(srcNode, destNode);

            var di = m_HashPrimes[dwHash & 15];
            var i = (dwHash >> 4) % m_nHashLinks;
            while (m_pHashLinks[i] != ENTRY_STATE_EMPTY)
            {
                var link = Link(m_pHashLinks[i]);
                if (srcNode == link.m_iSrcNode && destNode == link.m_iDestNode)
                {
                    break;
                }
                else
                {
                    i += di;
                    if (i >= m_nHashLinks) i -= m_nHashLinks;
                }
            }
            iKey = m_pHashLinks[i];
        }

        public void HashChoosePrimes(uint TableSize)
        {
            //TODO: implement
        }

        public void BuildLinkLookups()
        {
            m_nHashLinks = (3 * m_cLinks / 2) + 3;

            HashChoosePrimes(m_nHashLinks);
            m_pHashLinks = new short[m_nHashLinks];

            for (var i = 0; i < m_nHashLinks; ++i)
            {
                m_pHashLinks[i] = ENTRY_STATE_EMPTY;
            }

            for (var i = 0; i < m_cLinks; ++i)
            {
                var link = Link(i);
                HashInsert((short)link.m_iSrcNode, (short)link.m_iDestNode, (short)i);
            }
#if false
	        for (var i = 0; i < m_cLinks; ++i)
	        {
		        var link = Link(i);
		        HashSearch((short)link.m_iSrcNode, (short)link.m_iDestNode, out var iKey);
		        if (iKey != i)
		        {
			        Log.Alert(AlertType.AIConsole, $"HashLinks don't match ({i} versus {iKey})\n");
		        }
	        }
#endif
        }

        /// <summary>
        /// Renumber nodes so that nodes that link together are together
        /// </summary>
        public void SortNodes()
        {
            // We are using m_iPreviousNode to be the new node number.
            // After assigning new node numbers to everything, we move
            // things and patchup the links.
            //
            int iNodeCnt = 0;
            m_pNodes[0].m_iPreviousNode = iNodeCnt++;

            for (var i = 1; i < m_cNodes; ++i)
            {
                m_pNodes[i].m_iPreviousNode = UNNUMBERED_NODE;
            }

            for (var i = 0; i < m_cNodes; ++i)
            {
                // Run through all of this node's neighbors
                //
                for (int j = 0; j < m_pNodes[i].m_cNumLinks; ++j)
                {
                    int iDestNode = INodeLink(i, j);
                    if (m_pNodes[iDestNode].m_iPreviousNode == UNNUMBERED_NODE)
                    {
                        m_pNodes[iDestNode].m_iPreviousNode = iNodeCnt++;
                    }
                }
            }

            // Assign remaining node numbers to unlinked nodes.
            //
            for (var i = 0; i < m_cNodes; ++i)
            {
                if (m_pNodes[i].m_iPreviousNode == UNNUMBERED_NODE)
                {
                    m_pNodes[i].m_iPreviousNode = iNodeCnt++;
                }
            }

            // Alter links to reflect new node numbers.
            //
            for (var i = 0; i < m_cLinks; ++i)
            {
                m_pLinkPool[i].m_iSrcNode = m_pNodes[m_pLinkPool[i].m_iSrcNode].m_iPreviousNode;
                m_pLinkPool[i].m_iDestNode = m_pNodes[m_pLinkPool[i].m_iDestNode].m_iPreviousNode;
            }

            // Rearrange nodes to reflect new node numbering.
            //
            for (var i = 0; i < m_cNodes; ++i)
            {
                while (m_pNodes[i].m_iPreviousNode != i)
                {
                    // Move current node off to where it should be, and bring
                    // that other node back into the current slot.
                    //
                    int iDestNode = m_pNodes[i].m_iPreviousNode;
                    CNode TempNode = m_pNodes[iDestNode];
                    m_pNodes[iDestNode] = m_pNodes[i];
                    m_pNodes[i] = TempNode;
                }
            }
        }

        public Hull HullIndex(BaseEntity pEntity)
        {
            if (pEntity.MoveType == MoveType.Fly)
            {
                return Hull.Head;
            }

            if (pEntity.Mins == WorldConstants.POINT_HULL_MIN)
            {
                return Hull.Point;
            }
            else if (pEntity.Mins == WorldConstants.HUMAN_HULL_MIN)
            {
                return Hull.Human;
            }
            else if (pEntity.Mins == WorldConstants.LARGE_HULL_MIN)
            {
                return Hull.Large;
            }

            //	Log.Alert(AlertType.AIConsole, "Unknown Hull Mins!\n");
            return Hull.Human;
        }

        //what hull the monster uses
        public NodeType GetNodeType(BaseEntity pEntity)
        {
            if (pEntity.MoveType == MoveType.Fly)
            {
                if (pEntity.WaterLevel != WaterLevel.Dry)
                {
                    return NodeType.Water;
                }
                else
                {
                    return NodeType.Air;
                }
            }
            return NodeType.Land;
        }

        //TODO: define index
        public int CapIndex(NPCCapabilities afCapMask)
        {
            if (0 != (afCapMask & NPCCapabilities.DoorsGroup))
                return 1;
            return 0;
        }

        public CNode Node(int i)
        {
#if DEBUG
            if (m_pNodes == null || i < 0 || i > m_cNodes)
                Log.Alert(AlertType.Error, "Bad Node!\n");
#endif

            return m_pNodes[i];
        }

        public CLink Link(int i)
        {
#if DEBUG
            if (m_pLinkPool == null || i < 0 || i > m_cLinks)
                Log.Alert(AlertType.Error, "Bad link!\n");
#endif

            return m_pLinkPool[i];
        }

        public CLink NodeLink(int iNode, int iLink)
        {
            return Link(Node(iNode).m_iFirstLink + iLink);
        }

        public CLink NodeLink(CNode node, int iLink)
        {
            return Link(node.m_iFirstLink + iLink);
        }

        public int INodeLink(int iNode, int iLink)
        {
            return NodeLink(iNode, iLink).m_iDestNode;
        }
    }
}
