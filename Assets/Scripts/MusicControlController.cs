using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MusicControlController : MonoBehaviour
{
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Slider musicSlider;
    private bool isInitializing;

    private void Awake()
    {
        if (musicSlider == null)
        {
            musicSlider = GetComponentInChildren<Slider>(true);
        }
    }

    IEnumerator Start()
    {
        isInitializing = true;

        if (AudioManager.Instance != null)
        {
            if (musicSlider != null)
            {
                musicSlider.minValue = 0f;
                musicSlider.maxValue = 1f;
                musicSlider.value = AudioManager.Instance.GetMusicVolume();
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (musicToggle != null)
            {
                musicToggle.isOn = AudioManager.Instance.IsMusicEnabled();
                musicToggle.onValueChanged.RemoveAllListeners();
                musicToggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        yield return new WaitForEndOfFrame();

        isInitializing = false;
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isInitializing || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.SetMusicEnabled(isOn);
    }

    private void OnSliderChanged(float value)
    {
        if (isInitializing || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.SetMusicVolume(value);

        if (value > 0f && !AudioManager.Instance.IsMusicEnabled())
        {
            isInitializing = true;
            AudioManager.Instance.SetMusicEnabled(true);
            if (musicToggle != null)
            {
                musicToggle.SetIsOnWithoutNotify(true);
            }
            isInitializing = false;
        }
    }

    public void RefreshFromSaved()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }

        isInitializing = true;

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetMusicVolume());
        }

        if (musicToggle != null)
        {
            musicToggle.SetIsOnWithoutNotify(AudioManager.Instance.IsMusicEnabled());
        }

        isInitializing = false;
    }
}
