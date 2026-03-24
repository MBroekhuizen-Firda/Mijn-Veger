using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum CameraMode { TopDown, Follow }

    [Header("References")]
    public Transform Target;
    public Camera MainCamera;

    [Header("Top-Down Settings")]
    public float TopDownHeight = 20f;
    public float TopDownAngle = 90f; // straight down

    [Header("Follow Settings")]
    public float FollowDistance = 5f;
    public float FollowHeight = 3f;
    public float FollowAngle = 30f;

    [Header("Transition")]
    public float BlendSpeed = 3f;

    private CameraMode currentMode = CameraMode.TopDown;
    private Vector3 targetCameraPosition;
    private Quaternion targetCameraRotation;
    private Vector3 gridCenter;

    void Start()
    {
        if (MainCamera == null)
            MainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (MainCamera == null) return;

        UpdateTargetTransform();

        // Smoothly blend to target position/rotation
        MainCamera.transform.position = Vector3.Lerp(
            MainCamera.transform.position, targetCameraPosition, Time.deltaTime * BlendSpeed);
        MainCamera.transform.rotation = Quaternion.Slerp(
            MainCamera.transform.rotation, targetCameraRotation, Time.deltaTime * BlendSpeed);
    }

    void UpdateTargetTransform()
    {
        switch (currentMode)
        {
            case CameraMode.TopDown:
                targetCameraPosition = gridCenter + Vector3.up * TopDownHeight;
                targetCameraRotation = Quaternion.Euler(TopDownAngle, 0f, 0f);
                break;

            case CameraMode.Follow:
                if (Target == null) return;
                Vector3 behindOffset = -Target.forward * FollowDistance + Vector3.up * FollowHeight;
                targetCameraPosition = Target.position + behindOffset;
                targetCameraRotation = Quaternion.LookRotation(
                    Target.position + Vector3.up * 1f - targetCameraPosition);
                break;
        }
    }

    public void SetTopDownView(int gridWidth, int gridHeight)
    {
        gridCenter = new Vector3((gridWidth - 1f) / 2f, 0f, (gridHeight - 1f) / 2f);

        // Adjust height based on grid size
        float maxDimension = Mathf.Max(gridWidth, gridHeight);
        TopDownHeight = maxDimension * 1.15f;

        SetMode(CameraMode.TopDown);
    }

    public void SetMode(CameraMode mode)
    {
        currentMode = mode;
    }

    public void OnCharacterStartedMoving()
    {
        SetMode(CameraMode.Follow);
    }

    public void OnCharacterArrived()
    {
        SetMode(CameraMode.TopDown);
    }
}
