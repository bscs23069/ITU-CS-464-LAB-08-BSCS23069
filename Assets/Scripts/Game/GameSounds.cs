using UnityEngine;

// Listener: the sound of the run. A few AudioSources on the SFX group are used in turn, each sound
// with PlayOneShot so quick ones overlap, and a small random pitch so repeats don't sound copied.
// Coins picked up close together climb in pitch, a little reward for a good line.
// These sources don't ignore AudioListener.pause, so the game's sounds stop while it's paused.
public class GameSounds : MonoBehaviour
{
    [SerializeField] RunController run;
    [SerializeField] AudioSource[] voices;
    [SerializeField] AudioClip ready, go, jump, land, coin, hit, lose, finish;
    [SerializeField] float pitchJitter = 1f;   // ± semitones

    int next, combo;
    float lastCoin = -10f;

    void OnEnable()
    {
        run.Prepared += OnPrepared;
        run.Started += OnStarted;
        run.Jumped += OnJumped;
        run.Landed += OnLanded;
        run.CoinCollected += OnCoin;
        run.Damaged += OnDamaged;
        run.Died += OnDied;
        run.Won += OnWon;
    }

    void OnDisable()
    {
        run.Prepared -= OnPrepared;
        run.Started -= OnStarted;
        run.Jumped -= OnJumped;
        run.Landed -= OnLanded;
        run.CoinCollected -= OnCoin;
        run.Damaged -= OnDamaged;
        run.Died -= OnDied;
        run.Won -= OnWon;
    }

    void OnPrepared(int level)
    {
        // a restart cuts off anything still ringing from the last run
        foreach (var v in voices) v.Stop();
        combo = 0;
        Play(ready, 0.8f, 0f);
    }

    void OnStarted() => Play(go, 0.9f, 0f);
    void OnJumped() => Play(jump, 0.8f, pitchJitter);
    void OnLanded() => Play(land, 0.6f, pitchJitter);
    void OnDamaged(int hearts, int max) => Play(hit, 1f, pitchJitter * 0.5f);
    void OnDied() => Play(lose, 1f, 0f);
    void OnWon(RunResult r) => Play(finish, 1f, 0f);

    void OnCoin(int count, Vector2 at)
    {
        combo = Time.time - lastCoin < 0.5f ? Mathf.Min(combo + 1, 7) : 0;
        lastCoin = Time.time;
        Play(coin, 0.8f, 0.3f, combo);
    }

    void Play(AudioClip clip, float volume, float jitter, float semitones = 0f)
    {
        var v = voices[next];
        next = (next + 1) % voices.Length;
        v.pitch = Mathf.Pow(2f, (semitones + Random.Range(-jitter, jitter)) / 12f);
        v.PlayOneShot(clip, volume);
    }
}
