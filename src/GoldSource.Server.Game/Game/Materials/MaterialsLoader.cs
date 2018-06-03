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
using System.IO;

namespace Server.Game.Materials
{
    public static class MaterialsLoader
    {
        public static IDictionary<string, Material> LoadMaterials(string fileName)
        {
            try
            {
                using (var reader = new StreamReader(Engine.FileSystem.GetAbsolutePath(fileName)))
                {
                    var materials = new Dictionary<string, Material>();

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

                        // get texture type
                        var type = (MaterialTypeCode)char.ToUpperInvariant(line[0]);

                        // skip whitespace
                        line = line.Substring(1).TrimStart();

                        if (string.IsNullOrEmpty(line))
                        {
                            continue;
                        }

                        // get sentence name
                        var name = line.TrimEnd();

                        if (name.Length == 0)
                        {
                            continue;
                        }

                        if (materials.TryGetValue(name, out var material))
                        {
                            Log.Message($"Material {name} is listed multiple times in materials file {fileName}, overwriting previous type {material.Code} with {type}");
                        }

                        materials[name] = new Material(name, type);
                    }

                    return materials;
                }
            }
            catch (Exception e)
            {
                if (e is FileNotFoundException || e is DirectoryNotFoundException)
                {
                    return null;
                }

                throw;
            }
        }
    }
}
