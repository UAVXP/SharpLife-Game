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
using GoldSource.Server.Engine.API;
using GoldSource.Server.Engine.Game.API;
using GoldSource.Shared.Entities;
using Server.Game.Entities;
using System;

namespace Server.Game.API.Implementations
{
    public sealed class ServerInterface : IServerInterface
    {
        //TODO: put all constants that deal with mod names in one place
        public string GameDescription => "Half-Life";

        public bool IsActive { get; private set; }

        private IGlobalVars Globals { get; }

        private IEntityDictionary EntityDictionary { get; }

        public ServerInterface(IGlobalVars globals, IEntityDictionary entityDictionary)
        {
            Globals = globals ?? throw new ArgumentNullException(nameof(globals));
            EntityDictionary = entityDictionary ?? throw new ArgumentNullException(nameof(entityDictionary));
        }

        public void Initialize()
        {
        }

        public void Shutdown()
        {
        }

        public void Activate()
        {
            // It's possible that the engine will call this function more times than is necessary
            //  Therefore, only run it one time for each call to ServerActivate 
            if (IsActive)
            {
                return;
            }

            // Every call to ServerActivate should be matched by a call to ServerDeactivate
            IsActive = true;

            var clientMax = Globals.MaxClients;
            var edictCount = EntityDictionary.HighestInUse != -1 ? EntityDictionary.HighestInUse - 1 : 0;

            // Clients have not been initialized yet
            for (var i = 0; i < edictCount; ++i)
            {
                var edict = EntityDictionary.EdictByIndex(i);

                if (edict.Free)
                    continue;

                // Clients aren't necessarily initialized until ClientPutInServer()
                if (i < clientMax || edict.PrivateData == null)
                    continue;

                var pClass = edict.TryGetEntity();
                // Activate this entity if it's got a class & isn't dormant
                if (pClass != null && (pClass.Flags & EntFlags.Dormant) == 0)
                {
                    pClass.Activate();
                }
                else
                {
                    Log.Alert(AlertType.Console, $"Can't instance {edict.Vars.ClassName}");
                }
            }

            // Link user messages here to make sure first client can get them...
            //TODO:
            //LinkUserMessages();
        }

        public void Deactivate()
        {
            // It's possible that the engine will call this function more times than is necessary
            //  Therefore, only run it one time for each call to ServerActivate 
            if (!IsActive)
            {
                return;
            }

            IsActive = false;

            // Peform any shutdown operations here...
            //
        }

        public void StartFrame()
        {
        }

        public void SysError(string errorString)
        {
        }

        public void CvarValue(Edict pEnt, string value)
        {
        }

        public void CvarValue2(Edict pEnt, int requestID, string cvarName, string value)
        {
        }
    }
}
