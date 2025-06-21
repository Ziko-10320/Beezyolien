using System.Collections;
using UnityEngine;

public class BeezyAttack : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    public BeezyMovement playerMovement;

    // Public variables for damage logic
    public float damage = 10f; // Amount of damage dealt per attack
    public float damageRadius = 1f; // Radius of the attack
    public LayerMask enemyLayer; // Layer for enemies
    public Transform attackPoint; // Origin point of the attack
    // New variable to control movement
    public bool isAttacking = false;

    // 🔥 Attack Cooldown Variables
    public float attackCooldown = 1f; // Time between attacks
    private float lastAttackTime = 0f;
    [Header("Particles")]
    public ParticleSystem AttackSparks;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<BeezyMovement>();

        // Ensure attack points are assigned
        if (attackPoint == null)
            Debug.LogError("AttackPoint is not assigned in BeezyAttack script.");
    }

    void Update()
    {
        // Check if UpArrow or W is being held
        bool isHoldingUp = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);

        // Only allow attack input if:
        // - NOT holding up
        // - Player is grounded
        // - Not already attacking
        // - Cooldown has passed
        if (Input.GetKeyDown(KeyCode.X) && !isHoldingUp && playerMovement.isGrounded && !isAttacking &&
            Time.time >= lastAttackTime + attackCooldown)
        {
            // Stop all movement immediately
            rb.velocity = new Vector2(0, rb.velocity.y);
            animator.SetTrigger("Attack1");
            isAttacking = true;
            lastAttackTime = Time.time; // Record the time of this attack
        }
    }

    // Called from animation event
    public void DealDamage()
    {
        Transform currentAttackPoint = attackPoint;
        float currentDamageRadius = damageRadius;

        if (currentAttackPoint == null)
        {
            Debug.LogError("AttackPoint is missing in BeezyAttack script.");
            return;
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, currentDamageRadius, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            MushroomHealth mushroomHealth = enemy.GetComponent<MushroomHealth>();

            if (mushroomHealth != null)
            {
                Vector2 attackDirection = (Vector2)(enemy.transform.position - currentAttackPoint.position).normalized;
                mushroomHealth.TakeDamage(damage, attackDirection);
            }
        }
    }

    // Call this from the END of your attack animation using an Animation Event
    public void EndAttack()
    {
        isAttacking = false;
    }

    // Draw attack radius in editor
    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, damageRadius);
        }
    }

    public void PlayAttackSparks()
    {
        // Enable the ElectricityTrail particle system
        if (AttackSparks != null)
        {
            AttackSparks.Play();
        }
    }
}