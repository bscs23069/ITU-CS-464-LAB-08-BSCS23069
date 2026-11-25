using System.Collections;
using UnityEngine;

// Listener: the hopper's body. It stretches on take-off, squashes on landing and blinks after a hit
// while it can't be hurt again. All on the game clock, so Pause freezes it.
public class HopperFeedback : MonoBehaviour
{
    [SerializeField] RunController run;
    [SerializeField] RectTransform body;
    [SerializeField] CanvasGroup group;

    Coroutine blinking;

    void OnEnable()
    {
        run.Prepared += OnPrepared;
        run.Jumped += OnJumped;
        run.Landed += OnLanded;
        run.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        run.Prepared -= OnPrepared;
        run.Jumped -= OnJumped;
        run.Landed -= OnLanded;
        run.Damaged -= OnDamaged;
    }

    void OnPrepared(int level)
    {
        if (blinking != null) StopCoroutine(blinking);
        group.alpha = 1f;
        Tween.KillAll(body);
        body.localScale = Vector3.one;
    }

    // stretch on take-off, then back to round on the way up
    void OnJumped() =>
        Tween.Scale(body, new Vector3(0.85f, 1.2f, 1f), 0.1f, Ease.OutQuad,
            () => Tween.Scale(body, Vector3.one, 0.25f, Ease.OutQuad, null, 0.1f, false), 0f, false);

    // squash on landing, then back to round (OutBack)
    void OnLanded()
    {
        body.localScale = new Vector3(1.25f, 0.78f, 1f);
        Tween.Scale(body, Vector3.one, 0.3f, Ease.OutBack, null, 0f, false);
    }

    void OnDamaged(int hearts, int max)
    {
        if (blinking != null) StopCoroutine(blinking);
        blinking = StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        for (int i = 0; i < 6; i++)
        {
            group.alpha = 0.3f;
            yield return new WaitForSeconds(0.1f);
            group.alpha = 1f;
            yield return new WaitForSeconds(0.1f);
        }
        blinking = null;
    }
}
