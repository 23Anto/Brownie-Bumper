using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeButton : MonoBehaviour
{
    public int upgradeIndex;      // Which entry in GameManager.upgrades
    public TMP_Text label;
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
    }
}