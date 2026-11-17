using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public class Upgrade
{
    public string name;
    [TextArea(2, 4)] public string description;   // NEW: shown in the tooltip
    public long baseCost = 10;
    public float costMultiplier = 1.15f;
    public long clickBonus = 0;
    public long perSecondBonus = 0;
    [HideInInspector] public int level;
    [HideInInspector] public double produced;     // NEW: lifetime brownies from this upgrade

    public long CurrentCost => (long)(baseCost * Mathf.Pow(costMultiplier, level));
}

public enum BuffType { Production, Click }

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
        return (long)(power * ClickMultiplier);
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
    
        float cm = ClickMultiplier;
        foreach (var u in upgrades) u.produced += u.clickBonus * u.level * cm;
    }

    // --- Golden brownie buffs (temporary, never saved) ---
    float prodMult = 1f, clickMult = 1f;
    float prodBuffEnd, clickBuffEnd; // Time.unscaledTime deadlines

    public float ProductionMultiplier => Time.unscaledTime < prodBuffEnd ? prodMult : 1f;
    public float ClickMultiplier      => Time.unscaledTime < clickBuffEnd ? clickMult : 1f;
    public float ProductionBuffRemaining => Mathf.Max(0f, prodBuffEnd - Time.unscaledTime);
    public float ClickBuffRemaining      => Mathf.Max(0f, clickBuffEnd - Time.unscaledTime);

    public void GrantBrownies(long amount)
    {
        if (amount <= 0) return;
        Brownies += amount;
        TotalEarned += amount;
        OnStateChanged?.Invoke();
    }

    public void ApplyBuff(BuffType type, float multiplier, float seconds)
    {
        float end = Time.unscaledTime + seconds;
        if (type == BuffType.Production) { prodMult = multiplier; prodBuffEnd = end; }
        else                             { clickMult = multiplier; clickBuffEnd = end; }
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
    [SerializeField] float autosaveSeconds = 20f;
    float autosaveTimer;

    void Update()
    {
        // Earn a tiny slice each frame. Brownies is a whole number, so keep the remainder in the carry.
        passiveCarry += PerSecond() * ProductionMultiplier * (double)Time.deltaTime;

        float pm = ProductionMultiplier;
        foreach (var u in upgrades) u.produced += u.perSecondBonus * u.level * pm * (double)Time.deltaTime;

        long whole = (long)passiveCarry;
        if (whole > 0)
        {
            passiveCarry -= whole;
            Brownies += whole;
            TotalEarned += whole;
            OnStateChanged?.Invoke(); // Only refresh the UI when the total actually changed
        }

        autosaveTimer += Time.unscaledDeltaTime;
        
        if (autosaveTimer >= autosaveSeconds)
        {
            autosaveTimer = 0f;
            Save();
        }
    }


    // --- Secure Saving: write values and an authentication tag ---
    void OnApplicationPause(bool paused) { if (paused) Save(); }
    void OnApplicationQuit() { Save(); }

    public void Save()
    {
        // 1. Convert data to strings
        string browniesStr = Brownies.ToString();
        string totalEarnedStr = TotalEarned.ToString();

        // 2. Build a combined payload of ALL critical progression data
        // This ensures a cheater can't swap a high score from one value into another
        System.Text.StringBuilder payloadBuilder = new System.Text.StringBuilder();
        payloadBuilder.Append(browniesStr).Append("|").Append(totalEarnedStr);

        for (int i = 0; i < upgrades.Count; i++)
        {
            payloadBuilder.Append("|").Append(upgrades[i].level);
            PlayerPrefs.SetInt("upgrade_" + i, upgrades[i].level);
            PlayerPrefs.SetString("produced_" + i, upgrades[i].produced.ToString("R", CultureInfo.InvariantCulture));
        }

        // 3. Generate a single cryptographic signature for this exact game state
        string signature = SaveGuard.Sign(payloadBuilder.ToString());

        // 4. Commit values and signature safely to PlayerPrefs
        PlayerPrefs.SetString("brownies", browniesStr);
        PlayerPrefs.SetString("totalEarned", totalEarnedStr);
        PlayerPrefs.SetString("save_signature", signature); 
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        if (Instance == this) Save();   // covers any scene change
    }   
    public void Load()
    {
        // 1. Gather all current values sitting in PlayerPrefs
        string browniesStr = PlayerPrefs.GetString("brownies", "0");
        string totalEarnedStr = PlayerPrefs.GetString("totalEarned", "0");
        string savedSignature = PlayerPrefs.GetString("save_signature", "");

        // If no signature exists yet, it's a completely brand new local save file
        if (string.IsNullOrEmpty(savedSignature))
        {
            ResetToDefault();
            return;
        }

        // 2. Reconstruct the payload EXACTLY how it was built during Save()
        System.Text.StringBuilder payloadBuilder = new System.Text.StringBuilder();
        payloadBuilder.Append(browniesStr).Append("|").Append(totalEarnedStr);

        int[] cachedLevels = new int[upgrades.Count];
        for (int i = 0; i < upgrades.Count; i++)
        {
            cachedLevels[i] = PlayerPrefs.GetInt("upgrade_" + i, 0);
            payloadBuilder.Append("|").Append(cachedLevels[i]);
        }

        // 3. Calculate what the signature SHOULD be based on the local data
        string computedSignature = SaveGuard.Sign(payloadBuilder.ToString());

        // 4. Verify match. If they don't match, someone edited the text values!
        if (savedSignature != computedSignature)
        {
            Debug.LogError("[SaveGuard] Integrity check failed! PlayerPrefs data has been illegally modified.");
            HandleTamperedSave();
            return;
        }

        // 5. If clean, parse strings back into active game variables safely
        long.TryParse(browniesStr, out long savedBrownies);
        Brownies = savedBrownies;

        long.TryParse(totalEarnedStr, out long savedTotal);
        TotalEarned = savedTotal;

        for (int i = 0; i < upgrades.Count; i++)
        {
            upgrades[i].level = cachedLevels[i];
        }

        for (int i = 0; i < upgrades.Count; i++)
        {
            double.TryParse(PlayerPrefs.GetString("produced_" + i, "0"),
                NumberStyles.Float, CultureInfo.InvariantCulture, out double p);
            upgrades[i].produced = p;
        }
    }

    private void ResetToDefault()
    {
        Brownies = 0;
        TotalEarned = 0;
        for (int i = 0; i < upgrades.Count; i++) 
        {
        upgrades[i].level = 0;
        upgrades[i].produced = 0;
        }
    }

    private void HandleTamperedSave()
    {
        // Punish the edit (e.g., wipe their file completely, or lock their leaderboard access)
        ResetToDefault();
        Save(); // Overwrites the corrupted data with clean 0 values
        Debug.LogWarning("[SaveGuard] Tampered data cleared. Progression reset to zero.");
    }
}