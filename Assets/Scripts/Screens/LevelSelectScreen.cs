using UnityEngine;

// 3. Level select: twelve levels in a 3 × 4 grid. A level opens when the one before it is finished.
public class LevelSelectScreen : UIScreen
{
    [SerializeField] LevelCell[] cells;

    public override void OnEnter()
    {
        Refresh();
        // the tiles pop in one after another, reading order (OutBack)
        var t = new Transform[cells.Length];
        for (int i = 0; i < cells.Length; i++) t[i] = cells[i].transform;
        Stagger(t, 0.035f, 0.12f);
    }

    public override void OnResume() => Refresh();

    void Refresh()
    {
        foreach (var c in cells) c.Refresh();
    }

    public void Pick(int level)
    {
        var cell = cells[level - 1];
        if (!cell.Open)
        {
            // locked: shake "no" and say why
            UiSounds.Error();
            UiSounds.Buzz();
            Tween.Shake((RectTransform)cell.transform, 16f, 0.4f);
            Toast.Show($"Finish level {level - 1} first");
            return;
        }
        // the game opens through the loading screen, which loads the Gameplay scene
        UIManager.Instance.LoadLevel(level);
    }
}
