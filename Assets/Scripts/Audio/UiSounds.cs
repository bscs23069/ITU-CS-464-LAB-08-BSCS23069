using UnityEngine;

// The one UI audio player (Lecture 09): every button, panel and message plays through here,
// so no button needs its own AudioSource. Its source goes to the UI group of the mixer.
// It ignores AudioListener.pause, so the menus still click while the game is paused.
public class UiSounds : MonoBehaviour
{
    public static UiSounds Instance { get; private set; }

    [SerializeField] AudioSource ui;          // → UI group
    [SerializeField] AudioSource sfxPreview;  // → SFX group: lets the SFX slider be heard in the menus
    [SerializeField] AudioClip hover, click, back, open, close, pause, resume, error, coin, buy, star, whoosh, win, fail;
    [SerializeField] AudioClip sfxSample;

    void Awake()
    {
        Instance = this;
        ui.ignoreListenerPause = true;
        sfxPreview.ignoreListenerPause = true;
    }

    public static void Hover() => Play(i => i.hover, 0.5f);
    public static void Click() => Play(i => i.click);
    public static void Back() => Play(i => i.back);
    public static void Open() => Play(i => i.open);
    public static void Close() => Play(i => i.close);
    public static void Pause() => Play(i => i.pause);
    public static void Resume() => Play(i => i.resume);
    public static void Error() => Play(i => i.error);
    public static void Coin() => Play(i => i.coin);
    public static void Buy() => Play(i => i.buy);
    public static void Star() => Play(i => i.star);
    public static void Whoosh() => Play(i => i.whoosh, 0.8f);
    public static void Win() => Play(i => i.win);
    public static void Fail() => Play(i => i.fail);

    // PlayOneShot: short sounds overlap instead of cutting each other off
    static void Play(System.Func<UiSounds, AudioClip> pick, float volume = 1f)
    {
        if (Instance != null) Instance.ui.PlayOneShot(pick(Instance), volume);
    }

    // a short sample of the group a volume slider is changing, so you hear the new level
    public static void Preview(string parameter)
    {
        if (Instance == null) return;
        if (parameter == VolumeSettings.Sfx) Instance.sfxPreview.PlayOneShot(Instance.sfxSample);
        else if (parameter != VolumeSettings.Music) Click();   // the music is already playing
    }

    public static void Buzz()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (GameData.Vibration) Handheld.Vibrate();
#endif
    }
}
