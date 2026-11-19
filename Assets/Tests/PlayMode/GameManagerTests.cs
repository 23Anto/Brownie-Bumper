using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// =====================================================================================
//  ECONOMY: clicking, upgrades, costs, passive income
// =====================================================================================
public class EconomyTests : GameTestBase
{
    [Test] // TC-01
    public void NewGame_StartsAtZero()
    {
        var gm = CreateGame();
        Assert.AreEqual(0, gm.Brownies);
        Assert.AreEqual(0, gm.TotalEarned);
    }

    [Test] // TC-02
    public void ClickPower_WithNoUpgrades_IsOne()
    {
        var gm = CreateGame();
        Assert.AreEqual(1, gm.ClickPower());
    }

    [Test] // TC-03
    public void ClickPower_AddsClickBonusTimesLevelForEveryUpgrade()
    {
        var gm = CreateGame(MakeUpgrade("Spatula", clickBonus: 2), MakeUpgrade("Whisk", clickBonus: 5));
        gm.upgrades[0].level = 3;   // 2 * 3 = 6
        gm.upgrades[1].level = 1;   // 5 * 1 = 5
        Assert.AreEqual(1 + 6 + 5, gm.ClickPower());
    }

    [Test] // TC-04
    public void PerSecond_SumsPassiveBonusTimesLevel()
    {
        var gm = CreateGame(MakeUpgrade("Oven", perSecondBonus: 3), MakeUpgrade("Factory", perSecondBonus: 10));
        gm.upgrades[0].level = 2;   // 6
        gm.upgrades[1].level = 1;   // 10
        Assert.AreEqual(16, gm.PerSecond());
    }

    [Test] // TC-05
    public void Click_AddsClickPowerToBothBrowniesAndTotalEarned()
    {
        var gm = CreateGame(MakeUpgrade("Spatula", clickBonus: 4));
        gm.upgrades[0].level = 1;                 // click power = 1 + 4 = 5
        gm.Click();
        Assert.AreEqual(5, gm.Brownies);
        Assert.AreEqual(5, gm.TotalEarned);
    }

    [Test] // TC-06
    public void UpgradeCost_GrowsExponentiallyWithLevel()
    {
        var u = MakeUpgrade("Oven", baseCost: 10, costMultiplier: 1.15f);
        u.level = 0; Assert.AreEqual(10, u.CurrentCost);   // 10 * 1.15^0
        u.level = 2; Assert.AreEqual(13, u.CurrentCost);   // 10 * 1.15^2 = 13.2
        u.level = 5; Assert.AreEqual(20, u.CurrentCost);   // 10 * 1.15^5 = 20.1
    }

    [Test] // TC-07
    public void TryBuy_WhenAffordable_SpendsBrowniesAndLevelsUp_ButKeepsLifetimeTotal()
    {
        var gm = CreateGame(MakeUpgrade("Oven", baseCost: 10));
        gm.GrantBrownies(10);

        bool bought = gm.TryBuy(gm.upgrades[0]);

        Assert.IsTrue(bought);
        Assert.AreEqual(0, gm.Brownies);          // spent
        Assert.AreEqual(1, gm.upgrades[0].level);
        Assert.AreEqual(10, gm.TotalEarned);      // lifetime total (the leaderboard score) must not drop
    }

    [Test] // TC-08
    public void TryBuy_WhenTooPoor_ChangesNothing()
    {
        var gm = CreateGame(MakeUpgrade("Oven", baseCost: 10));
        gm.GrantBrownies(9);

        bool bought = gm.TryBuy(gm.upgrades[0]);

        Assert.IsFalse(bought);
        Assert.AreEqual(9, gm.Brownies);
        Assert.AreEqual(0, gm.upgrades[0].level);
    }

    [Test] // TC-09
    public void TryBuy_MakesTheNextLevelMoreExpensive()
    {
        var gm = CreateGame(MakeUpgrade("Oven", baseCost: 10, costMultiplier: 1.15f));
        gm.GrantBrownies(100);
        gm.TryBuy(gm.upgrades[0]);                // pays 10
        Assert.AreEqual(11, gm.upgrades[0].CurrentCost);
        Assert.AreEqual(90, gm.Brownies);
    }

    [Test] // TC-10
    public void GrantBrownies_IgnoresZeroAndNegativeAmounts()
    {
        var gm = CreateGame();
        gm.GrantBrownies(0);
        gm.GrantBrownies(-50);
        Assert.AreEqual(0, gm.Brownies);
        Assert.AreEqual(0, gm.TotalEarned);
    }

    [Test] // TC-11
    public void OnStateChanged_FiresOnClickGrantAndPurchase()
    {
        var gm = CreateGame(MakeUpgrade("Oven", baseCost: 1));
        int fired = 0;
        gm.OnStateChanged += () => fired++;

        gm.Click();                    // +1 brownie
        gm.GrantBrownies(5);
        gm.TryBuy(gm.upgrades[0]);

        Assert.AreEqual(3, fired);
    }

    [UnityTest] // TC-12  (the only time-based test: waits ~1.2 real seconds)
    public IEnumerator PassiveIncome_AccruesOverTime()
    {
        var gm = CreateGame(MakeUpgrade("Oven", perSecondBonus: 10));
        gm.upgrades[0].level = 1;                 // 10 brownies per second

        yield return new WaitForSecondsRealtime(1.2f);

        // Expect about 12. The window absorbs frame-timing jitter on slow machines.
        Assert.GreaterOrEqual(gm.Brownies, 9);
        Assert.LessOrEqual(gm.Brownies, 15);
        Assert.AreEqual(gm.Brownies, gm.TotalEarned);   // passive income counts toward the leaderboard score
    }
}

