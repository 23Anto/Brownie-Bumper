using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class PersistentBGM : MonoBehaviour
{
    private static PersistentBGM instance;
    private AudioSource music;
    private float baseVolume;

    private void Awake()
    {
        music = GetComponent<AudioSource>();
        music.playOnAwake = false;

        if (instance != null && instance != this)
        {
            music.Stop();
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        baseVolume = music.volume;
        music.loop = true;
        music.spatialBlend = 0f;
        music.ignoreListenerVolume = false;
        ApplyAudioSettings();

        if (music.clip != null && !music.isPlaying)
        {
            music.Play();
        }
    }

    public static void ApplyAudioSettings()
    {
        if (instance == null) return;
        instance.music.volume = instance.baseVolume * GameAudioPrefs.BgmVolume;
        instance.music.mute = GameAudioPrefs.BgmMuted;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
