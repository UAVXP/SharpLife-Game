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
using System;
using System.Collections.Generic;

namespace Server.Game.Sound
{
    public sealed class SentenceGroup
    {
        public string Name { get; }

        public int Index { get; }

        public IReadOnlyList<Sentence> Sentences { get; }

        public List<int> LRU { get; }

        public SentenceGroup(string name, int index, IReadOnlyList<Sentence> sentences)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(nameof(name));
            }

            Index = index;

            Sentences = sentences ?? throw new ArgumentNullException(nameof(sentences));

            //The original LRU was fixed at 32 entries; this matches the number of sentences
            LRU = new List<int>(Sentences.Count);

            InitLRU();
        }

        public void InitLRU()
        {
            for (var i = 0; i < LRU.Count; ++i)
            {
                LRU[i] = i;
            }

            // randomize array
            for (var i = 0; i < (LRU.Count * 4); ++i)
            {
                var j = EngineRandom.Long(0, LRU.Count - 1);
                var k = EngineRandom.Long(0, LRU.Count - 1);

                var temp = LRU[j];
                LRU[j] = LRU[k];
                LRU[k] = temp;
            }
        }
    }
}
