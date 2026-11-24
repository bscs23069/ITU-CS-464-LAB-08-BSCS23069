using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 5. Shop: spend coins on new hoppers, and a free gift once.
public class ShopScreen : UIScreen
{
    [SerializeField] ShopRow[] rows;
    [SerializeField] CoinPill coinPill;
    [SerializeField] Button giftButton;
    [SerializeField] TMP_Text giftLabel;
    [SerializeField] Image giftImage;
    [SerializeField] Color giftColor, usedColor;

    public override void OnEnter()
    {
        Refresh();
        var t = new Transform[rows.Length + 1];
        t[0] = giftButton.transform.parent;
        for (int i = 0; i < rows.Length; i++) t[i + 1] = rows[i].transform;
        Stagger(t, 0.06f, 0.12f);
    }

    public override void OnResume() => Refresh();

    void Refresh()
    {
        foreach (var r in rows) r.Refresh();
        bool used = GameData.GiftClaimed;
        giftLabel.text = used ? "Claimed" : "Free +100";
        giftImage.color = used ? usedColor : giftColor;
    }

    public void Buy(int row)
    {
        var r = rows[row];
        var h = GameData.Hoppers[r.hopper];
        if (GameData.Owned(r.hopper)) return;
        if (GameData.Coins < h.price)
        {
            // not enough: the button and the coin counter both say no, and the toast says how many more
            UiSounds.Error();
            UiSounds.Buzz();
            Tween.Shake(r.BuyRect, 14f, 0.4f);
            coinPill.Deny();
            Toast.Show($"You need {h.price - GameData.Coins} more coins");
            return;
        }
        GameData.Coins -= h.price;      // the counter counts down by itself
        GameData.Own(r.hopper);
        UiSounds.Buy();
        r.Refresh();
        r.Badge.localScale = Vector3.zero;
        Tween.Scale(r.Badge, Vector3.one, 0.4f, Ease.OutBack);
        Toast.Show(h.name + " is yours! Pick it in Characters");
    }

    public void Gift()
    {
        if (GameData.GiftClaimed)
        {
            UiSounds.Error();
            Tween.Shake((RectTransform)giftButton.transform, 10f, 0.3f);
            Toast.Show("Come back tomorrow for another gift");
            return;
        }
        GameData.GiftClaimed = true;
        GameData.Coins += 100;
        UiSounds.Coin();
        Refresh();
        Tween.PunchScale(giftButton.transform, 0.15f, 0.35f);
    }
}
