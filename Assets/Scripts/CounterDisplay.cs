using System;
using UnityEngine;
using TMPro;

public class CounterDisplay : MonoBehaviour
{
    public TMP_Text counterText;
    public float smoothing = 12f; // Higher = snappier, lower = floatier

    double shown;

    void Start()
    {
        if (GameManager.Instance != null) shown = GameManager.Instance.SmoothBrownies;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || counterText == null) return; // Skip if not set up yet

        double target = gm.SmoothBrownies;

        // Ease toward the real value so clicks and income roll up instead of jumping
        shown += (target - shown) * (1.0 - Math.Exp(-smoothing * Time.unscaledDeltaTime));
        if (Math.Abs(target - shown) < 0.01) shown = target;

        counterText.text = ((long)shown).ToString("N0"); // N0 adds thousands separators
    }
}