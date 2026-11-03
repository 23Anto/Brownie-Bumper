using System.Collections;
using UnityEngine;

public class BrownieBounce : MonoBehaviour
{
    public float squash = 0.15f;   // How much it shrinks on press (0.15 = 15%)
    public float duration = 0.35f; // Total length of the bounce
    public float bounces = 1.5f;   // Number of springy oscillations
    public float decay = 5f;       // Higher = settles faster

    Vector3 baseScale;
    Coroutine routine;

    void Awake()
    {
        baseScale = transform.localScale; // Remember the original size so we always return to it
    }

    public void Bounce()
    {
        // Restart cleanly on rapid clicks so the scale never compounds or drifts
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(BounceRoutine());
    }

    IEnumerator BounceRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);

            // Damped cosine: starts squashed, overshoots past normal, then settles to 1
            float factor = 1f - squash * Mathf.Cos(p * Mathf.PI * 2f * bounces) * Mathf.Exp(-decay * p);
            transform.localScale = baseScale * factor;
            yield return null;
        }
        transform.localScale = baseScale; // Snap exactly back to the original size
        routine = null;
    }
}