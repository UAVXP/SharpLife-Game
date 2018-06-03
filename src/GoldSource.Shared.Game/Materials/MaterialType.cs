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

using System;
using System.Collections.Generic;
using System.Linq;

namespace GoldSource.Shared.Game.Materials
{
    public sealed class MaterialType
    {
        public sealed class MovementSound
        {
            public string Sound { get; }

            public bool IsLeft { get; }

            public MovementSound(string sound, bool isLeft)
            {
                Sound = sound ?? throw new ArgumentNullException(nameof(sound));
                IsLeft = isLeft;
            }
        }

        public MaterialTypeCode Code { get; }

        public float ImpactVolume { get; }

        /// <summary>
        /// The volume that the crowbar should make when this material is being hit by it
        /// TODO: this is terrible
        /// </summary>
        public float CrowbarVolume { get; }

        public float WalkVolume { get; }

        public float RunVolume { get; }

        public int WalkTimeDelay { get; }

        public int RunTimeDelay { get; }

        /// <summary>
        /// If non-zero, this is how many steps to play sounds before skipping playback of one
        /// </summary>
        public int SkipStepInterval { get; set; }

        public IReadOnlyList<string> ImpactSounds { get; }

        public IReadOnlyList<MovementSound> MovementSounds { get; }

        public bool HasLeftMovementSounds { get; }

        public bool HasRightMovementSounds { get; }

        public delegate bool FilterDelegate(ref MaterialEmitEvent emitEvent);

        /// <summary>
        /// Optional filter predicate used to determine if a sound should play, and to allow the index to be overridden
        /// </summary>
        public FilterDelegate Filter { get; }

        public MaterialType(MaterialTypeCode code,
            float impactVolume, float crowbarVolume, float walkVolume, float runVolume,
            int walkTimeDelay, int runTimeDelay,
            IReadOnlyList<string> impactSounds, IReadOnlyList<MovementSound> movementSounds,
            FilterDelegate filter = null)
        {
            Code = code;
            ImpactVolume = impactVolume;
            CrowbarVolume = crowbarVolume;
            WalkVolume = walkVolume;
            RunVolume = runVolume;
            WalkTimeDelay = walkTimeDelay;
            RunTimeDelay = runTimeDelay;
            ImpactSounds = impactSounds ?? throw new ArgumentNullException(nameof(impactSounds));
            MovementSounds = movementSounds ?? throw new ArgumentNullException(nameof(movementSounds));

            //Cache these off so we don't have to keep checking
            HasLeftMovementSounds = MovementSounds.Any(s => s.IsLeft);
            HasRightMovementSounds = MovementSounds.Any(s => !s.IsLeft);

            Filter = filter;
        }
    }
}
