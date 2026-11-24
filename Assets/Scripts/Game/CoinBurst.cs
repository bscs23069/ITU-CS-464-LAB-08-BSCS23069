using UnityEngine;
using UnityEngine.UI;

// Listener: a little burst of gold sparks where a coin was picked up. The sparks are UI images from a
// small pool (a ParticleSystem doesn't draw on a Screen Space Overlay canvas), moving on the game clock.
public class CoinBurst : MonoBehaviour
{
    [SerializeField] RunController run;
    [SerializeField] Image sparkTemplate;
    [SerializeField] int perBurst = 6;
    [SerializeField] float distance = 90f, time = 0.35f;

    Image[] pool;
    int next;

    void Awake()
    {
        pool = new Image[perBurst * 3];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = Instantiate(sparkTemplate, sparkTemplate.transform.parent);
            pool[i].gameObject.SetActive(false);
        }
    }

    void OnEnable() => run.CoinCollected += OnCoin;
    void OnDisable() => run.CoinCollected -= OnCoin;

    void OnCoin(int count, Vector2 at)
    {
        float turn = Random.Range(0f, 360f);
        for (int i = 0; i < perBurst; i++)
        {
            var spark = pool[next];
            next = (next + 1) % pool.Length;
            var rt = spark.rectTransform;
            float a = (turn + i * 360f / perBurst) * Mathf.Deg2Rad;
            Vector2 to = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * distance;
            var color = spark.color;
            rt.anchoredPosition = at;
            spark.gameObject.SetActive(true);
            // fly out fast and settle (OutCubic) while fading
            Tween.Value(spark, "color", time, Ease.OutCubic, t =>
            {
                rt.anchoredPosition = Vector2.LerpUnclamped(at, to, t);
                color.a = 1f - t;
                spark.color = color;
            }, () => spark.gameObject.SetActive(false), 0f, false);
        }
    }
}
