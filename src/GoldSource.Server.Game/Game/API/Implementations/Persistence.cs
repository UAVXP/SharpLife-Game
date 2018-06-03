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

using GoldSource.Server.Engine.Game.API;
using GoldSource.Server.Engine.Persistence;
using GoldSource.Shared.Entities;

namespace Server.Game.API.Implementations
{
    public sealed class Persistence : IPersistence
    {
        //TODO: implement
        public void Save(Edict pent, SaveRestoreData saveData)
        {
        }

        public bool Restore(Edict pent, SaveRestoreData saveData, bool isGlobalEntity)
        {
            return true;
        }

        public void SaveGlobalState(SaveRestoreData saveData)
        {
        }

        public void RestoreGlobalState(SaveRestoreData saveData)
        {
        }

        public void ResetGlobalState()
        {
        }
    }
}
