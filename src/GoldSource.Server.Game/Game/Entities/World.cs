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

using GoldSource.Server.Game.Game.Entities.MetaData;
using GoldSource.Server.Game.GameRules;

namespace GoldSource.Server.Game.Game.Entities
{
    [LinkEntityToClass("worldspawn")]
    public class World : BaseEntity
    {
        //TODO: implement
        public static World WorldInstance { get; set; }

        /// <summary>
        /// The wad keyvalue
        /// </summary>
        [KeyValue]
        public string Wad { get; set; }

        public override void OnCreate()
        {
            base.OnCreate();

            WorldInstance = this;
        }

        public override void OnDestroy()
        {
            WorldInstance = null;

            base.OnDestroy();
        }

        public override void Spawn()
        {
            Log.Message($"Wad value is {Wad}");
            Precache();
        }

        public override void Precache()
        {
            Engine.GameRules = GameRulesFactory.Create();

            ClientPrecache();
        }

        public static void ClientPrecache()
        {
            // setup precaches always needed
            Engine.Server.PrecacheSound("player/sprayer.wav");           // spray paint sound for PreAlpha

            // Engine.Server.PrecacheSound("player/pl_jumpland2.wav");		// UNDONE: play 2x step sound

            Engine.Server.PrecacheSound("player/pl_fallpain2.wav");
            Engine.Server.PrecacheSound("player/pl_fallpain3.wav");

            Engine.Server.PrecacheSound("player/pl_step1.wav");      // walk on concrete
            Engine.Server.PrecacheSound("player/pl_step2.wav");
            Engine.Server.PrecacheSound("player/pl_step3.wav");
            Engine.Server.PrecacheSound("player/pl_step4.wav");

            Engine.Server.PrecacheSound("common/npc_step1.wav");     // NPC walk on concrete
            Engine.Server.PrecacheSound("common/npc_step2.wav");
            Engine.Server.PrecacheSound("common/npc_step3.wav");
            Engine.Server.PrecacheSound("common/npc_step4.wav");

            Engine.Server.PrecacheSound("player/pl_metal1.wav");     // walk on metal
            Engine.Server.PrecacheSound("player/pl_metal2.wav");
            Engine.Server.PrecacheSound("player/pl_metal3.wav");
            Engine.Server.PrecacheSound("player/pl_metal4.wav");

            Engine.Server.PrecacheSound("player/pl_dirt1.wav");      // walk on dirt
            Engine.Server.PrecacheSound("player/pl_dirt2.wav");
            Engine.Server.PrecacheSound("player/pl_dirt3.wav");
            Engine.Server.PrecacheSound("player/pl_dirt4.wav");

            Engine.Server.PrecacheSound("player/pl_duct1.wav");      // walk in duct
            Engine.Server.PrecacheSound("player/pl_duct2.wav");
            Engine.Server.PrecacheSound("player/pl_duct3.wav");
            Engine.Server.PrecacheSound("player/pl_duct4.wav");

            Engine.Server.PrecacheSound("player/pl_grate1.wav");     // walk on grate
            Engine.Server.PrecacheSound("player/pl_grate2.wav");
            Engine.Server.PrecacheSound("player/pl_grate3.wav");
            Engine.Server.PrecacheSound("player/pl_grate4.wav");

            Engine.Server.PrecacheSound("player/pl_slosh1.wav");     // walk in shallow water
            Engine.Server.PrecacheSound("player/pl_slosh2.wav");
            Engine.Server.PrecacheSound("player/pl_slosh3.wav");
            Engine.Server.PrecacheSound("player/pl_slosh4.wav");

            Engine.Server.PrecacheSound("player/pl_tile1.wav");      // walk on tile
            Engine.Server.PrecacheSound("player/pl_tile2.wav");
            Engine.Server.PrecacheSound("player/pl_tile3.wav");
            Engine.Server.PrecacheSound("player/pl_tile4.wav");
            Engine.Server.PrecacheSound("player/pl_tile5.wav");

            Engine.Server.PrecacheSound("player/pl_swim1.wav");      // breathe bubbles
            Engine.Server.PrecacheSound("player/pl_swim2.wav");
            Engine.Server.PrecacheSound("player/pl_swim3.wav");
            Engine.Server.PrecacheSound("player/pl_swim4.wav");

            Engine.Server.PrecacheSound("player/pl_ladder1.wav");    // climb ladder rung
            Engine.Server.PrecacheSound("player/pl_ladder2.wav");
            Engine.Server.PrecacheSound("player/pl_ladder3.wav");
            Engine.Server.PrecacheSound("player/pl_ladder4.wav");

            Engine.Server.PrecacheSound("player/pl_wade1.wav");      // wade in water
            Engine.Server.PrecacheSound("player/pl_wade2.wav");
            Engine.Server.PrecacheSound("player/pl_wade3.wav");
            Engine.Server.PrecacheSound("player/pl_wade4.wav");

            Engine.Server.PrecacheSound("debris/wood1.wav");         // hit wood texture
            Engine.Server.PrecacheSound("debris/wood2.wav");
            Engine.Server.PrecacheSound("debris/wood3.wav");

            Engine.Server.PrecacheSound("plats/train_use1.wav");     // use a train

            Engine.Server.PrecacheSound("buttons/spark5.wav");       // hit computer texture
            Engine.Server.PrecacheSound("buttons/spark6.wav");
            Engine.Server.PrecacheSound("debris/glass1.wav");
            Engine.Server.PrecacheSound("debris/glass2.wav");
            Engine.Server.PrecacheSound("debris/glass3.wav");

            Engine.Server.PrecacheSound(WorldConstants.SoundFlashlightOn);
            Engine.Server.PrecacheSound(WorldConstants.SoundFlashlightOff);

            // player gib sounds
            Engine.Server.PrecacheSound("common/bodysplat.wav");

            // player pain sounds
            Engine.Server.PrecacheSound("player/pl_pain2.wav");
            Engine.Server.PrecacheSound("player/pl_pain4.wav");
            Engine.Server.PrecacheSound("player/pl_pain5.wav");
            Engine.Server.PrecacheSound("player/pl_pain6.wav");
            Engine.Server.PrecacheSound("player/pl_pain7.wav");

            Engine.Server.PrecacheModel("models/player.mdl");

            // hud sounds

            Engine.Server.PrecacheSound("common/wpn_hudoff.wav");
            Engine.Server.PrecacheSound("common/wpn_hudon.wav");
            Engine.Server.PrecacheSound("common/wpn_moveselect.wav");
            Engine.Server.PrecacheSound("common/wpn_select.wav");
            Engine.Server.PrecacheSound("common/wpn_denyselect.wav");

            // geiger sounds

            Engine.Server.PrecacheSound("player/geiger6.wav");
            Engine.Server.PrecacheSound("player/geiger5.wav");
            Engine.Server.PrecacheSound("player/geiger4.wav");
            Engine.Server.PrecacheSound("player/geiger3.wav");
            Engine.Server.PrecacheSound("player/geiger2.wav");
            Engine.Server.PrecacheSound("player/geiger1.wav");

            if (Globals.giPrecacheGrunt)
            {
                //TODO
                //UTIL_PrecacheOther("monster_human_grunt");
            }
        }
    }
}
