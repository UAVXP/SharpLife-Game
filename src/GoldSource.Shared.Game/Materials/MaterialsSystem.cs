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

using GoldSource.FileSystem;
using Serilog;
using System;
using System.Collections.Generic;

namespace GoldSource.Shared.Game.Materials
{
    public sealed class MaterialsSystem
    {
        public const MaterialTypeCode DefaultMaterialType = MaterialTypeCode.Concrete;

        public IDictionary<MaterialTypeCode, MaterialType> MaterialTypes { get; } = new Dictionary<MaterialTypeCode, MaterialType>();

        public IReadOnlyDictionary<string, Material> MaterialsList { get; private set; } = new Dictionary<string, Material>();

        public MaterialsSystem()
        {
            AddMaterial(
                 new MaterialType(MaterialTypeCode.Concrete,
                 0.9f, 0.6f, 0.2f, 0.5f,
                 400, 300,
                new List<string>
                {
                    "player/pl_step1.wav",
                    "player/pl_step2.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_step1.wav", false),
                    new MaterialType.MovementSound("player/pl_step3.wav", false),
                    new MaterialType.MovementSound("player/pl_step2.wav", true),
                    new MaterialType.MovementSound("player/pl_step4.wav", true),
                }
                ));

            AddMaterial(
                new MaterialType(MaterialTypeCode.Metal,
                0.9f, 0.3f, 0.2f, 0.5f,
                 400, 300,
                new List<string>
                {
                    "player/pl_metal1.wav",
                    "player/pl_metal2.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_metal1.wav", false),
                    new MaterialType.MovementSound("player/pl_metal3.wav", false),
                    new MaterialType.MovementSound("player/pl_metal2.wav", true),
                    new MaterialType.MovementSound("player/pl_metal4.wav", true),
                }
                ));

