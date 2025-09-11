using UnityEngine;

public class SunRotation : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 1f;

    // Update is called once per frame
    void Update()
    {
        BasicSunRotation();
    }

    private void BasicSunRotation()
    {
        float rotationAngleX = 0f;

        rotationAngleX = rotationSpeed * Time.deltaTime;

        transform.Rotate(rotationAngleX, 0, 0);
    }
}
