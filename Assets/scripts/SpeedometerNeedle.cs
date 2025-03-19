using UnityEngine;
using UnityEngine.UI;

public class SpeedometerNeedle : MonoBehaviour
{
    public RectTransform needleTransform; // Assign the needle UI Image
    public CarController carController;   // Reference to your car script
    public float minRotation = -90f;      // Min rotation (needle at 0 speed)
    public float maxRotation = 90f;       // Max rotation (needle at max speed)
    public float maxSpeed = 50f;         // Set max speed of the car (adjust as needed)

    private float currentRotation;

    void Update()
    {
        if (carController == null || needleTransform == null) return;

        // Get current speed from car script
        float speed = carController.GetSpeed(); // Ensure this method exists in your car script

        // Convert speed to rotation angle
        float targetRotation = Mathf.Lerp(minRotation, maxRotation, speed / maxSpeed);

        // Smoothly rotate the needle
        currentRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * 5);
        needleTransform.rotation = Quaternion.Euler(0, 0, currentRotation);
    }
}
