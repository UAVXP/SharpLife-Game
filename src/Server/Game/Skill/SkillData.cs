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
using System;

namespace Server.Game.Skill
{
    public sealed class SkillData
    {
        public SkillLevel SkillLevel { get; set; }

        public float GetSkillCVar(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var completeName = $"{name}{(int)SkillLevel}";

            var value = CVar.GetFloat(completeName);

            if (value <= 0)
            {
                Log.Alert(AlertType.Console, $"GetSkillCVar Got a zero for {completeName}");
            }

            return value;
        }
    }
}
