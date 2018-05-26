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
using Server.Engine.API;
using Server.Game.Entities;
using Server.Game.Entities.Characters;
using Server.Game.Entities.Weapons;
using System;

namespace Server.GameRules
{
    public interface IGameRules
    {
        /// <summary>
        /// fill skill data struct with proper values
        /// </summary>
        void RefreshSkillData();

        /// <summary>
        /// GR_Think - runs every server frame, should handle any timer tasks, periodic events, etc
        /// </summary>
        void Think();

        /// <summary>
        /// Can this item spawn (eg monsters don't spawn in deathmatch)
        /// </summary>
        /// <param name="pEntity"></param>
        /// <returns></returns>
        bool IsAllowedToSpawn(BaseEntity pEntity);

        /// <summary>
        /// Are players allowed to switch on their flashlight?
        /// </summary>
        /// <returns></returns>
        bool AllowFlashlight();

        /// <summary>
        /// should the player switch to this weapon?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pWeapon"></param>
        /// <returns></returns>
        bool ShouldSwitchWeapon(BasePlayer pPlayer, BasePlayerItem pWeapon);

        /// <summary>
        /// I can't use this weapon anymore, get me the next best one
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pCurrentWeapon"></param>
        /// <returns></returns>
        bool GetNextBestWeapon(BasePlayer pPlayer, BasePlayerItem pCurrentWeapon);

        // Functions to verify the single/multiplayer status of a game

        /// <summary>
        /// is this a multiplayer game? (either coop or deathmatch)
        /// </summary>
        /// <returns></returns>
        bool IsMultiplayer();

        /// <summary>
        /// is this a deathmatch game?
        /// </summary>
        /// <returns></returns>
        bool IsDeathmatch();

        /// <summary>
        /// is this deathmatch game being played with team rules?
        /// </summary>
        /// <returns></returns>
        bool IsTeamplay();

        /// <summary>
        /// is this a coop game?
        /// </summary>
        /// <returns></returns>
        bool IsCoOp();

        /// <summary>
        /// this is the game name that gets seen in the server browser
        /// </summary>
        /// <returns></returns>
        string GetGameDescription();

        // Client connection/disconnection

        /// <summary>
        /// a client just connected to the server (player hasn't spawned yet)
        /// </summary>
        /// <param name="pEntity"></param>
        /// <param name="pszName"></param>
        /// <param name="pszAddress"></param>
        /// <param name="rejectReason"></param>
        /// <returns></returns>
        bool ClientConnected(Edict pEntity, string pszName, string pszAddress, out string rejectReason);

        /// <summary>
        /// the client dll is ready for updating
        /// </summary>
        /// <param name="pl"></param>
        void InitHUD(BasePlayer pl);

        /// <summary>
        /// a client just disconnected from the server
        /// </summary>
        /// <param name="pClient"></param>
        void ClientDisconnected(Edict pClient);

        /// <summary>
        /// the client needs to be informed of the current game mode
        /// </summary>
        /// <param name="pPlayer"></param>
        void UpdateGameMode(BasePlayer pPlayer);

        // Client damage rules

        /// <summary>
        /// this client just hit the ground after a fall. How much damage?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <returns></returns>
        float PlayerFallDamage(BasePlayer pPlayer);

        /// <summary>
        /// can this player take damage from this attacker?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pAttacker"></param>
        /// <returns></returns>
        bool PlayerCanTakeDamage(BasePlayer pPlayer, BaseEntity pAttacker);

        bool ShouldAutoAim(BasePlayer pPlayer, Edict target);

        // Client spawn/respawn control

        /// <summary>
        /// called by BasePlayer.Spawn just before releasing player into the game
        /// </summary>
        /// <param name="pPlayer"></param>
        void PlayerSpawn(BasePlayer pPlayer);

        /// <summary>
        /// called by BasePlayer.PreThink every frame, before physics are run and after keys are accepted
        /// </summary>
        /// <param name="pPlayer"></param>
        void PlayerThink(BasePlayer pPlayer);

