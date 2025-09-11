using System.Collections.Generic;
using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [SerializeField] List<GameObject> plane = new List<GameObject>();

    [SerializeField] private GameObject treePrefab;
    
    [SerializeField] private int numberOfObjects;
    [SerializeField] private float minDistance = 5f;

    [Header("Optional Settings")]
    [SerializeField] private float yOffSet = 0.99f;
    [SerializeField] private bool spawnOnStart = false;
    [SerializeField] private int maxSpawnAttempts = 10;

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnWithoutOverlap();
        }
    }

    public void SpawnWithoutOverlap()
    {
        Vector3[] spawnedPositions = new Vector3[numberOfObjects];
        int spawnedCounts = 0;
        int attempts = 0;
        int maxAttempts = numberOfObjects * maxSpawnAttempts;

        while(spawnedCounts < numberOfObjects && attempts < maxAttempts)
        {
            Vector3 randomPos = GetRandomPosition();

            if(IsValidPosition(randomPos, spawnedPositions, spawnedCounts))
            {
                Instantiate(treePrefab, randomPos, treePrefab.transform.rotation);
                spawnedPositions[spawnedCounts] = randomPos;
                spawnedCounts++;
            }

            attempts++;
        }

        if (spawnedCounts < numberOfObjects)
            Debug.Log("Could only spawned " + spawnedCounts + " out of " + numberOfObjects + " trees due to space constraints.");
        else
            Debug.Log("Sucessfully spawned " + numberOfObjects + " trees without causing any overlaps.");
    }

    private Vector3 GetRandomPosition()
    {
        Renderer planeRenderer = plane[Random.Range(0, plane.Count)].GetComponent<Renderer>();

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
