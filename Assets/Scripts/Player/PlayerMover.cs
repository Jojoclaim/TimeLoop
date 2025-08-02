/*using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float movementSmoothing = 0.05f; // Lower = smoother
    [SerializeField] private float cellsPerUnit = 16f;

    [Header("Grid Snapping")]
    [SerializeField] private float snapSpeed = 20f;
    [SerializeField] private float snapDelay = 0.05f;
    [SerializeField] private float snapDistanceThreshold = 0.3f; // In cells

    [Header("Sprite")]
    [SerializeField] private bool flipSprite = true;

    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // Cached values for performance
    private Vector2 movement;
    private Vector2 smoothedMovement;
    private Vector2 velocity;
    private float cellSize;
    private float snapDistanceSquared;
    private float timeSinceLastInput;
    private bool isSnapping;
    private Coroutine snapCoroutine;

    // Constants for optimization
    private const float INPUT_THRESHOLD = 0.01f;
    private const float INPUT_THRESHOLD_SQUARED = INPUT_THRESHOLD * INPUT_THRESHOLD;
    private const float DIAGONAL_FACTOR = 0.7071f; // 1/sqrt(2) for proper diagonal speed

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody2D>();
        spriteRenderer ??= GetComponent<SpriteRenderer>();

        // Configure Rigidbody2D
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Pre-calculate constants
        cellSize = 1f / cellsPerUnit;
        snapDistanceSquared = (snapDistanceThreshold * cellSize) * (snapDistanceThreshold * cellSize);
    }

    private void Update()
    {
        // Get raw input
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Quick check for any input
        if (h != 0f || v != 0f)
        {
            movement.x = h;
            movement.y = v;

            // Proper diagonal normalization
            if (h != 0f && v != 0f)
            {
                movement *= DIAGONAL_FACTOR;
            }

            timeSinceLastInput = 0f;

            // Cancel snapping
            if (isSnapping)
            {
                if (snapCoroutine != null)
                {
                    StopCoroutine(snapCoroutine);
                    snapCoroutine = null;
                }
                isSnapping = false;
            }

            // Sprite flipping
            if (flipSprite && h != 0f)
            {
                spriteRenderer.flipX = h < 0f;
            }
        }
        else
        {
            movement = Vector2.zero;
            timeSinceLastInput += Time.deltaTime;

            // Check for snapping
            if (timeSinceLastInput >= snapDelay && !isSnapping && snapCoroutine == null)
            {
                CheckAndStartSnap();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isSnapping)
        {
            // Smooth movement interpolation
            smoothedMovement = Vector2.Lerp(smoothedMovement, movement, 1f - movementSmoothing);
            velocity = smoothedMovement * moveSpeed;
            rb.linearVelocity = velocity;
        }
    }

    private void CheckAndStartSnap()
    {
        // Quick distance check using squared distance (no sqrt)
        Vector2 pos = rb.position;
        float nearestX = Mathf.Round(pos.x / cellSize) * cellSize;
        float nearestY = Mathf.Round(pos.y / cellSize) * cellSize;

        float dx = pos.x - nearestX;
        float dy = pos.y - nearestY;
        float distSq = dx * dx + dy * dy;

        if (distSq <= snapDistanceSquared && distSq > 0.0001f)
        {
            snapCoroutine = StartCoroutine(SnapToGrid(new Vector2(nearestX, nearestY)));
        }
    }

    private IEnumerator SnapToGrid(Vector2 targetPos)
    {
        isSnapping = true;

        Vector2 startPos = rb.position;
        Vector2 difference = targetPos - startPos;
        float distance = difference.magnitude;

        // Pre-calculate animation duration based on distance
        float duration = distance / (snapSpeed * cellSize);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Check for input interruption
            if (movement.sqrMagnitude > INPUT_THRESHOLD_SQUARED)
            {
                isSnapping = false;
                snapCoroutine = null;
                yield break;
            }

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth cubic easing
            t = t * t * (3f - 2f * t);

            rb.MovePosition(startPos + difference * t);
            rb.linearVelocity = Vector2.zero;

            yield return new WaitForFixedUpdate();
        }

        // Final position
        rb.position = targetPos;
        rb.linearVelocity = Vector2.zero;
        smoothedMovement = Vector2.zero;
        isSnapping = false;
        snapCoroutine = null;
    }

    private void OnValidate()
    {
        rb = rb != null ? rb : GetComponent<Rigidbody2D>();
        spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        cellsPerUnit = Mathf.Max(1f, cellsPerUnit);
        movementSmoothing = Mathf.Clamp01(movementSmoothing);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Only draw when selected to save performance
        Vector2 playerPos = transform.position;

        // Draw minimal grid visualization
        Gizmos.color = new Color(1f, 1f, 1f, 0.15f);

        float cs = Application.isPlaying ? cellSize : (1f / cellsPerUnit);
        int range = 3;

        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                if (x == 0 && y == 0) continue;

                Vector2 gridPos = new Vector2(
                    Mathf.Round(playerPos.x / cs + x) * cs,
                    Mathf.Round(playerPos.y / cs + y) * cs
                );

                Gizmos.DrawWireCube(gridPos, Vector3.one * cs * 0.8f);
            }
        }

        // Show current grid position
        if (isSnapping)
        {
            Gizmos.color = Color.yellow;
            Vector2 nearest = new Vector2(
                Mathf.Round(playerPos.x / cs) * cs,
                Mathf.Round(playerPos.y / cs) * cs
            );
            Gizmos.DrawWireCube(nearest, Vector3.one * cs);
        }
    }
#endif
}*/

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
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