using UnityEngine;

public class OnBrownieClick : MonoBehaviour
{
    public BrownieBounce bounce; 

    public void OnButtonClick()
    {
        GameManager.Instance.Click();
        if (bounce != null) bounce.Bounce();
    }
}