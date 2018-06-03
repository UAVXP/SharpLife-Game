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

using GoldSource.Shared.Entities;
using Server.Game.Entities.MetaData;

namespace Server.Game.Entities.Triggers
{
    /// <summary>
    /// This entity will copy its render parameters (renderfx, rendermode, rendercolor, renderamt) to its targets when triggered.
    /// </summary>
    [LinkEntityToClass("env_render")]
    public class RenderFxManager : BaseEntity
    {
        /// <summary>
        /// Flags to indicate masking off various render parameters that are normally copied to the targets
        /// </summary>
        public static class SF
        {
            public const uint MaskFX = 1 << 0;
            public const uint MaskAmount = 1 << 1;
            public const uint MaskMode = 1 << 2;
            public const uint MaskColor = 1 << 3;
        }

        public override void Spawn()
        {
            Solid = Solid.Not;
        }

        public override void Use(BaseEntity pActivator, BaseEntity pCaller, UseType useType, float value)
        {
            if (!string.IsNullOrEmpty(Target))
            {
                for (BaseEntity target = null; (target = EntUtils.FindEntityByTargetName(target, Target)) != null;)
                {
                    if ((SpawnFlags & SF.MaskFX) == 0)
                    {
                        target.RenderEffect = RenderEffect;
                    }

                    if ((SpawnFlags & SF.MaskAmount) == 0)
                    {
                        target.RenderAmount = RenderAmount;
                    }

                    if ((SpawnFlags & SF.MaskMode) == 0)
                    {
                        target.RenderMode = RenderMode;
                    }

                    if ((SpawnFlags & SF.MaskColor) == 0)
                    {
                        target.RenderColor = RenderColor;
                    }
                }
            }
        }
    }
}
