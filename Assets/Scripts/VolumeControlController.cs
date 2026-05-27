using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VolumeControlController : MonoBehaviour
{
    [SerializeField] private Toggle volumeToggle;
    [SerializeField] private Slider volumeSlider;
    private bool isInitializing;

    private void Awake()
    {
        if (volumeSlider == null)
        {
            volumeSlider = GetComponentInChildren<Slider>(true);
        }
    }

    IEnumerator Start()
    {
        isInitializing = true;

        if (AudioManager.Instance != null)
        {
            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.value = AudioManager.Instance.GetMasterVolume();
                volumeSlider.onValueChanged.RemoveAllListeners();
                volumeSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (volumeToggle != null)
            {
                volumeToggle.isOn = AudioManager.Instance.IsSoundEnabled();
                volumeToggle.onValueChanged.RemoveAllListeners();
                volumeToggle.onValueChanged.AddListener(OnToggleChanged);
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

        AudioManager.Instance.SetSoundEnabled(isOn);
    }

    private void OnSliderChanged(float value)
    {
        if (isInitializing || AudioManager.Instance == null)
        {
            return;
        }

        AudioManager.Instance.SetMasterVolume(value);

        if (value > 0f && !AudioManager.Instance.IsSoundEnabled())
        {
            isInitializing = true;
            AudioManager.Instance.SetSoundEnabled(true);
            if (volumeToggle != null)
            {
                volumeToggle.SetIsOnWithoutNotify(true);
            }
            isInitializing = false;
        }
    }
}
