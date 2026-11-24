using UnityEngine;

public enum ScreenId { Title, MainMenu, LevelSelect, Characters, Shop, Settings, Game, Pause, Results, Credits, Loading }

// One screen of the menu. UIManager decides when it shows; the screen only fills itself in.
[RequireComponent(typeof(CanvasGroup))]
public class UIScreen : MonoBehaviour
{
    public ScreenId id;
    [Tooltip("Shown on top of the screen below it, with a dimmed background (Pause, Results)")]
    public bool overlay;
    [Tooltip("For an overlay: the dark layer behind the card")]
    public CanvasGroup dim;
    [Tooltip("For an overlay: the card that pops in")]
    public RectTransform card;

    public RectTransform Rect => (RectTransform)transform;
    public CanvasGroup Group => GetComponent<CanvasGroup>();

    // called as the screen starts to come in (fill in the numbers, start the stagger)
    public virtual void OnEnter() { }
    // called when a screen on top of it closes and it is the top again
    public virtual void OnResume() { }
    // called as the screen starts to leave
    public virtual void OnExit() { }
    // the back key or the back button: return true if the screen dealt with it itself
    public virtual bool OnBack() => false;

    // for the back button in the top-left corner of every inner screen
    public void GoBack() => UIManager.Instance.Back();

    // pop a list of things in one after another (OutBack): the eye follows the order
    protected static void Stagger(Transform[] items, float gap, float start = 0.1f)
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i].localScale = Vector3.zero;
            Tween.Scale(items[i], Vector3.one, 0.35f, Ease.OutBack, null, start + i * gap);
        }
    }
}
