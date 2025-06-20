using UnityEngine;

public class PogController : MonoBehaviour
{
    [Header("Pog Settings")]
    public float pogDownForce = -10f;
    public float bounceHeight = 8f;

    [Header("Pog Check Point")]
    public Transform pogCheckPoint; // Drag your pog check transform here
    public float maxPogDistance = 1.5f; // Max allowed distance to start pog
    public LayerMask groundLayer;

    [Header("Particles")]
    public ParticleSystem pogStartEffect1;
    public ParticleSystem pogStartEffect2;
    public ParticleSystem pogLandEffect1;
    public ParticleSystem pogLandEffect2;

    [Header("References")]
    public Animator animator;
    public Rigidbody2D rb;

    // Upward attack variables
    public Transform upwardAttackPoint;
    public float upwardAttackRadius = 1f;

    private bool isPogging = false;
    private bool hasCollided = false;

    void Update()
    {
        HandlePogInput();
        HandleUpwardAttackInput();
    }

    void HandlePogInput()
    {
        // Check if currently holding Down/S
        bool isHoldingDirection = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

        // Check if X was just pressed
        bool xPressed = Input.GetKeyDown(KeyCode.X);

        if (xPressed && isHoldingDirection && !isPogging)
        {
            // Raycast from pogCheckPoint downward
            RaycastHit2D hit = Physics2D.Raycast(pogCheckPoint.position, Vector2.down, maxPogDistance, groundLayer);

            // Only allow pog if:
            // - There's ground within pog distance
            // - Player is NOT touching the ground right now
            if (hit.collider != null && hit.distance > 0.1f)
            {
                StartPog();
            }
        }
    }

    void HandleUpwardAttackInput()
    {
        // Check if holding Up/W
        bool isHoldingUp = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);

        // Check if pressing X
        bool xPressed = Input.GetKeyDown(KeyCode.X);

        if (xPressed && isHoldingUp && !isPogging)
        {
            TriggerUpwardAttack();
        }
    }

    void TriggerUpwardAttack()
    {
        animator.SetTrigger("UpwardAttack");
        Debug.Log("🚀 Upward Attack Triggered!");
    }

    void StartPog()
    {
        animator.SetTrigger("StartPog");
        animator.SetBool("isPogging", true);
        isPogging = true;
        hasCollided = false;

        // Apply downward force
        rb.velocity = new Vector2(rb.velocity.x, pogDownForce);

        PlayParticle(pogStartEffect1);
        PlayParticle(pogStartEffect2);
    }

    public void FinishPog()
    {
        animator.SetBool("isPogging", false);
        isPogging = false;
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (isPogging && !hasCollided && IsContactWithinDistance(col))
        {
            Debug.Log("💥 Pog Collision Detected!");

            hasCollided = true;

            PlayParticle(pogLandEffect1);
            PlayParticle(pogLandEffect2);

            rb.velocity = new Vector2(rb.velocity.x, bounceHeight);

            Invoke(nameof(FinishPog), 0.4f); // Adjust based on bounce height
        }
    }

    bool IsContactWithinDistance(Collision2D col)
    {
        foreach (ContactPoint2D contact in col.contacts)
        {
            float distance = Vector2.Distance(contact.point, pogCheckPoint.position);
            if (distance <= 0.5f)
            {
                return true;
            }
        }
        return false;
    }

    void PlayParticle(ParticleSystem ps)
    {
        if (ps != null && !ps.isPlaying)
        {
            ps.Play();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (pogCheckPoint != null)
        {
            // Line from player to pogCheckPoint
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, pogCheckPoint.position);

            // Line showing the max pog distance below pogCheckPoint
            Vector2 direction = -Vector2.up; // Downward
            Vector2 endPoint = (Vector2)pogCheckPoint.position + direction * maxPogDistance;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pogCheckPoint.position, endPoint);
        }

        // Draw upward attack gizmo
        if (upwardAttackPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(upwardAttackPoint.position, upwardAttackRadius);
        }
    }
}