        /// <summary>
        /// is this player allowed to respawn now?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <returns></returns>
        bool PlayerCanRespawn(BasePlayer pPlayer);

        /// <summary>
        /// When in the future will this player be able to spawn?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <returns></returns>
        float PlayerSpawnTime(BasePlayer pPlayer);

        /// <summary>
        /// Place this player on their spawnspot and face them the proper direction.
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <returns></returns>
        BaseEntity GetPlayerSpawnSpot(BasePlayer pPlayer);

        bool AllowAutoTargetCrosshair();

        /// <summary>
        /// handles the user commands;  returns true if command handled properly
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        bool ClientCommand(BasePlayer pPlayer, ICommand command);

        /// <summary>
        /// the player has changed userinfo;  can change it now
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="infobuffer"></param>
        void ClientUserInfoChanged(BasePlayer pPlayer, IntPtr infobuffer);

        // Client kills/scoring

        /// <summary>
        /// how many points do I award whoever kills this player?
        /// </summary>
        /// <param name="pAttacker"></param>
        /// <param name="pKilled"></param>
        /// <returns></returns>
        int PointsForKill(BasePlayer pAttacker, BasePlayer pKilled);

        /// <summary>
        /// Called each time a player dies
        /// </summary>
        /// <param name="pVictim"></param>
        /// <param name="pKiller"></param>
        /// <param name="pInflictor"></param>
        void PlayerKilled(BasePlayer pVictim, BaseEntity pKiller, BaseEntity pInflictor);

        /// <summary>
        /// Call this from within a GameRules class to report an obituary
        /// </summary>
        /// <param name="pVictim"></param>
        /// <param name="pKiller"></param>
        /// <param name="pInflictor"></param>
        void DeathNotice(BasePlayer pVictim, BaseEntity pKiller, BaseEntity pInflictor);

        // Weapon retrieval

        /// <summary>
        /// The player is touching an CBasePlayerItem, do I give it to him?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pWeapon"></param>
        /// <returns></returns>
        bool CanHavePlayerItem(BasePlayer pPlayer, BasePlayerItem pWeapon);

        /// <summary>
        /// Called each time a player picks up a weapon from the ground
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pWeapon"></param>
        void PlayerGotWeapon(BasePlayer pPlayer, BasePlayerItem pWeapon);

        // Weapon spawn/respawn control

        /// <summary>
        /// should this weapon respawn?
        /// </summary>
        /// <param name="pWeapon"></param>
        /// <returns></returns>
        WeaponRespawn WeaponShouldRespawn(BasePlayerItem pWeapon);

        /// <summary>
        /// when may this weapon respawn?
        /// </summary>
        /// <param name="pWeapon"></param>
        /// <returns></returns>
        float WeaponRespawnTime(BasePlayerItem pWeapon);

        /// <summary>
        /// Returns 0 if the weapon can respawn now,
        /// otherwise it returns the time at which it can try to spawn again.
        /// </summary>
        /// <param name="pWeapon"></param>
        /// <returns></returns>
        float WeaponTryRespawn(BasePlayerItem pWeapon);

        /// <summary>
        /// where in the world should this weapon respawn?
        /// </summary>
        /// <param name="pWeapon"></param>
        /// <returns></returns>
        Vector WeaponRespawnSpot(BasePlayerItem pWeapon);

        // Item retrieval

        /// <summary>
        /// is this player allowed to take this item?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pItem"></param>
        /// <returns></returns>
        bool CanHaveItem(BasePlayer pPlayer, BaseItem pItem);

        /// <summary>
        /// call each time a player picks up an item (battery, healthkit, longjump)
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pItem"></param>
        void PlayerGotItem(BasePlayer pPlayer, BaseItem pItem);

        // Item spawn/respawn control

