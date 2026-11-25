using UnityEngine;
using UnityEngine.UI;

// Listener: the whole screen flashes red for a moment when the hopper is hurt.
public class HitFlash : MonoBehaviour
{
    [SerializeField] Image flash;
    [SerializeField] float strength = 0.35f, time = 0.4f;

    RunController run;

    // the run is in the Gameplay scene: this HUD part finds it when the Game screen opens
    void OnEnable()
    {
        run = RunController.Current;
        if (run == null) return;
        run.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        if (run == null) return;
        run.Damaged -= OnDamaged;
        run = null;
    }

    void Awake() => SetAlpha(0f);

    void OnDamaged(int left, int max)
    {
        // quick in, slower out (OutQuad), so it reads as a sting, not a fade
        Tween.Value(flash, "color", time, Ease.OutQuad, t => SetAlpha(strength * (1f - t)));
    }

    void SetAlpha(float a)
    {
        var c = flash.color;
        c.a = a;
        flash.color = c;
    }
}
