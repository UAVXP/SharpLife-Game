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
using System.IO;
using System.Reflection;

namespace Server.Game
{
    internal static class Log
    {
        private const string LOG_FILENAME = "SharpLife-Game-Server.log";

        internal static void Message(string message)
        {
            File.AppendAllText(LOG_FILENAME, $"[{DateTimeOffset.Now}]: {message}{Environment.NewLine}");
        }

        internal static void Exception(Exception e)
        {
            Message($"Exception {e.GetType().Name}: {e.Message}\nStack trace:\n{e.StackTrace}");

            if (e is ReflectionTypeLoadException reflEx)
            {
                Message("Loader exceptions:");
                foreach (var ex in reflEx.LoaderExceptions)
                {
                    Exception(ex);
                }
            }
        }

        public static void Alert(AlertType atype, string format, params object[] args)
        {
            var text = string.Format(format, args);

            Engine.Server.Alert(atype, text);
        }

        /// <summary>
        /// Log to the engine log file
        /// </summary>
        /// <param name="text"></param>
        public static void EngineLog(string text)
        {
            // Print to server console
            Alert(AlertType.Logged, "{0}", text);
        }
    }
}
