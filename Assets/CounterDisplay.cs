using UnityEngine;
using TMPro;

public class CounterDisplay : MonoBehaviour
{
    public TMP_Text counterText;

    void Start()
    {
        GameManager.Instance.OnStateChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= Refresh;
    }

    void Refresh()
    {
        counterText.text = GameManager.Instance.Brownies.ToString();
    }
}