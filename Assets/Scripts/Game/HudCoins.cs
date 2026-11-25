using TMPro;
using UnityEngine;

// Listener: the coin counter in the HUD. It bumps (Punch) each time a coin comes in.
public class HudCoins : MonoBehaviour
{
    [SerializeField] TMP_Text value;
    [SerializeField] RectTransform counter;

    RunController run;

    // the run is in the Gameplay scene: this HUD part finds it when the Game screen opens
    void OnEnable()
    {
        run = RunController.Current;
        if (run == null) return;
        run.Prepared += OnPrepared;
        run.CoinCollected += OnCoin;
    }

    void OnDisable()
    {
        if (run == null) return;
        run.Prepared -= OnPrepared;
        run.CoinCollected -= OnCoin;
        run = null;
    }

    void OnPrepared(int level) => value.text = "0";

    void OnCoin(int count, Vector2 at)
    {
        value.text = count.ToString();
        Tween.PunchScale(counter, 0.25f, 0.25f);
    }
}
