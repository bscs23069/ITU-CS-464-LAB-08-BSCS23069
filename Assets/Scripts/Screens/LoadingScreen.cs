using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The Gameplay scene: loaded on top of the menu scene (additive), so the menus, the HUD, the music
// and the settings all stay alive, and unloaded again when you leave the game.
public static class GameScene
{
    public const string Name = "Gameplay";
    static AsyncOperation unloading;

    public static bool Loaded => SceneManager.GetSceneByName(Name).isLoaded;
    public static bool Unloading => unloading != null && !unloading.isDone;

    public static void Unload()
    {
        if (!Loaded || Unloading) return;
        unloading = SceneManager.UnloadSceneAsync(Name);
    }
}

// 11. Loading: between the level grid and the game. It loads the Gameplay scene in the background
// with LoadSceneAsync and shows the real progress. The Game screen opens only when the scene is in.
public class LoadingScreen : UIScreen
{
    [SerializeField] Image fill, hopperBody;
    [SerializeField] TMP_Text title, percent, tip;
    [SerializeField] float minShow = 0.9f;   // a load this small is nearly instant: don't let the bar just flash

    public int Level { get; set; } = 1;
    public float Shown { get; private set; }     // what the bar shows, 0 to 1
    public float Real { get; private set; }      // what the loader reports, 0 to 1

    static readonly string[] Tips =
    {
        "Tap anywhere to jump",
        "Coins in the air need a jump",
        "Jump over the spiky rocks",
        "Three hits and the run is over",
        "Collect 90% of the coins for three stars",
    };

    public override void OnEnter()
    {
        title.text = "Level " + Level;
        hopperBody.color = GameData.Hoppers[GameData.Selected].color;
        tip.text = Tips[(Level - 1) % Tips.Length];
        Real = 0f;
        Show(0f);
        StartCoroutine(Load());
    }

    // nothing to go back to in the middle of a load: Esc does nothing here
    public override bool OnBack() => true;

    IEnumerator Load()
    {
        // a game that is still loaded (Next from Results) goes first
        while (GameScene.Unloading) yield return null;
        if (GameScene.Loaded)
        {
            var old = SceneManager.UnloadSceneAsync(GameScene.Name);
            while (!old.isDone) yield return null;
        }

        var op = SceneManager.LoadSceneAsync(GameScene.Name, LoadSceneMode.Additive);
        // hold the scene back at 90% (loaded, not switched on) until the bar has caught up
        op.allowSceneActivation = false;
        float start = Time.unscaledTime;
        while (true)
        {
            // Unity's progress stops at 0.9 while activation waits, so 0.9 means "all loaded"
            Real = Mathf.Clamp01(op.progress / 0.9f);
            Show(Mathf.Min(Real, (Time.unscaledTime - start) / minShow));
            if (Shown >= 1f) break;
            yield return null;
        }
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        // the scene is in and its RunController is awake: now, and only now, the game opens
        while (UIManager.Instance.Busy) yield return null;
        UIManager.Instance.Get<GameScreen>().Level = Level;
        UIManager.Instance.Replace(ScreenId.Game);
    }

    void Show(float v)
    {
        Shown = v;
        fill.fillAmount = v;
        percent.text = Mathf.RoundToInt(v * 100f) + "%";
    }
}
