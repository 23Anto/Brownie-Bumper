using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Shared setup for every Brownie Bumper test.
///  - Gives each test a fake clock (so click timing and buff expiry are deterministic).
///  - Snapshots and restores the developer's real PlayerPrefs save, so running the
///    tests never wipes (or inherits) your own progress.
/// </summary>
public abstract class GameTestBase
{
    protected float now;   // fake clock, in seconds. Tests advance it by hand.

    const int MaxUpgradeSlots = 10;
    readonly List<GameObject> spawned = new List<GameObject>();
    readonly Dictionary<string, string> savedStrings = new Dictionary<string, string>();
    readonly Dictionary<string, int> savedInts = new Dictionary<string, int>();

    static readonly string[] StringKeys = BuildStringKeys();
    static readonly string[] IntKeys = BuildIntKeys();

    static string[] BuildStringKeys()
    {
        var keys = new List<string> { "brownies", "totalEarned", "save_signature" };
        for (int i = 0; i < MaxUpgradeSlots; i++) keys.Add("produced_" + i);
        return keys.ToArray();
    }

    static string[] BuildIntKeys()
    {
        var keys = new List<string>();
        for (int i = 0; i < MaxUpgradeSlots; i++) keys.Add("upgrade_" + i);
        return keys.ToArray();
    }

    [SetUp]
    public void BaseSetUp()
    {
        now = 0f;
        savedStrings.Clear();
        savedInts.Clear();
        foreach (var k in StringKeys) if (PlayerPrefs.HasKey(k)) savedStrings[k] = PlayerPrefs.GetString(k);
        foreach (var k in IntKeys)    if (PlayerPrefs.HasKey(k)) savedInts[k]    = PlayerPrefs.GetInt(k);
        ClearGameKeys();   // every test starts as a brand-new player
    }

    [TearDown]
    public void BaseTearDown()
    {
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);   // runs OnDestroy -> Save()
        spawned.Clear();

        ClearGameKeys();                                    // throw away the test data...
        foreach (var kv in savedStrings) PlayerPrefs.SetString(kv.Key, kv.Value);
        foreach (var kv in savedInts)    PlayerPrefs.SetInt(kv.Key, kv.Value);   // ...and put the real save back
        PlayerPrefs.Save();
    }

    static void ClearGameKeys()
    {
        foreach (var k in StringKeys) PlayerPrefs.DeleteKey(k);
        foreach (var k in IntKeys)    PlayerPrefs.DeleteKey(k);
    }

    // ---- helpers -------------------------------------------------------------

    protected static Upgrade MakeUpgrade(string name, long baseCost = 10, float costMultiplier = 1.15f,
                                         long clickBonus = 0, long perSecondBonus = 0)
    {
        return new Upgrade
        {
            name = name,
            baseCost = baseCost,
            costMultiplier = costMultiplier,
            clickBonus = clickBonus,
            perSecondBonus = perSecondBonus
        };
    }

    /// <summary>Creates a GameManager with the given upgrades. Awake (and therefore Load) runs here.</summary>
    protected GameManager CreateGame(params Upgrade[] upgrades)
    {
        var go = new GameObject("GameManager (test)");
        go.SetActive(false);                       // hold Awake back until the upgrade list is set
        var gm = go.AddComponent<GameManager>();
        gm.upgrades = new List<Upgrade>(upgrades);
        gm.Clock = () => now;
        go.SetActive(true);                        // Awake -> Load happens now
        spawned.Add(go);
        return gm;
    }

    protected static void DestroyGame(GameManager gm)
    {
        Object.DestroyImmediate(gm.gameObject);    // runs OnDestroy -> Save()
    }

    /// <summary>Advance the fake clock, then click once.</summary>
    protected void ClickAfter(GameManager gm, float seconds)
    {
        now += seconds;
        gm.Click();
    }
}
