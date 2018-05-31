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
using GoldSource.Server.Engine;
using GoldSource.Shared.Engine;
using GoldSource.Shared.Engine.Sound;
using GoldSource.Shared.Entities;
using Server.Game.Entities.MetaData;
using Server.Game.Entities.Sound;
using Server.Game.Entities.Weapons;
using Server.Utility;

namespace Server.Game.Entities.Characters
{
    [LinkEntityToClass("player")]
    public class BasePlayer : BaseCharacter
    {
        public EHandle<BasePlayer> m_hObserverTarget;
        public float m_flNextObserverInput;
        public int m_iObserverWeapon;  // weapon of current tracked target
        public int m_iObserverLastMode;// last used observer mode

        /// <summary>
        /// See that is shared between client & server for shared weapons code
        /// </summary>
        public uint RandomSeed { get; set; }

        private int m_iPlayerSound;// the index of the sound list slot reserved for this player
        private int m_iTargetVolume;// ideal sound volume. 
        private int m_iWeaponVolume;// how loud the player's weapon is right now.
        private int m_iExtraSoundTypes;// additional classification for this weapon's sound
        private int m_iWeaponFlash;// brightness of the weapon flash
        private float m_flStopExtraSoundTime;

        private float m_flFlashLightTime;   // Time until next battery draw/Recharge
        private int m_iFlashBattery;        // Flashlight Battery Draw

        private InputKeys CurrentButtons
        {
            get => (InputKeys)Button;
            set => Button = (int)value;
        }

        private InputKeys m_afButtonLast;
        private InputKeys m_afButtonPressed;
        private InputKeys m_afButtonReleased;

        public EHandle<EnvSound> LastSound;         // last sound entity to modify player room type
        public float SoundRoomType;      // last roomtype set by sound entity
        public float SoundRange;         // dist from player to sound entity

        public float m_flFallVelocity;

        //private int m_rgItems[MAX_ITEMS];
        private int m_fKnownItem;       // True when a new item needs to be added
        private int m_fNewAmmo;         // True when a new item has been added

        private PhysicsFlags m_afPhysicsFlags;  // physics flags - set when 'normal' physics should be revisited or overriden
        private float m_fNextSuicideTime; // the time after which the player can next use the suicide command

        // these are time-sensitive things that we keep track of
        private float m_flTimeStepSound;    // when the last stepping sound was made
        private float m_flTimeWeaponIdle; // when to play another weapon idle animation.
        private float m_flSwimTime;     // how long player has been underwater
        private float m_flDuckTime;     // how long we've been ducking
        private float m_flWallJumpTime;	// how long until next walljump

        private float m_flSuitUpdate;                   // when to play next suit update
        //private int m_rgSuitPlayList[CSUITPLAYLIST];// next sentencenum to play for suit update
        private int m_iSuitPlayNext;                // next sentence slot for queue storage;
        //private int m_rgiSuitNoRepeat[CSUITNOREPEAT];       // suit sentence no repeat list
        //private float m_rgflSuitNoRepeatTime[CSUITNOREPEAT];    // how long to wait before allowing repeat
        private int m_lastDamageAmount;     // Last damage taken
        private float m_tbdPrev;                // Time-based damage timer

        private float m_flgeigerRange;      // range to nearest radiation source
        private float m_flgeigerDelay;      // delay per update of range msg to client
        private int m_igeigerRangePrev;
        private int m_iStepLeft;            // alternate left/right foot stepping sound
        //private char m_szTextureName[CBTEXTURENAMEMAX]; // current texture name we're standing on
        private char m_chTextureType;		// current texture type

        private int m_idrowndmg;            // track drowning damage taken
        private int m_idrownrestored;       // track drowning damage restored

        private int m_bitsHUDDamage;        // Damage bits for the current fame. These get sent to 
                                            // the hude via the DAMAGE message
        private bool m_fInitHUD;                // True when deferred HUD restart msg needs to be sent
        private bool m_fGameHUDInitialized;
        private int m_iTrain;               // Train control position
        private bool m_fWeapon;             // Set this to FALSE to force a reset of the current weapon HUD info

        private EHandle<BaseEntity> m_pTank;                // the tank which the player is currently controlling,  null if no tank
        private float m_fDeadTime;          // the time at which the player died  (used in PlayerDeathThink())

