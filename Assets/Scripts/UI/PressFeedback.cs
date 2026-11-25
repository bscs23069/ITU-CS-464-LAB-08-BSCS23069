using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Every button: it sinks while the finger is on it (OutQuad, quick) and springs back when released (OutBack),
// and the click makes a sound, so a press is felt even before the screen changes.
// With a mouse, pointing at a button plays a soft tick (a phone has no hover, so touch doesn't).
public class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler
{
    public enum Sound { Click, Back, None }
    public Sound sound = Sound.Click;
    public float pressedScale = 0.92f;

    Selectable selectable;

    void Awake() => selectable = GetComponent<Selectable>();

    bool Live => selectable == null || selectable.IsInteractable();

    public void OnPointerEnter(PointerEventData e)
    {
        bool mouse = e is ExtendedPointerEventData x && x.pointerType == UIPointerType.MouseOrPen;
        if (!mouse || !Live || UIManager.Instance == null || UIManager.Instance.Busy) return;
        UiSounds.Hover();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!Live) return;
        Tween.Scale(transform, Vector3.one * pressedScale, 0.08f, Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData e)
    {
        Tween.Scale(transform, Vector3.one, 0.25f, Ease.OutBack);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!Live) return;
        if (sound == Sound.Click) UiSounds.Click();
        else if (sound == Sound.Back) UiSounds.Back();
    }

    void OnDisable() => transform.localScale = Vector3.one;
}
