using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform Target;
    public float Distance = 5f;
    public float OrbitSpeed = 0.2f;

    private float yaw;
    private float pitch;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x > 180f ? angles.x - 360f : angles.x;

        UpdateCameraPosition();
    }

    private void LateUpdate()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * OrbitSpeed;
            pitch -= mouseDelta.y * OrbitSpeed;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
        }

        UpdateCameraPosition();
    }

    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = Target.position - rotation * Vector3.forward * Distance;
        transform.rotation = rotation;
    }
}