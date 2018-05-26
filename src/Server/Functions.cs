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
using Server.Game.Entities;
using Server.Game.Entities.Characters;
using Server.Game.Entities.Weapons;
using Server.GameRules;
using Server.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Game
{
    public static class Functions
    {
        public static int ShouldSimplify(int routeType)
        {
            routeType &= ~bits_MF_IS_GOAL;

            if ((routeType == bits_MF_TO_PATHCORNER) || (routeType & bits_MF_DONT_SIMPLIFY))
                return false;
            return true;
        }

        public static void ScriptEntityCancel(Edict pentCine)
        {
            // make sure they are a scripted_sequence
            if (FClassnameIs(pentCine, CLASSNAME))
            {
                CCineMonster pCineTarget = GetClassPtr((CCineMonster)VARS(pentCine));
                // make sure they have a monster in mind for the script
                BaseEntity pEntity = pCineTarget.m_hTargetEnt;
                CBaseMonster pTarget = null;
                if (pEntity != null)
                    pTarget = pEntity.MyMonsterPointer();

                if (pTarget)
                {
                    // make sure their monster is actually playing a script
                    if (pTarget.m_MonsterState == MONSTERSTATE_SCRIPT)
                    {
                        // tell them do die
                        pTarget.m_scriptState = CCineMonster.SCRIPT_CLEANUP;
                        // do it now
                        pTarget.CineCleanup();
                    }
                }
            }
        }

        /*
        *	Tracers fire every fourth bullet
        *	=========================================================
        *	MaxAmmoCarry - pass in a name and this function will tell
        *	you the maximum amount of that type of ammunition that a 
        *	player can carry.
        *	=========================================================
        */
        public static int MaxAmmoCarry(int iszName)
        {
            for (int i = 0; i < MAX_WEAPONS; i++)
            {
                if (BasePlayerItem::ItemInfoArray[i].pszAmmo1 && !strcmp(STRING(iszName), BasePlayerItem::ItemInfoArray[i].pszAmmo1))
                    return BasePlayerItem::ItemInfoArray[i].iMaxAmmo1;
                if (BasePlayerItem::ItemInfoArray[i].pszAmmo2 && !strcmp(STRING(iszName), BasePlayerItem::ItemInfoArray[i].pszAmmo2))
                    return BasePlayerItem::ItemInfoArray[i].iMaxAmmo2;
            }

            Log.Alert(AlertType.Console, "MaxAmmoCarry() doesn't recognize '%s'!\n", STRING(iszName));
            return -1;
        }
        //Precaches the ammo and queues the ammo info for sending to clients
        public static void AddAmmoNameToAmmoRegistry(char szAmmoname)
        {
            // make sure it's not already in the registry
            for (int i = 0; i < MAX_AMMO_SLOTS; i++)
            {
                if (!BasePlayerItem::AmmoInfoArray[i].pszName)
                    continue;

                if (stricmp(BasePlayerItem.AmmoInfoArray[i].pszName, szAmmoname) == 0)
                    return; // ammo already in registry, just quite
            }

            giAmmoIndex++;
            ASSERT(giAmmoIndex < MAX_AMMO_SLOTS);
            if (giAmmoIndex >= MAX_AMMO_SLOTS)
                giAmmoIndex = 0;

            BasePlayerItem::AmmoInfoArray[giAmmoIndex].pszName = szAmmoname;
            BasePlayerItem::AmmoInfoArray[giAmmoIndex].iId = giAmmoIndex;   // yes, this info is redundant
        }
        //Precaches the weapon and queues the weapon info for sending to clients
        public static void UTIL_PrecacheOtherWeapon(string szClassname)
        {
            var entity = Engine.EntityRegistry.CreateInstance(szClassname);

            if (entity == null)
            {
                Log.Alert(AlertType.Console, $"null Ent in UTIL_PrecacheOtherWeapon {szClassname}\n");
                return;
            }

            if (entity != null)
            {
                ItemInfo II;
                entity.Precache();
                memset(&II, 0, sizeof II);
                if (((BasePlayerItem)entity).GetItemInfo(&II))
                {
                    BasePlayerItem::ItemInfoArray[II.iId] = II;

                    if (II.pszAmmo1 && *II.pszAmmo1)
                    {
                        AddAmmoNameToAmmoRegistry(II.pszAmmo1);
                    }

                    if (II.pszAmmo2 && *II.pszAmmo2)
                    {
                        AddAmmoNameToAmmoRegistry(II.pszAmmo2);
                    }

                    memset(&II, 0, sizeof II);
                }
            }

            REMOVE_ENTITY(entity.Edict());
        }
        //called by worldspawn
        public static void W_Precache()
        {
            memset(BasePlayerItem::ItemInfoArray, 0, sizeof(BasePlayerItem::ItemInfoArray));
            memset(BasePlayerItem::AmmoInfoArray, 0, sizeof(BasePlayerItem::AmmoInfoArray));
            Globals.giAmmoIndex = 0;

            // custom items...

            // common world objects
            EntUtils.PrecacheOther("item_suit");
            EntUtils.PrecacheOther("item_battery");
            EntUtils.PrecacheOther("item_antidote");
            EntUtils.PrecacheOther("item_security");
            EntUtils.PrecacheOther("item_longjump");

            // shotgun
            UTIL_PrecacheOtherWeapon("weapon_shotgun");
            EntUtils.PrecacheOther("ammo_buckshot");

            // crowbar
            UTIL_PrecacheOtherWeapon("weapon_crowbar");

            // glock
            UTIL_PrecacheOtherWeapon("weapon_9mmhandgun");
            EntUtils.PrecacheOther("ammo_9mmclip");

            // mp5
            UTIL_PrecacheOtherWeapon("weapon_9mmAR");
            EntUtils.PrecacheOther("ammo_9mmAR");
            EntUtils.PrecacheOther("ammo_ARgrenades");

#if !OEM_BUILD && !HLDEMO_BUILD
            // python
            UTIL_PrecacheOtherWeapon("weapon_357");
            EntUtils.PrecacheOther("ammo_357");
#endif

#if !OEM_BUILD && !HLDEMO_BUILD
            // gauss
            UTIL_PrecacheOtherWeapon("weapon_gauss");
            EntUtils.PrecacheOther("ammo_gaussclip");
#endif

#if !OEM_BUILD && !HLDEMO_BUILD
            // rpg
            UTIL_PrecacheOtherWeapon("weapon_rpg");
            EntUtils.PrecacheOther("ammo_rpgclip");
#endif

#if !OEM_BUILD && !HLDEMO_BUILD
            // crossbow
            UTIL_PrecacheOtherWeapon("weapon_crossbow");
            EntUtils.PrecacheOther("ammo_crossbow");
#endif

#if !OEM_BUILD && !HLDEMO_BUILD
            // egon
            UTIL_PrecacheOtherWeapon("weapon_egon");
#endif

            // tripmine
            UTIL_PrecacheOtherWeapon("weapon_tripmine");

#if !OEM_BUILD && !HLDEMO_BUILD
            // satchel charge
            UTIL_PrecacheOtherWeapon("weapon_satchel");
#endif

            // hand grenade
            UTIL_PrecacheOtherWeapon("weapon_handgrenade");

#if !OEM_BUILD && !HLDEMO_BUILD
            // squeak grenade
            UTIL_PrecacheOtherWeapon("weapon_snark");
#endif

#if !OEM_BUILD && !HLDEMO_BUILD
            // hornetgun
            UTIL_PrecacheOtherWeapon("weapon_hornetgun");
#endif


#if !OEM_BUILD && !HLDEMO_BUILD
            if (Engine.GameRules.IsDeathmatch())
            {
                EntUtils.PrecacheOther("weaponbox");// container for dropped deathmatch weapons
            }
#endif

            Globals.g_sModelIndexFireball = Engine.Server.PrecacheModel("sprites/zerogxplode.spr");// fireball
            Globals.g_sModelIndexWExplosion = Engine.Server.PrecacheModel("sprites/WXplo1.spr");// underwater fireball
            Globals.g_sModelIndexSmoke = Engine.Server.PrecacheModel("sprites/steam1.spr");// smoke
            Globals.g_sModelIndexBubbles = Engine.Server.PrecacheModel("sprites/bubble.spr");//bubbles
            Globals.g_sModelIndexBloodSpray = Engine.Server.PrecacheModel("sprites/bloodspray.spr"); // initial blood
            Globals.g_sModelIndexBloodDrop = Engine.Server.PrecacheModel("sprites/blood.spr"); // splattered blood 

            Globals.g_sModelIndexLaser = Engine.Server.PrecacheModel(Globals.g_pModelNameLaser);
            Globals.g_sModelIndexLaserDot = Engine.Server.PrecacheModel("sprites/laserdot.spr");


            // used by explosions
            Engine.Server.PrecacheModel("models/grenade.mdl");
            Engine.Server.PrecacheModel("sprites/explode1.spr");

            Engine.Server.PrecacheSound("weapons/debris1.wav");// explosion aftermaths
            Engine.Server.PrecacheSound("weapons/debris2.wav");// explosion aftermaths
            Engine.Server.PrecacheSound("weapons/debris3.wav");// explosion aftermaths

            Engine.Server.PrecacheSound("weapons/grenade_hit1.wav");//grenade
            Engine.Server.PrecacheSound("weapons/grenade_hit2.wav");//grenade
            Engine.Server.PrecacheSound("weapons/grenade_hit3.wav");//grenade

            Engine.Server.PrecacheSound("weapons/bullet_hit1.wav");  // hit by bullet
            Engine.Server.PrecacheSound("weapons/bullet_hit2.wav");  // hit by bullet

            Engine.Server.PrecacheSound("items/weapondrop1.wav");// weapon falls to the ground

        }
    }
}
