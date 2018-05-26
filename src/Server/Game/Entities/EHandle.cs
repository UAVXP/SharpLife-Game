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
using System.Collections.Generic;

namespace Server.Game.Entities
{
    /// <summary>
    /// Safe way to point to CBaseEntities who may die between frames
    /// </summary>
    /// <typeparam name="T">Type of the entity to store a reference to</typeparam>
    public struct EHandle<T> : System.IEquatable<EHandle<T>> where T: BaseEntity
    {
        //We could just store the reference directly here, but then the object would be kept alive by handles
        //This way, the reference is stored only in the edict, which can be invalidated on entity destruction
        //Then we can easily check if the object exists without keeping it alive
        //TODO: if the entity list is managed by the game, we can replace this with a packed index + serial number integer
        private Edict _edict;
        private int _serialNumber;

        //Added a check here to see if the object actually exists
        //There are edge cases during map changes where the serial number matches but the object has been destroyed
        public bool Valid => _edict != null
                    && _edict.SerialNumber == _serialNumber
                    && _edict.PrivateData != null;

        public Edict Edict => Valid ? _edict : null;

        public T Entity
        {
            get => Edict?.Entity<T>();
            set => Set(value);
        }

        /// <summary>
        /// Implicitly convert to bool to test if the handle points to a valid entity
        /// </summary>
        /// <param name="handle"></param>
        public static implicit operator bool(EHandle<T> handle) => handle.Valid;

        /// <summary>
        /// Implicitly convert to the entity object
        /// Returns null if the entity does not exist
        /// </summary>
        /// <param name="handle"></param>
        public static implicit operator T(EHandle<T> handle) => handle.Entity;

        public EHandle(T entity = null)
        {
            _edict = null;
            _serialNumber = 0;

            Set(entity);
        }

        /// <summary>
        /// Sets the entity being referenced
        /// </summary>
        /// <param name="entity"></param>
        public void Set(T entity)
        {
            if (entity != null)
            {
                _edict = entity.Edict();

                if (_edict != null)
                {
                    _serialNumber = _edict.SerialNumber;
                }
            }
            else
            {
                _edict = null;
                _serialNumber = 0;
            }
        }

        public override int GetHashCode()
        {
            var hashCode = 1431447612;
            hashCode = (hashCode * -1521134295) + EqualityComparer<Edict>.Default.GetHashCode(_edict);
            return (hashCode * -1521134295) + _serialNumber.GetHashCode();
        }

        public bool Equals(EHandle<T> other)
        {
            return Entity == other.Entity;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EHandle<T> other))
            {
                return false;
            }

            return Equals(other);
        }

        public static bool operator ==(EHandle<T> self, BaseEntity other)
        {
            return self.Entity == other;
        }

        public static bool operator !=(EHandle<T> self, BaseEntity other)
        {
            return !(self == other);
        }

        /// <summary>
        /// Creates a copy of this ehandle type with the given entity as the entity to point at
        /// This avoids the need to specify the type explicitly
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public EHandle<T> CopyWith(T entity)
        {
            return new EHandle<T>(entity);
        }
    }
}