        private bool m_fNoPlayerSound;  // a debugging feature. Player makes no sound if this is true. 
        private bool m_fLongJump; // does this player have the longjump module?

        private float m_tSneaking;
        private int m_iUpdateTime;      // stores the number of frame ticks before sending HUD update messages
        private int m_iClientHealth;    // the health currently known by the client.  If this changes, send a new
        private int m_iClientBattery;   // the Battery currently known by the client.  If this changes, send a new
        private int m_iHideHUD;     // the players hud weapon info is to be hidden
        private int m_iClientHideHUD;
        private int m_iFOV;         // field of view
        private int m_iClientFOV;   // client's known FOV
                                    // usable player items 
                                    //private BasePlayerItem m_rgpPlayerItems[MAX_ITEM_TYPES];
        public BasePlayerItem m_pActiveItem;
        private BasePlayerItem m_pClientActiveItem;  // client version of the active item
        private BasePlayerItem m_pLastItem;
        // shared ammo slots
        //private int m_rgAmmo[MAX_AMMO_SLOTS];
        //private int m_rgAmmoLast[MAX_AMMO_SLOTS];

        private Vector m_vecAutoAim;
        private bool m_fOnTarget;
        private int m_iDeaths;
        private float m_iRespawnFrames; // used in PlayerDeathThink() to make sure players can always respawn

        int m_lastx, m_lasty;  // These are the previous update's crosshair angles, DON"T SAVE/RESTORE


        private int CustomSprayFrames;// Custom clan logo frames for this player
        private float m_flNextDecalTime;// next time this player can spray a decal

        public float NextChatTime;

        private float m_flStartCharge;
        public float m_flAmmoStartCharge;
        private float m_flPlayAftershock;
        public float m_flNextAmmoBurn;// while charging, when to absorb another unit of player's ammo?

        public override void Spawn()
        {
            //TODO: define default health & armor somewhere
            Health = 100;
            ArmorValue = 0;
            TakeDamageState = TakeDamageState.Aim;
            Solid = Solid.SlideBox;
            MoveType = MoveType.Walk;
            MaxHealth = Health;
            Flags &= EntFlags.Proxy; // keep proxy flag sey by engine
            Flags |= EntFlags.Client;
            pev.AirFinished = Engine.Globals.Time + 12;
            Damage = 2;               // initial water damage
            Effects = EntityEffects.None;
            DeadFlag = DeadFlag.No;
            pev.DamageTake = 0;
            pev.DamageSave = 0;
            Friction = 1.0f;
            Gravity = 1.0f;
            m_bitsHUDDamage = -1;
            m_bitsDamageType = 0;
            m_afPhysicsFlags = 0;
            m_fLongJump = false;// no longjump module. 
            var physicsInfo = Engine.Server.GetClientPhysicsInfoBuffer(Edict());

            physicsInfo.SetValue("slj", "0");
            physicsInfo.SetValue("hl", "1");

            pev.FieldOfView = m_iFOV = 0;// init field of view.
            m_iClientFOV = -1; // make sure fov reset is sent

            m_flNextDecalTime = 0;// let this player decal as soon as he spawns.

            m_flgeigerDelay = Engine.Globals.Time + 2.0f;    // wait a few seconds until user-defined message registrations
                                                             // are recieved by all clients

            m_flTimeStepSound = 0;
            m_iStepLeft = 0;
            m_flFieldOfView = 0.5f;// some monsters use this to determine whether or not the player is looking at them.

            m_bloodColor = BloodColor.Red;
            m_flNextAttack = WeaponUtils.WeaponTimeBase();
            StartSneaking();

            m_iFlashBattery = 99;
            m_flFlashLightTime = 1; // force first message

            // dont let uninitialized value here hurt the player
            m_flFallVelocity = 0;

            Engine.GameRules.SetDefaultPlayerTeam(this);
            Engine.GameRules.GetPlayerSpawnSpot(this);

            SetModel("models/player.mdl");
            Globals.g_ulModelIndexPlayer = ModelIndex;
            //TODO
            //Sequence = LookupActivity(ACT_IDLE);

            if ((Flags & EntFlags.Ducking) != 0)
            {
                SetSize(WorldConstants.DUCK_HULL_MIN, WorldConstants.DUCK_HULL_MAX);
            }
            else
            {
                SetSize(WorldConstants.HULL_MIN, WorldConstants.HULL_MAX);
            }

            ViewOffset = WorldConstants.ViewOffset;
            Precache();
            m_HackedGunPos = new Vector(0, 32, 0);

            //TODO
            //if (m_iPlayerSound == SOUNDLIST_EMPTY)
            //{
            //    Log.Alert(AlertType.Console, "Couldn't alloc player sound slot!\n");
            //}

            m_fNoPlayerSound = false;// normal sound behavior.

            m_pLastItem = null;
            m_fInitHUD = true;
            m_iClientHideHUD = -1;  // force this to be recalculated
            m_fWeapon = false;
            m_pClientActiveItem = null;
            m_iClientBattery = -1;

            // reset all ammo values to 0
            //TODO
            //for (int i = 0; i < MAX_AMMO_SLOTS; i++)
            //{
            //    m_rgAmmo[i] = 0;
            //    m_rgAmmoLast[i] = 0;  // client ammo values also have to be reset  (the death hud clear messages does on the client side)
            //}

            m_lastx = m_lasty = 0;

            NextChatTime = Engine.Globals.Time;

            Engine.GameRules.PlayerSpawn(this);
        }

