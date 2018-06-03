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

using GoldSource.Mathlib;
using GoldSource.Server.Engine.API;
using GoldSource.Server.Game.Game.Entities;
using GoldSource.Server.Game.Game.Entities.Characters;
using GoldSource.Server.Game.Game.Entities.Weapons;
using GoldSource.Shared.Entities;
using GoldSource.Shared.Game;
using System;

namespace GoldSource.Server.Game.GameRules
{
    public abstract class BaseGameRules : IGameRules
    {
        public virtual void RefreshSkillData()
        {
            //TODO:
        }

        public abstract void Think();

        public abstract bool IsAllowedToSpawn(BaseEntity pEntity);

        public abstract bool AllowFlashlight();

        public abstract bool ShouldSwitchWeapon(BasePlayer pPlayer, BasePlayerItem pWeapon);

        public abstract bool GetNextBestWeapon(BasePlayer pPlayer, BasePlayerItem pCurrentWeapon);

        public abstract bool IsMultiplayer();

        public abstract bool IsDeathmatch();

        public virtual bool IsTeamplay()
        {
            return false;
        }

        public abstract bool IsCoOp();

        public virtual string GetGameDescription()
        {
            return "Half-Life";
        }

        public abstract bool ClientConnected(Edict pEntity, string pszName, string pszAddress, out string rejectReason);

        public abstract void InitHUD(BasePlayer pl);

        public abstract void ClientDisconnected(Edict pClient);

        public virtual void UpdateGameMode(BasePlayer pPlayer)
        {
        }

        public abstract float PlayerFallDamage(BasePlayer pPlayer);

        public virtual bool PlayerCanTakeDamage(BasePlayer pPlayer, BaseEntity pAttacker)
        {
            return true;
        }

        public bool ShouldAutoAim(BasePlayer pPlayer, Edict target)
        {
            return true;
        }

        public abstract void PlayerSpawn(BasePlayer pPlayer);

        public abstract void PlayerThink(BasePlayer pPlayer);

        public abstract bool PlayerCanRespawn(BasePlayer pPlayer);

        public abstract float PlayerSpawnTime(BasePlayer pPlayer);

        public virtual BaseEntity GetPlayerSpawnSpot(BasePlayer pPlayer)
        {
            var spawnSpot = PlayerUtils.EntSelectSpawnPoint(pPlayer);

            pPlayer.Origin = spawnSpot.Origin + new Vector(0, 0, 1);
            pPlayer.ViewAngle = WorldConstants.g_vecZero;
            pPlayer.Velocity = WorldConstants.g_vecZero;
            pPlayer.Angles = spawnSpot.Angles;
            pPlayer.PunchAngle = WorldConstants.g_vecZero;
            pPlayer.FixAngle = FixAngleMode.ForceViewAngles;

            return spawnSpot;
        }

        public virtual bool AllowAutoTargetCrosshair()
        {
            return true;
        }

        public virtual bool ClientCommand(BasePlayer pPlayer, ICommand command)
        {
            return false;
        }

        public virtual void ClientUserInfoChanged(BasePlayer pPlayer, IntPtr infobuffer)
        {
        }

        public abstract int PointsForKill(BasePlayer pAttacker, BasePlayer pKilled);

        public abstract void PlayerKilled(BasePlayer pVictim, BaseEntity pKiller, BaseEntity pInflictor);

        public abstract void DeathNotice(BasePlayer pVictim, BaseEntity pKiller, BaseEntity pInflictor);

        public virtual bool CanHavePlayerItem(BasePlayer pPlayer, BasePlayerItem pWeapon)
        {
            // only living players can have items
            if (pPlayer.DeadFlag != DeadFlag.No)
            {
                return false;
            }

            if (pWeapon.pszAmmo1() != null)
            {
                if (!CanHaveAmmo(pPlayer, pWeapon.pszAmmo1(), pWeapon.iMaxAmmo1()))
                {
                    // we can't carry anymore ammo for this gun. We can only 
                    // have the gun if we aren't already carrying one of this type
                    if (pPlayer.HasPlayerItem(pWeapon))
                    {
                        return false;
                    }
                }
            }
            else
            {
                // weapon doesn't use ammo, don't take another if you already have it.
                if (pPlayer.HasPlayerItem(pWeapon))
                {
                    return false;
                }
            }

            // note: will fall through to here if GetItemInfo doesn't fill the struct!
            return true;
        }

        public abstract void PlayerGotWeapon(BasePlayer pPlayer, BasePlayerItem pWeapon);

        public abstract WeaponRespawn WeaponShouldRespawn(BasePlayerItem pWeapon);

        public abstract float WeaponRespawnTime(BasePlayerItem pWeapon);

        public abstract float WeaponTryRespawn(BasePlayerItem pWeapon);

        public abstract Vector WeaponRespawnSpot(BasePlayerItem pWeapon);

        public abstract bool CanHaveItem(BasePlayer pPlayer, BaseItem pItem);

        public abstract void PlayerGotItem(BasePlayer pPlayer, BaseItem pItem);

        public abstract ItemRespawn ItemShouldRespawn(BaseItem pItem);

        public abstract float ItemRespawnTime(BaseItem pItem);

        public abstract Vector ItemRespawnSpot(BaseItem pItem);

        public virtual bool CanHaveAmmo(BasePlayer pPlayer, string pszAmmoName, int iMaxCarry)
        {
            if (pszAmmoName != null)
            {
                var iAmmoIndex = BasePlayer.GetAmmoIndex(pszAmmoName);

                if (iAmmoIndex > -1)
                {
                    if (pPlayer.AmmoInventory(iAmmoIndex) < iMaxCarry)
                    {
                        // player has room for more of this type of ammo
                        return true;
                    }
                }
            }

            return false;
        }

        public abstract void PlayerGotAmmo(BasePlayer pPlayer, string szName, int iCount);

        public abstract AmmoRespawn AmmoShouldRespawn(BaseAmmo pAmmo);

        public abstract float AmmoRespawnTime(BaseAmmo pAmmo);

        public abstract Vector AmmoRespawnSpot(BaseAmmo pAmmo);

        public abstract float HealthChargerRechargeTime();

        public virtual float HEVChargerRechargeTime()
        {
            return 0;
        }

        public abstract DropGun DeadPlayerWeapons(BasePlayer pPlayer);

        public abstract DropAmmo DeadPlayerAmmo(BasePlayer pPlayer);

        public abstract string GetTeamID(BaseEntity pEntity);

        public abstract Relationship PlayerRelationship(BaseEntity pPlayer, BaseEntity pTarget);

        public virtual int GetTeamIndex(string pTeamName)
        {
            return -1;
        }

        public virtual string GetIndexedTeamName(int teamIndex)
        {
            return string.Empty;
        }

        public virtual bool IsValidTeam(string pTeamName)
        {
            return true;
        }

        public virtual void ChangePlayerTeam(BasePlayer pPlayer, string pTeamName, bool bKill, bool bGib)
        {
        }

        public virtual string SetDefaultPlayerTeam(BasePlayer pPlayer)
        {
            return string.Empty;
        }

        public virtual bool PlayTextureSounds()
        {
            return true;
        }

        public virtual bool PlayFootstepSounds(BasePlayer pl, float fvol)
        {
            return true;
        }

        public abstract bool AllowMonsters();

        public virtual void EndMultiplayerGame()
        {
        }
    }
}
