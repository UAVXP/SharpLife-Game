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
using Server.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Game.GlobalState
{
    public sealed class GlobalState
    {
        private List<GlobalEntity> List { get; set; }

        public GlobalState()
        {
            Reset();
        }

        public void Reset()
        {
            List = new List<GlobalEntity>();
        }

        public void ClearStates()
        {
            Reset();
        }

        public void EntityAdd(string globalname, string mapName, GlobalEState state)
        {
            if (EntityInTable(globalname))
            {
                throw new ArgumentException("Global entity already in list", nameof(globalname));
            }

            List.Add(new GlobalEntity
            {
                Name = globalname,
                LevelName = mapName,
                State = state
            });
        }

        public void EntitySetState(string globalname, GlobalEState state)
        {
            var pEnt = Find(globalname);

            if (pEnt != null)
                pEnt.State = state;
        }

        public void EntityUpdate(string globalname, string mapname)
        {
            var pEnt = Find(globalname);

            if (pEnt != null)
                pEnt.LevelName = mapname;
        }

        public GlobalEntity EntityFromTable(string globalname)
        {
            return Find(globalname);
        }

        public GlobalEState EntityGetState(string globalname)
        {
            var pEnt = Find(globalname);
            if (pEnt != null)
            {
                return pEnt.State;
            }

            return GlobalEState.Off;
        }

        public bool EntityInTable(string globalname)
        {
            return Find(globalname) != null;
        }

        public bool Save(CSave save)
        {
            //TODO: implement
            /*
            if (!save.WriteFields("GLOBAL", this, m_SaveData, ARRAYSIZE(m_SaveData)))
                return false;

            var pEntity = m_pList;
            for (var i = 0; i < m_listCount && pEntity; ++i)
            {
                if (!save.WriteFields("GENT", pEntity, gGlobalEntitySaveData, ARRAYSIZE(gGlobalEntitySaveData)))
                    return false;

                pEntity = pEntity->pNext;
            }
            */

            return true;
        }

        public bool Restore(CRestore restore)
        {
            //TODO: implement
            /*
            ClearStates();
            if (!restore.ReadFields("GLOBAL", this, m_SaveData, ARRAYSIZE(m_SaveData)))
                return false;

            var listCount = m_listCount;    // Get new list count
            m_listCount = 0;                // Clear loaded data

            globalentity_t tmpEntity;

            for (var i = 0; i < listCount; ++i)
            {
                if (!restore.ReadFields("GENT", &tmpEntity, gGlobalEntitySaveData, ARRAYSIZE(gGlobalEntitySaveData)))
                    return false;
                EntityAdd(MAKE_STRING(tmpEntity.name), MAKE_STRING(tmpEntity.levelName), tmpEntity.state);
            }
            */

            return true;
        }

        public void DumpGlobals()
        {
            Log.Alert(AlertType.Console, "-- Globals --\n");

            foreach (var entity in List)
            {
                Log.Alert(AlertType.Console, $"{entity.Name}: {entity.LevelName} ({entity.State.ToString()})\n");
            }
        }

        private GlobalEntity Find(string globalname)
        {
            if (string.IsNullOrEmpty(globalname))
            {
                return null;
            }

            return List.FirstOrDefault(e => e.Name == globalname);
        }
    }
}
