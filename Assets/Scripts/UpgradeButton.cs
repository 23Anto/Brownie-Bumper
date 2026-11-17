using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UpgradeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int upgradeIndex;
    public TMP_Text label;
    public Button button;

    bool hovering;

    void Start()
    {
        button.onClick.AddListener(() => GameManager.Instance.TryBuy(GameManager.Instance.upgrades[upgradeIndex]));
        GameManager.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    void OnDestroy() { if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh; }

    void OnDisable() { if (hovering) HideTooltip(); }

    void Refresh()
    {
        var u = GameManager.Instance.upgrades[upgradeIndex];
        label.text = $"{u.name} (Lv {u.level})\nCost: {u.CurrentCost}";
        button.interactable = GameManager.Instance.Brownies >= u.CurrentCost;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hovering = true;
        if (TooltipUI.Instance != null)
            TooltipUI.Instance.Show((RectTransform)button.transform, BuildTooltip);
    }

    public void OnPointerExit(PointerEventData e) => HideTooltip();

    void HideTooltip()
    {
        hovering = false;
        if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();
    }

    string BuildTooltip()
    {
        var gm = GameManager.Instance;
        var u = gm.upgrades[upgradeIndex];
        var sb = new StringBuilder();

        sb.AppendLine($"<b><size=130%>{u.name}</size></b>");
        sb.AppendLine($"Owned: <b>{u.level}</b>");
        if (!string.IsNullOrWhiteSpace(u.description))
            sb.AppendLine($"<i>{u.description}</i>");
        sb.AppendLine();

        // What one level gives
        if (u.clickBonus > 0)     sb.AppendLine($"Each level: +{u.clickBonus} per click");
        if (u.perSecondBonus > 0) sb.AppendLine($"Each level: +{u.perSecondBonus} brownies/sec");

        // What you're getting right now
        if (u.level > 0)
        {
            if (u.clickBonus > 0)
                sb.AppendLine($"Currently: +{SaveManager.FormatNumber(u.clickBonus * u.level)} per click");
            if (u.perSecondBonus > 0)
            {
                string line = $"Currently: +{SaveManager.FormatNumber(u.perSecondBonus * u.level)}/sec";
                long total = gm.PerSecond();
                if (total > 0)
                    line += $" ({100.0 * u.perSecondBonus * u.level / total:0}% of income)";
                sb.AppendLine(line);
            }
        }

        sb.AppendLine($"Produced so far: <b>{SaveManager.FormatNumber(u.produced)}</b>");
        sb.AppendLine();

        // Cost / affordability
        long cost = u.CurrentCost;
        if (gm.Brownies >= cost)
        {
            sb.Append($"<color=#7CFC00>Cost: {SaveManager.FormatNumber(cost)}</color>");
        }
        else
        {
            long missing = cost - gm.Brownies;
            sb.Append($"<color=#FF6B6B>Cost: {SaveManager.FormatNumber(cost)}</color>");
            sb.Append($"\nNeed {SaveManager.FormatNumber(missing)} more");
            long ps = gm.PerSecond();
            if (ps > 0) sb.Append($" (~{Mathf.CeilToInt((float)missing / ps)}s)");
        }
        return sb.ToString();
    }
}