        /// <summary>
        /// Should this item respawn?
        /// </summary>
        /// <param name="pItem"></param>
        /// <returns></returns>
        ItemRespawn ItemShouldRespawn(BaseItem pItem);

        /// <summary>
        /// when may this item respawn?
        /// </summary>
        /// <param name="pItem"></param>
        /// <returns></returns>
        float ItemRespawnTime(BaseItem pItem);

        /// <summary>
        /// Where should this item respawn?
        /// Some game variations may choose to randomize spawn locations
        /// </summary>
        /// <param name="pItem"></param>
        /// <returns></returns>
        Vector ItemRespawnSpot(BaseItem pItem);

        // Ammo retrieval

        /// <summary>
        /// can this player take more of this ammo?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pszAmmoName"></param>
        /// <param name="iMaxCarry"></param>
        /// <returns></returns>
        bool CanHaveAmmo(BasePlayer pPlayer, string pszAmmoName, int iMaxCarry);

        /// <summary>
        /// called each time a player picks up some ammo in the world
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="szName"></param>
        /// <param name="iCount"></param>
        void PlayerGotAmmo(BasePlayer pPlayer, string szName, int iCount);

        // Ammo spawn/respawn control

        /// <summary>
        /// should this ammo item respawn?
        /// by default, everything spawns
        /// </summary>
        /// <param name="pAmmo"></param>
        /// <returns></returns>
        AmmoRespawn AmmoShouldRespawn(BaseAmmo pAmmo);

        /// <summary>
        /// when should this ammo item respawn?
        /// </summary>
        /// <param name="pAmmo"></param>
        /// <returns></returns>
        float AmmoRespawnTime(BaseAmmo pAmmo);

        /// <summary>
        /// where in the world should this ammo item respawn?
        /// </summary>
        /// <param name="pAmmo"></param>
        /// <returns></returns>
        Vector AmmoRespawnSpot(BaseAmmo pAmmo);

        // Healthcharger respawn control

        /// <summary>
        /// how long until a depleted HealthCharger recharges itself?
        /// </summary>
        /// <returns></returns>
        float HealthChargerRechargeTime();

        /// <summary>
        /// how long until a depleted HealthCharger recharges itself?
        /// </summary>
        /// <returns></returns>
        float HEVChargerRechargeTime();

        // What happens to a dead player's weapons

        /// <summary>
        /// what do I do with a player's weapons when he's killed?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <returns></returns>
        DropGun DeadPlayerWeapons(BasePlayer pPlayer);

        // What happens to a dead player's ammo	

        /// <summary>
        /// Do I drop ammo when the player dies? How much?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <returns></returns>
        DropAmmo DeadPlayerAmmo(BasePlayer pPlayer);

        // Teamplay stuff

        /// <summary>
        /// what team is this entity on?
        /// </summary>
        /// <param name="pEntity"></param>
        /// <returns></returns>
        string GetTeamID(BaseEntity pEntity);

        /// <summary>
        /// What is the player's relationship with this entity?
        /// </summary>
        /// <param name="pPlayer"></param>
        /// <param name="pTarget"></param>
        /// <returns></returns>
        Relationship PlayerRelationship(BaseEntity pPlayer, BaseEntity pTarget);

        int GetTeamIndex(string pTeamName);

        string GetIndexedTeamName(int teamIndex);

        bool IsValidTeam(string pTeamName);

        void ChangePlayerTeam(BasePlayer pPlayer, string pTeamName, bool bKill, bool bGib);

        string SetDefaultPlayerTeam(BasePlayer pPlayer);

        // Sounds
        bool PlayTextureSounds();

        bool PlayFootstepSounds(BasePlayer pl, float fvol);

        // Monsters

        /// <summary>
        /// are monsters allowed
        /// </summary>
        /// <returns></returns>
        bool AllowMonsters();

        /// <summary>
        /// Immediately end a multiplayer game
        /// </summary>
        void EndMultiplayerGame();
    }
}
