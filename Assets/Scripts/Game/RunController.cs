using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public struct RunResult
{
    public int level, collected, total, stars;
}

// The run itself, in the Gameplay scene: the hopper, the coins, the rocks and the rules.
// It doesn't play sounds, update the HUD or shake anything. When something happens it raises
// an event, and each listener does its own part (Lecture 09: the publisher doesn't know who listens):
//   GameSounds, HopperFeedback, WorldShake, CoinBurst  (in this scene)
//   GameScreen, HudCoins, HudHearts, HitFlash           (the HUD, in the menu scene)
public class RunController : MonoBehaviour
{
    public static RunController Current { get; private set; }

    public event Action<int> Prepared;               // a fresh run is laid out (level)
    public event Action Started;                     // "Go!"
    public event Action Jumped;
    public event Action Landed;
    public event Action<int, Vector2> CoinCollected; // coins so far, where the coin was
    public event Action<int, int> Damaged;           // hearts left, most hearts
    public event Action Died;                        // out of hearts
    public event Action<RunResult> Won;              // reached the end

    [SerializeField] RectTransform world, hopper, coinTemplate, coinLayer, rockTemplate, rockLayer;
    [SerializeField] Image hopperColor;
    [SerializeField] RectTransform[] clouds, tufts;

    public const float Speed = 520f;        // px per second
    public const float HopperX = -300f;
    const float JumpSpeed = 1500f, Gravity = 3600f;   // a jump peaks at v² / 2g = 312 px, 0.83 s in the air
    const float Ground = 60f;               // the hopper's centre above the ground line
    public const int MaxHearts = 3;
    const float SafeAfterHit = 1.2f;        // seconds with no more damage after a hit

    public int Level { get; private set; } = 1;
    public int Collected { get; private set; }
    public int Hearts { get; private set; } = MaxHearts;
    public int Total => coins.Count;
    public int Rocks => rocks.Count;
    public bool Running => running;
    public float Progress => length > 0f ? Mathf.Clamp01(time / length) : 0f;

    float length, time, y, vy, safeUntil;
    bool running, grounded = true;

    struct Spot { public float at, height; public RectTransform rt; public bool taken; }
    readonly List<Spot> coins = new List<Spot>();
    readonly List<Spot> rocks = new List<Spot>();

