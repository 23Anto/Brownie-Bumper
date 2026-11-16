using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    public int upgradeIndex;
    public TMP_Text label;
    public TMP_Text gainText;    // NEW: small text under the button
    public Button button;

    void Start()
    {
        button.onClick.AddListener(() => GameManager.Instance.TryBuy(GameManager.Instance.upgrades[upgradeIndex]));
        GameManager.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh; }

    void Refresh()
    {
        var u = GameManager.Instance.upgrades[upgradeIndex];
        label.text = $"{u.name} (Lv {u.level})\nCost: {u.CurrentCost}";
        button.interactable = GameManager.Instance.Brownies >= u.CurrentCost; // Grey out if unaffordable

        if (gainText != null) gainText.text = GainText(u); // NEW
    }

    // NEW: what one level of this upgrade gives
    string GainText(Upgrade u)
    {
        string s = "";
        if (u.perSecondBonus > 0) s += $"+{u.perSecondBonus:N0} per second";
        if (u.clickBonus > 0) s += (s.Length > 0 ? "\n" : "") + $"+{u.clickBonus:N0} per click";
        return s;
    }
}