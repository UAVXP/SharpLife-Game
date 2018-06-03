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

using GoldSource.Server.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoldSource.Server.Game.Game.Sound
{
    /// <summary>
    /// Loads sentences text files
    /// </summary>
    public static class SentencesLoader
    {
        /// <summary>
        /// Loads sentences from a file
        /// </summary>
        /// <param name="fileName">Name of the file to load</param>
        /// <param name="maxSentences">Maximum number of sentences to load</param>
        /// <param name="maxSentenceLength">Maximum length of a sentence value</param>
        /// <param name="maxGroups">Maximum number of groups to load</param>
        /// <returns></returns>
        public static (IDictionary<string, SentenceGroup>, IList<Sentence>) LoadSentences(string fileName, int maxSentences, int maxSentenceLength, int maxGroups)
        {
            try
            {
                using (var reader = new StreamReader(Engine.FileSystem.GetAbsolutePath(fileName)))
                {
                    var groups = new Dictionary<string, List<Sentence>>();
                    var sentences = new List<Sentence>();

                    var sentenceCount = 0;

                    string previousGroup = null;

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        // skip whitespace
                        line = line.TrimStart();

                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        // skip comment lines
                        if (line[0] == '/' || !char.IsLetterOrDigit(line[0]))
                        {
                            continue;
                        }

                        // get sentence name
                        var endOfGroup = line.IndexOf(' ');

                        //No sentence specified for group, ignore
                        if (endOfGroup == -1)
                        {
                            continue;
                        }

                        if (sentenceCount > maxSentences)
                        {
                            Log.Alert(AlertType.Error, "Too many sentences in sentences.txt!");
                            break;
                        }

                        var name = line.Substring(0, endOfGroup);
                        var sentenceString = line.Substring(endOfGroup + 1);

                        if (sentenceString.Length > maxSentenceLength)
                        {
                            Log.Alert(AlertType.Warning, $"Sentence {sentenceString} longer than {maxSentenceLength} letters");
                        }

                        var groupIndexStart = endOfGroup - 1;

                        if (groupIndexStart < 0 || !char.IsDigit(name[groupIndexStart]))
                        {
                            continue;
                        }

                        // cut out suffix numbers
                        while (groupIndexStart > 0 && char.IsDigit(name[groupIndexStart]))
                        {
                            --groupIndexStart;
                        }

                        if (groupIndexStart <= 0)
                        {
                            continue;
                        }

                        //Find or create group
                        var groupName = name.Substring(0, groupIndexStart);

                        //The original version could create new groups that actually match other sentence groups if they're non-sequential in the file
                        //This reduces the number of groups
                        //TODO: this ignores the second group
                        List<Sentence> group = null;

                        if (previousGroup == groupName)
                        {
                            group = groups[groupName];
                        }
                        else if (!groups.ContainsKey(groupName))
                        {
                            // name doesn't match with prev name,
                            // copy name into group, init count to 1
                            if (groups.Count + 1 >= maxGroups)
                            {
                                Log.Alert(AlertType.Error, "Too many sentence groups in sentences.txt!");
                                break;
                            }

                            group = new List<Sentence>();

                            groups.Add(groupName, group);

                            previousGroup = groupName;
                        }

                        if (group != null)
                        {
                            var sentence = new Sentence(sentenceString, sentenceCount);
                            group.Add(sentence);
                            sentences.Add(sentence);
                        }

                        ++sentenceCount;
                    }

                    //Remap to string => group
                    var index = 0;
                    return (groups.ToDictionary(kv => kv.Key, kv => new SentenceGroup(kv.Key, index++, kv.Value)), sentences);
                }
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException || e is DirectoryNotFoundException)
                {
                    return (null, null);
                }

                throw;
            }
        }
    }
}
