using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VolumeToggleController : MonoBehaviour
{
    [SerializeField] private Toggle volumeToggle;
    private bool isInitializing;

    IEnumerator Start()
    {
        isInitializing = true;

        if (volumeToggle != null)
        {
            bool soundOn = AudioManager.Instance == null || AudioManager.Instance.IsSoundEnabled();
            volumeToggle.isOn = soundOn;

            volumeToggle.onValueChanged.RemoveAllListeners();
            volumeToggle.onValueChanged.AddListener(OnVolumeChanged);
        }

        yield return new WaitForEndOfFrame();

        isInitializing = false;
    }

    private void OnVolumeChanged(bool isOn)
    {
        if (isInitializing)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSoundEnabled(isOn);
        }
    }
}
