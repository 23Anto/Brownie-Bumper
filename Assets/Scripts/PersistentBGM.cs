using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class PersistentBGM : MonoBehaviour
{
    private static PersistentBGM instance;

    private const string VolumeKey = "settings_volume";
    private const string MuteKey = "settings_mute";

    private void Awake()
    {
        AudioSource music = GetComponent<AudioSource>();
        music.playOnAwake = false;

        if (instance != null && instance != this)
        {
            music.Stop();
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        music.loop = true;
        music.spatialBlend = 0f;
        music.ignoreListenerVolume = false;

        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        bool muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        AudioListener.volume = muted ? 0f : savedVolume;

        if (music.clip != null && !music.isPlaying)
        {
            music.Play();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}

