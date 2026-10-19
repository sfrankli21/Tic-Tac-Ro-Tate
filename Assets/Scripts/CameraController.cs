using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private enum OrbitState
    {
        Manual,
        Focusing,
        Returning
    }

    [Header("Orbit Settings")]
    public Transform Target;
    public float Distance = 5f;
    public float OrbitSpeed = 0.2f;

    [Header("Rotation Selection Camera Focus")]
    public float FocusDuration = 0.5f;
    public float ReturnDuration = 0.5f;

    private float yaw;
    private float pitch;
    private OrbitState orbitState;
    private Quaternion previousOrbitRotation;
    private bool focusSessionActive;
    private Quaternion transitionStartRotation;
    private Quaternion transitionEndRotation;
    private float transitionElapsed;
    private float transitionDuration;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x > 180f ? angles.x - 360f : angles.x;

        UpdateCameraPosition(Quaternion.Euler(pitch, yaw, 0f));
    }

    private void LateUpdate()
    {
        if (orbitState == OrbitState.Manual)
        {
            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();

                yaw += mouseDelta.x * OrbitSpeed;
                pitch -= mouseDelta.y * OrbitSpeed;
                pitch = Mathf.Clamp(pitch, -89f, 89f);
            }

            UpdateCameraPosition(Quaternion.Euler(pitch, yaw, 0f));
            return;
        }

        transitionElapsed += Time.deltaTime;
        float t = transitionDuration <= 0f ? 1f : Mathf.Clamp01(transitionElapsed / transitionDuration);
        t = t * t * (3f - 2f * t);

        UpdateCameraPosition(Quaternion.Slerp(transitionStartRotation, transitionEndRotation, t));

        if (t >= 1f)
        {
            orbitState = OrbitState.Manual;
            SyncOrbitAngles(transitionEndRotation);
        }
    }

    public void FocusOnFace(Vector3 outwardNormal, Vector3 faceUp)
    {
        if (!focusSessionActive)
        {
            previousOrbitRotation = transform.rotation;
            focusSessionActive = true;
        }

        Quaternion focusedRotation = Quaternion.LookRotation(-outwardNormal.normalized, faceUp.normalized);
        StartTransition(focusedRotation, FocusDuration, OrbitState.Focusing);
    }

    public void ReleaseFaceFocus()
    {
        if (!focusSessionActive)
            return;

        focusSessionActive = false;
        StartTransition(previousOrbitRotation, ReturnDuration, OrbitState.Returning);
    }

    private void StartTransition(Quaternion targetRotation, float duration, OrbitState state)
    {
        transitionStartRotation = transform.rotation;
        transitionEndRotation = targetRotation;
        transitionElapsed = 0f;
        transitionDuration = Mathf.Max(0f, duration);
        orbitState = state;
    }

    private void SyncOrbitAngles(Quaternion rotation)
    {
        Vector3 right = rotation * Vector3.right;
        Vector3 forward = rotation * Vector3.forward;
        yaw = Mathf.Atan2(-right.z, right.x) * Mathf.Rad2Deg;
        pitch = Mathf.Asin(Mathf.Clamp(-forward.y, -1f, 1f)) * Mathf.Rad2Deg;
    }

    private void UpdateCameraPosition(Quaternion rotation)
    {
        transform.position = Target.position - rotation * Vector3.forward * Distance;
        transform.rotation = rotation;
    }
}
