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

using Server.Game.Entities.MetaData;
using Server.Persistence;

namespace Server.Game.Entities.Plats
{
    public class BasePlatTrain : BaseToggle
    {
        public static class SF
        {
            public const uint Toggle = 1 << 0;
        }

        /// <summary>
        /// sound a plat makes while moving
        /// </summary>
        [KeyValue]
        [Persist]
        public byte MoveSnd;

        /// <summary>
        /// sound a plat makes when it stops
        /// </summary>
        [KeyValue]
        [Persist]
        public byte StopSnd;

        /// <summary>
        /// Sound volume
        /// </summary>
        [KeyValue]
        [Persist]
        protected float Volume;

        protected string NoiseMoving
        {
            get => pev.Noise;
            set => pev.Noise = value;
        }

        protected string NoiseArrived
        {
            get => pev.Noise1;
            set => pev.Noise1 = value;
        }

        public override EntityCapabilities ObjectCaps() => base.ObjectCaps() & ~EntityCapabilities.AcrossTransition;

        public override bool KeyValue(string key, string value)
        {
            if (key == "height")
            {
                float.TryParse(value, out Height);
                return true;
            }
            else if (key == "rotation")
            {
                float.TryParse(value, out FinalAngle.x);
                return true;
            }

            return base.KeyValue(key, value);
        }

        public override void Precache()
        {
            // set the plat's "in-motion" sound
            switch (MoveSnd)
            {
                case 0:
                    NoiseMoving = "common/null.wav";
                    break;
                case 1:
                    NoiseMoving = "plats/bigmove1.wav";
                    break;
                case 2:
                    NoiseMoving = "plats/bigmove2.wav";
                    break;
                case 3:
                    NoiseMoving = "plats/elevmove1.wav";
                    break;
                case 4:
                    NoiseMoving = "plats/elevmove2.wav";
                    break;
                case 5:
                    NoiseMoving = "plats/elevmove3.wav";
                    break;
                case 6:
                    NoiseMoving = "plats/freightmove1.wav";
                    break;
                case 7:
                    NoiseMoving = "plats/freightmove2.wav";
                    break;
                case 8:
                    NoiseMoving = "plats/heavymove1.wav";
                    break;
                case 9:
                    NoiseMoving = "plats/rackmove1.wav";
                    break;
                case 10:
                    NoiseMoving = "plats/railmove1.wav";
                    break;
                case 11:
                    NoiseMoving = "plats/squeekmove1.wav";
                    break;
                case 12:
                    NoiseMoving = "plats/talkmove1.wav";
                    break;
                case 13:
                    NoiseMoving = "plats/talkmove2.wav";
                    break;
                default:
                    NoiseMoving = "common/null.wav";
                    break;
            }

            Engine.Server.PrecacheSound(NoiseMoving);

            // set the plat's 'reached destination' stop sound
            switch (StopSnd)
            {
                case 0:
                    NoiseArrived = "common/null.wav";
                    break;
                case 1:
                    NoiseArrived = "plats/bigstop1.wav";
                    break;
                case 2:
                    NoiseArrived = "plats/bigstop2.wav";
                    break;
                case 3:
                    NoiseArrived = "plats/freightstop1.wav";
                    break;
                case 4:
                    NoiseArrived = "plats/heavystop2.wav";
                    break;
                case 5:
                    NoiseArrived = "plats/rackstop1.wav";
                    break;
                case 6:
                    NoiseArrived = "plats/railstop1.wav";
                    break;
                case 7:
                    NoiseArrived = "plats/squeekstop1.wav";
                    break;
                case 8:
                    NoiseArrived = "plats/talkstop1.wav";
                    break;

                default:
                    NoiseArrived = "common/null.wav";
                    break;
            }

            Engine.Server.PrecacheSound(NoiseArrived);
        }

        // This is done to fix spawn flag collisions between this class and a derived class
        public virtual bool IsTogglePlat() { return (SpawnFlags & SF.Toggle) != 0; }
    }
}
