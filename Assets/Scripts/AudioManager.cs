using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private const string SoundEnabledKey = "SoundEnabled";
    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicEnabledKey = "MusicEnabled";
    private const string MusicVolumeKey = "MusicVolume";
    private const float DefaultMusicVolume = 0.1f;

    private static AudioManager instance = null;
    public static AudioManager Instance { get => instance; }

    public AudioClip clickSound;
    public AudioClip musicClip;

    private AudioSource audioSource;
    private AudioSource musicSource;
    private string nextSceneName;
    private float volumeBeforeMute = 1f;
    private bool soundEnabled = true;
    private float musicVolumeBeforeMute = DefaultMusicVolume;
    private bool musicEnabled = true;

    void Awake()
    {
        // Keep the scene instance UI buttons reference. Destroy the older
        // persistent copy so MainMenu/Level1 OnClick targets stay valid.
        if (instance != null && instance != this)
        {
            Destroy(instance.gameObject);
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        SetupMusicSource();
        AudioListener.volume = 1f;
        ApplySavedSoundSetting();
        ApplySavedMusicSetting();
        EnsureMusicPlaying();
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void SetupMusicSource()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length > 1)
        {
            musicSource = sources[1];
        }
        else
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        if (musicClip != null)
        {
            musicSource.clip = musicClip;
        }
    }

    void ApplySfxVolume()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.volume = soundEnabled ? volumeBeforeMute : 0f;
        audioSource.mute = !soundEnabled;
    }

    void ApplyMusicPlaybackState()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.volume = musicVolumeBeforeMute;

        if (musicEnabled)
        {
            if (musicSource.clip == null || musicSource.isPlaying)
            {
                return;
            }

            musicSource.UnPause();
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
        else if (musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    void EnsureMusicPlaying()
    {
        if (musicSource == null || musicClip == null)
        {
            return;
        }

        if (musicSource.clip != musicClip)
        {
            musicSource.clip = musicClip;
        }

        if (!musicEnabled)
        {
            return;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
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
        ApplySfxVolume();
    }

    public void SetSoundEnabled(bool enabled)
    {
        if (enabled)
        {
            if (!soundEnabled)
            {
                volumeBeforeMute = GetMasterVolume();
            }
            soundEnabled = true;
        }
        else
        {
            if (soundEnabled)
            {
                volumeBeforeMute = GetMasterVolume();
            }
            soundEnabled = false;
        }

        PlayerPrefs.SetInt(SoundEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    private void ApplySavedSoundSetting()
    {
        volumeBeforeMute = GetMasterVolume();
        soundEnabled = PlayerPrefs.GetInt(SoundEnabledKey, 1) == 1;
        ApplySfxVolume();
    }

    public bool IsMusicEnabled()
    {
        return musicEnabled;
    }

    public float GetMusicVolume()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        musicVolumeBeforeMute = volume;
        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
        PlayerPrefs.Save();

        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }

    public void SetMusicEnabled(bool enabled)
    {
        if (enabled)
        {
            if (!musicEnabled)
            {
                musicVolumeBeforeMute = GetMusicVolume();
            }
            musicEnabled = true;
        }
        else
        {
            if (musicEnabled)
            {
                musicVolumeBeforeMute = GetMusicVolume();
            }
            musicEnabled = false;
        }

        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicPlaybackState();
    }

    private void ApplySavedMusicSetting()
    {
        musicVolumeBeforeMute = GetMusicVolume();
        musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        ApplyMusicPlaybackState();
    }

    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(SoundEnabledKey);
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(MusicEnabledKey);
        PlayerPrefs.DeleteKey(MusicVolumeKey);
        PlayerPrefs.Save();
        ApplySavedSoundSetting();
        ApplySavedMusicSetting();
        EnsureMusicPlaying();
    }

    public void PlaySound()
    {
        if (instance != null && instance != this)
        {
            instance.PlaySound();
            return;
        }

        if (clickSound != null && soundEnabled)
        {
            audioSource.PlayOneShot(clickSound, volumeBeforeMute);
        }
    }

    public void PlaySoundAndTransition(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        // If a stale scene copy somehow receives the click, forward to the live singleton.
        if (instance != null && instance != this)
        {
            instance.PlaySoundAndTransition(sceneName);
            return;
        }

        nextSceneName = sceneName;

        if (clickSound != null && soundEnabled && audioSource != null)
        {
            audioSource.clip = clickSound;
            audioSource.loop = false;
            audioSource.volume = volumeBeforeMute;
            audioSource.mute = false;
            audioSource.Play();
            StartCoroutine(LoadNextSceneAfterClickSound());
            return;
        }

        LoadNextScene();
    }

    IEnumerator LoadNextSceneAfterClickSound()
    {
        const float maxWaitSeconds = 2f;
        float elapsed = 0f;

        // Wait until SFX ends, but never block scene load indefinitely.
        while (audioSource != null && audioSource.isPlaying && elapsed < maxWaitSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
            return;

        string scene = nextSceneName;
        nextSceneName = "";
        SceneManager.LoadScene(scene);
    }
}
