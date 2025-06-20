using UnityEngine;

[DisallowMultipleComponent]
public class SlowOnlyThese : MonoBehaviour
{
    [Header("Settings")]
    public KeyCode slowKey = KeyCode.T;
    public float slowTimeScale = 0.2f; // e.g. 0.2 = 5x slower
    public float slowDuration = 3f;
    public float cooldown = 5f;

    [Header("Objects to Slow Down")]
    public Rigidbody2D[] rigidbodiesToSlow; // Drag multiple boxes/enemies here
    public Animator[] animatorsToSlow;      // Optional
    public ParticleSystem[] particlesToSlow; // Optional

    private bool isSlowing = false;
    private float lastUseTime = -Mathf.Infinity;

    // Storage for original values
    private Vector2[] originalVelocities;
    private float[] originalAngularVelocities;
    private float[] originalAnimatorSpeeds;
    private float[] originalParticleSpeeds;

    [SerializeField] private float accelerationTime = 0.1f;

    void Update()
    {
        if (Input.GetKeyDown(slowKey) && !isSlowing && Time.time >= lastUseTime + cooldown)
        {
            Debug.Log("[SlowOnlyThese] Activating Slow on Assigned Objects...");
            StartCoroutine(ActivateSlow());
            lastUseTime = Time.time;
        }
    }

    System.Collections.IEnumerator ActivateSlow()
    {
        isSlowing = true;

        int rbCount = rigidbodiesToSlow != null ? rigidbodiesToSlow.Length : 0;

        // Save original values
        originalVelocities = new Vector2[rbCount];
        originalAngularVelocities = new float[rbCount];

        for (int i = 0; i < rbCount; i++)
        {
            if (rigidbodiesToSlow[i] != null)
            {
                originalVelocities[i] = rigidbodiesToSlow[i].velocity;
                originalAngularVelocities[i] = rigidbodiesToSlow[i].angularVelocity;

                // Apply slow effect to velocity and angular velocity
                rigidbodiesToSlow[i].velocity = originalVelocities[i] * slowTimeScale;
                rigidbodiesToSlow[i].angularVelocity = originalAngularVelocities[i] * slowTimeScale;
            }
        }

        // Save and apply animator slowdown
        int animCount = animatorsToSlow != null ? animatorsToSlow.Length : 0;
        originalAnimatorSpeeds = new float[animCount];

        for (int i = 0; i < animCount; i++)
        {
            if (animatorsToSlow[i] != null)
            {
                originalAnimatorSpeeds[i] = animatorsToSlow[i].speed;
                animatorsToSlow[i].speed *= slowTimeScale;
            }
        }

        // Save and apply particle system slowdown
        int psCount = particlesToSlow != null ? particlesToSlow.Length : 0;
        originalParticleSpeeds = new float[psCount];

        for (int i = 0; i < psCount; i++)
        {
            if (particlesToSlow[i] != null)
            {
                var main = particlesToSlow[i].main;
                originalParticleSpeeds[i] = main.simulationSpeed;
                main.simulationSpeed *= slowTimeScale;
            }
        }

        // Simulate manual physics update per object
        float elapsed = 0f;

        while (elapsed < slowDuration)
        {
            for (int i = 0; i < rbCount; i++)
            {
                if (rigidbodiesToSlow[i] != null)
                {
                    Rigidbody2D rb = rigidbodiesToSlow[i];

                    // Apply gravity manually (scaled)
                    Vector2 gravity = Physics2D.gravity * Time.fixedDeltaTime * slowTimeScale;
                    rb.velocity += gravity;

                    // Smooth velocity transition
                    rb.velocity = Vector2.Lerp(
                        rb.velocity,
                        originalVelocities[i] * slowTimeScale,
                        Time.fixedDeltaTime / accelerationTime
                    );
                }
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // Restore everything after duration
        for (int i = 0; i < rbCount; i++)
        {
            if (rigidbodiesToSlow[i] != null)
            {
                rigidbodiesToSlow[i].velocity = originalVelocities[i];
                rigidbodiesToSlow[i].angularVelocity = originalAngularVelocities[i];
            }
        }

        for (int i = 0; i < animCount; i++)
        {
            if (animatorsToSlow[i] != null)
            {
                animatorsToSlow[i].speed = originalAnimatorSpeeds[i];
            }
        }

        for (int i = 0; i < psCount; i++)
        {
            if (particlesToSlow[i] != null)
            {
                var main = particlesToSlow[i].main;
                main.simulationSpeed = originalParticleSpeeds[i];
            }
        }

        isSlowing = false;
    }
}