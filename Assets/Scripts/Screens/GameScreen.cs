using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 7. The game's HUD. The run itself (the hopper, coins and rocks) is in the Gameplay scene, drawn on
// its own canvas under this one. This screen takes the taps, runs the "Ready? / Go!" banner,
// and listens for the end of the run to show Results.
public class GameScreen : UIScreen, IPointerDownHandler
{
    [SerializeField] Image progressFill;
    [SerializeField] TMP_Text levelText, bannerText;
    [SerializeField] RectTransform banner;
    [SerializeField] CanvasGroup bannerGroup;

    public int Level { get; set; } = 1;
    public RunController Run => run;

    RunController run;
    System.Action pendingResults;   // the run ended while Pause was open: show Results when it closes

    void OnEnable()
    {
        run = RunController.Current;
        if (run == null) return;
        run.Won += OnWon;
        run.Died += OnDied;
    }

    void OnDisable()
    {
        if (run == null) return;
        run.Won -= OnWon;
        run.Died -= OnDied;
        run = null;
    }

    public override void OnEnter() => StartRun();
    public void Restart() => StartRun();

    void StartRun()
    {
        pendingResults = null;
        levelText.text = "Level " + Level;
        run.Prepare(Level);
        // "Ready?" then "Go!": the banner pops (OutBack) and the run starts when it goes
        Banner("Ready?", () => Banner("Go!", run.Go, 0.35f), 0.6f);
    }

    void Banner(string text, System.Action then, float hold)
    {
        bannerText.text = text;
        banner.localScale = Vector3.one * 0.6f;
        bannerGroup.alpha = 0f;
        Tween.Scale(banner, Vector3.one, 0.3f, Ease.OutBack, null, 0f, false);
        Tween.Fade(bannerGroup, 1f, 0.15f, Ease.OutQuad, () =>
            Tween.Fade(bannerGroup, 0f, 0.2f, Ease.InQuad, then, hold));
    }

    void OnWon(RunResult r) => Banner("Finish!", () => ShowResults(() =>
        UIManager.Instance.Get<ResultsScreen>().Set(r.level, r.collected, r.total, r.stars)), 0.5f);

    void OnDied()
    {
        int level = run.Level, collected = run.Collected, total = run.Total;
        Banner("Ouch!", () => ShowResults(() =>
            UIManager.Instance.Get<ResultsScreen>().SetLost(level, collected, total)), 0.6f);
    }

    void ShowResults(System.Action fill)
    {
        if (UIManager.Instance.Top != this || UIManager.Instance.Busy) { pendingResults = fill; return; }
        pendingResults = null;
        fill();
        UIManager.Instance.Push(ScreenId.Results);
    }

    public void OnPointerDown(PointerEventData e) => Jump();
    public void Jump() => run?.Jump();
    public void Pause() => UIManager.Instance.Push(ScreenId.Pause);

    void Update()
    {
        if (run == null || UIManager.Instance.Top != this) return;
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) Jump();
        progressFill.fillAmount = run.Progress;
        if (pendingResults != null && !UIManager.Instance.Busy) ShowResults(pendingResults);
    }
}