            AddMaterial(
                new MaterialType(MaterialTypeCode.Dirt,
                0.9f, 0.1f, 0.25f, 0.55f,
                 400, 300,
                new List<string>
                {
                    "player/pl_dirt1.wav",
                    "player/pl_dirt2.wav",
                    "player/pl_dirt3.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_dirt1.wav", false),
                    new MaterialType.MovementSound("player/pl_dirt3.wav", false),
                    new MaterialType.MovementSound("player/pl_dirt2.wav", true),
                    new MaterialType.MovementSound("player/pl_dirt4.wav", true),
                }
                ));

            AddMaterial(
                new MaterialType(MaterialTypeCode.Vent,
                0.5f, 0.3f, 0.4f, 0.7f,
                 400, 300,
                new List<string>
                {
                    "player/pl_duct1.wav",
                    "player/pl_duct1.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_duct1.wav", false),
                    new MaterialType.MovementSound("player/pl_duct3.wav", false),
                    new MaterialType.MovementSound("player/pl_duct2.wav", true),
                    new MaterialType.MovementSound("player/pl_duct4.wav", true),
                }
                ));

            AddMaterial(
                new MaterialType(MaterialTypeCode.Grate,
                0.9f, 0.5f, 0.2f, 0.5f,
                 400, 300,
                new List<string>
                {
                    "player/pl_grate1.wav",
                    "player/pl_grate4.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_grate1.wav", false),
                    new MaterialType.MovementSound("player/pl_grate3.wav", false),
                    new MaterialType.MovementSound("player/pl_grate2.wav", true),
                    new MaterialType.MovementSound("player/pl_grate4.wav", true),
                }
                ));

            AddMaterial(
                new MaterialType(MaterialTypeCode.Tile,
                0.8f, 0.2f, 0.2f, 0.5f,
                 400, 300,
                new List<string>
                {
                    "player/pl_tile1.wav",
                    "player/pl_tile3.wav",
                    "player/pl_tile2.wav",
                    "player/pl_tile4.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_tile1.wav", false),
                    new MaterialType.MovementSound("player/pl_tile3.wav", false),
                    new MaterialType.MovementSound("player/pl_tile2.wav", true),
                    new MaterialType.MovementSound("player/pl_tile4.wav", true),
                    new MaterialType.MovementSound("player/pl_tile5.wav", true),
                }
                ));

            AddMaterial(
                new MaterialType(MaterialTypeCode.Slosh,
                0.9f, 0.0f, 0.2f, 0.5f,
                 400, 300,
                new List<string>
                {
                    "player/pl_slosh1.wav",
                    "player/pl_slosh3.wav",
                    "player/pl_slosh2.wav",
                    "player/pl_slosh4.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_slosh1.wav", false),
                    new MaterialType.MovementSound("player/pl_slosh3.wav", false),
                    new MaterialType.MovementSound("player/pl_slosh2.wav", true),
                    new MaterialType.MovementSound("player/pl_slosh4.wav", true),
                }
                ));

            AddMaterial(
                 new MaterialType(MaterialTypeCode.Wood,
                 0.9f, 0.2f, 0.2f, 0.5f,
                 400, 300,
                new List<string>
                {
                    "debris/wood1.wav",
                    "debris/wood2.wav",
                    "debris/wood3.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_step1.wav", false),
                    new MaterialType.MovementSound("player/pl_step3.wav", false),
                    new MaterialType.MovementSound("player/pl_step2.wav", true),
                    new MaterialType.MovementSound("player/pl_step4.wav", true),
                }
                ));

            AddMaterial(
                new MaterialType(MaterialTypeCode.Glass,
                0.8f, 0.2f, 0.2f, 0.5f,
                 400, 300,
                new List<string>
                {
                    "debris/glass1.wav",
                    "debris/glass2.wav",
                    "debris/glass3.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_step1.wav", false),
                    new MaterialType.MovementSound("player/pl_step3.wav", false),
                    new MaterialType.MovementSound("player/pl_step2.wav", true),
                    new MaterialType.MovementSound("player/pl_step4.wav", true),
                }
                ));

            AddMaterial(
                new MaterialType(MaterialTypeCode.Computer,
                0.8f, 0.2f, 0.2f, 0.5f,
                400, 300,
                new List<string>
                {
                    "debris/glass1.wav",
                    "debris/glass2.wav",
                    "debris/glass3.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_step1.wav", false),
                    new MaterialType.MovementSound("player/pl_step3.wav", false),
                    new MaterialType.MovementSound("player/pl_step2.wav", true),
                    new MaterialType.MovementSound("player/pl_step4.wav", true),
                }
                ));

            bool FleshFilter(ref MaterialEmitEvent emitEvent)
            {
                if (emitEvent.BulletType == Bullet.PlayerCrowbar)
                {
                    return false; // crowbar already makes this sound
                }

                emitEvent.Attenuation = 1.0f;

                return true;
            }

            AddMaterial(
                new MaterialType(MaterialTypeCode.Flesh,
                1.0f, 0.2f, 0.2f, 0.5f,
                400, 300,
                new List<string>
                {
                    "weapons/bullet_hit1.wav",
                    "weapons/bullet_hit2.wav"
                },
                new List<MaterialType.MovementSound>
                {
                    new MaterialType.MovementSound("player/pl_step1.wav", false),
                    new MaterialType.MovementSound("player/pl_step3.wav", false),
                    new MaterialType.MovementSound("player/pl_step2.wav", true),
                    new MaterialType.MovementSound("player/pl_step4.wav", true),
                },
                FleshFilter));
        }

        public void AddMaterial(MaterialType materialType)
        {
            if (materialType == null)
            {
                throw new ArgumentNullException(nameof(materialType));
            }

            if (MaterialTypes.ContainsKey(materialType.Code))
            {
                throw new ArgumentException("Material type already known");
            }

            MaterialTypes.Add(materialType.Code, materialType);
        }

        public void LoadMaterialsFromFile(IFileSystem fileSystem, ILogger logger, string fileName)
        {
            var materials = MaterialsLoader.LoadMaterials(fileSystem, logger, fileName);

            MaterialsList = (IReadOnlyDictionary<string, Material>)(materials ?? new Dictionary<string, Material>());
        }

        public MaterialTypeCode Find(string name)
        {
            if (MaterialsList.TryGetValue(name, out var material))
            {
                return material.Code;
            }

            return DefaultMaterialType;
        }

        public MaterialType GetMaterialType(string name, MaterialTypeCode defaultMaterialType = DefaultMaterialType)
        {
            var code = defaultMaterialType;

            if (MaterialsList.TryGetValue(name, out var material))
            {
                code = material.Code;
            }

            return MaterialTypes[code];
        }
    }
}
