using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameCore.High;
using Mirror;

namespace GameCore
{
    public interface IEntity : IDeath, IHealth, ITakeDamage
    {
        public static T GetEntityByNetId<T>(uint netIdToFind) where T : Component, IEntity
        {
            NetworkIdentity[] nis = GameObject.FindObjectsOfType<NetworkIdentity>();

            for (int i = 0; i < nis.Length; i++)
                if (nis[i].netId == netIdToFind)
                    return nis[i].GetComponent<T>();

            return null;
        }
    }
}
