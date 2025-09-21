using UnityEngine;

public interface IPoolable
{
    GameObject CreateNewPoolObject(); 
    GameObject GetObject();
    void ReturnObject(GameObject objectToReturn);
    void OnPoolRetrieve(GameObject objectToRetrieve);
    void OnPoolReturn(GameObject objectToRetrieve);
}
