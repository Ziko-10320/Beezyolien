using UnityEngine;

public class SlowMotionDebugger : MonoBehaviour
{
    public KeyCode slowKey = KeyCode.T; // Press T to slow down
    public float slowScale = 0.1f;
    public float normalScale = 1f;

    void Update()
    {
        if (Input.GetKeyDown(slowKey))
        {
            Time.timeScale = slowScale;
            Debug.Log("Time Scale: " + Time.timeScale);
        }

        if (Input.GetKeyUp(slowKey))
        {
            Time.timeScale = normalScale;
            Debug.Log("Time Scale: " + Time.timeScale);
        }
    }
}