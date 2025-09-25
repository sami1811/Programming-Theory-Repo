using UnityEngine;

public interface IPoolable
{
    GameObject CreateNewPoolObject(); 
    GameObject GetObject();
    void ReturnToPool(GameObject objectToReturn);
    void OnPoolRetrieve(GameObject objectToRetrieve);
    void OnPoolReturn(GameObject objectToRetrieve);
}
