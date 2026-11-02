using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Upgrade
{
    public string name;
    public long baseCost = 10;
    public float costMultiplier = 1.15f; // Cost growth per level (common idle-game curve)
    public long clickBonus = 0;          // Extra brownies per click, per level
    public long perSecondBonus = 0;      // Passive brownies/sec, per level
    [HideInInspector] public int level;

    public long CurrentCost => (long)(baseCost * Mathf.Pow(costMultiplier, level));
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Data")]
    public List<Upgrade> upgrades = new List<Upgrade>();
    public long Brownies { get; private set; }
    public long TotalEarned { get; private set; } // Lifetime brownies
    public event Action OnStateChanged; 

    public double SmoothBrownies => Brownies + passiveCarry;

    [Header("Anti-Cheat Settings")]
    [SerializeField] private int maxClicksPerSecond = 15;            // Max speed allowed
    [SerializeField] private float suspiciousVarianceThreshold = 0.002f; // Inhuman precision (2ms)
    [SerializeField] private int samplesToTest = 10;                 // Clicks needed to run the check
    
    private Queue<float> clickTimestamps = new Queue<float>();

    void Awake() { Instance = this; Load(); }

    public long ClickPower()
    {
        long power = 1;
        foreach (var u in upgrades) power += u.clickBonus * u.level;
        return power;
    }

    public long PerSecond()
    {
        long total = 0;
        foreach (var u in upgrades) total += u.perSecondBonus * u.level;
        return total;
    }

    public void Click()
    {
        float currentTime = Time.unscaledTime;
        clickTimestamps.Enqueue(currentTime);

        while (clickTimestamps.Count > samplesToTest)
        {
            clickTimestamps.Dequeue();
        }

        if (clickTimestamps.Count >= samplesToTest)
        {
            if (!IsClickLegitimate())
            {
                return;
            }
        }

        long gained = ClickPower();
        Brownies += gained;
        TotalEarned += gained;
        OnStateChanged?.Invoke();
    }

    private bool IsClickLegitimate()
    {
        float[] timestamps = clickTimestamps.ToArray();
        float totalIntervals = 0f;
        List<float> intervals = new List<float>();

        // Maps out click intervals
        for (int i = 1; i < timestamps.Length; i++)
        {
            float interval = timestamps[i] - timestamps[i - 1];
            intervals.Add(interval);
            totalIntervals += interval;
        }

        float averageInterval = totalIntervals / intervals.Count;
        float currentCPS = 1f / averageInterval;

        // Speed Limit
        if (currentCPS > maxClicksPerSecond)
        {
            Debug.LogWarning($"[Anti-Cheat] Click blocked. CPS too high: {currentCPS:F1}");
            return false;
        }

        // Punishes uniform clicks
        float variance = 0f;
        foreach (float interval in intervals)
        {
            variance += Mathf.Abs(interval - averageInterval);
        }
        variance /= intervals.Count;

        if (variance < suspiciousVarianceThreshold)
        {
            Debug.LogError($"[Anti-Cheat] Auto-clicker detected! Inhuman click timing variance: {variance:F5}s");
            PunishCheater();
            return false;
        }

        return true;
    }

    private void PunishCheater()
    {

        Debug.LogWarning("Cheater flagged! Taking actions...");
    }

    public bool TryBuy(Upgrade u)
    {
        if (Brownies < u.CurrentCost) return false;
        Brownies -= u.CurrentCost;
        u.level++;
        OnStateChanged?.Invoke();
        return true;
    }

    double passiveCarry; // Fractional brownies that haven't added up to a whole one yet

    void Update()
    {
        // Earn a tiny slice each frame. Brownies is a whole number, so keep the remainder in the carry.
        passiveCarry += PerSecond() * (double)Time.deltaTime;
        long whole = (long)passiveCarry;
        if (whole > 0)
        {
            passiveCarry -= whole;
            Brownies += whole;
            TotalEarned += whole;
            OnStateChanged?.Invoke(); // Only refresh the UI when the total actually changed
        }
    }

    // --- Saving: write on pause/quit instead of every click ---
    void OnApplicationPause(bool paused) { if (paused) Save(); }
    void OnApplicationQuit() { Save(); }

    void Save()
    {
        PlayerPrefs.SetString("brownies", Brownies.ToString());
        PlayerPrefs.SetString("totalEarned", TotalEarned.ToString());
        for (int i = 0; i < upgrades.Count; i++)
            PlayerPrefs.SetInt("upgrade_" + i, upgrades[i].level);
        PlayerPrefs.Save();
    }

    void Load()
    {
        long.TryParse(PlayerPrefs.GetString("brownies", "0"), out long saved);
        Brownies = saved;

        long.TryParse(PlayerPrefs.GetString("totalEarned", "0"), out long savedTotal);
        TotalEarned = savedTotal;

        for (int i = 0; i < upgrades.Count; i++)
            upgrades[i].level = PlayerPrefs.GetInt("upgrade_" + i, 0);
    }
}
