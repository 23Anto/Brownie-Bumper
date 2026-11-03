using UnityEngine;
using TMPro;

public class OnBrownieClick : MonoBehaviour
{
    // Reference to the UI Text component
    public TMP_Text Counter;

    // Click counter
    public int clickCount = 0;

    void Start()
    {
        clickCount = PlayerPrefs.GetInt("CounterName", 0);
        UpdateUI();
    }

    public void OnButtonClick()
    {
        clickCount++; // Updates the value of Counter

        PlayerPrefs.SetInt("CounterName", clickCount); // Sets save value to amount of clicks
        PlayerPrefs.Save();

        UpdateUI();   
    }

    void UpdateUI()
    {
        if (Counter != null)
        {
            Counter.text = clickCount.ToString(); // Updates the UI through TMP_Text
        }
    }

}
