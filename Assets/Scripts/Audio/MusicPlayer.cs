using UnityEngine;

// Two looping tracks, menu and game, on two AudioSources in the Music group.
// Changing track crossfades: the new one fades in while the old one fades out (equal power,
// so the loudness doesn't dip in the middle). While the game is paused the music keeps playing,
// softer, so the pause feels calm rather than broken.
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    public enum Track { Menu, Game }

    [SerializeField] AudioSource a, b;
    [SerializeField] AudioClip menu, game;
    [SerializeField] float level = 0.55f;       // under the effects
    [SerializeField] float fadeTime = 1.2f;
    [SerializeField] float pausedLevel = 0.4f;  // how loud it stays while paused

    AudioSource current, previous;
    float fade = 1f, duck = 1f, duckTarget = 1f;

    public Track Playing { get; private set; }
    public bool Ducked => duckTarget < 1f;
    public AudioSource Current => current;

    void Awake()
    {
        Instance = this;
        foreach (var s in new[] { a, b })
        {
            s.loop = true;
            s.ignoreListenerPause = true;   // it decides for itself what to do while paused
            s.volume = 0f;
        }
        current = a;
        current.clip = menu;
        current.Play();
        Playing = Track.Menu;
    }

    public void Set(Track track, bool paused)
    {
        duckTarget = paused ? pausedLevel : 1f;
        if (track == Playing) return;
        Playing = track;
        previous = current;
        current = current == a ? b : a;
        current.clip = track == Track.Menu ? menu : game;
        current.time = 0f;
        current.volume = 0f;
        current.Play();
        fade = 0f;
    }

    // unscaled time: menus and pause run at timeScale 0
    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        duck = Mathf.MoveTowards(duck, duckTarget, dt / 0.35f);
        if (fade < 1f)
        {
            fade = Mathf.Min(1f, fade + dt / fadeTime);
            if (fade >= 1f && previous != null) { previous.Stop(); previous = null; }
        }
        current.volume = Mathf.Sin(fade * Mathf.PI * 0.5f) * duck * level;
        if (previous != null) previous.volume = Mathf.Cos(fade * Mathf.PI * 0.5f) * duck * level;
    }
}
