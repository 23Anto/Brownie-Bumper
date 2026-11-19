using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject SettingsPanel;
    [SerializeField] private Slider VolumeSlider;
    [SerializeField] private Toggle MuteToggle;
    [SerializeField] private Slider SfxVolumeSlider;
    [SerializeField] private Toggle SfxMuteToggle;

    private bool settingsChanged;

    private void Start()
    {
        ReadSettings();
        VolumeSlider.onValueChanged.AddListener(_ => ApplyBgm());
        MuteToggle.onValueChanged.AddListener(_ => ApplyBgm());
        if (SfxVolumeSlider != null && SfxMuteToggle != null)
        {
            SfxVolumeSlider.onValueChanged.AddListener(_ => ApplySfx());
            SfxMuteToggle.onValueChanged.AddListener(_ => ApplySfx());
        }

        SettingsPanel.SetActive(false);
    }

    private void ReadSettings()
    {
        VolumeSlider.SetValueWithoutNotify(GameAudioPrefs.BgmVolume);
        MuteToggle.SetIsOnWithoutNotify(GameAudioPrefs.BgmMuted);
        if (SfxVolumeSlider != null && SfxMuteToggle != null)
        {
            SfxVolumeSlider.SetValueWithoutNotify(GameAudioPrefs.SfxVolume);
            SfxMuteToggle.SetIsOnWithoutNotify(GameAudioPrefs.SfxMuted);
        }
    }

    private void ApplyBgm()
    {
        PlayerPrefs.SetFloat(GameAudioPrefs.BgmVolumeKey, VolumeSlider.value);
        PlayerPrefs.SetInt(GameAudioPrefs.BgmMuteKey, MuteToggle.isOn ? 1 : 0);
        PersistentBGM.ApplyAudioSettings();
        settingsChanged = true;
    }

    private void ApplySfx()
    {
        PlayerPrefs.SetFloat(GameAudioPrefs.SfxVolumeKey, SfxVolumeSlider.value);
        PlayerPrefs.SetInt(GameAudioPrefs.SfxMuteKey, SfxMuteToggle.isOn ? 1 : 0);
        MenuSfx.ApplyAudioSettings();
        settingsChanged = true;
    }

    public void Open()
    {
        ReadSettings();
        SettingsPanel.SetActive(true);
    }

    public void Close()
    {
        SettingsPanel.SetActive(false);
        SaveSettings();
    }

    private void OnDestroy() => SaveSettings();

    private void SaveSettings()
    {
        if (!settingsChanged) return;
        PlayerPrefs.Save();
        settingsChanged = false;
    }
}

internal static class GameAudioPrefs
{
    // The original audio preferences now apply to background music.
    internal const string BgmVolumeKey = "settings_volume";
    internal const string BgmMuteKey = "settings_mute";
    internal const string SfxVolumeKey = "settings_sfx_volume";
    internal const string SfxMuteKey = "settings_sfx_mute";
    internal const float DefaultSfxVolume = 0.22f;

    internal static float BgmVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
    internal static bool BgmMuted => PlayerPrefs.GetInt(BgmMuteKey, 0) == 1;
    internal static float SfxVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
    internal static bool SfxMuted => PlayerPrefs.GetInt(SfxMuteKey, 0) == 1;
}
