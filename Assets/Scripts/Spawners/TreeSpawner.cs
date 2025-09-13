using System;
using UnityEngine;

public class TreeSpawner : GlobalSpawner
{
    private void Start()
    {
        if(spawnOnStart)
        {
            if (objectPrefab != null)
            {
                SpawnWithoutOverlap(objectPrefab);
            }
            else
            {
                Debug.Log("No tree prefab is found. Please assign one.");
            }
        }
    }
}
