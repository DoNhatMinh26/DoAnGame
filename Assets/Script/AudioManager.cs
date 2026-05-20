using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [Header("Bộ phát âm thanh")]
    public AudioSource musicSource; // Dùng cho nhạc nền (Loop)
    public AudioSource sfxSource;   // Dùng cho hiệu ứng (Ting, Click...)

    [Header("Audio hiệu ứng chung")]
    public AudioClip soundCorrect;
    public AudioClip soundWrong;
    public AudioClip soundClick;
    public AudioClip soundWin;
    public AudioClip soundLose;
    public AudioClip soundCoin;
    // (removed: UI hover/panel and timer warning/tick clips)

    [Header("Countdown Voice Clips")]
    public AudioClip countdown3Clip;
    public AudioClip countdown2Clip;
    public AudioClip countdown1Clip;
    public AudioClip countdownReadyClip;
    public AudioClip countdownGoClip;

    [Header("Audio Menu chính")]
    public AudioClip musicMenu;       // Nhạc ở Menu chính

    [Header("Audio Game Class")]
    public AudioClip musicClassMode;  // Nhạc chế độ Lớp học (UiClass)

    [Header("Audio Game Phòng thủ")]
    public AudioClip musicDefenseMode; // Nhạc chế độ Phòng thủ (UiTp)

    [Header("Audio Game Phi thuyền")]
    public AudioClip musicSpaceMode;   // Nhạc chế độ Không gian

    [Header("Audio Game Multiplayer")]
    public AudioClip musicMultiplayer; // Nhạc chế độ Multiplayer

    [Header("Cài đặt âm thanh")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private Coroutine fadeMusicCoroutine;

    private void Awake()
    {
        // Singleton: Giữ cho hệ thống âm thanh tồn tại xuyên suốt các Scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Tự động phát nhạc nền khi game bắt đầu
        PlayMusic(musicMenu);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Khi quay về Main Menu scene từ Win/Lose, nhạc có thể đã bị Stop trước đó.
        if (scene.name == "GameUIPlay 1" && (musicSource == null || !musicSource.isPlaying))
        {
            PlayMainMenuMusic();
        }
    }

    // Hàm phát nhạc nền (lặp lại)
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.volume = masterVolume * musicVolume;
        musicSource.Play();
    }

    // Hàm phát nhạc nền với fade in
    public void PlayMusicWithFade(AudioClip clip, float fadeDuration = 0.5f)
    {
        if (clip == null || musicSource == null) return;

        // Dừng fade coroutine cũ nếu có
        if (fadeMusicCoroutine != null)
        {
            StopCoroutine(fadeMusicCoroutine);
        }

        fadeMusicCoroutine = StartCoroutine(FadeMusicCoroutine(clip, fadeDuration));
    }

    private IEnumerator FadeMusicCoroutine(AudioClip clip, float fadeDuration)
    {
        if (fadeDuration <= 0f)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = masterVolume * musicVolume;
            musicSource.Play();
            yield break;
        }

        // Fade out nhạc cũ
        float elapsedTime = 0f;
        float startVolume = musicSource.volume;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeDuration);
            yield return null;
        }

        // Chuyển clip
        musicSource.clip = clip;
        musicSource.loop = true;

        // Fade in nhạc mới
        elapsedTime = 0f;
        float targetVolume = masterVolume * musicVolume;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsedTime / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        musicSource.Play();
    }

    // Hàm phát hiệu ứng âm thanh (phát một lần)
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.volume = masterVolume * sfxVolume;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayWinSFX()
    {
        StopMusicImmediatelyForResult();
        PlaySFX(soundWin);
    }

    public void PlayLoseSFX()
    {
        StopMusicImmediatelyForResult();
        PlaySFX(soundLose);
    }

    private void StopMusicImmediatelyForResult()
    {
        if (fadeMusicCoroutine != null)
        {
            StopCoroutine(fadeMusicCoroutine);
            fadeMusicCoroutine = null;
        }

        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // (removed public helpers for hover/panel/timer clips)

    // Countdown public API
    public void PlayCountdown3()
    {
        PlaySFX(countdown3Clip);
    }

    public void PlayCountdown2()
    {
        PlaySFX(countdown2Clip);
    }

    public void PlayCountdown1()
    {
        PlaySFX(countdown1Clip);
    }

    public void PlayCountdownReady()
    {
        PlaySFX(countdownReadyClip);
    }

    public void PlayCountdownGo()
    {
        PlaySFX(countdownGoClip);
    }

    // Dừng nhạc nền
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    // Tạm dừng nhạc nền
    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    // Tiếp tục phát nhạc nền
    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    // Điều chỉnh âm lượng chính
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    // Điều chỉnh âm lượng nhạc nền
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    // Điều chỉnh âm lượng hiệu ứng
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    // Cập nhật âm lượng cho tất cả nguồn âm thanh
    private void UpdateVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = masterVolume * musicVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = masterVolume * sfxVolume;
        }
    }

    // ===== Các phương thức chế độ game =====
    public void PlayClassModeMusic()
    {
        PlayMusicWithFade(musicClassMode);
    }

    public void PlayDefenseModeMusic()
    {
        PlayMusicWithFade(musicDefenseMode);
    }

    public void PlaySpaceModeMusic()
    {
        PlayMusicWithFade(musicSpaceMode);
    }

    public void PlayMultiplayerModeMusic()
    {
        PlayMusicWithFade(musicMultiplayer);
    }

    public void PlayMainMenuMusic()
    {
        PlayMusicWithFade(musicMenu);
    }
}
