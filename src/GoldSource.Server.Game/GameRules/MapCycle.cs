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
using GoldSource.Server.Game.Game;
using GoldSource.Server.Game.Utility;
using GoldSource.Server.Game.Utility.KeyValues;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoldSource.Server.Game.GameRules
{
    /// <summary>
    /// Stores the map cycle
    /// </summary>
    public class MapCycle
    {
        public class Map
        {
            public string MapName { get; set; }

            public int MinPlayers { get; set; }

            public int MaxPlayers { get; set; }

            public InfoKeyValues Rules { get; set; }
        }

        /// <summary>
        /// Gets the list of maps
        /// </summary>
        public IList<Map> Maps { get; }

        public MapCycle(IList<Map> maps)
        {
            Maps = maps ?? throw new ArgumentNullException(nameof(maps));
        }

        /// <summary>
        /// Parses mapcycle.txt file into MapCycle class
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static MapCycle LoadFromFile(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException(nameof(fileName));
            }

            var maps = new List<Map>();

            try
            {
                var path = Game.Engine.FileSystem.GetAbsolutePath(fileName);

                var contents = File.ReadAllText(path);

                var tokenizer = new Tokenizer(contents);

                // the first map name in the file becomes the default
                while (tokenizer.Next())
                {
                    var map = tokenizer.Token;

                    string buffer = null;

                    // Any more tokens on this line?
                    if (tokenizer.TokenWaiting)
                    {
                        tokenizer.Next();

                        if (tokenizer.Token.Length > 0)
                        {
                            buffer = tokenizer.Token;
                        }
                    }

                    // Check map
                    if (Game.Engine.Server.IsMapValid(map))
                    {
                        // Create entry
                        var item = new Map
                        {
                            MapName = map,
                            MinPlayers = 0,
                            MaxPlayers = 0
                        };

                        if (buffer != null)
                        {
                            item.Rules = InfoKeyValues.StringToBuffer(buffer);

                            var minPlayers = item.Rules.Get("minplayers");

                            if (minPlayers.Length > 0)
                            {
                                int.TryParse(minPlayers, out var result);

                                item.MinPlayers = Math.Min(Math.Max(0, result), Game.Engine.Globals.MaxClients);

                                item.Rules.Remove("minplayers");
                            }

                            var maxPlayers = item.Rules.Get("maxplayers");

                            if (maxPlayers.Length > 0)
                            {
                                int.TryParse(maxPlayers, out var result);

                                item.MaxPlayers = Math.Min(Math.Max(0, result), Game.Engine.Globals.MaxClients);

                                item.Rules.Remove("maxplayers");
                            }
                        }
                        else
                        {
                            item.Rules = new InfoKeyValues();
                        }

                        maps.Add(item);
                    }
                    else
                    {
                        Log.Alert(AlertType.Console, $"Skipping {map} from mapcycle, not a valid map\n");
                    }
                }
            }
            catch (Exception e)
            {
                //Always return a valid map cycle, even if we couldn't read from the file
                Log.Message($"While attempting to load the mapcycle from file \"{fileName}\"");
                Log.Exception(e);
            }

            return new MapCycle(maps);
        }
    }
}
