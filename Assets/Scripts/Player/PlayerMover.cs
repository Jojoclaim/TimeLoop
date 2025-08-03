using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteAnimator))]
public class PlayerMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 20f; // New: acceleration for smoother starts/stops
    [SerializeField] private float deceleration = 15f; // New: deceleration when stopping
    [SerializeField] private float cellsPerUnit = 16f;

    [Header("Grid Snapping")]
    [SerializeField] private float snapSpeed = 15f;
    [SerializeField] private float snapDelay = 0.1f; // Slightly increased for better feel
    [SerializeField] private float snapDistanceThreshold = 0.4f; // In cells
    [SerializeField] private AnimationCurve snapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // New: customizable easing

    [Header("Sprite")]
    [SerializeField] private bool flipSprite = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private SpriteAnimator spriteAnimator;

    // Movement state
    private Vector2 inputVector;
    private Vector2 currentVelocity;
    private Vector2 velocityRef; // For SmoothDamp
    private float cellSize;
    private float snapDistanceSquared;
    private float timeSinceLastInput;
    private bool isSnapping;
    private Coroutine snapCoroutine;
    private Vector2 lastNonZeroInput; // Track last movement direction

    // Constants
    private const float MOVEMENT_EPSILON = 0.01f;
    private const float DIAGONAL_NORMALIZATION = 0.7071f; // 1/sqrt(2)
    private const float MIN_SNAP_DISTANCE_SQ = 0.0001f;

    private void Awake()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteAnimator = GetComponent<SpriteAnimator>();

        // Configure Rigidbody2D
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection

        // Pre-calculate values
        cellSize = 1f / cellsPerUnit;
        snapDistanceSquared = (snapDistanceThreshold * cellSize) * (snapDistanceThreshold * cellSize);
    }

    private void Update()
    {
        HandleInput();
        UpdateSpriteDirection();
    }

    private void FixedUpdate()
    {
        if (!isSnapping)
        {
            ApplyMovement();
        }

        // Update animator state
        if (spriteAnimator != null)
        {
            spriteAnimator.SetBool("IsMoving", currentVelocity.sqrMagnitude > MOVEMENT_EPSILON);
        }
    }

    private void HandleInput()
    {
        // Get raw input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Create input vector
        inputVector = new Vector2(horizontal, vertical);

        // Handle input
        if (inputVector.sqrMagnitude > MOVEMENT_EPSILON)
        {
            // Normalize diagonal movement
            if (Mathf.Abs(horizontal) > 0 && Mathf.Abs(vertical) > 0)
            {
                inputVector *= DIAGONAL_NORMALIZATION;
            }

            lastNonZeroInput = inputVector;
            timeSinceLastInput = 0f;

            // Cancel any ongoing snap
            CancelSnapping();
        }
        else
        {
            inputVector = Vector2.zero;
            timeSinceLastInput += Time.deltaTime;

            // Start snap check after delay
            if (timeSinceLastInput >= snapDelay && !isSnapping &&
                currentVelocity.sqrMagnitude < MOVEMENT_EPSILON) // Only snap when truly stopped
            {
                TryStartSnapping();
            }
        }
    }

    private void ApplyMovement()
    {
        // Calculate target velocity
        Vector2 targetVelocity = inputVector * moveSpeed;

        // Use different smoothing based on whether we're accelerating or decelerating
        float smoothTime = inputVector.sqrMagnitude > 0 ? 1f / acceleration : 1f / deceleration;

        // Smooth velocity change
        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref velocityRef,
            smoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        // Apply velocity
        rb.linearVelocity = currentVelocity;
    }

    private void UpdateSpriteDirection()
    {
        if (flipSprite && Mathf.Abs(inputVector.x) > MOVEMENT_EPSILON)
        {
            spriteRenderer.flipX = inputVector.x < 0;
        }
    }

    private void CancelSnapping()
    {
        if (isSnapping)
        {
            if (snapCoroutine != null)
            {
                StopCoroutine(snapCoroutine);
                snapCoroutine = null;
            }
            isSnapping = false;
        }
    }

    private void TryStartSnapping()
    {
        Vector2 currentPos = rb.position;
        Vector2 nearestGridPos = GetNearestGridPosition(currentPos);

        float distanceSquared = (currentPos - nearestGridPos).sqrMagnitude;

        if (distanceSquared > MIN_SNAP_DISTANCE_SQ && distanceSquared <= snapDistanceSquared)
        {
            snapCoroutine = StartCoroutine(SnapToGridCoroutine(nearestGridPos));
        }
    }

    private Vector2 GetNearestGridPosition(Vector2 position)
    {
        float nearestX = Mathf.Round(position.x / cellSize) * cellSize;
        float nearestY = Mathf.Round(position.y / cellSize) * cellSize;
        return new Vector2(nearestX, nearestY);
    }

    private IEnumerator SnapToGridCoroutine(Vector2 targetPosition)
    {
        isSnapping = true;

        Vector2 startPosition = rb.position;
        float distance = Vector2.Distance(startPosition, targetPosition);
        float duration = distance / (snapSpeed * cellSize);
        float elapsed = 0f;

        // Clear any residual velocity
        rb.linearVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;

        while (elapsed < duration)
        {
            // Check for input interruption
            if (inputVector.sqrMagnitude > MOVEMENT_EPSILON)
            {
                isSnapping = false;
                snapCoroutine = null;
                yield break;
            }

            elapsed += Time.fixedDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);

            // Use animation curve for smooth easing
            float curveValue = snapCurve.Evaluate(normalizedTime);

            Vector2 newPosition = Vector2.Lerp(startPosition, targetPosition, curveValue);
            rb.MovePosition(newPosition);

            yield return new WaitForFixedUpdate();
        }

        // Ensure final position is exact
        rb.position = targetPosition;
        rb.linearVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;
        velocityRef = Vector2.zero;

        isSnapping = false;
        snapCoroutine = null;
    }

    private void OnValidate()
    {
        // Clamp values to reasonable ranges
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        acceleration = Mathf.Max(0.1f, acceleration);
        deceleration = Mathf.Max(0.1f, deceleration);
        cellsPerUnit = Mathf.Max(1f, cellsPerUnit);
        snapSpeed = Mathf.Max(0.1f, snapSpeed);
        snapDelay = Mathf.Max(0f, snapDelay);
        snapDistanceThreshold = Mathf.Max(0.1f, snapDistanceThreshold);

        // Update calculated values
        if (Application.isPlaying)
        {
            cellSize = 1f / cellsPerUnit;
            snapDistanceSquared = (snapDistanceThreshold * cellSize) * (snapDistanceThreshold * cellSize);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        Vector2 playerPos = transform.position;
        float cs = cellSize;

        // Draw nearby grid
        Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
        int gridRange = 4;

        for (int x = -gridRange; x <= gridRange; x++)
        {
            for (int y = -gridRange; y <= gridRange; y++)
            {
                Vector2 gridPos = new Vector2(
                    Mathf.Round(playerPos.x / cs + x) * cs,
                    Mathf.Round(playerPos.y / cs + y) * cs
                );

                Gizmos.DrawWireCube(gridPos, Vector3.one * cs * 0.9f);
            }
        }

        // Highlight current grid cell
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Vector2 currentGridPos = GetNearestGridPosition(playerPos);
        Gizmos.DrawCube(currentGridPos, Vector3.one * cs);

        // Show snap target when snapping
        if (isSnapping && snapCoroutine != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(currentGridPos, Vector3.one * cs);
            Gizmos.DrawLine(playerPos, currentGridPos);
        }

        // Show snap threshold radius
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.DrawWireDisc(playerPos, Vector3.forward, snapDistanceThreshold * cs);

        // Show velocity vector
        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(playerPos, currentVelocity.normalized * 0.5f);
        }
    }
#endif
}