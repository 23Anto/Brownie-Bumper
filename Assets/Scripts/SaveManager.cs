using System;
using System.Globalization;
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
        if (pauseStatus)
        {
            ExecuteSave();
        }
    }

    static readonly string[] NumberSuffixes = { "", "K", "M", "B", "T", "Q" };

    public static string FormatNumber(double value)
    {
        if (double.IsNaN(value)) return "0";
        if (double.IsInfinity(value)) value = value < 0d ? long.MinValue : long.MaxValue;

        double amount = Math.Abs(value);
        if (amount < 1d) return "0";
        string sign = value < 0d ? "-" : "";
        if (amount < 1000d)
            return sign + Math.Floor(amount).ToString("0", CultureInfo.InvariantCulture);

        int unit = 0;
        while (amount >= 1000d && unit < NumberSuffixes.Length - 1)
        {
            amount /= 1000d;
            unit++;
        }
        if (amount >= 1000000d)
            return sign + amount.ToString("0.##e+0", CultureInfo.InvariantCulture) + NumberSuffixes[unit];

        amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (amount >= 1000d && unit < NumberSuffixes.Length - 1)
        {
            amount /= 1000d;
            unit++;
        }
        return sign + amount.ToString("#,0.##", CultureInfo.InvariantCulture) + NumberSuffixes[unit];
    }
}