using UnityEngine;

public class BeezyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeedMax = 8f;
    public float moveSpeedMid = 4f;
    public float accelerationTime = 0.5f;

    [Header("Jump Settings")]
    public float jumpForce = 12f;
    private bool isJumping = false;
    public bool canJump = true;

    [Header("GroundCheck Settings")]
    public LayerMask groundLayer;
    public Transform groundCheckPoint;
    public float groundCheckRadius = 0.1f;
    public bool isGrounded = false;

    [Header("Slide Settings")]
    public float slideSpeed = 14f;
    public float slideDuration = 0.5f;
    private bool isSliding = false;
    private float slideTimer = 0f;

    [Header("Dash Settings")]
    public float forwardDashSpeed = 16f;
    public float forwardDashDuration = 0.2f;
    public float upwardDashSpeed = 12f;
    public float upwardDashDuration = 0.3f;
    public float maxAirDashes = 2;
    public float dashInterval = 0.3f; // Time between each dash
    public float dashCooldown = 3f;   // Total cooldown before next set
    public float dashWindowDuration = 0.5f; // Time between dashes before resetting
    public float dashRecoveryTime = 2f;    // Time to fully recover all dashes

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;
    private float dashCooldownTimer = -Mathf.Infinity;
    private int dashCount = 0;
    private Vector2 dashDirection = Vector2.zero;
    public bool canFlip = true;
    private float dashWindowTimer = 0f;
    private int dashCharges = 2;
    private bool usedSecondDash = false;
    private bool dashCooldownStarted = false;

    [Header("References")]
    public Animator animator;
    public ParticleSystem landDust;
    public ParticleSystem jumpDust;
    public ParticleSystem SlideBurstDust;
    public ParticleSystem SlideBurstDust2;
    public ParticleSystem SlideDust;
    public ParticleSystem SlideDust2;
    public ParticleSystem SlideDust3;
    public ParticleSystem SlideDust4;
    public ParticleSystem Dust;
    public ParticleSystem DashExplosion;
    public ParticleSystem DashTrail;
    public ParticleSystem DashPoint;
    public bool startsFacingRight = true;

    [Header("Diagonal Jump Settings")]
    public float diagonalJumpXForce = 6f; // Horizontal force
    public float diagonalJumpYForce = 8f; // Vertical force
    public float diagonalJumpDuration = 0.3f; // How long the jump lasts
    private bool isDiagonalJumping = false;
    private float diagonalJumpTimer = 0f;

    private Rigidbody2D rb;
    private float targetSpeed;
    private float currentSpeed;
    private float moveDirection = 0f;
    private bool isMoving = false;
    public bool isFacingRight = true;
    public bool isTouchingWall = false;
    private bool wasFalling = false;
    
    private BeezyAttack PlayerAttack;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerAttack = GetComponent<BeezyAttack>(); // This line was missing
        isFacingRight = startsFacingRight;

        // Set dust flip to (0, 0) initially (match editor)
        if (SlideBurstDust != null)
        {
            var renderer = SlideBurstDust.GetComponent<ParticleSystemRenderer>();
            renderer.flip = new Vector2(0, 0);
        }

        if (SlideBurstDust2 != null)
        {
            var renderer = SlideBurstDust2.GetComponent<ParticleSystemRenderer>();
            renderer.flip = new Vector2(0, 0);
        }

        if (DashExplosion != null)
        {
            var renderer = DashExplosion.GetComponent<ParticleSystemRenderer>();
            renderer.flip = new Vector2(0, 0);
        }
    }

    void Update()
    {
        // Slide interrupt with diagonal jump
        if (Input.GetKeyDown(KeyCode.Space) && isSliding)
        {
            Debug.Log("🚀 Diagonal Jump Triggered!");
            StartDiagonalJump();
        }
       
        // Only allow input if NOT sliding or dashing
        if (!isSliding && !isDashing && !PlayerAttack.isAttacking)
        {
            // Input Handling
            moveDirection = Input.GetAxisRaw("Horizontal");

            // Determine if moving
            isMoving = Mathf.Abs(moveDirection) > 0.1f;
            animator.SetBool("isRunning", isMoving);

            // Handle direction change
            if (moveDirection != 0)
            {
                bool newFacingDirection = (moveDirection > 0);
                if (newFacingDirection != isFacingRight)
                {
                    Flip();
                }
            }

            /// Jump
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded && canJump)
            {
                rb.velocity = Vector2.zero;
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                animator.SetTrigger("jump");
                isJumping = true;

                // Play jump dust
                if (jumpDust != null)
                {
                    jumpDust.Play();
                }
            }
        }
        
        // Slide Input
        if (Input.GetKeyDown(KeyCode.C) && isGrounded && !isSliding)
        {

           
        Debug.Log("Slide key pressed and conditions met");
            StartSlide();

            // Play slide burst dust
            if (SlideBurstDust != null) SlideBurstDust.Play();
            if (SlideBurstDust2 != null) SlideBurstDust2.Play();
            if (SlideDust != null) SlideDust.Play();
            if (SlideDust2 != null) SlideDust2.Play();
            if (SlideDust3 != null) SlideDust3.Play();
            if (SlideDust4 != null) SlideDust4.Play();
        }

        // Air Dash Input
        if (Input.GetKeyDown(KeyCode.C) && !isGrounded && !isSliding)
        {
            float timeSinceLastDash = Time.time - lastDashTime;

            // First dash always allowed
            if (dashCharges >= 1)
            {
                bool upwardDash = Input.GetAxisRaw("Vertical") > 0 || Input.GetKey(KeyCode.Z);
                string dashAnim = upwardDash ? "upwardDash" : "dash";
                animator.SetTrigger(dashAnim);

                StartAirDash(upwardDash);

                dashCharges--;
                lastDashTime = Time.time;
                usedSecondDash = (dashCharges == 0); // True if we just used second dash

                Debug.Log($"Used Dash. Remaining Charges: {dashCharges}");
            }
            else if (!dashCooldownStarted)
            {
                // Start full recovery only after both dashes used
                dashCooldownStarted = true;
                dashCooldownTimer = Time.time + dashRecoveryTime;
            }
        }
        // Recover second dash if not used in time
        if (!isDashing && !isGrounded && Time.time - lastDashTime >= dashWindowDuration && dashCharges == 1)
        {
            dashCharges = 2;
            Debug.Log("Recovered second dash");
        }

        // If both dashes used → start full recovery
        if (usedSecondDash && Time.time >= dashCooldownTimer && dashCooldownStarted)
        {
            dashCharges = 2;
            dashCooldownStarted = false;
            Debug.Log("All dashes recovered!");
        }

        // Falling and Landing Logic
        bool isFallingNow = rb.velocity.y < 0 && !isGrounded;
        animator.SetBool("isFalling", isFallingNow);

        if (!isGrounded)
        {
            dashWindowTimer += Time.deltaTime;

            if (dashWindowTimer > dashWindowDuration && dashCount < maxAirDashes)
            {
                dashCount = 0;
                dashWindowTimer = 0f;
                Debug.Log("Dash count reset due to timeout");
            }
        }
        else
        {
            dashWindowTimer = 0f; // Reset when grounded
        }

        if (!isGrounded)
        {
            wasFalling = isFallingNow;
        }
        else
        {
            if (wasFalling && rb.velocity.y <= 0.1f && isGrounded)
            {
                animator.SetBool("isLanding", true);
                animator.SetBool("isFalling", false);

                // Play dust effect
                if (landDust != null)
                {
                    landDust.Play();
                }

                // Reset after short delay
                Invoke(nameof(ResetLanding), 0.1f);

                wasFalling = false;
                isJumping = false;
            }
            else
            {
                animator.SetBool("isLanding", false);
            }
        }
    }

    void FixedUpdate()
    {

        // Skip all movement logic if attacking
        if (PlayerAttack != null && PlayerAttack.isAttacking)
        {
            return;
        }

        // Ground Check
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

        // Target Speed Calculation
        if (isMoving && !isSliding && !isDashing && !isTouchingWall)
        {
            targetSpeed = moveSpeedMax;
        }
        else if (!isSliding && !isDashing)
        {
            targetSpeed = 0f;
        }

        // Smooth acceleration using Lerp
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime / accelerationTime);

        // Apply movement only if not sliding or dashing
        if (!isSliding && !isDashing && !PlayerAttack.isAttacking)
        {
            rb.velocity = new Vector2(moveDirection * currentSpeed, rb.velocity.y);
        }

        // Slide Movement
        if (isSliding)
        {
            slideTimer += Time.fixedDeltaTime;

            if (slideTimer < slideDuration)
            {
                float slideMove = isFacingRight ? 1 : -1;
                rb.velocity = new Vector2(slideMove * slideSpeed, rb.velocity.y);
            }
            else
            {
                isSliding = false;
            }
        }

        // Diagonal Jump Logic
        if (isDiagonalJumping)
        {
            diagonalJumpTimer += Time.fixedDeltaTime;
            if (diagonalJumpTimer < diagonalJumpDuration)
            {
                // Lock horizontal input during jump
                moveDirection = 0f;
                // Optionally maintain the initial velocity or keep updating it
                float dirX = isFacingRight ? 1 : -1;
                rb.velocity = new Vector2(dirX * diagonalJumpXForce, diagonalJumpYForce);
            }
            else
            {
                isDiagonalJumping = false;
                canFlip = true; // Re-enable flipping after jump ends
            }
        }

        // Air Dash Movement
        if (isDashing)
        {
            dashTimer += Time.fixedDeltaTime;

            float dashDuration = dashDirection.y > 0 ? upwardDashDuration : forwardDashDuration;

            if (dashTimer < dashDuration)
            {
                rb.gravityScale = 0f;
                rb.velocity = dashDirection * (dashDirection.y > 0 ? upwardDashSpeed : forwardDashSpeed);
            }
            else
            {
                isDashing = false;
                dashDirection = Vector2.zero;
                rb.gravityScale = 5f;
                canFlip = true;

                // Trigger cooldown after second dash
                if (dashCount >= maxAirDashes)
                {
                    dashCooldownTimer = Time.time + dashCooldown;
                }
            }
        }
        else
        {
            // Enforce cooldown even when grounded
            float timeSinceCooldownEnd = Time.time - dashCooldownTimer;
            if (timeSinceCooldownEnd >= 0f)
            {
                // Optional: show UI or visual feedback
            }
        }
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = 0f;
        animator.SetTrigger("Slide");
        Debug.Log("Slide triggered");
    }

    void StartDiagonalJump()
    {
        Debug.Log("🚀 Starting Diagonal Jump!");
        isSliding = false;
        animator.SetTrigger("diagonalJump"); // Make sure this trigger exists
        isDiagonalJumping = true;
        diagonalJumpTimer = 0f;

        // Determine horizontal direction
        float dirX = isFacingRight ? 1 : -1;

        // Apply both X and Y forces
        Vector2 jumpVelocity = new Vector2(dirX * diagonalJumpXForce, diagonalJumpYForce);
        rb.velocity = jumpVelocity;

        // Disable flip and movement during jump
        canFlip = false;
    }
    void StartAirDash(bool upwardDash)
    {
        isDashing = true;
        dashTimer = 0f;

        // Set dash direction
        if (upwardDash)
        {
            dashDirection = Vector2.up;
        }
        else
        {
            float dir = isFacingRight ? 1 : -1;
            dashDirection = new Vector2(dir, 0);
        }
        if (DashExplosion != null) DashExplosion.Play();
        if (DashTrail != null) DashTrail.Play();
        if (DashPoint != null) DashPoint.Play();

        // Prevent flip during dash
        canFlip = false;
    }

    public void Flip()
    {
        if (!canFlip) return;

        isFacingRight = !isFacingRight;
        if (isGrounded)
            Dust.Play();

        // Rotate 180 degrees on Y axis
        transform.Rotate(0f, 180f, 0f);

        // Flip dust using Renderer.flip.x only
        if (SlideBurstDust != null)
        {
            var renderer = SlideBurstDust.GetComponent<ParticleSystemRenderer>();
            float targetFlipX = isFacingRight ? 0 : 1;
            renderer.flip = new Vector2(targetFlipX, 0);
        }

        if (SlideBurstDust2 != null)
        {
            var renderer = SlideBurstDust2.GetComponent<ParticleSystemRenderer>();
            float targetFlipX = isFacingRight ? 0 : 1;
            renderer.flip = new Vector2(targetFlipX, 0);
        }

        if (DashExplosion != null)
        {
            var renderer = DashExplosion.GetComponent<ParticleSystemRenderer>();
            float targetFlipX = isFacingRight ? 0 : 1;
            renderer.flip = new Vector2(targetFlipX, 0);
        }
    }
   


    void ResetLanding()
    {
        animator.SetBool("isLanding", false);
        animator.Play("Idle");
    }
    // Visualize ground check in editor
    private void OnDrawGizmos()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}