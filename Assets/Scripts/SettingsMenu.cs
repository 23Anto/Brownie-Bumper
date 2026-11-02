using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] GameObject SettingsPanel;
    [SerializeField] Slider VolumeSlider;
    [SerializeField] Toggle MuteToggle;

    const string VolKey = "settings_volume", MuteKey = "settings_mute";

    // Apply saved audio settings on launch, whichever scene loads first
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnLaunch() =>
        AudioListener.volume = PlayerPrefs.GetInt(MuteKey, 0) == 1 ? 0f : PlayerPrefs.GetFloat(VolKey, 1f);

    void Start()
    {
        VolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VolKey, 1f));
        MuteToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(MuteKey, 0) == 1);
        VolumeSlider.onValueChanged.AddListener(_ => Apply());
        MuteToggle.onValueChanged.AddListener(_ => Apply());
        SettingsPanel.SetActive(false);
    }

    void Apply()
    {
        PlayerPrefs.SetFloat(VolKey, VolumeSlider.value);
        PlayerPrefs.SetInt(MuteKey, MuteToggle.isOn ? 1 : 0);
        AudioListener.volume = MuteToggle.isOn ? 0f : VolumeSlider.value;
    }

    public void Open()  => SettingsPanel.SetActive(true);   // hook SettingsButton here
    public void Close() => SettingsPanel.SetActive(false);  // hook Close button here
}