using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private Animator animator;

    // Public variables for damage logic
    public float damage = 10f; // Amount of damage dealt per attack
    public float pogDamage = 15f; // Amount of damage dealt during the Pog
    public float damageRadius = 1f; // Radius of the attack
    public LayerMask enemyLayer; // Layer for enemies
    public Transform attackPoint; // The origin point of the attack
    
    
    // Upward attack variables
    public Transform upwardAttackPoint; // Separate attack point for upward attack
    public float upwardAttackRadius = 1f; // Radius of the upward attack
    private bool isUpwardAttack = false; // Is the player performing an upward attack?

    // Pog Mechanic Variables
    public float pogForce = -20f; // Instant downward force applied during Pog
    public float pogDuration = 0.2f; // Duration of the downward force
    public float pogBounceHeight = 5f; // Default bounce height (used for max height)
    public float mediumBounceHeight = 3f; // Medium bounce height
    public float minBounceHeight = 1.5f; // Minimum bounce height
    public float minGroundDistance = 0.5f; // Minimum distance from the ground to start a Pog
    public ParticleSystem pogLandingParticles; // Particle system to play on landing
    public Transform pogAttackPoint; // Attack point for the Pog
    public float pogDamageRadius = 1f; // Radius of the Pog's damage zone
    public bool isPogging = false; // Is the player performing a Pog?
    private int successivePogCount = 0; // Tracks consecutive Pogs
    private Rigidbody2D rb;

    // Reference to the PlayerMovement script
    private BeezyMovement playerMovement;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<BeezyMovement>();

        // Ensure the attackPoint, upwardAttackPoint, and pogAttackPoint are assigned
        if (attackPoint == null)
        {
            Debug.LogError("AttackPoint is not assigned in PlayerAttack script.");
        }
        if (upwardAttackPoint == null)
        {
            Debug.LogError("UpwardAttackPoint is not assigned in PlayerAttack script.");
        }
        if (pogAttackPoint == null)
        {
            Debug.LogError("PogAttackPoint is not assigned in PlayerAttack script.");
        }
    }

    void Update()
    {
        // Handle attack input
        if (Input.GetKeyDown(KeyCode.X))
        {
            // Check if the player is pressing the Up Arrow Key for an upward attack
            if (Input.GetKey(KeyCode.UpArrow))
            {
                isUpwardAttack = true;
                animator.SetTrigger("UpwardAttack");
            }
            // Check if the player is pressing the Down Arrow Key for a Pog
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) && !playerMovement.isGrounded)
            {
                // Only start the Pog if the player is far enough from the ground
                if (!IsCloseToGround())
                {
                    StartCoroutine(Pog());
                }
            }
            else
            {
                isUpwardAttack = false;
                animator.SetTrigger("Attack1");
            }
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            animator.SetTrigger("Special");
            
        }
    }

    // This method will be called from the Attack1 or UpwardAttack animation event
    public void DealDamage()
    {
        // Determine which attack point to use based on the attack type
        Transform currentAttackPoint = isUpwardAttack ? upwardAttackPoint : attackPoint;
        float currentDamageRadius = isUpwardAttack ? upwardAttackRadius : damageRadius;

        if (currentAttackPoint == null)
        {
            Debug.LogError("AttackPoint is missing in PlayerAttack script.");
            return;
        }

        // Detect enemies within the damage radius around the attackPoint
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, currentDamageRadius, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Get the MushroomHealth component from the enemy
            MushroomHealth mushroomHealth = enemy.GetComponent<MushroomHealth>();

            if (mushroomHealth != null)
            {
                // Calculate the direction of the knockback
                Vector2 attackDirection = (Vector2)(enemy.transform.position - currentAttackPoint.position).normalized;
                Debug.Log("Attack direction: " + attackDirection);

                // Deal damage to the mushroom
                mushroomHealth.TakeDamage(damage, attackDirection);
            }
        }
    }

   

    // Draw the damage radius in the Scene view for debugging
    private void OnDrawGizmosSelected()
    {
        // Draw the normal attack radius
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, damageRadius);
        }

        // Draw the upward attack radius
        if (upwardAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(upwardAttackPoint.position, upwardAttackRadius);
        }

        // Draw the Pog attack radius
        if (pogAttackPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pogAttackPoint.position, pogDamageRadius);
        }
    }

    // Check if the player is close to the ground
    private bool IsCloseToGround()
    {
        return Physics2D.Raycast(transform.position, Vector2.down, minGroundDistance, playerMovement.groundLayer);
    }

    // Pog Mechanic
    IEnumerator Pog()
    {
        if (isPogging) yield break; // Prevent multiple Pogs

        isPogging = true;

        // Trigger the Pog animation as a trigger
        animator.SetTrigger("Pog");

        // Store the original gravity scale
        float originalGravityScale = rb.gravityScale;

        // Apply an instant downward force
        rb.velocity = new Vector2(rb.velocity.x, pogForce);

        // Disable gravity temporarily to avoid interference
        rb.gravityScale = 0f;

        // Wait for the duration of the Pog
        yield return new WaitForSeconds(pogDuration);

        // Re-enable gravity
        rb.gravityScale = originalGravityScale;

        // Flag to ensure damage is dealt only once per Pog
        bool hasDealtPogDamage = false;

        // Check for collisions with the ground or enemies
        bool bounced = false;

        // Check for ground collision
        if (playerMovement.isGrounded)
        {
            BounceAndPlayParticles();
            bounced = true;
        }

        // Check for enemy collisions using the Pog attack point
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(pogAttackPoint.position, pogDamageRadius, enemyLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            MushroomHealth mushroomHealth = enemy.GetComponent<MushroomHealth>();

            if (mushroomHealth != null && !hasDealtPogDamage)
            {
                // Calculate the direction of the knockback
                Vector2 attackDirection = (Vector2)(enemy.transform.position - pogAttackPoint.position).normalized;

                // Deal damage to the enemy using pogDamage
                mushroomHealth.TakeDamage(pogDamage, attackDirection);

                // Bounce off the enemy
                BounceAndPlayParticles();

                // Mark that damage has been dealt
                hasDealtPogDamage = true;
                bounced = true;
            }
        }

        // If no collision occurred, wait until the player reaches the peak of the bounce
        if (!bounced)
        {
            // Play the animation until the player reaches the peak of the bounce
            while (rb.velocity.y > 0)
            {
                yield return null; // Wait until the velocity becomes zero or negative
            }
        }

        isPogging = false;
    }

    // Helper method to handle bouncing and playing particles
    private void BounceAndPlayParticles()
    {
        // Play landing particles
        if (pogLandingParticles != null)
        {
            pogLandingParticles.Play();
        }

        // Determine bounce height based on successive Pogs
        float bounceHeight = GetBounceHeight();

        // Bounce the player upward to a specific height
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Sqrt(2 * Physics2D.gravity.magnitude * bounceHeight));
    }

    // Reset successive Pog count when grounded
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerMovement.groundLayer) != 0)
        {
            successivePogCount = 0; // Reset the counter when the player touches the ground
        }
    }

    // Determine bounce height based on successive Pogs
    private float GetBounceHeight()
    {
        successivePogCount++;
        return successivePogCount switch
        {
            1 => pogBounceHeight, // Max bounce height
            2 => mediumBounceHeight, // Medium bounce height
            _ => minBounceHeight // Minimum bounce height
        };
    }
}