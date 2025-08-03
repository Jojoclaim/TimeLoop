using UnityEngine;

[RequireComponent(typeof(SpriteAnimator))]
public class PlayerAnimator : MonoBehaviour
{
    private SpriteAnimator spriteAnimator;
    private Rigidbody2D rb;
    private PlayerAttack playerAttack;
    private bool previousCharging;

    private const float MOVEMENT_EPSILON = 0.01f;

    private void Awake()
    {
        spriteAnimator = GetComponent<SpriteAnimator>();
        rb = GetComponent<Rigidbody2D>();
        playerAttack = GetComponent<PlayerAttack>();
    }

    private void Update()
    {
        if (spriteAnimator == null || rb == null || playerAttack == null) return;

        bool isMoving = rb.linearVelocity.sqrMagnitude > MOVEMENT_EPSILON;
        spriteAnimator.SetBool("IsMoving", isMoving);

        bool currentCharging = playerAttack.IsCharging;
        spriteAnimator.SetBool("Preparing", currentCharging);

        if (previousCharging && !currentCharging)
        {
            spriteAnimator.SetTrigger("OnAttack");
        }

        previousCharging = currentCharging;
    }
}