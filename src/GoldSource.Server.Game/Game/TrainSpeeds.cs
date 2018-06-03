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

namespace GoldSource.Server.Game.Game
{
    public static class TrainSpeeds
    {
        public const int Active = 0x80;
        public const int New = 0xc0;
        public const int Off = 0x00;
        public const int Neutral = 0x01;
        public const int Slow = 0x02;
        public const int Medium = 0x03;
        public const int Fast = 0x04;
        public const int Back = 0x05;

        public static int TrainSpeed(int iSpeed, int iMax)
        {
            var fSpeed = ((float)iSpeed) / iMax;

            if (iSpeed < 0)
            {
                return Back;
            }
            else if (iSpeed == 0)
            {
                return Neutral;
            }
            else if (fSpeed < 0.33)
            {
                return Slow;
            }
            else if (fSpeed < 0.66)
            {
                return Medium;
            }
            else
            {
                return Fast;
            }
        }
    }
}
