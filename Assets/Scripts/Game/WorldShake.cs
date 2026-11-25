using UnityEngine;

// Listener: a short camera-style shake of the whole world on a hit, a bigger one when the last heart goes.
public class WorldShake : MonoBehaviour
{
    [SerializeField] RunController run;
    [SerializeField] RectTransform world;
    [SerializeField] float hitPixels = 18f, diePixels = 34f;

    void OnEnable()
    {
        run.Damaged += OnDamaged;
        run.Died += OnDied;
    }

    void OnDisable()
    {
        run.Damaged -= OnDamaged;
        run.Died -= OnDied;
    }

    void OnDamaged(int hearts, int max)
    {
        if (hearts > 0) Tween.Shake(world, hitPixels, 0.3f);
    }

    void OnDied() => Tween.Shake(world, diePixels, 0.5f);
}
