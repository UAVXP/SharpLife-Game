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
/*
*	=========================================================
*	CQueuePriority - Priority queue (smallest item out first).
*	=========================================================
*/

using System;
using System.Collections.Generic;

namespace GoldSource.Server.Game.Navigation
{
    /// <summary>
    /// Priority queue (smallest item out first)
    /// </summary>
    public class CQueuePriority
    {
        private struct Node
        {
            public int Id;
            public float Priority;
        }

        private List<Node> _heap = new List<Node>();

        public bool Empty => 0 == _heap.Count;

        public int Count => _heap.Count;

        public CQueuePriority()
        {
        }

        public void Insert(int value, float priority)
        {
            _heap.Add(new Node { Id = value, Priority = priority });

            Heap_SiftUp();
        }

        public int Remove(out float priority)
        {
            if (Empty)
            {
                throw new InvalidOperationException("Can't remove elements from an empty priority queue");
            }

            int iReturn = _heap[0].Id;
            priority = _heap[0].Priority;

            _heap[0] = _heap[_heap.Count - 1];
            _heap.RemoveAt(_heap.Count - 1);

            Heap_SiftDown(0);
            return iReturn;
        }

        private static int HEAP_LEFT_CHILD(int x) => (2 * (x) + 1);
        private static int HEAP_RIGHT_CHILD(int x) => (2 * (x) + 2);
        private static int HEAP_PARENT(int x) => (((x) - 1) / 2);

        private void Heap_SiftDown(int subRoot)
        {
            int parent = subRoot;
            int child = HEAP_LEFT_CHILD(parent);

            var Ref = _heap[parent];

            while (child < _heap.Count)
            {
                int rightchild = HEAP_RIGHT_CHILD(parent);
                if (rightchild < _heap.Count)
                {
                    if (_heap[rightchild].Priority < _heap[child].Priority)
                    {
                        child = rightchild;
                    }
                }
                if (Ref.Priority <= _heap[child].Priority)
                {
                    break;
                }

                _heap[parent] = _heap[child];
                parent = child;
                child = HEAP_LEFT_CHILD(parent);
            }

            _heap[parent] = Ref;
        }

        private void Heap_SiftUp()
        {
            int child = _heap.Count - 1;

            while (0 != child)
            {
                int parent = HEAP_PARENT(child);
                if (_heap[parent].Priority <= _heap[child].Priority)
                {
                    break;
                }

                var Tmp = _heap[child];
                _heap[child] = _heap[parent];
                _heap[parent] = Tmp;

                child = parent;
            }
        }
    }
}
