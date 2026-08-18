using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private const string SoundEnabledKey = "SoundEnabled";
    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicEnabledKey = "MusicEnabled";
    private const string MusicVolumeKey = "MusicVolume";
    private const string MusicTrackIndexKey = "MusicTrackIndex";
    private const string MusicTrackTimeKey = "MusicTrackTime";
    private const float DefaultMusicVolume = 0.1f;
    private const float FadeDurationSeconds = 5f;

    private static AudioManager instance = null;
    public static AudioManager Instance { get => instance; }

    public AudioClip clickSound;
    public AudioClip[] musicPlaylist;

    private AudioSource audioSource;
    private AudioSource musicSource;
    private string nextSceneName;
    private float volumeBeforeMute = 1f;
    private bool soundEnabled = true;
    private float musicVolumeBeforeMute = DefaultMusicVolume;
    private bool musicEnabled = true;
    private int currentTrackIndex;
    private float fadeMultiplier = 1f;
    private Coroutine fadeRoutine;
    private bool isFading;
    private bool suppressSaveOnDestroy;
    private bool musicPausedByToggle;
    private bool playbackSuspended;
    private bool hadActivePlayback;

    void Awake()
    {
        int restoreIndex = PlayerPrefs.GetInt(MusicTrackIndexKey, 0);
        float restoreTime = PlayerPrefs.GetFloat(MusicTrackTimeKey, 0f);

        // Keep the scene instance UI buttons reference. Destroy the older
        // persistent copy so MainMenu/Level1 OnClick targets stay valid.
        if (instance != null && instance != this)
        {
            restoreIndex = instance.currentTrackIndex;
            restoreTime = instance.GetPlaybackTime();
            fadeMultiplier = instance.fadeMultiplier;
            instance.PrepareForReplacement();
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
        musicVolumeBeforeMute = GetMusicVolume();
        musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        RestorePlayback(restoreIndex, restoreTime);
        SavePlaylistPosition();
    }

    void Update()
    {
        UpdateMusicFadeWatch();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SavePlaylistPosition();
            StopFadeRoutine();
            playbackSuspended = true;
        }
        else
        {
            playbackSuspended = false;
            RestoreFromPlayerPrefs();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SavePlaylistPosition();
            StopFadeRoutine();
            playbackSuspended = true;
        }
        else
        {
            playbackSuspended = false;
            RestoreFromPlayerPrefs();
        }
    }

    void OnApplicationQuit()
    {
        SavePlaylistPosition();
    }

    void OnDestroy()
    {
        if (!suppressSaveOnDestroy)
        {
            SavePlaylistPosition();
        }

        StopFadeRoutine();

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
        musicSource.loop = false;
        musicSource.spatialBlend = 0f;
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

    void ApplyMusicVolume()
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.volume = musicVolumeBeforeMute * fadeMultiplier;
    }

    void ApplyMusicPlaybackState()
    {
        if (musicSource == null)
        {
            return;
        }

        ApplyMusicVolume();

        if (musicEnabled)
        {
            if (musicSource.clip == null)
            {
                RestoreFromPlayerPrefs();
                return;
            }

            musicPausedByToggle = false;
            musicSource.UnPause();
            if (!musicSource.isPlaying)
            {
                float time = GetPlaybackTime();
                PlayClipAtTime(musicSource.clip, time);
            }

            UpdateMusicFadeWatch();
        }
        else
        {
            SavePlaylistPosition();
            StopFadeRoutine();
            musicPausedByToggle = true;
            if (musicSource.isPlaying)
            {
                musicSource.Pause();
            }
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
        ApplyMusicVolume();
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

    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(SoundEnabledKey);
        PlayerPrefs.DeleteKey(MasterVolumeKey);
        PlayerPrefs.DeleteKey(MusicEnabledKey);
        PlayerPrefs.DeleteKey(MusicVolumeKey);
        PlayerPrefs.Save();
        ApplySavedSoundSetting();
        musicVolumeBeforeMute = GetMusicVolume();
        musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        ApplyMusicPlaybackState();
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

    void PrepareForReplacement()
    {
        suppressSaveOnDestroy = true;
        StopFadeRoutine();
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    void RestoreFromPlayerPrefs()
    {
        int index = PlayerPrefs.GetInt(MusicTrackIndexKey, 0);
        float time = PlayerPrefs.GetFloat(MusicTrackTimeKey, 0f);
        RestorePlayback(index, time);
    }

    void RestorePlayback(int index, float time)
    {
        int count = PlaylistCount();
        if (musicSource == null || count == 0)
        {
            return;
        }

        currentTrackIndex = ((index % count) + count) % count;
        AudioClip clip = GetPlaylistClip(currentTrackIndex);
        if (clip == null)
        {
            return;
        }

        bool alreadyOnTrack = musicSource.clip == clip;
        bool playbackMatches = musicEnabled ? musicSource.isPlaying : !musicSource.isPlaying;

        if (alreadyOnTrack && playbackMatches)
        {
            ApplyMusicVolume();
            if (musicEnabled)
            {
                UpdateMusicFadeWatch();
            }
            return;
        }

        PlayClipAtTime(clip, time);
        ApplyMusicVolume();

        if (!musicEnabled)
        {
            StopFadeRoutine();
            musicPausedByToggle = true;
            musicSource.Pause();
            return;
        }

        musicPausedByToggle = false;

        UpdateMusicFadeWatch();
    }

    void PlayClipAtTime(AudioClip clip, float time)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        musicSource.loop = false;
        musicSource.clip = clip;
        musicSource.Play();

        float seek = time;
        if (clip.length > 0f)
        {
            seek = Mathf.Clamp(time, 0f, Mathf.Max(0f, clip.length - 0.05f));
        }
        else
        {
            seek = Mathf.Max(0f, time);
        }

        if (seek > 0f)
        {
            musicSource.time = seek;
        }

        hadActivePlayback = musicEnabled;
    }

    void PlayNextTrack()
    {
        int count = PlaylistCount();
        if (count == 0)
        {
            return;
        }

        int startIndex = currentTrackIndex;
        for (int i = 0; i < count; i++)
        {
            currentTrackIndex = (currentTrackIndex + 1) % count;
            AudioClip clip = GetPlaylistClip(currentTrackIndex);
            if (clip != null)
            {
                PlayClipAtTime(clip, 0f);
                return;
            }
        }

        currentTrackIndex = startIndex;
    }

    int PlaylistCount()
    {
        return musicPlaylist != null ? musicPlaylist.Length : 0;
    }

    AudioClip GetPlaylistClip(int index)
    {
        int count = PlaylistCount();
        if (count == 0)
        {
            return null;
        }

        index = ((index % count) + count) % count;
        return musicPlaylist[index];
    }

    float GetPlaybackTime()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            return 0f;
        }

        return musicSource.time;
    }

    float GetRemainingTime()
    {
        if (musicSource == null || musicSource.clip == null)
        {
            return 0f;
        }

        float length = musicSource.clip.length;
        if (length <= 0f)
        {
            return float.MaxValue;
        }

        return length - musicSource.time;
    }

    void SavePlaylistPosition()
    {
        if (musicSource == null)
        {
            return;
        }

        PlayerPrefs.SetInt(MusicTrackIndexKey, currentTrackIndex);
        PlayerPrefs.SetFloat(MusicTrackTimeKey, GetPlaybackTime());
        PlayerPrefs.Save();
    }

    void UpdateMusicFadeWatch()
    {
        if (playbackSuspended || isFading || !musicEnabled || musicSource == null || musicSource.clip == null)
        {
            return;
        }

        if (!musicSource.isPlaying)
        {
            if (hadActivePlayback && !musicPausedByToggle)
            {
                hadActivePlayback = false;
                StartFadeRoutine(FadeOutThenNext(0f));
            }
            return;
        }

        hadActivePlayback = true;

        float remaining = GetRemainingTime();
        if (remaining <= FadeDurationSeconds)
        {
            StartFadeRoutine(FadeOutThenNext(Mathf.Max(0f, remaining)));
            return;
        }

        if (fadeMultiplier < 0.999f)
        {
            float remainingFadeIn = FadeDurationSeconds * (1f - fadeMultiplier);
            StartFadeRoutine(FadeTo(1f, remainingFadeIn));
        }
    }

    void StartFadeRoutine(IEnumerator routine)
    {
        StopFadeRoutine();
        fadeRoutine = StartCoroutine(routine);
    }

    void StopFadeRoutine()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        isFading = false;
    }

    IEnumerator FadeOutThenNext(float duration)
    {
        isFading = true;
        yield return FadeVolume(0f, duration);

        if (!musicEnabled)
        {
            isFading = false;
            fadeRoutine = null;
            yield break;
        }

        PlayNextTrack();
        fadeMultiplier = 0f;
        ApplyMusicVolume();
        yield return FadeVolume(1f, FadeDurationSeconds);
        isFading = false;
        fadeRoutine = null;
    }

    IEnumerator FadeTo(float targetMultiplier, float duration)
    {
        isFading = true;
        yield return FadeVolume(targetMultiplier, duration);
        isFading = false;
        fadeRoutine = null;
    }

    IEnumerator FadeVolume(float targetMultiplier, float duration)
    {
        float start = fadeMultiplier;
        if (duration <= 0f)
        {
            fadeMultiplier = targetMultiplier;
            ApplyMusicVolume();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!musicEnabled)
            {
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            fadeMultiplier = Mathf.Lerp(start, targetMultiplier, Mathf.Clamp01(elapsed / duration));
            ApplyMusicVolume();
            yield return null;
        }

        fadeMultiplier = targetMultiplier;
        ApplyMusicVolume();
    }
}