// =====================================================================================
//  ANTI-CHEAT: click-speed limit and autoclicker (timing regularity) detection
//  The check only starts once 10 clicks have been recorded (samplesToTest).
// =====================================================================================
public class AntiCheatTests : GameTestBase
{
    [Test] // TC-13
    public void HumanLikeClicking_IsFullyAccepted()
    {
        var gm = CreateGame();
        // ~4.5 clicks per second with natural, uneven gaps
        float[] gaps = { 0.21f, 0.18f, 0.25f, 0.19f, 0.23f, 0.20f, 0.27f, 0.17f,
                         0.22f, 0.24f, 0.20f, 0.26f, 0.18f, 0.23f };
        foreach (float g in gaps) ClickAfter(gm, g);

        Assert.AreEqual(gaps.Length, gm.Brownies);
        Assert.AreEqual(gaps.Length, gm.TotalEarned);
    }

    [Test] // TC-14
    public void ClickingFasterThanTheLimit_IsBlockedOnceTheCheckStarts()
    {
        var gm = CreateGame();
        // 100 clicks per second, limit is 15
        for (int i = 0; i < 25; i++) ClickAfter(gm, 0.01f);

        // The first 9 clicks happen before the 10-sample check starts (documented behaviour);
        // everything after that is rejected.
        Assert.AreEqual(9, gm.Brownies);
        Assert.AreEqual(9, gm.TotalEarned);
    }

    [Test] // TC-15
    public void PerfectlyRegularClicking_AtLegalSpeed_IsFlaggedAsAutoclicker()
    {
        var gm = CreateGame();
        LogAssert.Expect(LogType.Error, new Regex("Auto-clicker detected"));

        // exactly 10 clicks per second, to the millisecond: under the speed limit, but inhuman
        for (int i = 0; i < 10; i++) ClickAfter(gm, 0.1f);

        Assert.AreEqual(9, gm.Brownies);          // the 10th click triggers the check and is rejected
        Assert.AreEqual(9, gm.TotalEarned);
    }

    [Test] // TC-16
    public void BlockedClicks_DoNotRaiseTheLeaderboardScoreOrUpgradeStats()
    {
        var gm = CreateGame(MakeUpgrade("Spatula", clickBonus: 1));
        gm.upgrades[0].level = 1;                 // click power 2
        for (int i = 0; i < 25; i++) ClickAfter(gm, 0.01f);   // speed-limited

        Assert.AreEqual(9 * 2, gm.TotalEarned);
        Assert.AreEqual(9 * 1, gm.upgrades[0].produced, 1e-9);   // "produced" stat only counts accepted clicks
    }
}

// =====================================================================================
//  GOLDEN BROWNIE BUFFS: temporary multipliers
// =====================================================================================
public class BuffTests : GameTestBase
{
    [Test] // TC-17
    public void ProductionBuff_AppliesThenExpires()
    {
        var gm = CreateGame();
        gm.ApplyBuff(BuffType.Production, 7f, 30f);

        now = 0f;    Assert.AreEqual(7f, gm.ProductionMultiplier);
        now = 29.9f; Assert.AreEqual(7f, gm.ProductionMultiplier);
        now = 30.1f; Assert.AreEqual(1f, gm.ProductionMultiplier);
    }

    [Test] // TC-18
    public void BuffRemaining_CountsDownAndNeverGoesNegative()
    {
        var gm = CreateGame();
        gm.ApplyBuff(BuffType.Click, 10f, 15f);

        now = 5f;  Assert.AreEqual(10f, gm.ClickBuffRemaining, 0.001f);
        now = 99f; Assert.AreEqual(0f, gm.ClickBuffRemaining, 0.001f);
    }

    [Test] // TC-19
    public void ClickBuff_MultipliesClickPower_AndEndsWhenItExpires()
    {
        var gm = CreateGame();
        gm.ApplyBuff(BuffType.Click, 10f, 15f);

        Assert.AreEqual(10, gm.ClickPower());
        gm.Click();
        Assert.AreEqual(10, gm.Brownies);

        now = 15.5f;
        Assert.AreEqual(1, gm.ClickPower());
    }

    [Test] // TC-20
    public void Buffs_AreIndependent_AndDoNotChangeTheBasePerSecondRate()
    {
        var gm = CreateGame(MakeUpgrade("Oven", perSecondBonus: 4));
        gm.upgrades[0].level = 2;                 // base 8 / sec

        gm.ApplyBuff(BuffType.Production, 7f, 30f);

        Assert.AreEqual(1f, gm.ClickMultiplier);  // a production buff must not boost clicks
        Assert.AreEqual(8, gm.PerSecond());       // base rate is unchanged; the multiplier is applied separately
    }

    [Test] // TC-21
    public void ApplyingTheSameBuffAgain_ReplacesItInsteadOfStacking()
    {
        var gm = CreateGame();
        gm.ApplyBuff(BuffType.Click, 10f, 15f);
        gm.ApplyBuff(BuffType.Click, 2f, 5f);

        Assert.AreEqual(2f, gm.ClickMultiplier);  // not 20
        now = 5.5f;
        Assert.AreEqual(1f, gm.ClickMultiplier);
    }
}
