using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UltimateSpawner;
using PathologicalGames;

public class SpawnFromPool : MonoBehaviour
{
    SpawnPool enemies;
    string associatedPool;

    void Awake()
    {
        // Hook up the handler
        UltimateSpawning.OnUltimateSpawnerInstantiate = HandleSpawnerInstanitate;
    }

    void Start()
    {
        associatedPool = gameObject.GetComponent<SpawnPool>().poolName;
        enemies = PoolManager.Pools[associatedPool];
    }
    Object HandleSpawnerInstanitate(Object prefab, Vector3 position, Quaternion rotation)
    {
        return enemies.Spawn(prefab.name, position, rotation).gameObject;
    }

}