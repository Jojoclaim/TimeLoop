using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class PixelPerfectCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 0, -10);

    [Header("Pixel Perfect Settings")]
    [SerializeField] private int pixelsPerUnit = 16;
    [SerializeField] private int targetVerticalResolution = 180;

    [Header("Follow Settings")]
    [SerializeField] private FollowMode followMode = FollowMode.Smooth;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 2f;

    [Header("Dead Zone")]
    [SerializeField] private bool useDeadZone = true;
    [SerializeField] private Vector2 deadZoneSize = new Vector2(2f, 1f);

    [Header("Camera Boundaries")]
    [SerializeField] private bool useBoundaries = false;
    [SerializeField] private Bounds cameraBounds;

    [Header("Screen Shake")]
    [SerializeField] private float shakeDecay = 0.5f;

    [Header("Advanced Settings")]
    [SerializeField] private bool roundToPixel = true;
    [SerializeField] private bool useSubPixelMovement = true;
    [SerializeField] private float subPixelThreshold = 0.1f;

    public enum FollowMode
    {
        Instant,
        Smooth,
        SmoothWithLookAhead,
        CinematicDamping
    }

    private Camera cam;
    private float pixelScale;
    private Vector3 velocity = Vector3.zero;
    private Vector3 desiredPosition;
    private Vector3 lookAheadPos;
    private Vector3 lastTargetPosition;
    private float shakeIntensity = 0f;
    private Vector3 shakeOffset;
    private Vector2 subPixelOffset;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;

        CalculatePixelScale();

        if (target != null)
        {
            desiredPosition = target.position + targetOffset;
            transform.position = RoundToPixel(desiredPosition);
            lastTargetPosition = target.position;
        }
    }

    private void Start()
    {
        StartCoroutine(LateFixedUpdate());
    }

    private void CalculatePixelScale()
    {
        float targetAspectRatio = (float)Screen.width / Screen.height;
        float targetOrthographicSize = targetVerticalResolution / (pixelsPerUnit * 2f);
        cam.orthographicSize = targetOrthographicSize;
        pixelScale = 1f / pixelsPerUnit;
    }

    private IEnumerator LateFixedUpdate()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();
            UpdateCameraPosition();
        }
    }

    private void UpdateCameraPosition()
    {
        if (target == null) return;

        // Calculate base desired position
        Vector3 targetPos = target.position;
        Vector3 targetDelta = targetPos - lastTargetPosition;

        // Apply look-ahead if enabled
        if (followMode == FollowMode.SmoothWithLookAhead)
        {
            lookAheadPos = Vector3.Lerp(lookAheadPos, targetDelta.normalized * lookAheadDistance,
                                       Time.deltaTime * lookAheadSpeed);
            targetPos += lookAheadPos;
        }

        // Calculate desired position with offset
        desiredPosition = targetPos + targetOffset;

        // Apply dead zone
        if (useDeadZone && followMode != FollowMode.Instant)
        {
            Vector3 currentPos = transform.position;
            float deltaX = desiredPosition.x - currentPos.x;
            float deltaY = desiredPosition.y - currentPos.y;

            if (Mathf.Abs(deltaX) < deadZoneSize.x)
                desiredPosition.x = currentPos.x;
            if (Mathf.Abs(deltaY) < deadZoneSize.y)
                desiredPosition.y = currentPos.y;
        }

        // Apply follow mode
        Vector3 newPosition = desiredPosition;
        switch (followMode)
        {
            case FollowMode.Instant:
                newPosition = desiredPosition;
                break;

            case FollowMode.Smooth:
            case FollowMode.SmoothWithLookAhead:
                newPosition = Vector3.Lerp(transform.position, desiredPosition,
                                         Time.deltaTime * smoothSpeed);
                break;

            case FollowMode.CinematicDamping:
                newPosition = Vector3.SmoothDamp(transform.position, desiredPosition,
                                               ref velocity, 1f / smoothSpeed);
                break;
        }

        // Apply screen shake
        if (shakeIntensity > 0)
        {
            shakeOffset = Random.insideUnitCircle * shakeIntensity;
            shakeIntensity -= shakeDecay * Time.deltaTime;
            newPosition += (Vector3)shakeOffset;
        }

        // Apply boundaries
        if (useBoundaries)
        {
            newPosition.x = Mathf.Clamp(newPosition.x, cameraBounds.min.x, cameraBounds.max.x);
            newPosition.y = Mathf.Clamp(newPosition.y, cameraBounds.min.y, cameraBounds.max.y);
        }

        // Handle sub-pixel movement for extra smoothness
        if (useSubPixelMovement && roundToPixel)
        {
            Vector3 pixelPerfectPos = RoundToPixel(newPosition);
            Vector3 subPixelDelta = newPosition - pixelPerfectPos;

            // Accumulate sub-pixel movement
            subPixelOffset += new Vector2(subPixelDelta.x, subPixelDelta.y);

            // Apply sub-pixel offset when it exceeds threshold
            if (Mathf.Abs(subPixelOffset.x) >= subPixelThreshold)
            {
                pixelPerfectPos.x += Mathf.Sign(subPixelOffset.x) * pixelScale;
                subPixelOffset.x = 0;
            }
            if (Mathf.Abs(subPixelOffset.y) >= subPixelThreshold)
            {
                pixelPerfectPos.y += Mathf.Sign(subPixelOffset.y) * pixelScale;
                subPixelOffset.y = 0;
            }

            transform.position = pixelPerfectPos;
        }
        else
        {
            transform.position = roundToPixel ? RoundToPixel(newPosition) : newPosition;
        }

        lastTargetPosition = target.position;
    }

    private Vector3 RoundToPixel(Vector3 position)
    {
        float x = Mathf.Round(position.x * pixelsPerUnit) / pixelsPerUnit;
        float y = Mathf.Round(position.y * pixelsPerUnit) / pixelsPerUnit;
        return new Vector3(x, y, position.z);
    }

    public void ScreenShake(float intensity)
    {
        shakeIntensity = intensity;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            lastTargetPosition = target.position;
        }
    }

    public void SetFollowMode(FollowMode mode)
    {
        followMode = mode;
    }

    public void SnapToTarget()
    {
        if (target != null)
        {
            desiredPosition = target.position + targetOffset;
            transform.position = roundToPixel ? RoundToPixel(desiredPosition) : desiredPosition;
            lookAheadPos = Vector3.zero;
            velocity = Vector3.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw dead zone
        if (useDeadZone)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Vector3 deadZoneCenter = transform.position;
            deadZoneCenter.z = 0;
            Gizmos.DrawCube(deadZoneCenter, new Vector3(deadZoneSize.x * 2, deadZoneSize.y * 2, 0.1f));
        }

        // Draw boundaries
        if (useBoundaries)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);
        }
    }
}