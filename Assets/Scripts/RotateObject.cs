using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Rotation Settings")]
    public RotationAxis Axis = RotationAxis.Y;
    public float RotationSpeed = 90f;

    private void Update()
    {
        Vector3 rotationAxis = Axis switch
        {
            RotationAxis.X => Vector3.right,
            RotationAxis.Y => Vector3.up,
            RotationAxis.Z => Vector3.forward,
            _ => Vector3.up
        };

        transform.Rotate(rotationAxis, RotationSpeed * Time.deltaTime, Space.Self);
    }
}