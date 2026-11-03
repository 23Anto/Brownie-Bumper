using UnityEngine;

public class ShowResolution : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Window Width: " + Screen.width + ", Height: " + Screen.height);
        Debug.Log("Screen Resolution: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height);
    }
}
