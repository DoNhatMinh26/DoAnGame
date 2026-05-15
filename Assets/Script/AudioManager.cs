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

    // Hàm phát nhạc nền (lặp lại)
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    // Hàm phát hiệu ứng âm thanh (phát một lần)
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void audioLopHoc()
    {
        
        AudioManager.Instance.PlayMusic(AudioManager.Instance.musicClassMode);
       
    }

    
    public void audioPhongThu()
    {
        AudioManager.Instance.PlayMusic(AudioManager.Instance.musicDefenseMode);
       
    }

   
    public void audioPhiThuyen()
    {
        AudioManager.Instance.PlayMusic(AudioManager.Instance.musicSpaceMode);
       
    }

    
    public void audioMultiplayer()
    {
        AudioManager.Instance.PlayMusic(AudioManager.Instance.musicMultiplayer);
       
    }
    public void audioMenuChinh()
    {
        AudioManager.Instance.PlayMusic(AudioManager.Instance.musicMenu);
        // Code quay về Scene Menu hoặc đóng các Panel chơi game
    }
}
