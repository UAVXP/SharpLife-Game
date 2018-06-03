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
using System.Collections.Generic;

namespace GoldSource.Server.Game.Game.Sound
{
    public sealed class SentencesSystem
    {
        private const int MaxSentences = 1536;
        private const int MaxSentenceLength = 15;
        private const int MaxGroups = 200;

        public const int InvalidSentenceIndex = -1;
        public const int InvalidGroupIndex = -1;

        /// <summary>
        /// Marker value used to indicate that an LRU entry has been recently used and should not be picked
        /// </summary>
        private const int LRURecentlyUsed = -1;

        public IReadOnlyDictionary<string, SentenceGroup> Groups { get; private set; } = new Dictionary<string, SentenceGroup>();

        public IReadOnlyList<Sentence> SentencesList { get; private set; } = new List<Sentence>();

        public void LoadSentencesFromFile(string fileName)
        {
            var (groups, sentences) = SentencesLoader.LoadSentences(fileName, MaxSentences, MaxSentenceLength, MaxGroups);

            Groups = (IReadOnlyDictionary<string, SentenceGroup>)(groups ?? new Dictionary<string, SentenceGroup>());
            SentencesList = (IReadOnlyList<Sentence>)(sentences ?? new List<Sentence>());
        }

        public int GetGroupIndex(string sentenceGroup)
        {
            if (sentenceGroup == null)
            {
                return InvalidGroupIndex;
            }

            // search Groups for match on sentenceGroup
            if (Groups.TryGetValue(sentenceGroup, out var group))
            {
                return group.Index;
            }

            return InvalidGroupIndex;
        }

        public int GetSentenceIndex(string sample, out string sentencenum)
        {
            sample = sample.Substring(1);

            // this is a sentence name; lookup sentence number
            // and give to engine as string.
            for (var i = 0; i < SentencesList.Count; ++i)
            {
                if (sample.Equals(SentencesList[i].Value, StringComparison.OrdinalIgnoreCase))
                {
                    sentencenum = $"!{i}";
                    return i;
                }
            }

            // sentence name not found!
            sentencenum = string.Empty;
            return InvalidSentenceIndex;
        }

        public int PickLeastRecentlyUsed(string sentenceGroup, out string found)
        {
            if (!Groups.TryGetValue(sentenceGroup, out var group))
            {
                found = string.Empty;
                return InvalidSentenceIndex;
            }

            var lru = group.LRU;

            //Make 2 attempts to find a suitable sentence, to avoid infinite loops
            for (var attempt = 1; attempt <= 2; ++attempt)
            {
                for (var i = 0; i < lru.Count; ++i)
                {
                    if (lru[i] != LRURecentlyUsed)
                    {
                        var ipick = lru[i];
                        lru[i] = LRURecentlyUsed;
                        found = $"!{group.Name}{ipick}";
                        return ipick;
                    }
                }

                //Init the LRU again to mark sentences as free
                group.InitLRU();
            }

            found = string.Empty;
            return InvalidSentenceIndex;
        }

        public int PickSequentialLeastRecentlyUsed(string sentenceGroup, out string found, int pick, bool reset)
        {
            found = string.Empty;

            if (!Groups.TryGetValue(sentenceGroup, out var group))
            {
                found = string.Empty;
                return InvalidSentenceIndex;
            }

            if (group.Sentences.Count == 0)
            {
                return InvalidSentenceIndex;
            }

            pick = Math.Min(pick, group.Sentences.Count - 1);

            found = $"!{group.Name}{pick}";

            if (pick >= group.Sentences.Count)
            {
                if (reset)
                {
                    // reset at end of list
                    return 0;
                }
                else
                {
                    return group.Sentences.Count;
                }
            }

            return pick + 1;
        }
    }
}
