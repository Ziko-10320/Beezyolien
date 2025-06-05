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
    public float groundCheckRadius = 0.2f;
    private bool isGrounded = false;

    [Header("Slide Settings")]
    public float slideSpeed = 14f;
    public float slideDuration = 0.5f;
    private bool isSliding = false;
    private float slideTimer = 0f;

    [Header("References")]
    public Animator animator;
    public ParticleSystem landDust;

    private Rigidbody2D rb;
    private float targetSpeed;
    private float currentSpeed;
    private float moveDirection = 0f;
    private bool isMoving = false;
    private bool isFacingRight = true;
    private bool wasFalling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Only allow input if NOT sliding
        if (!isSliding)
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
                rb.velocity = Vector2.zero; // Reset vertical velocity
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                animator.SetTrigger("jump");
                isJumping = true;
            }
        }

        // Slide Input
        if (Input.GetKeyDown(KeyCode.C) && isGrounded && !isSliding)
        {
            Debug.Log("Slide key pressed and conditions met");
            StartSlide();
        }

        // Falling and Landing Logic
        bool isFallingNow = rb.velocity.y < 0 && !isGrounded;
        animator.SetBool("isFalling", isFallingNow);

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
            void ResetLanding()
            {
                animator.SetBool("isLanding", false);
            }
        }
    }

    void FixedUpdate()
    {
        // Ground Check
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);

        // Target Speed Calculation
        if (isMoving && !isSliding)
        {
            targetSpeed = moveSpeedMax;
        }
        else if (!isSliding)
        {
            targetSpeed = 0f;
        }

        // Smooth acceleration using Lerp
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.fixedDeltaTime / accelerationTime);

        // Apply movement only if not sliding
        if (!isSliding)
        {
            rb.velocity = new Vector2(moveDirection * currentSpeed, rb.velocity.y);
        }

        // Slide Movement
        if (isSliding)
        {
            slideTimer += Time.fixedDeltaTime;

            if (slideTimer < slideDuration)
            {
                // Use facing direction during slide
                float slideMove = isFacingRight ? 1 : -1;
                rb.velocity = new Vector2(slideMove * slideSpeed, rb.velocity.y);
            }
            else
            {
                isSliding = false;
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

    void Flip()
    {
        isFacingRight = !isFacingRight;

        // Rotate 180 degrees on Y axis
        transform.Rotate(0f, 180f, 0f);
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