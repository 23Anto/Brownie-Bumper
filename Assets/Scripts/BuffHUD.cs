using UnityEngine;
using TMPro;

public class BuffHUD : MonoBehaviour
{
    [SerializeField] TMP_Text buffText;   // lists active buffs
    [SerializeField] TMP_Text toastText;  // must NOT be on this script's own GameObject
    float toastUntil;

    void OnEnable()  { GoldenBrownie.OnReward += ShowToast; }
    void OnDisable() { GoldenBrownie.OnReward -= ShowToast; }

    void ShowToast(string msg)
    {
        toastText.text = msg;
        toastText.gameObject.SetActive(true);
        toastUntil = Time.unscaledTime + 3f;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        string s = "";
        if (gm.ProductionBuffRemaining > 0f) s += $"Frenzy x{gm.ProductionMultiplier:0}  {gm.ProductionBuffRemaining:0}s\n";
        if (gm.ClickBuffRemaining > 0f)      s += $"Click Frenzy x{gm.ClickMultiplier:0}  {gm.ClickBuffRemaining:0}s";
        buffText.text = s;

        if (toastText.gameObject.activeSelf && Time.unscaledTime > toastUntil)
            toastText.gameObject.SetActive(false);
    }
}