using UnityEngine;

[RequireComponent(typeof(SpriteAnimator), typeof(ZombieMover), typeof(ZombieAttack))]
public class ZombieAnimation : MonoBehaviour
{
    private SpriteAnimator spriteAnimator;
    private ZombieMover zombieMover;
    private ZombieAttack zombieAttack;

    private void Awake()
    {
        spriteAnimator = GetComponent<SpriteAnimator>();
        zombieMover = GetComponent<ZombieMover>();
        zombieAttack = GetComponent<ZombieAttack>();
    }

    private void Update()
    {
        bool isMoving = zombieMover.Velocity.sqrMagnitude > 0.01f;
        bool isAttacking = zombieAttack.IsAttacking;

        spriteAnimator.SetBool("isMove", isMoving);
        spriteAnimator.SetBool("isAttack", isAttacking);
    }
}