        public void PreThink()
        {
            var buttonsChanged = (m_afButtonLast ^ CurrentButtons);    // These buttons have changed this frame

            // Debounced button codes for pressed/released
            // UNDONE: Do we need auto-repeat?
            m_afButtonPressed = buttonsChanged & CurrentButtons;       // The changed ones still down are "pressed"
            m_afButtonReleased = buttonsChanged & (~CurrentButtons);   // The ones not down are "released"

            Engine.GameRules.PlayerThink(this);

            //TODO:
        }

        public void PostThink()
        {
            //TODO:
            // do weapon stuff
            ItemPostFrame();

            //TODO:

            // Track button info so we can detect 'pressed' and 'released' buttons next frame
            m_afButtonLast = CurrentButtons;
        }

        /// <summary>
        /// Called every frame by the player PostThink
        /// </summary>
        private void ItemPostFrame()
        {
            // check if the player is using a tank
            if (m_pTank)
            {
                return;
            }

#if CLIENT_WEAPONS
            if (m_flNextAttack > 0)
#else
            if (Engine.Globals.Time < m_flNextAttack)
#endif
            {
                return;
            }

            ImpulseCommands();

            if (m_pActiveItem == null)
            {
                return;
            }

            m_pActiveItem.ItemPostFrame();
        }

        public bool IsObserver()
        {
            return false;
        }

        /// <summary>
        /// Returns the # of custom frames this player's custom clan logo contains
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        public int GetCustomDecalFrames()
        {
            return CustomSprayFrames;
        }

        /// <summary>
        /// UNDONE:  Determine real frame limit, 8 is a placeholder.
        /// Note:  -1 means no custom frames present
        /// </summary>
        /// <param name="nFrames"></param>
        public void SetCustomDecalFrames(int nFrames)
        {
            if (nFrames > 0 && nFrames < 8)
            {
                CustomSprayFrames = nFrames;
            }
            else
            {
                CustomSprayFrames = -1;
            }
        }

        //TODO
        public static int GetAmmoIndex(string name)
        {
            return -1;
        }

        //TODO
        public int AmmoInventory(int index)
        {
            return 0;
        }

        //TODO
        public bool HasPlayerItem(BasePlayerItem item)
        {
            return false;
        }