    void Awake()
    {
        // opened on its own in the editor: go to the menu, which loads this scene the proper way
        if (UIManager.Instance == null)
        {
            SceneManager.LoadScene(0);
            return;
        }
        Current = this;
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    public void Prepare(int level)
    {
        Level = level;
        length = 9f + level * 0.75f;
        Clear(coins);
        Clear(rocks);
        // runs of ground coins, then an arc in the air that follows a jump, often with a rock under it
        var rng = new System.Random(level * 97);
        float rockChance = Mathf.Min(0.9f, 0.3f + level * 0.08f);
        int arc = 0;
        for (float at = 1.4f; at < length - 1f;)
        {
            int ground = rng.Next(2, 4);
            for (int i = 0; i < ground; i++, at += 0.22f) Add(coins, coinTemplate, coinLayer, at, 0f);
            at += 0.5f;
            Add(coins, coinTemplate, coinLayer, at, 200f);
            Add(coins, coinTemplate, coinLayer, at + 0.16f, 290f);
            Add(coins, coinTemplate, coinLayer, at + 0.32f, 200f);
            // the first arc never has a rock (you learn to jump for coins first), the second always has one
            if (arc == 1 || (arc > 1 && rng.NextDouble() < rockChance)) Add(rocks, rockTemplate, rockLayer, at + 0.16f, 0f);
            arc++;
            at += 0.48f + 0.7f + (float)rng.NextDouble() * 0.4f;
        }

        time = 0f; y = 0f; vy = 0f; grounded = true; running = false; safeUntil = -1f;
        Collected = 0;
        Hearts = MaxHearts;
        hopperColor.color = GameData.Hoppers[GameData.Selected].color;
        Place();
        Prepared?.Invoke(level);
    }

    void Clear(List<Spot> list)
    {
        foreach (var s in list) if (s.rt != null) Destroy(s.rt.gameObject);
        list.Clear();
    }

    void Add(List<Spot> list, RectTransform template, RectTransform layer, float at, float height)
    {
        var rt = Instantiate(template, layer);
        rt.gameObject.SetActive(true);
        rt.localScale = Vector3.one;
        list.Add(new Spot { at = at, height = height, rt = rt });
    }

    public void Go()
    {
        if (running || Hearts <= 0) return;
        running = true;
        Started?.Invoke();
    }

    public void Jump()
    {
        if (!running || !grounded) return;
        grounded = false;
        vy = JumpSpeed;
        Jumped?.Invoke();
    }

    // the game clock (Time.deltaTime): at timeScale 0 (Pause, menus) nothing moves
    void Update()
    {
        if (!running) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        time += dt;
        if (!grounded)
        {
            vy -= Gravity * dt;
            y += vy * dt;
            if (y <= 0f)
            {
                y = 0f; vy = 0f; grounded = true;
                Landed?.Invoke();
            }
        }
        Place();
        Collect();
        HitRocks();
        if (running && time >= length) Finish();
    }

    void Place()
    {
        hopper.anchoredPosition = new Vector2(HopperX, Ground + y);
        foreach (var c in coins) c.rt.anchoredPosition = new Vector2(HopperX + (c.at - time) * Speed, Ground + c.height);
        foreach (var r in rocks) r.rt.anchoredPosition = new Vector2(HopperX + (r.at - time) * Speed, -4f);
        // the clouds drift slower than the ground: depth
        float w = world.rect.width + 400f;
        for (int i = 0; i < clouds.Length; i++)
        {
            var p = clouds[i].anchoredPosition;
            p.x = Mathf.Repeat(i * 420f - time * Speed * 0.15f + w / 2f, w) - w / 2f;
            clouds[i].anchoredPosition = p;
        }
        for (int i = 0; i < tufts.Length; i++)
        {
            var p = tufts[i].anchoredPosition;
            p.x = Mathf.Repeat(i * 230f - time * Speed + w / 2f, w) - w / 2f;
            tufts[i].anchoredPosition = p;
        }
    }

    void Collect()
    {
        for (int i = 0; i < coins.Count; i++)
        {
            var c = coins[i];
            if (c.taken) continue;
            float dx = Mathf.Abs(c.rt.anchoredPosition.x - HopperX);
            float dy = Mathf.Abs(c.height - y);
            if (dx < 70f && dy < 90f)
            {
                c.taken = true;
                coins[i] = c;
                Collected++;
                // the coin itself pops and goes; everything else (sound, sparks, counter) listens
                var rt = c.rt;
                Tween.Scale(rt, Vector3.one * 1.6f, 0.18f, Ease.OutQuad, () => rt.gameObject.SetActive(false), 0f, false);
                CoinCollected?.Invoke(Collected, rt.anchoredPosition);
            }
        }
    }

    void HitRocks()
    {
        if (time < safeUntil) return;
        foreach (var r in rocks)
        {
            if (Mathf.Abs(r.rt.anchoredPosition.x - HopperX) < 70f && y < 60f)
            {
                Hearts--;
                safeUntil = time + SafeAfterHit;
                Damaged?.Invoke(Hearts, MaxHearts);
                if (Hearts <= 0)
                {
                    running = false;
                    Died?.Invoke();
                }
                return;
            }
        }
    }

    void Finish()
    {
        running = false;
        int stars = 1 + (Collected >= Total * 0.6f ? 1 : 0) + (Collected >= Total * 0.9f ? 1 : 0);
        Won?.Invoke(new RunResult { level = Level, collected = Collected, total = Total, stars = stars });
    }

    // how far ahead the next rock is, in px (for the test that plays the game)
    public float NextRockAhead
    {
        get
        {
            float best = float.MaxValue;
            foreach (var r in rocks)
            {
                float d = (r.at - time) * Speed;
                if (d > -70f && d < best) best = d;
            }
            return best;
        }
    }
}
