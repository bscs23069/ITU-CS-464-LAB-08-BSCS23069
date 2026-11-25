using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 4. Characters: flick through the hoppers with the arrows and pick one. Locked ones point to the shop.
public class CharacterScreen : UIScreen
{
    [SerializeField] RectTransform hopperCard;
    [SerializeField] CanvasGroup cardGroup;
    [SerializeField] Image body;
    [SerializeField] TMP_Text nameText, blurbText, actionLabel;
    [SerializeField] Image actionButton;
    [SerializeField] GameObject selectedBadge, lockBadge;
    [SerializeField] TMP_Text priceText;
    [SerializeField] Image[] dots;
    [SerializeField] Color primary, secondary, dotOn, dotOff;

    int index;
    bool moving;

    public override void OnEnter()
    {
        index = GameData.Selected;
        Show();
    }

    public override void OnResume() => Show();

    void Show()
    {
        var h = GameData.Hoppers[index];
        bool owned = GameData.Owned(index), chosen = GameData.Selected == index;
        body.color = h.color;
        nameText.text = h.name;
        blurbText.text = h.blurb;
        selectedBadge.SetActive(chosen);
        lockBadge.SetActive(!owned);
        priceText.text = h.price.ToString();
        actionLabel.text = chosen ? "Selected" : owned ? "Select" : "Get in shop";
        actionButton.color = chosen ? secondary : primary;
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].color = i == index ? dotOn : dotOff;
            Tween.Scale(dots[i].transform, Vector3.one * (i == index ? 1.35f : 1f), 0.2f, Ease.OutQuad);
        }
    }

    public void Next() => Change(+1);
    public void Previous() => Change(-1);

    // the hopperCard leaves the way you swiped (InQuad, fast exit) and the next comes in from the other side (OutCubic)
    void Change(int dir)
    {
        if (moving) return;
        moving = true;
        index = (index + dir + GameData.Hoppers.Length) % GameData.Hoppers.Length;
        Tween.Move(hopperCard, new Vector2(-dir * 420f, 0f), 0.14f, Ease.InQuad);
        Tween.Fade(cardGroup, 0f, 0.14f, Ease.InQuad, () =>
        {
            Show();
            hopperCard.anchoredPosition = new Vector2(dir * 420f, 0f);
            Tween.Move(hopperCard, Vector2.zero, 0.26f, Ease.OutCubic, () => moving = false);
            Tween.Fade(cardGroup, 1f, 0.2f, Ease.OutQuad);
        });
    }

    public void Action()
    {
        if (GameData.Selected == index) { Tween.PunchScale(hopperCard, 0.05f, 0.25f); return; }
        if (!GameData.Owned(index))
        {
            // the logical next step: go and buy it
            UIManager.Instance.Push(ScreenId.Shop);
            return;
        }
        GameData.Selected = index;
        Show();
        UiSounds.Star();
        // the "selected" tick pops (OutBack) so the choice is confirmed where you were looking
        selectedBadge.transform.localScale = Vector3.zero;
        Tween.Scale(selectedBadge.transform, Vector3.one, 0.35f, Ease.OutBack);
        Toast.Show(GameData.Hoppers[index].name + " is ready to hop!");
    }
}
