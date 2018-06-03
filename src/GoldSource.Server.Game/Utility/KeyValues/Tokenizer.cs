﻿/***
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
using System.Linq;

namespace Server.Utility.KeyValues
{
    /// <summary>
    /// Tokenizes a string
    /// </summary>
    public sealed class Tokenizer
    {
        private readonly string _data;

        public string Token { get; private set; } = string.Empty;

        public int Index { get; private set; }

        /// <summary>
        /// Whether there is data left to be read
        /// </summary>
        public bool HasNext => Index < _data.Length;

        /// <summary>
        /// Returns true if additional data is waiting to be processed on this line
        /// </summary>
        public bool TokenWaiting
        {
            get
            {
                var index = Index;

                while (index < Token.Length && Token[index] != '\n')
                {
                    if (!char.IsWhiteSpace(Token[index]) || char.IsLetterOrDigit(Token[index]))
                    {
                        return true;
                    }

                    ++index;
                }

                return false;
            }
        }

        /// <summary>
        /// Characters to treat as their own tokens
        /// </summary>
        private static readonly char[] SingleCharacters =
        {
            '{',
            '}',
            '(',
            ')',
            '\'',
            ','
        };

        public Tokenizer(string data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        private void SkipWhitespace()
        {
            while (Index < _data.Length)
            {
                if (!char.IsWhiteSpace(_data[Index]))
                {
                    break;
                }

                ++Index;
            }
        }

        private bool SkipCommentLine()
        {
            if (Index + 1 < _data.Length && _data[Index] == '/' && _data[Index + 1] == '/')
            {
                var index = _data.IndexOf('\n');

                if (index == -1)
                {
                    Index = _data.Length;
                    return false;
                }

                Index = index + 1;

                return true;
            }

            return false;
        }

        public bool Next()
        {
            Token = string.Empty;

            if (!HasNext)
            {
                return false;
            }

            bool checkComments;

            do
            {
                checkComments = false;

                SkipWhitespace();

                if (!HasNext)
                {
                    return false;
                }

                if (SkipCommentLine())
                {
                    checkComments = true;
                }
                else if (!HasNext)
                {
                    return false;
                }
            }
            while (checkComments);

            // handle quoted strings specially
            if (_data[Index] == '\"')
            {
                ++Index;

                var startIndex = Index;

                while (HasNext)
                {
                    if (_data[Index] == '\"')
                    {
                        break;
                    }

                    ++Index;
                }

                Token = _data.Substring(startIndex, Index - startIndex);

                if (HasNext)
                {
                    ++Index;
                }

                return true;
            }

            // parse single characters
            {
                var c = _data[Index];

                if (SingleCharacters.Any(ch => ch == c))
                {
                    Token = new string(_data[Index++], 1);

                    return true;
                }
            }

            // parse a regular word
            {
                var startIndex = Index;

                char c;

                do
                {
                    c = _data[++Index];
                }
                while (HasNext && !char.IsWhiteSpace(c) && !SingleCharacters.Any(ch => ch == c));

                //Either we're out of data, or we hit whitespace or a single character
                //In any case, we'll want the token to exclude the last character
                Token = _data.Substring(startIndex, Index - startIndex - 1);
            }

            return true;
        }
    }
}
