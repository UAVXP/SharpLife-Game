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
using System.Linq;

namespace GoldSource.Server.Game.Utility
{
    /// <summary>
    /// Stores key-value pairs and provides operations for them
    /// </summary>
    public sealed class InfoKeyValues
    {
        public const char InfoBufferDelimiter = '\\';

        private IList<KeyValuePair<string, string>> KeyValues { get; }

        /// <summary>
        /// Constructs an empty buffer
        /// </summary>
        public InfoKeyValues()
        {
            KeyValues = new List<KeyValuePair<string, string>>();
        }

        public InfoKeyValues(IList<KeyValuePair<string, string>> keyValues)
        {
            ValidateKeyValues(keyValues);

            KeyValues = keyValues;
        }

        public string Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var index = KeyValues.IndexOf(kv => kv.Key == key);

            if (index != -1)
            {
                return KeyValues[index].Value;
            }

            return string.Empty;
        }

        public void Set(string key, string value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (key.Length <= 0)
            {
                throw new ArgumentException("Keys must contain at least one character", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            ValidateInput(key);
            ValidateInput(value);

            var newPair = new KeyValuePair<string, string>(key, value);

            var index = KeyValues.IndexOf(kv => kv.Key == key);

            if (index != -1)
            {
                KeyValues[index] = newPair;
            }
            else
            {
                KeyValues.Add(newPair);
            }
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var index = KeyValues.IndexOf(kv => kv.Key == key);

            if (index != -1)
            {
                KeyValues.RemoveAt(index);
            }
        }

        private static void ValidateKeyValues(IList<KeyValuePair<string, string>> keyValues)
        {
            if (keyValues == null)
            {
                throw new ArgumentNullException(nameof(keyValues));
            }

            foreach (var kv in keyValues)
            {
                ValidateInput(kv.Key);
                ValidateInput(kv.Value);
            }
        }

        private static void ValidateInput(string input)
        {
            if (input.IndexOf(InfoBufferDelimiter) != -1)
            {
                throw new ArgumentException($"Can't use keys or values with a {InfoBufferDelimiter}");
            }

            if (input.IndexOf("..") != -1)
            {
                throw new ArgumentException("Can't use keys or values with a ..");
            }

            if (input.IndexOf('\"') != -1)
            {
                throw new ArgumentException("Can't use keys or values with a \"");
            }
        }

        /// <summary>
        /// Converts an info buffer to a list of key-value pairs
        /// Info buffers have the format "\key\value\anotherkey\anothervalue\"
        /// The first \ is optional, but should be present for correctness
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static InfoKeyValues StringToBuffer(string buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            var list = new List<KeyValuePair<string, string>>();

            if (buffer.Length > 0)
            {
                var tokens = buffer.Split(InfoBufferDelimiter, StringSplitOptions.None).ToList();

                if (tokens.Count > 0)
                {
                    //We need to handle empty tokens, but if the buffer starts and/or ends with a delimiter, those strings need to be ignored
                    var ignoreFirst = buffer.StartsWith(InfoBufferDelimiter);
                    var ignoreLast = buffer.EndsWith(InfoBufferDelimiter);

                    var start = ignoreFirst ? 1 : 0;
                    var end = ignoreLast ? tokens.Count - 1 : tokens.Count;

                    tokens = tokens.GetRange(start, end - start);

                    //This will ignore the last key if it has no value, just like the original
                    for (var index = 0; index < tokens.Count; index += 2)
                    {
                        list.Add(new KeyValuePair<string, string>(tokens[index], tokens[index + 1]));
                    }
                }
            }

            //Constructor takes care of key-value validation
            return new InfoKeyValues(list);
        }
    }
}
