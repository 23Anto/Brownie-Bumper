using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// =====================================================================================
//  SAVE SYSTEM + SAVEGUARD (HMAC) TAMPER DETECTION
//  Order matters in the tamper tests: the first GameManager must be destroyed BEFORE the
//  save is edited, because destroying it triggers a fresh, validly-signed Save().
// =====================================================================================
public class SaveProtectionTests : GameTestBase
{
    static Upgrade[] TwoUpgrades() => new[]
    {
        MakeUpgrade("Spatula", clickBonus: 2),
        MakeUpgrade("Oven", perSecondBonus: 3)
    };

    /// <summary>Plays a short session, saves, and closes the game. Leaves a valid signed save behind.</summary>
    GameManager PlaySessionAndSave()
    {
        var gm = CreateGame(TwoUpgrades());
        gm.GrantBrownies(1000);
        gm.TryBuy(gm.upgrades[0]);
        gm.TryBuy(gm.upgrades[0]);
        gm.Save();
        return gm;
    }

    [Test] // TC-22
    public void SaveThenLoad_RestoresBrowniesTotalAndUpgradeLevels()
    {
        var first = PlaySessionAndSave();
        long brownies = first.Brownies, total = first.TotalEarned;
        Assert.AreEqual(2, first.upgrades[0].level);
        DestroyGame(first);

        var second = CreateGame(TwoUpgrades());     // "relaunch the game"

        Assert.AreEqual(brownies, second.Brownies);
        Assert.AreEqual(total, second.TotalEarned);
        Assert.AreEqual(2, second.upgrades[0].level);
        Assert.AreEqual(0, second.upgrades[1].level);
    }

    [Test] // TC-23
    public void LeavingTheScene_SavesProgress()      // regression test for "data disappears after returning from the leaderboard"
    {
        var first = CreateGame(TwoUpgrades());
        first.GrantBrownies(250);
        DestroyGame(first);                          // scene change destroys the object; no explicit Save() call

        var second = CreateGame(TwoUpgrades());
        Assert.AreEqual(250, second.Brownies);
        Assert.AreEqual(250, second.TotalEarned);
    }

    [Test] // TC-24
    public void ProducedStat_IsSavedAndRestored()
    {
        var first = CreateGame(TwoUpgrades());
        first.upgrades[0].level = 3;                 // click bonus 2 * level 3 = 6 per click
        ClickAfter(first, 0.2f);
        Assert.AreEqual(6.0, first.upgrades[0].produced, 1e-9);
        first.Save();
        DestroyGame(first);

        var second = CreateGame(TwoUpgrades());
        Assert.AreEqual(6.0, second.upgrades[0].produced, 1e-9);
    }

    [TestCase("brownies", "999999999")]
    [TestCase("totalEarned", "999999999999")]        // the leaderboard score
    [TestCase("save_signature", "not-a-real-signature")] // TC-25
    public void EditedSaveString_IsDetected_AndProgressIsReset(string key, string newValue)
    {
        DestroyGame(PlaySessionAndSave());
        PlayerPrefs.SetString(key, newValue);        // the "cheat"
        LogAssert.Expect(LogType.Error, new Regex("Integrity check failed"));

        var reloaded = CreateGame(TwoUpgrades());

        Assert.AreEqual(0, reloaded.Brownies);
        Assert.AreEqual(0, reloaded.TotalEarned);
        Assert.AreEqual(0, reloaded.upgrades[0].level);
    }

    [Test] // TC-26
    public void EditedUpgradeLevel_IsDetected_AndProgressIsReset()
    {
        DestroyGame(PlaySessionAndSave());
        PlayerPrefs.SetInt("upgrade_1", 99);         // give myself 99 ovens
        LogAssert.Expect(LogType.Error, new Regex("Integrity check failed"));

        var reloaded = CreateGame(TwoUpgrades());

        Assert.AreEqual(0, reloaded.upgrades[1].level);
        Assert.AreEqual(0, reloaded.Brownies);
    }

    [Test] // TC-27
    public void AfterATamperReset_TheCleanSaveLoadsWithoutFurtherErrors()
    {
        DestroyGame(PlaySessionAndSave());
        PlayerPrefs.SetString("brownies", "5");
        LogAssert.Expect(LogType.Error, new Regex("Integrity check failed"));
        var afterReset = CreateGame(TwoUpgrades());
        DestroyGame(afterReset);

        // no LogAssert.Expect here: a second error log would fail this test
        var again = CreateGame(TwoUpgrades());
        Assert.AreEqual(0, again.Brownies);
    }

    [Test] // TC-28
    public void MissingSignature_IsTreatedAsANewGame_NotAsTampering()
    {
        DestroyGame(PlaySessionAndSave());
        PlayerPrefs.DeleteKey("save_signature");

        var reloaded = CreateGame(TwoUpgrades());    // must not log an error

        Assert.AreEqual(0, reloaded.Brownies);
        Assert.AreEqual(0, reloaded.TotalEarned);
    }

    [Test] // TC-29  KNOWN LIMITATION: update this test when save versioning is added
    public void AddingAnUpgrade_InvalidatesExistingSaves()
    {
        DestroyGame(PlaySessionAndSave());           // saved with 2 upgrades
        LogAssert.Expect(LogType.Error, new Regex("Integrity check failed"));

        var withThree = CreateGame(MakeUpgrade("Spatula", clickBonus: 2),
                                   MakeUpgrade("Oven", perSecondBonus: 3),
                                   MakeUpgrade("Factory", perSecondBonus: 20));

        Assert.AreEqual(0, withThree.Brownies);      // old saves are wiped because the signed payload changed
    }

    [Test] // TC-30
    public void ActiveBuffs_AreNotSaved()
    {
        var first = CreateGame(TwoUpgrades());
        first.ApplyBuff(BuffType.Click, 10f, 600f);
        first.Save();
        DestroyGame(first);

        var second = CreateGame(TwoUpgrades());
        Assert.AreEqual(1f, second.ClickMultiplier);
        Assert.AreEqual(1f, second.ProductionMultiplier);
    }
}

// =====================================================================================
//  SaveGuard.Sign directly
// =====================================================================================
public class SaveGuardTests
{
    [Test] // TC-31
    public void Sign_IsDeterministic()
    {
        Assert.AreEqual(SaveGuard.Sign("10|20|1|2"), SaveGuard.Sign("10|20|1|2"));
    }

    [Test] // TC-32
    public void Sign_ChangesWhenAnyPartOfThePayloadChanges()
    {
        string baseline = SaveGuard.Sign("10|20|1|2");
        Assert.AreNotEqual(baseline, SaveGuard.Sign("11|20|1|2"));   // brownies
        Assert.AreNotEqual(baseline, SaveGuard.Sign("10|21|1|2"));   // total earned
        Assert.AreNotEqual(baseline, SaveGuard.Sign("10|20|1|3"));   // an upgrade level
    }

    [Test] // TC-33
    public void Sign_ReturnsABase64Sha256Tag()
    {
        Assert.AreEqual(44, SaveGuard.Sign("anything").Length);       // 32 bytes -> 44 base64 chars
    }
}
