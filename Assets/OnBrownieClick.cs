using UnityEngine;

public class OnBrownieClick : MonoBehaviour
{
    public void OnButtonClick()
    {
        GameManager.Instance.Click();
    }
}