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

    // Cost scales exponentially so early upgrades feel cheap and late ones feel earned
    public long CurrentCost => (long)(baseCost * Mathf.Pow(costMultiplier, level));
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public List<Upgrade> upgrades = new List<Upgrade>(); // Configure in the Inspector
    public long Brownies { get; private set; }
    public long TotalEarned { get; private set; } // Lifetime brownies, used for the leaderboard
    public event Action OnStateChanged; // UI subscribes instead of being called directly

    float passiveTimer;

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
        long gained = ClickPower();
        Brownies += gained;
        TotalEarned += gained;
        OnStateChanged?.Invoke();
    }

    public bool TryBuy(Upgrade u)
    {
        if (Brownies < u.CurrentCost) return false;
        Brownies -= u.CurrentCost;
        u.level++;
        OnStateChanged?.Invoke();
        return true;
    }

    void Update()
    {
        // Passive income ticks once per second
        passiveTimer += Time.deltaTime;
        if (passiveTimer >= 1f)
        {
            passiveTimer -= 1f;
            long passive = PerSecond();
            Brownies += passive;
            TotalEarned += passive;
            OnStateChanged?.Invoke();
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