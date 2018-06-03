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

using GoldSource.Server.Game.Game.Entities;
using System;

namespace GoldSource.Server.Game.Navigation
{
    [Serializable]
    public class CLink
    {
        public int m_iSrcNode; //the node that 'owns' this link ( keeps us from having to make reverse lookups )
        public int m_iDestNode;

        [NonSerialized]
        private EHandle<BaseEntity> _linkEntity;

        //TODO: verify that this never gets serialized
        /// <summary>
        /// the entity that blocks this connection (doors, etc)
        /// </summary>
        public BaseEntity LinkEntity
        {
            get => _linkEntity.Entity;
            set => _linkEntity.Set(value);
        }

        public string m_szLinkEntModelname; //the unique name of the brush model that blocks the connection (this is kept for save/restore)
        public LinkFlags m_afLinkInfo; //information about this link
        public float m_flWeight;
    }
}