        /// <summary>
        /// handles USE keypress
        /// </summary>
        private void PlayerUse()
        {
            if (IsObserver())
            {
                return;
            }

            // Was use pressed or released?
            if (((CurrentButtons | m_afButtonPressed | m_afButtonReleased) & InputKeys.Use) == 0)
            {
                return;
            }

            // Hit Use on a train?
            if ((m_afButtonPressed & InputKeys.Use) != 0)
            {
                if (m_pTank)
                {
                    // Stop controlling the tank
                    // TODO: Send HUD Update
                    m_pTank.Entity.Use(this, this, UseType.Off, 0);
                    m_pTank.Set(null);
                    return;
                }
                else
                {
                    if ((m_afPhysicsFlags & PhysicsFlags.OnTrain) != 0)
                    {
                        m_afPhysicsFlags &= ~PhysicsFlags.OnTrain;
                        m_iTrain = TrainSpeeds.New | TrainSpeeds.Off;
                        return;
                    }
                    else
                    {   // Start controlling the train!
                        var pTrain = GroundEntity;

                        if (pTrain != null
                            && (CurrentButtons & InputKeys.Jump) == 0
                            && (Flags & EntFlags.OnGround) != 0
                            && (pTrain.ObjectCaps() & EntityCapabilities.DirectionalUse) != 0
                            && pTrain.OnControls(this))
                        {
                            m_afPhysicsFlags |= PhysicsFlags.OnTrain;
                            m_iTrain = TrainSpeeds.TrainSpeed((int)pTrain.Speed, pTrain.Impulse);
                            m_iTrain |= TrainSpeeds.New;
                            EmitSound(SoundChannel.Item, "plats/train_use1.wav", 0.8f);
                            return;
                        }
                    }
                }
            }

            BaseEntity pObject = null;
            BaseEntity pClosest = null;
            Vector vecLOS;
            var flMaxDot = ViewField.Narrow;

            MathUtils.MakeVectors(ViewAngle);// so we know which way we are facing

            while ((pObject = EntUtils.FindEntityInSphere(pObject, Origin, WorldConstants.PlayerUseSearchRadius)) != null)
            {
                if ((pObject.ObjectCaps() & (EntityCapabilities.ImpulseUse | EntityCapabilities.ContinuousUse | EntityCapabilities.OnOffUse)) != 0)
                {
                    // !!!PERFORMANCE- should this check be done on a per case basis AFTER we've determined that
                    // this object is actually usable? This dot is being done for every object within PLAYER_SEARCH_RADIUS
                    // when player hits the use key. How many objects can be in that area, anyway? (sjb)
                    vecLOS = (EntUtils.BrushModelOrigin(pObject) - (Origin + ViewOffset));

                    // This essentially moves the origin of the target to the corner nearest the player to test to see 
                    // if it's "hull" is in the view cone
                    vecLOS = MathUtils.ClampVectorToBox(vecLOS, pObject.Size * 0.5f);

                    var flDot = vecLOS.DotProduct(Engine.Globals.ForwardVector);
                    if (flDot > flMaxDot)
                    {// only if the item is in front of the user
                        pClosest = pObject;
                        flMaxDot = flDot;
                        //				Log.Alert(AlertType.Console, $"{pObject.ClassName} : {flDot}\n");
                    }
                    //			Log.Alert(AlertType.Console, $"{pObject.ClassName} : {flDot}\n");
                }
            }
            pObject = pClosest;

            // Found an object
            if (pObject != null)
            {
                //!!!UNDONE: traceline here to prevent USEing buttons through walls			
                var caps = pObject.ObjectCaps();

                if ((m_afButtonPressed & InputKeys.Use) != 0)
                {
                    EmitSound(SoundChannel.Item, "common/wpn_select.wav", 0.4f);
                }

                if (((CurrentButtons & InputKeys.Use) != 0 && (caps & EntityCapabilities.ContinuousUse) != 0)
                     || ((m_afButtonPressed & InputKeys.Use) != 0 && (caps & (EntityCapabilities.ImpulseUse | EntityCapabilities.OnOffUse)) != 0))
                {
                    if ((caps & EntityCapabilities.ContinuousUse) != 0)
                    {
                        m_afPhysicsFlags |= PhysicsFlags.Using;
                    }

                    pObject.Use(this, this, UseType.Set, 1);
                }
                // UNDONE: Send different USE codes for ON/OFF.  Cache last ONOFF_USE object to send 'off' if you turn away
                else if ((m_afButtonReleased & InputKeys.Use) != 0 && (pObject.ObjectCaps() & EntityCapabilities.OnOffUse) != 0) // BUGBUG This is an "off" use
                {
                    pObject.Use(this, this, UseType.Set, 0);
                }
            }
            else
            {
                if ((m_afButtonPressed & InputKeys.Use) != 0)
                {
                    EmitSound(SoundChannel.Item, "common/wpn_denyselect.wav", 0.4f);
                }
            }
        }

        private void ImpulseCommands()
        {
            TraceResult tr;// UNDONE: kill me! This is temporary for PreAlpha CDs

            // Handle use events
            PlayerUse();

            //TODO: implement
        }
    }
}
