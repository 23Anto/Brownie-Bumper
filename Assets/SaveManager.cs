using System.Collections;
using UnityEngine;
using TMPro;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Autosave Settings")]
    [SerializeField] private float saveIntervalSeconds = 20f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(AutoSaveLoop());
    }

    private IEnumerator AutoSaveLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(saveIntervalSeconds);
            ExecuteSave();
        }
    }

    public void ExecuteSave()
    {
        PlayerPrefs.Save();
        Debug.Log($"[SaveManager] Autosaved state to disk at {System.DateTime.Now:T}");
    }

    private void OnApplicationQuit()
    {
        ExecuteSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) ExecuteSave();
    }

    public static string FormatNumber(double value)
    {
        if (value >= 1e12) return (value / 1e12).ToString("F2") + "T";
        if (value >= 1e9) return (value / 1e9).ToString("F2") + "B";
        if (value >= 1e6) return (value / 1e6).ToString("F2") + "M";
        if (value >= 1e3) return (value / 1e3).ToString("F2") + "K";
        return value.ToString("N0");
    }
}