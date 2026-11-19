using UnityEngine;

[DisallowMultipleComponent]
public sealed class MenuSfx : MonoBehaviour
{
    private const string PopResource = "UIBubblePop";
    private static AudioSource player;
    private static AudioClip pop;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPlayback()
    {
        player = null;
        pop = null;
    }

    public void PlayButtonPop()
    {
        if (GameAudioPrefs.SfxMuted || GameAudioPrefs.SfxVolume <= 0f) return;
        if (pop == null) pop = Resources.Load<AudioClip>(PopResource);
        if (pop == null) return;

        if (player == null)
        {
            var audioObject = new GameObject("Menu SFX Player");
            DontDestroyOnLoad(audioObject);
            player = audioObject.AddComponent<AudioSource>();
            player.playOnAwake = false;
            player.spatialBlend = 0f;
            ApplyAudioSettings();
        }

        player.PlayOneShot(pop);
    }

    public static void ApplyAudioSettings()
    {
        if (player == null) return;
        player.volume = GameAudioPrefs.SfxVolume;
        player.mute = GameAudioPrefs.SfxMuted;
    }
}
