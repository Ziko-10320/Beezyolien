﻿using UnityEngine;

public class BeezyMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeedMax = 8f;
    public float moveSpeedMid = 4f;
    public float accelerationTime = 0.5f;

    [Header("Jump Settings")]
    public float jumpPower = 12f;
    private bool isJumping = false;
    private bool jumpPressedInAir = false;
    private bool canJump = true;


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
    public float secondDashReductionPercent = 0.4f; // 0.4 = 40% of normal duration
    // New variables ↓↓↓                                      
    private float originalForwardDashDuration;
    private float originalUpwardDashDuration;
    private bool usedFirstDash = false;
    private bool usedSecondDash = false;


    private bool isDashing = false;
    private float dashTimer = 0f;
    private float lastDashTime = -Mathf.Infinity;
    private float dashCooldownTimer = -Mathf.Infinity;
    private int dashCount = 0;
    private Vector2 dashDirection = Vector2.zero;
    public bool canFlip = true;
    private float dashWindowTimer = 0f;
    private int dashCharges = 2;
    private bool dashCooldownStarted = false;

    [Header("Particules")]
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
    public ParticleSystem DiagonalDust;
    public bool startsFacingRight = true;

    [Header("Diagonal Jump Settings")]
    public float diagonalJumpXForce = 6f; // Horizontal force
    public float diagonalJumpYForce = 8f; // Vertical force
    public float diagonalJumpDuration = 0.3f; // How long the jump lasts
    private bool isDiagonalJumping = false;
    private float diagonalJumpTimer = 0f;
    private bool justStartedSlideThisFrame = false;


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

        originalForwardDashDuration = forwardDashDuration;
        originalUpwardDashDuration = upwardDashDuration;

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

        if (DiagonalDust != null)
        {
            var renderer = DiagonalDust.GetComponent<ParticleSystemRenderer>();
            renderer.flip = new Vector2(0, 0);
        }
    }

    void Update()
    {
        // Slide interrupt with diagonal jump
        if (Input.GetKeyDown(KeyCode.Space) && isSliding && !justStartedSlideThisFrame)
        {
            Debug.Log("🚀 Diagonal Jump Triggered!");
            StartDiagonalJump();

            if (DiagonalDust != null) DiagonalDust.Play();
            return;
        }

        // Only allow input if NOT sliding or dashing
        if (!isSliding && !isDashing && !PlayerAttack.isAttacking && !isDiagonalJumping)
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

        }
        // Handle Jump Input
        bool jumpKeyPressedThisFrame = Input.GetKeyDown(KeyCode.Space);
        bool slideKeyPressedThisFrame = Input.GetKeyDown(KeyCode.C);

        // Handle jump input
        if (jumpKeyPressedThisFrame)
        {
            if (!isSliding && !PlayerAttack.isAttacking && canJump)
            {
                if (isGrounded)
                {
                    // Grounded → Perform jump immediately
                    PerformJump();
                }
                else
                {
                    // Mid-air → record input, but don't jump yet
                    jumpPressedInAir = true;
                }
            }
        }
        // Slide Input
        if (Input.GetKeyDown(KeyCode.C) && isGrounded && !isSliding && !PlayerAttack.isAttacking)
        {

           
        Debug.Log("Slide key pressed and conditions met");
            StartSlide();
            justStartedSlideThisFrame = true; // ← Set flag to block diagonal jump
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
            // Only allow input if NOT attacking or diagonal jumping
            if (PlayerAttack != null && PlayerAttack.isAttacking)
                return;

            float timeSinceLastDash = Time.time - lastDashTime;

            // First dash always allowed
            if (dashCharges >= 1)
            {
                bool upwardDash = Input.GetAxisRaw("Vertical") > 0 || Input.GetKey(KeyCode.Z);
                string dashAnim = upwardDash ? "upwardDash" : "dash";
                animator.SetTrigger(dashAnim);

                // Apply reduced duration if it's the second dash
                float customDuration = upwardDash ? upwardDashDuration : forwardDashDuration;

                if (dashCharges == 1)
                {
                    customDuration *= secondDashReductionPercent; // Controlled by inspector
                }

                StartAirDash(upwardDash, customDuration);

                dashCharges--;
                lastDashTime = Time.time;
                usedSecondDash = (dashCharges == 0);

                Debug.Log($"Used Dash. Remaining Charges: {dashCharges}, Duration: {customDuration}");
            }
            else if (!dashCooldownStarted)
            {
                dashCooldownStarted = true;
                dashCooldownTimer = Time.time + dashRecoveryTime;
            }
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

        // Reset the flag at the end of the frame
        justStartedSlideThisFrame = false;
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

        // If landed and jump was pressed in air → perform jump
        if (isGrounded && jumpPressedInAir && canJump)
        {
            PerformJump();
            jumpPressedInAir = false; // Reset flag
        }

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
                // During jump → lock to set X/Y velocity
                float dirX = isFacingRight ? 1 : -1;
                rb.velocity = new Vector2(dirX * diagonalJumpXForce, diagonalJumpYForce);
            }
            else
            {
                // After jump duration → let physics take over
                isDiagonalJumping = false;
                canFlip = true;

                Debug.Log("Momentum applied after diagonal jump.");
            }
        }
        else if (!isSliding && !isDashing && !PlayerAttack.isAttacking)
        {
            // If not sliding/dashing/attacking → apply deceleration
            float deceleration = 0.92f; // Tweak this to control how fast they slow down
            Vector2 currentVelocity = rb.velocity;

            // Only affect horizontal movement
            if (Mathf.Abs(currentVelocity.x) > 0.1f)
            {
                currentVelocity.x *= deceleration;
                rb.velocity = currentVelocity;
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
        // If grounded or dash window expired → reset dash durations and charges
        if (isGrounded || Time.time - lastDashTime >= dashWindowDuration)
        {
            dashCharges = 2;
            usedSecondDash = false;
            dashCooldownStarted = false;

            forwardDashDuration = originalForwardDashDuration;
            upwardDashDuration = originalUpwardDashDuration;

            Debug.Log("Dash reset: grounded or timeout");
        }

        // If both dashes used → start full recovery
        if (usedSecondDash && Time.time >= dashCooldownTimer && dashCooldownStarted)
        {
            dashCharges = 2;
            usedSecondDash = false;
            dashCooldownStarted = false;

            forwardDashDuration = originalForwardDashDuration;
            upwardDashDuration = originalUpwardDashDuration;

            Debug.Log("All dashes recovered after cooldown!");
        }
    }

    void PerformJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0); // Reset Y velocity
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse); // Apply jump force
        animator.SetTrigger("jump");
        if (jumpDust != null) jumpDust.Play();
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
        animator.SetTrigger("diagonalJump");
        isDiagonalJumping = true;
        diagonalJumpTimer = 0f;

        // Stop all slide dust effects
        StopSlideDustEffects();

        // Determine direction
        float dirX = isFacingRight ? 1 : -1;

        // Apply diagonal jump velocity
        rb.velocity = new Vector2(dirX * diagonalJumpXForce, diagonalJumpYForce);

        // Disable flip and movement during jump
        canFlip = false;
    }
    void StopSlideDustEffects()
    {
        if (SlideBurstDust != null) SlideBurstDust.Stop();
        if (SlideBurstDust2 != null) SlideBurstDust2.Stop();
        if (SlideDust != null) SlideDust.Stop();
        if (SlideDust2 != null) SlideDust2.Stop();
        if (SlideDust3 != null) SlideDust3.Stop();
        if (SlideDust4 != null) SlideDust4.Stop();
    }
    void StartAirDash(bool upwardDash, float customDuration = -1f)
    {
        isDashing = true;
        dashTimer = 0f;

        // Set dash direction
        if (upwardDash)
        {
            dashDirection = Vector2.up;
            if (customDuration <= 0)
                upwardDashDuration = originalUpwardDashDuration;
            else
                upwardDashDuration = customDuration;
        }
        else
        {
            float dir = isFacingRight ? 1 : -1;
            dashDirection = new Vector2(dir, 0);
            if (customDuration <= 0)
                forwardDashDuration = originalForwardDashDuration;
            else
                forwardDashDuration = customDuration;
        }

        if (DashExplosion != null) DashExplosion.Play();
        if (DashTrail != null) DashTrail.Play();
        if (DashPoint != null) DashPoint.Play();

        rb.gravityScale = 0f;
        rb.velocity = dashDirection * (upwardDash ? upwardDashSpeed : forwardDashSpeed);

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

        if (DiagonalDust != null)
        {
            var renderer = DiagonalDust.GetComponent<ParticleSystemRenderer>();
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