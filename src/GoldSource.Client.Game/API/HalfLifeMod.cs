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
using GoldSource.Shared.Engine.PlayerPhysics;
using GoldSource.Shared.Game.API;
using GoldSource.Shared.Game.Materials;
using GoldSource.Shared.Game.PlayerPhysics;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GoldSource.Client.Game.API
{
    public sealed class HalfLifeMod : BaseMod
    {
        /// <summary>
        /// Used to resolve engine and server interfaces
        /// </summary>
        private IServiceProvider ServiceProvider { get; set; }

        public override void Startup(IServiceCollection services)
        {
            services.AddSingleton<IPlayerPhysics, PlayerPhysics>();
            services.AddSingleton<MaterialsSystem>();
        }

        public override void Initialize(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public override void Shutdown()
        {
        }
    }
}
