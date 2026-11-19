using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Keep the signed-long save and leaderboard format without wrapping to negatives.
public static class BrownieMath
{
    public static long Add(long left, long right) =>
        left > long.MaxValue - right ? long.MaxValue : left + right;

    public static long Multiply(long value, int count)
    {
        if (value <= 0 || count <= 0) return 0;
        return value > long.MaxValue / count ? long.MaxValue : value * count;
    }

    public static long FromDouble(double value)
    {
        if (!(value > 0d)) return 0;
        return value >= (double)long.MaxValue ? long.MaxValue : (long)value;
    }
}

[Serializable]
public class Upgrade
{
    public string name;
    [TextArea(2, 4)] public string description;   // NEW: shown in the tooltip
    public long baseCost = 10;
    public double costMultiplier = 1.15d;
    public long clickBonus = 0;
    public long perSecondBonus = 0;
    [HideInInspector] public int level;
    [HideInInspector] public double produced;     // NEW: lifetime brownies from this upgrade

    public long CurrentCost => TryGetCost(out long cost) ? cost : long.MaxValue;

    public bool TryGetCost(out long cost)
    {
        cost = 0;
        if (baseCost < 0 || level < 0 || level == int.MaxValue
            || !(costMultiplier > 0d) || double.IsInfinity(costMultiplier))
            return false;
        if (level == 0 || costMultiplier == 1d || baseCost == 0)
        {
            cost = baseCost;
            return true;
        }

        double price = baseCost * Math.Pow(costMultiplier, level);
        if (!(price >= 0d)) return false;
        // Round up the geometric price and keep it within the supported balance range.
        cost = BrownieMath.FromDouble(Math.Ceiling(price));
        return true;
    }
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

    public Func<float> Clock = () => Time.unscaledTime;

    [Header("Anti-Cheat Settings")]
    [SerializeField] private int maxClicksPerSecond = 15;            // Max speed allowed
    [SerializeField] private float suspiciousVarianceThreshold = 0.002f; // Inhuman precision (2ms)
    [SerializeField] private int samplesToTest = 10;                 // Clicks needed to run the check
    
    private Queue<float> clickTimestamps = new Queue<float>();

    void Awake() { Instance = this; Load(); }

    public long ClickPower()
    {
        long power = 1;
        foreach (var u in upgrades)
            power = BrownieMath.Add(power, BrownieMath.Multiply(u.clickBonus, u.level));
        float multiplier = ClickMultiplier;
        return multiplier == 1f ? power : BrownieMath.FromDouble((double)power * multiplier);
    }

    public long PerSecond()
    {
        long total = 0;
        foreach (var u in upgrades)
            total = BrownieMath.Add(total, BrownieMath.Multiply(u.perSecondBonus, u.level));
        return total;
    }

    public double CurrentProductionRate => (double)PerSecond() * ProductionMultiplier;

    public void Click()
    {
        float currentTime = Clock();
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

        GrantBrownies(ClickPower());
    
        float cm = ClickMultiplier;
        foreach (var u in upgrades) u.produced += (double)u.clickBonus * u.level * cm;
    }

    // --- Golden brownie buffs (temporary, never saved) ---
    float prodMult = 1f, clickMult = 1f;
    float prodBuffEnd, clickBuffEnd; // Clock() deadlines

    public float ProductionMultiplier => Clock() < prodBuffEnd ? prodMult : 1f;
    public float ClickMultiplier      => Clock() < clickBuffEnd ? clickMult : 1f;
    public float ProductionBuffRemaining => Mathf.Max(0f, prodBuffEnd - Clock());
    public float ClickBuffRemaining      => Mathf.Max(0f, clickBuffEnd - Clock());

    public void GrantBrownies(long amount)
    {
        if (amount <= 0) return;
        Brownies = BrownieMath.Add(Brownies, amount);
        TotalEarned = BrownieMath.Add(TotalEarned, amount);
        OnStateChanged?.Invoke();
    }

    public void ApplyBuff(BuffType type, float multiplier, float seconds)
    {
        float end = Clock() + seconds;
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
        if (u == null || !u.TryGetCost(out long cost) || Brownies < cost) return false;
        Brownies -= cost;
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
        passiveCarry += CurrentProductionRate * Time.deltaTime;

        float pm = ProductionMultiplier;
        foreach (var u in upgrades) u.produced += (double)u.perSecondBonus * u.level * pm * Time.deltaTime;

        double whole = Math.Floor(passiveCarry);
        if (whole > 0d)
        {
            passiveCarry = double.IsInfinity(whole) ? 0d : passiveCarry - whole;
            GrantBrownies(BrownieMath.FromDouble(whole));
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
        long.TryParse(totalEarnedStr, out long savedTotal);
        TotalEarned = savedTotal < 0 ? long.MaxValue : savedTotal;

        // A legacy purchase could set the sign bit by subtracting a wrapped price.
        // Keep the remaining balance, bounded by the verified lifetime earnings.
        Brownies = savedBrownies < 0
            ? Math.Min(savedBrownies - long.MinValue, TotalEarned)
            : savedBrownies;

        for (int i = 0; i < upgrades.Count; i++)
        {
            upgrades[i].level = Math.Max(0, cachedLevels[i]);
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