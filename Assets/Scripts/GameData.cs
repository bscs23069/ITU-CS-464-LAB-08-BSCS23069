using System;
using UnityEngine;

// Everything the menus remember, saved with PlayerPrefs.
public static class GameData
{
    public const int LevelCount = 12;

    public struct Hopper
    {
        public string name;
        public Color color;
        public int price;   // 0 = owned from the start
        public string blurb;
        public Hopper(string n, string hex, int p, string b)
        {
            name = n; ColorUtility.TryParseHtmlString(hex, out color); price = p; blurb = b;
        }
    }

    public static readonly Hopper[] Hoppers =
    {
        new Hopper("Pip",   "#2ED3B7", 0,   "Always ready to hop."),
        new Hopper("Mango", "#FF9A3D", 0,   "Sunny and a bit too fast."),
        new Hopper("Berry", "#E05BD2", 150, "Bounces higher in its dreams."),
        new Hopper("Mint",  "#7BE07A", 250, "Fresh from the cloud fields."),
        new Hopper("Frost", "#8FD3FF", 400, "Cool under pressure."),
    };

    public static event Action Changed;   // the coin and star pills listen to this
    static void Save() { PlayerPrefs.Save(); Changed?.Invoke(); }

    public static int Coins
    {
        get => PlayerPrefs.GetInt("coins", 200);
        set { PlayerPrefs.SetInt("coins", Mathf.Max(0, value)); Save(); }
    }

    public static int GetStars(int level) => PlayerPrefs.GetInt("stars" + level, 0);
    public static void SetStars(int level, int stars)
    {
        if (stars > GetStars(level)) { PlayerPrefs.SetInt("stars" + level, stars); Save(); }
    }

    public static int TotalStars
    {
        get { int s = 0; for (int i = 1; i <= LevelCount; i++) s += GetStars(i); return s; }
    }

    // a level opens when the one before it has been finished
    public static bool Unlocked(int level) => level == 1 || GetStars(level - 1) > 0;

    public static bool Owned(int hopper) => Hoppers[hopper].price == 0 || PlayerPrefs.GetInt("owned" + hopper, 0) == 1;
    public static void Own(int hopper) { PlayerPrefs.SetInt("owned" + hopper, 1); Save(); }

    public static int Selected
    {
        get => PlayerPrefs.GetInt("selected", 0);
        set { PlayerPrefs.SetInt("selected", value); Save(); }
    }

    public static bool GiftClaimed
    {
        get => PlayerPrefs.GetInt("gift", 0) == 1;
        set { PlayerPrefs.SetInt("gift", value ? 1 : 0); Save(); }
    }

    public static bool Vibration { get => PlayerPrefs.GetInt("vibrate", 1) == 1; set => PlayerPrefs.SetInt("vibrate", value ? 1 : 0); }
    public static bool ReduceMotion { get => PlayerPrefs.GetInt("reduceMotion", 0) == 1; set => PlayerPrefs.SetInt("reduceMotion", value ? 1 : 0); }

    // Reset progress clears coins, stars and hoppers, but keeps the player's settings (volumes and switches)
    public static void ResetAll()
    {
        var volumes = new float[VolumeSettings.Parameters.Length];
        for (int i = 0; i < volumes.Length; i++) volumes[i] = VolumeSettings.Get(VolumeSettings.Parameters[i]);
        bool vibration = Vibration, reduceMotion = ReduceMotion;
        PlayerPrefs.DeleteAll();
        for (int i = 0; i < volumes.Length; i++) PlayerPrefs.SetFloat(VolumeSettings.Parameters[i], volumes[i]);
        Vibration = vibration;
        ReduceMotion = reduceMotion;
        Save();
    }
}
