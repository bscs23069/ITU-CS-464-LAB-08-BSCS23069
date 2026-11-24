using UnityEngine;
using UnityEngine.UI;

// Listener: the three hearts in the HUD. A lost heart turns dark and pops, and the pill shakes.
public class HudHearts : MonoBehaviour
{
    [SerializeField] Image[] hearts;
    [SerializeField] RectTransform pill;
    [SerializeField] Color full, empty;

    RunController run;

    // the run is in the Gameplay scene: this HUD part finds it when the Game screen opens
    void OnEnable()
    {
        run = RunController.Current;
        if (run == null) return;
        run.Prepared += OnPrepared;
        run.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        if (run == null) return;
        run.Prepared -= OnPrepared;
        run.Damaged -= OnDamaged;
        run = null;
    }

    void OnPrepared(int level) => Show(RunController.MaxHearts);

    void OnDamaged(int left, int max)
    {
        Show(left);
        if (left < hearts.Length)
        {
            var lost = hearts[left].transform;
            lost.localScale = Vector3.one * 1.5f;
            Tween.Scale(lost, Vector3.one, 0.35f, Ease.OutBack);
        }
        Tween.Shake(pill, 12f, 0.3f);
    }

    void Show(int left)
    {
        for (int i = 0; i < hearts.Length; i++) hearts[i].color = i < left ? full : empty;
    }
}
