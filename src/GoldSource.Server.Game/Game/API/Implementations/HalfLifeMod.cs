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
using GoldSource.Server.Engine.API;
using GoldSource.Server.Engine.CVar;
using GoldSource.Server.Engine.Entities;
using GoldSource.Server.Engine.Game.API;
using GoldSource.Server.Game.Engine;
using GoldSource.Server.Game.Game.Entities;
using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Server.Game.Game.Materials;
using GoldSource.Server.Game.Game.Networking;
using GoldSource.Server.Game.Game.Sound;
using GoldSource.Shared.Engine.PlayerPhysics;
using GoldSource.Shared.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace GoldSource.Server.Game.Game.API.Implementations
{
    /// <summary>
    /// Entry point for the Half-Life SDK mod
    /// </summary>
    public sealed class HalfLifeMod : BaseMod
    {
        /// <summary>
        /// Used to resolve engine and server interfaces
        /// </summary>
        private IServiceProvider ServiceProvider { get; set; }

        public override void Startup(IServiceCollection services)
        {
            EngineRandom.SeedRandomNumberGenerator();

            services.AddSingleton<IServerInterface, ServerInterface>();
            services.AddSingleton<IGameClients, GameClients>();
            services.AddSingleton<IEntities, Entities>();
            services.AddSingleton<INetworking, Networking>();
            services.AddSingleton<IPersistence, Persistence>();
            services.AddSingleton<IPlayerPhysics, PlayerPhysics>();
            services.AddSingleton<IEntityRegistry, EntityRegistry>();
            services.AddSingleton<SentencesSystem>();
            services.AddSingleton<MaterialsSystem>();
            services.AddSingleton<ISoundSystem, SoundSystem>();
            services.AddSingleton<INetworkMessages, NetworkMessages>();
        }

        public override void Initialize(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            Engine.Server = ServiceProvider.GetRequiredService<IEngineServer>();
            Engine.Globals = ServiceProvider.GetRequiredService<IGlobalVars>();
            Engine.FileSystem = ServiceProvider.GetRequiredService<IFileSystem>();
            Server.Game.Engine.Trace.EngineTrace = ServiceProvider.GetRequiredService<ITrace>();
            Engine.EntityDictionary = ServiceProvider.GetRequiredService<IEntityDictionary>();
            Engine.EntityRegistry = ServiceProvider.GetRequiredService<IEntityRegistry>();
            Engine.Entities = ServiceProvider.GetRequiredService<IEngineEntities>();
            Engine.Sound = ServiceProvider.GetRequiredService<ISoundSystem>();
            CVar.EngineCVar = ServiceProvider.GetRequiredService<ICVar>();
            NetMessage.EngineNetworking = ServiceProvider.GetRequiredService<IEngineNetworking>();
            NetMessage.NetworkMessages = ServiceProvider.GetRequiredService<INetworkMessages>();

            EntUtils.Initialize(ServiceProvider.GetRequiredService<IEntityDictionary>());

            SetupFileSystem();

            Globals.g_psv_gravity = CVar.GetCVar("sv_gravity");
            Globals.g_psv_aim = CVar.GetCVar("sv_aim");
            Globals.g_footsteps = CVar.GetCVar("mp_footsteps");

            //Set up our own entities
            var entities = EntityRegistryUtils.CollectEntityClasses(Assembly.GetExecutingAssembly());

            entities.ForEach(i => Engine.EntityRegistry.AddEntityClass(i));

            //Link messages now so it gets done
            LinkUserMessages();
        }

        public override void Shutdown()
        {
        }

        private void SetupFileSystem()
        {
            //Add mod specific paths here
        }

        private void LinkUserMessages()
        {
            var networkMessages = ServiceProvider.GetRequiredService<INetworkMessages>();

            //Never used
            //networkMessages.RegisterMessage("SelAmmo", Marshal.SizeOf<SelAmmo>());
            networkMessages.RegisterMessage("CurWeapon", 3);
            networkMessages.RegisterMessage("Geiger", 1);
            networkMessages.RegisterMessage("Flashlight", 2);
            networkMessages.RegisterMessage("FlashBat", 1);
            networkMessages.RegisterMessage("Health", 1);
            networkMessages.RegisterMessage("Damage", 12);
            networkMessages.RegisterMessage("Battery", 2);
            networkMessages.RegisterMessage("Train", 1);
            //networkMessages.RegisterMessage( "HudTextPro", -1 );
            networkMessages.RegisterMessage("HudText", -1); // we don't use the message but 3rd party addons may!
            networkMessages.RegisterMessage("SayText", -1);
            networkMessages.RegisterMessage("TextMsg", -1);
            networkMessages.RegisterMessage("WeaponList", -1);
            networkMessages.RegisterMessage("ResetHUD", 1);     // called every respawn
            networkMessages.RegisterMessage("InitHUD", 0);       // called every time a new player joins the server
            networkMessages.RegisterMessage("GameTitle", 1);
            networkMessages.RegisterMessage("DeathMsg", -1);
            networkMessages.RegisterMessage("ScoreInfo", 9);
            networkMessages.RegisterMessage("TeamInfo", -1);  // sets the name of a player's team
            networkMessages.RegisterMessage("TeamScore", -1);  // sets the score of a team on the scoreboard
            networkMessages.RegisterMessage("GameMode", 1);
            networkMessages.RegisterMessage("MOTD", -1);
            networkMessages.RegisterMessage("ServerName", -1);
            networkMessages.RegisterMessage("AmmoPickup", 2);
            networkMessages.RegisterMessage("WeapPickup", 1);
            networkMessages.RegisterMessage("ItemPickup", -1);
            networkMessages.RegisterMessage("HideWeapon", 1);
            networkMessages.RegisterMessage("SetFOV", 1);
            networkMessages.RegisterMessage("ShowMenu", -1);
            networkMessages.RegisterMessage("ScreenShake", Marshal.SizeOf<ScreenShake>());
            networkMessages.RegisterMessage("ScreenFade", Marshal.SizeOf<ScreenFade>());
            networkMessages.RegisterMessage("AmmoX", 2);
            networkMessages.RegisterMessage("TeamNames", -1);

            networkMessages.RegisterMessage("StatusText", -1);
            networkMessages.RegisterMessage("StatusValue", 3);

            networkMessages.RegisterMessage("VoiceMask", VoiceGameManager.VOICE_MAX_PLAYERS_DW * 4 * 2);
            networkMessages.RegisterMessage("ReqState", 0);
        }
    }
}
