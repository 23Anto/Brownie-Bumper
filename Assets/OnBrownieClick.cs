using UnityEngine;
using TMPro;

public class OnBrownieClick : MonoBehaviour
{
    // Reference to the UI Text component
    public TMP_Text Counter;

    // Click counter
    private int clickCount = 0;

    void Start()
    {
        UpdateUI();
    }

    public void OnButtonClick()
    {
        clickCount++; 
        UpdateUI();   
    }

    void UpdateUI()
    {
        Counter.text = clickCount.ToString();
    }

}
