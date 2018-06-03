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

namespace Server.GameRules
{
    public static class GameRulesFactory
    {
        public static IGameRules Create()
        {
            return new HalfLifeRules();
            //TODO
            /*
            SERVER_COMMAND("exec game.cfg\n");
            SERVER_EXECUTE();

            if (!gpGlobals->deathmatch)
            {
                // generic half-life
                g_teamplay = 0;
                return new CHalfLifeRules;
            }
            else
            {
                if (teamplay.value > 0)
                {
                    // teamplay

                    g_teamplay = 1;
                    return new CHalfLifeTeamplay;
                }
                if ((int)gpGlobals->deathmatch == 1)
                {
                    // vanilla deathmatch
                    g_teamplay = 0;
                    return new CHalfLifeMultiplay;
                }
                else
                {
                    // vanilla deathmatch??
                    g_teamplay = 0;
                    return new CHalfLifeMultiplay;
                }
            }
            */
        }
    }
}
