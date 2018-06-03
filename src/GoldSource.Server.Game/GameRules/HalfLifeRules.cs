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
using GoldSource.Server.Game.Game.Entities;
using GoldSource.Server.Game.Game.Entities.Characters;
using GoldSource.Server.Game.Game.Entities.Weapons;
using GoldSource.Shared.Entities;
using GoldSource.Shared.Game;

namespace GoldSource.Server.Game.GameRules
{
    /// <summary>
    /// rules for the single player Half-Life game
    /// </summary>
    public class HalfLifeRules : BaseGameRules
    {
        public HalfLifeRules()
        {
            RefreshSkillData();
        }

        public override void Think()
        {
            //Nothing
        }

        public override bool IsAllowedToSpawn(BaseEntity pEntity)
        {
            return true;
        }

        public override bool AllowFlashlight()
        {
            return true;
        }

        public override bool ShouldSwitchWeapon(BasePlayer pPlayer, BasePlayerItem pWeapon)
        {
            if (pPlayer.m_pActiveItem == null)
            {
                // player doesn't have an active item!
                return true;
            }

            return pPlayer.m_pActiveItem.CanHolster();
        }

        public override bool GetNextBestWeapon(BasePlayer pPlayer, BasePlayerItem pCurrentWeapon)
        {
            return false;
        }

        public override bool IsMultiplayer()
        {
            return false;
        }

        public override bool IsDeathmatch()
        {
            return false;
        }

        public override bool IsCoOp()
        {
            return false;
        }

        public override bool ClientConnected(Edict pEntity, string pszName, string pszAddress, out string rejectReason)
        {
            rejectReason = string.Empty;

            return true;
        }

        public override void InitHUD(BasePlayer pl)
        {
            //Nothing
        }

        public override void ClientDisconnected(Edict pClient)
        {
            //Nothing
        }

        public override float PlayerFallDamage(BasePlayer pPlayer)
        {
            // subtract off the speed at which a player is allowed to fall without being hurt,
            // so damage will be based on speed beyond that, not the entire fall
            pPlayer.m_flFallVelocity -= WorldConstants.PlayerMaxSafeFallSpeed;
            return pPlayer.m_flFallVelocity * WorldConstants.DamageForFallSpeed;
        }

        public override void PlayerSpawn(BasePlayer pPlayer)
        {
            //Nothing
        }

        public override void PlayerThink(BasePlayer pPlayer)
        {
            //Nothing
        }

        public override bool PlayerCanRespawn(BasePlayer pPlayer)
        {
            return true;
        }

        public override float PlayerSpawnTime(BasePlayer pPlayer)
        {
            return Game.Engine.Globals.Time;
        }

        public override int PointsForKill(BasePlayer pAttacker, BasePlayer pKilled)
        {
            return 1;
        }

        public override void PlayerKilled(BasePlayer pVictim, BaseEntity pKiller, BaseEntity pInflictor)
        {
            //Nothing
        }

        public override void DeathNotice(BasePlayer pVictim, BaseEntity pKiller, BaseEntity pInflictor)
        {
            //Nothing
        }

        public override void PlayerGotWeapon(BasePlayer pPlayer, BasePlayerItem pWeapon)
        {
            //Nothing
        }

        public override WeaponRespawn WeaponShouldRespawn(BasePlayerItem pWeapon)
        {
            return WeaponRespawn.No;
        }

        public override float WeaponRespawnTime(BasePlayerItem pWeapon)
        {
            return -1;
        }

        public override float WeaponTryRespawn(BasePlayerItem pWeapon)
        {
            return 0;
        }

        public override Vector WeaponRespawnSpot(BasePlayerItem pWeapon)
        {
            return pWeapon.Origin;
        }

        public override bool CanHaveItem(BasePlayer pPlayer, BaseItem pItem)
        {
            return true;
        }

        public override void PlayerGotItem(BasePlayer pPlayer, BaseItem pItem)
        {
            //Nothing
        }

        public override ItemRespawn ItemShouldRespawn(BaseItem pItem)
        {
            return ItemRespawn.No;
        }

        public override float ItemRespawnTime(BaseItem pItem)
        {
            return -1;
        }

        public override Vector ItemRespawnSpot(BaseItem pItem)
        {
            return pItem.Origin;
        }

        public override void PlayerGotAmmo(BasePlayer pPlayer, string szName, int iCount)
        {
            //Nothing
        }

        public override AmmoRespawn AmmoShouldRespawn(BaseAmmo pAmmo)
        {
            return AmmoRespawn.No;
        }

        public override float AmmoRespawnTime(BaseAmmo pAmmo)
        {
            return -1;
        }

        public override Vector AmmoRespawnSpot(BaseAmmo pAmmo)
        {
            return pAmmo.Origin;
        }

        public override float HealthChargerRechargeTime()
        {
            return 0;// don't recharge
        }

        public override DropGun DeadPlayerWeapons(BasePlayer pPlayer)
        {
            return DropGun.No;
        }

        public override DropAmmo DeadPlayerAmmo(BasePlayer pPlayer)
        {
            return DropAmmo.No;
        }

        public override string GetTeamID(BaseEntity pEntity)
        {
            return string.Empty;
        }

        public override Relationship PlayerRelationship(BaseEntity pPlayer, BaseEntity pTarget)
        {
            return Relationship.NotTeammate;
        }

        public override bool AllowMonsters()
        {
            return true;
        }
    }
}
