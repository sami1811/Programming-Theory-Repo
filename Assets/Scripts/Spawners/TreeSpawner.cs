using System;
using UnityEngine;

public class TreeSpawner : GlobalSpawner
{
    [Header("Tree Spawn Settings")]
    [SerializeField] private float respawnDelay = 0f;
    [SerializeField] private int maxAttempts = 0;

    private void LateUpdate()
    {
        StartRespawn(respawnDelay, maxAttempts);
    }
}
