using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// One machine for every screen (Lecture 08): a stack of screens.
// Push goes deeper (slides in from the right), Back comes out (slides back to the right),
// an overlay pops up over the screen below, and only this script touches the panels and the clock.
// Lab 08: it also decides what the audio does after every move (AfterMove), and starts a level
// through the Loading screen (LoadLevel).
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] UIScreen[] screens;
    [SerializeField] CanvasGroup screensRoot;   // blocks input while a transition plays

    public const float SlideTime = 0.35f;
    public const float PopTime = 0.3f;

    readonly List<UIScreen> stack = new List<UIScreen>();
    int busy;   // transitions still playing

    public UIScreen Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
    public bool Busy => busy > 0;
    public IReadOnlyList<UIScreen> Stack => stack;

    float Width => ((RectTransform)screensRoot.transform).rect.width;

    void Awake()
    {
        Instance = this;
        Tween.ReduceMotion = GameData.ReduceMotion;
        foreach (var s in screens) s.gameObject.SetActive(false);
    }

    void Start()
    {
        var title = Get(ScreenId.Title);
        stack.Add(title);
        Reset(title);
        title.gameObject.SetActive(true);
        title.OnEnter();
        AfterMove();
    }

    public UIScreen Get(ScreenId id)
    {
        foreach (var s in screens) if (s.id == id) return s;
        return null;
    }

    public T Get<T>() where T : UIScreen
    {
        foreach (var s in screens) if (s is T t) return t;
        return null;
    }

    // ---------- the four moves ----------

    // go one level deeper
    public void Push(ScreenId id)
    {
        if (Busy) return;
        var from = Top;
        var to = Get(id);
        stack.Add(to);
        from.Group.interactable = false;
        from.OnExit();
        if (to.overlay)
        {
            if (to.id == ScreenId.Pause) UiSounds.Pause(); else UiSounds.Open();
            PopIn(to);
        }
        else SlideIn(to, from, forward: true);
        AfterMove();
    }

    // come back one level
    public void Back()
    {
        if (Busy || stack.Count <= 1) return;
        var from = Top;
        stack.RemoveAt(stack.Count - 1);
        var to = Top;
        from.OnExit();
        if (from.overlay)
        {
            if (from.id == ScreenId.Pause) UiSounds.Resume(); else UiSounds.Close();
            PopOut(from, to);
        }
        else
        {
            UiSounds.Back();   // back / cancel
            SlideOut(from, to);
        }
        AfterMove();
    }

    // swap the top screen for another at the same level (Title → Main Menu)
    public void Replace(ScreenId id)
    {
        if (Busy) return;
        var from = Top;
        var to = Get(id);
        stack[stack.Count - 1] = to;
        from.OnExit();
        CrossFade(from, to);
        AfterMove();
    }

    // straight back to the main menu from anywhere (Quit, Menu)
    public void Home() => BackTo(ScreenId.MainMenu);

    // back several levels at once, in one slide
    public void BackTo(ScreenId target)
    {
        if (Busy) return;
        int home = stack.FindIndex(s => s.id == target);
        if (home < 0 || home == stack.Count - 1) return;
        var from = Top;
        // everything between the top and the main menu just goes, unseen behind the top screen
        for (int i = stack.Count - 2; i > home; i--) { stack[i].OnExit(); stack[i].gameObject.SetActive(false); }
        stack.RemoveRange(home + 1, stack.Count - home - 1);
        from.OnExit();
        UiSounds.Back();
        SlideOut(from, Top);
        AfterMove();
    }

    // ---------- the motion ----------

    void Reset(UIScreen s)
    {
        s.Rect.anchoredPosition = Vector2.zero;
        s.Rect.localScale = Vector3.one;
        s.Group.alpha = 1f;
        s.Group.interactable = true;
    }

    void Begin() { busy++; screensRoot.blocksRaycasts = false; }
    void End() { busy--; if (busy <= 0) { busy = 0; screensRoot.blocksRaycasts = true; } }

    // forward: the new screen slides in from the right (OutCubic, arrives fast and settles),
    // the old one drifts a third of the way left and fades, so you read "deeper"
    void SlideIn(UIScreen to, UIScreen from, bool forward)
    {
        Begin();
        UiSounds.Whoosh();
        Reset(to);
        to.transform.SetAsLastSibling();
        to.Rect.anchoredPosition = new Vector2(Width, 0f);
        to.gameObject.SetActive(true);
        to.OnEnter();
        Tween.Move(to.Rect, Vector2.zero, SlideTime, Ease.OutCubic, End);

        // an overlay (Settings opened from Pause) just stays where it is, under the new screen
        if (from.overlay) return;
        Tween.Move(from.Rect, new Vector2(-Width * 0.3f, 0f), SlideTime, Ease.InOutQuad);
        Tween.Fade(from.Group, 0f, SlideTime, Ease.InOutQuad, () =>
        {
            from.gameObject.SetActive(false);
            Reset(from);
        });
    }

    // back: the top screen slides off to the right (InCubic, leaves fast), the one below comes back from the left
    void SlideOut(UIScreen from, UIScreen to)
    {
        Begin();
        UiSounds.Whoosh();
        from.transform.SetAsLastSibling();
        if (!to.gameObject.activeSelf)
        {
            to.gameObject.SetActive(true);
            to.Rect.anchoredPosition = new Vector2(-Width * 0.3f, 0f);
            to.Group.alpha = 0f;
        }
        to.Group.interactable = true;
        to.OnResume();
        Tween.Move(to.Rect, Vector2.zero, SlideTime, Ease.OutCubic);
        Tween.Fade(to.Group, 1f, SlideTime, Ease.OutCubic);
        Tween.Move(from.Rect, new Vector2(Width, 0f), SlideTime * 0.85f, Ease.InCubic, () =>
        {
            from.gameObject.SetActive(false);
            Reset(from);
            End();
        });
    }

    // an overlay: the dim fades in under it and the card pops (OutBack overshoots, so it reads "on top, look here")
    void PopIn(UIScreen to)
    {
        Begin();
        Reset(to);
        to.transform.SetAsLastSibling();
        to.gameObject.SetActive(true);
        to.OnEnter();
        if (to.dim != null) { to.dim.alpha = 0f; Tween.Fade(to.dim, 1f, 0.2f, Ease.OutQuad); }
        if (to.card != null)
        {
            to.card.localScale = Vector3.one * 0.8f;
            var cg = to.card.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 0f; Tween.Fade(cg, 1f, 0.15f, Ease.OutQuad); }
            Tween.Scale(to.card, Vector3.one, PopTime, Ease.OutBack, End);
        }
        else End();
    }

    // an overlay going away: the card shrinks a little and fades (InBack pulls back first), the dim lifts
    void PopOut(UIScreen from, UIScreen to)
    {
        Begin();
        to.Group.interactable = true;
        to.OnResume();
        if (from.dim != null) Tween.Fade(from.dim, 0f, 0.2f, Ease.InQuad);
        var cg = from.card != null ? from.card.GetComponent<CanvasGroup>() : null;
        if (cg != null) Tween.Fade(cg, 0f, 0.2f, Ease.InQuad, null, 0.05f);
        Tween.Scale(from.card, Vector3.one * 0.85f, 0.25f, Ease.InBack, () =>
        {
            from.gameObject.SetActive(false);
            from.card.localScale = Vector3.one;
            if (cg != null) cg.alpha = 1f;
            End();
        });
    }

    // same level: fade across with a slight zoom
    void CrossFade(UIScreen from, UIScreen to)
    {
        Begin();
        Reset(to);
        to.transform.SetAsLastSibling();
        to.gameObject.SetActive(true);
        to.Group.alpha = 0f;
        to.Rect.localScale = Vector3.one * 1.06f;
        to.OnEnter();
        Tween.Fade(to.Group, 1f, 0.4f, Ease.OutCubic);
        Tween.Scale(to.transform, Vector3.one, 0.45f, Ease.OutCubic, End);
        Tween.Fade(from.Group, 0f, 0.3f, Ease.InQuad, () => { from.gameObject.SetActive(false); Reset(from); });
    }

    // start a level: everything above the level grid goes (an old game and its results),
    // and the Loading screen fades in. It loads the Gameplay scene and then opens the Game screen.
    public void LoadLevel(int level)
    {
        if (Busy) return;
        int keep = stack.FindIndex(s => s.id == ScreenId.LevelSelect);
        var from = Top;
        for (int i = stack.Count - 2; i > keep; i--) { stack[i].OnExit(); stack[i].gameObject.SetActive(false); }
        stack.RemoveRange(keep + 1, stack.Count - keep - 1);
        from.OnExit();
        var loading = Get<LoadingScreen>();
        loading.Level = level;
        stack.Add(loading);
        CrossFade(from, loading);
        AfterMove();
    }

    // After every move: the clock, the audio and the Gameplay scene follow the screen on top.
    // - The clock runs only while the game itself is on top: menus and Pause freeze it.
    // - Paused (Pause, or Settings opened from Pause): the game's sound effects pause with AudioListener.pause.
    //   The UI and the music ignore that pause on purpose: buttons still click, and the music plays on, softer.
    // - The music is the game track while a run is on screen, and crossfades back to the menu track
    //   for Results and the menus.
    // - Once the game has left the stack, the Gameplay scene is unloaded.
    void AfterMove()
    {
        bool playing = Top != null && Top.id == ScreenId.Game;
        bool inGame = stack.Exists(s => s.id == ScreenId.Game);
        bool paused = inGame && !playing && Top.id != ScreenId.Results;
        Time.timeScale = playing ? 1f : 0f;
        AudioListener.pause = paused;
        if (MusicPlayer.Instance != null)
            MusicPlayer.Instance.Set(inGame && Top.id != ScreenId.Results ? MusicPlayer.Track.Game : MusicPlayer.Track.Menu, paused);
        if (!inGame && Top != null && Top.id != ScreenId.Loading) GameScene.Unload();
    }

    void Update()
    {
        // Android's back button arrives as Escape
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame || Busy) return;
        if (Top.OnBack()) return;
        switch (Top.id)
        {
            case ScreenId.Game: Push(ScreenId.Pause); break;
            case ScreenId.Title:
            case ScreenId.MainMenu:
            case ScreenId.Results:
            case ScreenId.Loading: break;   // these have their own way forward
            default: Back(); break;
        }
    }
}
