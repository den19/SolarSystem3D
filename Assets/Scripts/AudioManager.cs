using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private const string SoundEnabledKey = "SoundEnabled";
    private const string MasterVolumeKey = "MasterVolume";

    private static AudioManager instance = null;
    public static AudioManager Instance { get => instance; }

    public AudioClip clickSound;

    private AudioSource audioSource;
    private string nextSceneName;
    private float volumeBeforeMute = 1f;
    private bool soundEnabled = true;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            //Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            ApplySavedSoundSetting();
        }

        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public bool IsSoundEnabled()
    {
        return soundEnabled;
    }

    public float GetMasterVolume()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
    }

    public void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        volumeBeforeMute = volume;
        PlayerPrefs.SetFloat(MasterVolumeKey, volume);
        PlayerPrefs.Save();

        if (soundEnabled)
        {
            AudioListener.volume = volume;
        }
    }

    public void SetSoundEnabled(bool enabled)
    {
        if (enabled)
        {
            if (!soundEnabled)
            {
                float volume = GetMasterVolume();
                volumeBeforeMute = volume;
                AudioListener.volume = volume;
            }
            soundEnabled = true;
        }
        else
        {
            if (soundEnabled)
            {
                float current = AudioListener.volume > 0f ? AudioListener.volume : volumeBeforeMute;
                SetMasterVolume(current);
            }
            AudioListener.volume = 0f;
            soundEnabled = false;
        }

        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplySavedSoundSetting()
    {
        volumeBeforeMute = GetMasterVolume();
        soundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;

        if (soundEnabled)
        {
            AudioListener.volume = volumeBeforeMute;
        }
        else
        {
            AudioListener.volume = 0f;
        }
    }

    public void PlaySound()
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    public void PlaySoundAndTransition(string sceneName)
    {
        if (clickSound != null)
        {
            audioSource.clip = clickSound;
            audioSource.loop = false;
            audioSource.Play();
            nextSceneName = sceneName;
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            nextSceneName = "";
        }
    }

    void Update()
    {
        if (!audioSource.isPlaying && !string.IsNullOrEmpty(nextSceneName))
        {
            LoadNextScene();
        }
    }
}
