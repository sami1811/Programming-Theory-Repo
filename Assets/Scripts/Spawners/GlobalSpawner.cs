using System.Collections.Generic;
using UnityEngine;

public class GlobalSpawner : MonoBehaviour
{
    [Header("Spawn Area Settings")]
    public List<GameObject> spawnAreas = new List<GameObject>();

    [Header("Spawner Settings")]
    public GameObject objectPrefab;
    public int numberOfObjects;
    public float minDistance = 5f;
    public float yOffSet = 0.99f;
    public bool spawnOnStart = false;

    [Header("Spawn Loop Settings")]
    public int maxSpawnAttempts = 10;

    protected virtual void SpawnWithoutOverlap(GameObject objectToSpawn)
    {
        objectToSpawn = objectPrefab;
        Vector3[] spawnedPositions = new Vector3[numberOfObjects];
        int spawnedCounts = 0;
        int attempts = 0;
        int maxAttempts = numberOfObjects * maxSpawnAttempts;

        while(spawnedCounts < numberOfObjects && attempts < maxAttempts)
        {
            Vector3 randomPos = GetRandomPosition();

            if(IsValidPosition(randomPos, spawnedPositions, spawnedCounts))
            {
                Instantiate(objectToSpawn, randomPos, objectToSpawn.transform.rotation);
                spawnedPositions[spawnedCounts] = randomPos;
                spawnedCounts++;
            }

            attempts++;
        }

        if (spawnedCounts < numberOfObjects)
            Debug.Log("Could only spawned " + spawnedCounts + " out of " + numberOfObjects + " " + objectToSpawn.name + " due to space constraints.");
        else
            Debug.Log("Sucessfully spawned " + numberOfObjects + " " + objectToSpawn.name + " without causing any overlaps.");
    }

    private Vector3 GetRandomPosition()
    {
        Renderer planeRenderer = spawnAreas[Random.Range(0, spawnAreas.Count)].GetComponent<Renderer>();

        if (planeRenderer == null)
        {
            Debug.Log("No plane renderer found.");
            return Vector3.zero;
        }
          
        Bounds bounds = planeRenderer.bounds;

        Vector3 randomPos = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.max.y + yOffSet,
            Random.Range(bounds.min.z, bounds.max.z));

        return randomPos;
    }

    private bool IsValidPosition(Vector3 position, Vector3[] existingPos, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float distance = Vector3.Distance(position, existingPos[i]);

            if(distance < minDistance)
                return false;
        }

        return true;
    }
}
