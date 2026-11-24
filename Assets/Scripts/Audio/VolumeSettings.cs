using UnityEngine;
using UnityEngine.Audio;

// The four volumes. A slider gives 0 to 1, but the mixer works in decibels, and loudness is heard
// on a log scale, so 0.5 on the slider is −6 dB (half the amplitude), not half the fader.
// The values are saved with PlayerPrefs and put back into the mixer every time the game starts.
public class VolumeSettings : MonoBehaviour
{
    public static VolumeSettings Instance { get; private set; }

    [SerializeField] AudioMixer mixer;

    // the names the mixer exposes, one per group, and the PlayerPrefs keys
    public const string Master = "MasterVolume", Music = "MusicVolume", Sfx = "SfxVolume", Ui = "UiVolume";
    public static readonly string[] Parameters = { Master, Music, Sfx, Ui };

    public const float Silent = -80f;        // the mixer's floor
    public const float MinLinear = 0.0001f;  // Log10(0) is minus infinity, so at or below this the group is muted

    void Awake() => Instance = this;

    // AudioMixer.SetFloat doesn't stick in Awake, so the saved values go in at Start
    void Start()
    {
        foreach (var p in Parameters) Apply(p, Get(p));
    }

    public static float ToDecibels(float v) => v <= MinLinear ? Silent : Mathf.Log10(v) * 20f;

    public static float Default(string p) => p == Music ? 0.7f : p == Ui ? 0.8f : 1f;
    public static float Get(string p) => PlayerPrefs.GetFloat(p, Default(p));

    public void Set(string p, float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(p, v);
        Apply(p, v);
    }

    void Apply(string p, float v) => mixer.SetFloat(p, ToDecibels(v));

    public float Decibels(string p)
    {
        mixer.GetFloat(p, out float db);
        return db;
    }
}
