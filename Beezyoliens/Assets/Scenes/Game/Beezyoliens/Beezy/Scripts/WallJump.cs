using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class WallJump : MonoBehaviour
{
    [Header("Wall Settings")]
    public LayerMask wallLayer;
    public Transform wallCheckPoint;
    public float wallCheckRadius = 0.2f;
    public float wallStickSpeed = 2f;

    [Header("Wall Jump Settings")]
    public float wallJumpDistanceX = 2f;
    public float wallJumpDistanceY = 1f;
    public float wallJumpDuration = 0.3f;

    [Header("Animation & Effects")]
    public Animator animator;
    public ParticleSystem wallSlideDust;

    public LayerMask groundLayer;
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.1f;

    [Header("References from Movement Script")]
    public BeezyMovement movementScript;

    private Rigidbody2D rb;
    private bool isOnWall = false;
    private int lastWallInstanceID = -1;
    private Coroutine wallJumpCoroutine = null;
    private Vector2 wallJumpDirection = Vector2.zero;
    private bool wasGrounded = false;
    private bool isFacingRight => movementScript.isFacingRight;
    private bool wasFalling = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        

        // Try to auto-find animator
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Track falling state for early detection
        wasFalling = animator.GetBool("isFalling");

        // Run wall detection every frame
        EarlyWallDetection();

        CheckWall();
        HandleWallJump();
    }
    void EarlyWallDetection()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(wallCheckPoint.position, wallCheckRadius, wallLayer);

        foreach (Collider2D col in hits)
        {
            if (col != null && !IsGrounded() && (rb.velocity.y < 0f || wasFalling))
            {
                int currentWallID = col.gameObject.GetInstanceID();

                if (lastWallInstanceID != currentWallID)
                {
                    lastWallInstanceID = currentWallID;
                    isOnWall = true;
                    animator.SetBool("isWallSliding", true);
                    movementScript.isTouchingWall = true;
                    movementScript.canFlip = false;
                    if (wallSlideDust != null) wallSlideDust.Play();
                }

                rb.velocity = new Vector2(rb.velocity.x, -wallStickSpeed);
                return;
            }
        }

        isOnWall = false;
        animator.SetBool("isWallSliding", false);
        movementScript.isTouchingWall = false;
        movementScript.canFlip = true;
    }

    void CheckWall()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(wallCheckPoint.position, wallCheckRadius, wallLayer);

        foreach (Collider2D col in hits)
        {
            if (col != null && !IsGrounded() && rb.velocity.y < 0f)
            {
                int currentWallID = col.gameObject.GetInstanceID();

                
                // Only apply wall stick if it's a new wall
                if (lastWallInstanceID != currentWallID)
                {
                    lastWallInstanceID = currentWallID;
                    isOnWall = true;
                    // Slide down wall slowly
                    rb.velocity = new Vector2(rb.velocity.x, -wallStickSpeed);
                    animator.SetBool("isWallSliding", true);
                    wallJumpDirection = isFacingRight ? Vector2.left * wallJumpDistanceX : Vector2.right * wallJumpDistanceX;
                    wallJumpDirection += Vector2.up * wallJumpDistanceY;

                    // Block movement and flip
                    movementScript.isTouchingWall = true;
                    movementScript.canFlip = false;

                    if (wallSlideDust != null)
                        wallSlideDust.Play();
                }

                

                return;
            }
        }

        isOnWall = false;
        animator.SetBool("isWallSliding", false);

        // Allow movement again
        movementScript.isTouchingWall = false;
        movementScript.canFlip = true;
    }

    void HandleWallJump()
    {
        if (isOnWall && Input.GetKeyDown(KeyCode.Space))
        {
            // Calculate jump vector away from wall
            float horizontal = isFacingRight ? -wallJumpDistanceX : wallJumpDistanceX;
            Vector2 jumpVector = new Vector2(horizontal, wallJumpDistanceY);

            wallJumpCoroutine = StartCoroutine(ManualWallJump(jumpVector));
            movementScript.canFlip = true;
            movementScript.Flip(); // Optional: call Flip manually
        }

        // Reset when grounded
        if (IsGrounded())
        {
            isOnWall = false;
            lastWallInstanceID = -1;
            animator.SetBool("isWallSliding", false);

            // Reset facing direction if needed
            if (wasGrounded == false)
            {
                wasGrounded = true;
                // Optional: reset facing direction on ground
            }
        }
        else
        {
            wasGrounded = false;
        }
    }

    IEnumerator ManualWallJump(Vector2 direction)
    {

        float elapsedTime = 0f;
        Vector2 startPosition = rb.position;

        while (elapsedTime < wallJumpDuration)
        {
            rb.MovePosition(Vector2.Lerp(startPosition, startPosition + direction, elapsedTime / wallJumpDuration));
            elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Snap to final position
        rb.MovePosition(startPosition + direction);

        // Trigger jump anim
        animator.SetTrigger("jump");

        // Flip character mid-air
       
       
        UpdateParticleSystemFlips();

        // Reset flags
        isOnWall = false;
        lastWallInstanceID = -1;
        wallJumpCoroutine = null;
        movementScript.isTouchingWall = false;
        movementScript.canFlip = true;
    }

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
    }

    void UpdateParticleSystemFlips()
    {
        if (wallSlideDust != null)
        {
            var renderer = wallSlideDust.GetComponent<ParticleSystemRenderer>();
            float targetFlipX = isFacingRight ? 0 : 1;
            renderer.flip = new Vector2(targetFlipX, 0);
        }

        // Add other particle systems here as needed
    }

    // Visualize wallCheckPoint as line gizmo (one direction only)
    private void OnDrawGizmos()
    {
        if (wallCheckPoint != null)
        {
            Gizmos.color = isOnWall ? Color.blue : Color.yellow;

            // Draw line in X direction only
            Vector3 wallDirection = isFacingRight ? Vector3.left : Vector3.right;
            Vector3 gizmosEnd = wallCheckPoint.position + wallDirection * wallCheckRadius;
            Gizmos.DrawLine(wallCheckPoint.position, gizmosEnd);
        }
    }
}