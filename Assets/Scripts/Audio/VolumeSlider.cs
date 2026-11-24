using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One volume row in Settings: the slider sets its mixer group and shows the level as a percentage,
// or "Off" at the bottom. It plays a short sample of its group while you drag, and saves when you let go.
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] string parameter;   // MasterVolume, MusicVolume, SfxVolume or UiVolume
    [SerializeField] TMP_Text valueText;

    Slider slider;
    float nextPreview;

    public string Parameter => parameter;
    public Slider Slider => slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    void OnEnable()
    {
        slider.SetValueWithoutNotify(VolumeSettings.Get(parameter));
        Show(slider.value);
        slider.onValueChanged.AddListener(Changed);
    }

    void OnDisable()
    {
        slider.onValueChanged.RemoveListener(Changed);
        PlayerPrefs.Save();
    }

    void Changed(float v)
    {
        VolumeSettings.Instance.Set(parameter, v);
        Show(v);
        if (Time.unscaledTime >= nextPreview)
        {
            nextPreview = Time.unscaledTime + 0.15f;
            UiSounds.Preview(parameter);
        }
    }

    void Show(float v) => valueText.text = v <= VolumeSettings.MinLinear ? "Off" : Mathf.RoundToInt(v * 100f) + "%";

    public void OnPointerUp(PointerEventData e) => PlayerPrefs.Save();
}
