using UnityEngine;

[RequireComponent(typeof(SpriteAnimator), typeof(ZombieMover), typeof(ZombieAttack))]
public class ZombieAnimation : MonoBehaviour
{
    private SpriteAnimator spriteAnimator;
    private ZombieMover zombieMover;
    private ZombieAttack zombieAttack;

    private Vector3 previousPosition;
    private bool wasAttacking = false;

    private void Awake()
    {
        spriteAnimator = GetComponent<SpriteAnimator>();
        zombieMover = GetComponent<ZombieMover>();
        zombieAttack = GetComponent<ZombieAttack>();
        previousPosition = transform.position;
    }

    private void Update()
    {
        // Check for movement based on transform position change
        Vector3 currentPosition = transform.position;
        bool isMoving = (currentPosition - previousPosition).sqrMagnitude > 0.0001f;
        previousPosition = currentPosition;

        spriteAnimator.SetBool("IsWalking", isMoving);

        // Check for attack trigger
        bool isAttacking = zombieAttack.IsAttacking;
        if (!wasAttacking && isAttacking)
        {
            spriteAnimator.SetTrigger("Attack");
        }
        wasAttacking = isAttacking;
    }
}