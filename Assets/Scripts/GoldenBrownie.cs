using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class GoldenBrownie : MonoBehaviour
{
    public static event Action<string> OnReward; // HUD listens to show a toast

    [SerializeField] Button button;
    [SerializeField] float lifetime = 13f;
    [SerializeField] float fadeTime = 1.5f;
    [SerializeField] float bobAmount = 12f, bobSpeed = 2f;

    [Header("Reward weights")]
    [SerializeField] float luckyWeight = 50, frenzyWeight = 35, clickFrenzyWeight = 15;

    [Header("Reward tuning")]
    [SerializeField] float luckyBankPercent = 0.15f;
    [SerializeField] float luckyMaxSecondsOfProduction = 900f;
    [SerializeField] float frenzyMultiplier = 7f,  frenzySeconds = 30f;
    [SerializeField] float clickMultiplier = 10f,  clickSeconds = 15f;

    CanvasGroup group;
    RectTransform rt;
    Vector3 basePos;
    float age;
    bool claimed;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        rt = (RectTransform)transform;
    }

    void Start()
    {
        basePos = rt.localPosition; // spawner sets position before Start runs
        button.onClick.AddListener(Claim);
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;
        if (age >= lifetime) { Destroy(gameObject); return; }

        // Fade in quickly, fade out for the last few seconds
        float fadeIn  = Mathf.Clamp01(age / 0.3f);
        float fadeOut = Mathf.Clamp01((lifetime - age) / fadeTime);
        group.alpha = Mathf.Min(fadeIn, fadeOut);

        rt.localPosition = basePos + Vector3.up * Mathf.Sin(age * bobSpeed) * bobAmount;
    }

    void Claim()
    {
        if (claimed) return;
        claimed = true;
        button.interactable = false;

        var gm = GameManager.Instance;
        float roll = UnityEngine.Random.value * (luckyWeight + frenzyWeight + clickFrenzyWeight);
        string msg;

        if (roll < luckyWeight)
        {
            // Capped by both your bank and your production so it scales with progress
            long fromBank = (long)(gm.Brownies * luckyBankPercent);
            long fromProd = (long)(gm.PerSecond() * luckyMaxSecondsOfProduction);
            long bonus = Math.Min(fromBank, fromProd) + 13;
            gm.GrantBrownies(bonus);
            msg = $"Lucky! +{bonus:N0} brownies";
        }
        else if (roll < luckyWeight + frenzyWeight)
        {
            gm.ApplyBuff(BuffType.Production, frenzyMultiplier, frenzySeconds);
            msg = $"Frenzy! x{frenzyMultiplier:0} production for {frenzySeconds:0}s";
        }
        else
        {
            gm.ApplyBuff(BuffType.Click, clickMultiplier, clickSeconds);
            msg = $"Click Frenzy! x{clickMultiplier:0} clicks for {clickSeconds:0}s";
        }

        OnReward?.Invoke(msg);
        Destroy(gameObject);
    }
}