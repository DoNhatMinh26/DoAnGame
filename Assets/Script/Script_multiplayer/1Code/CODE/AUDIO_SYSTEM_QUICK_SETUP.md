# 🎵 Audio System - Quick Setup Guide

## 📋 Tổng Quan

Hướng dẫn này sẽ giúp bạn:
1. Tạo AudioManager để quản lý âm thanh
2. Tích hợp với SettingsPopup hiện có (slider âm lượng)
3. Thêm âm thanh cho countdown "3, 2, 1, Ready, GO!"
4. Thêm âm thanh click cho buttons
5. Setup trong 30 phút!

---

## 🚀 BƯỚC 1: Tạo AudioManager Script

### 1.1. Tạo file mới
**Path**: `Assets/Script/Script_multiplayer/1Code/CODE/AudioManager.cs`

### 1.2. Copy code sau:

```csharp
using UnityEngine;
using System.Collections;

namespace DoAnGame.Audio
{
    /// <summary>
    /// Quản lý tất cả âm thanh trong game
    /// Singleton - DontDestroyOnLoad
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Music Clips")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip battleMusic;
        [SerializeField] private AudioClip victoryMusic;

        [Header("UI Sound Effects")]
        [SerializeField] private AudioClip buttonClickSound;

        [Header("Countdown Voice")]
        [SerializeField] private AudioClip countdown3Sound;
        [SerializeField] private AudioClip countdown2Sound;
        [SerializeField] private AudioClip countdown1Sound;
        [SerializeField] private AudioClip countdownReadySound;
        [SerializeField] private AudioClip countdownGoSound;

        [Header("Battle Sound Effects")]
        [SerializeField] private AudioClip correctAnswerSound;
        [SerializeField] private AudioClip wrongAnswerSound;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        private const string MASTER_VOLUME_KEY = "GameVolume"; // Dùng chung với SettingsPopup

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadVolumeSettings();

            Debug.Log("[AudioManager] Initialized");
        }

        private void InitializeAudioSources()
        {
            // Tạo AudioSource nếu chưa có
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (uiSource == null)
            {
                uiSource = gameObject.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }

            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
            }
        }

        private void LoadVolumeSettings()
        {
            // Load master volume từ PlayerPrefs (sync với SettingsPopup)
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            ApplyVolumeSettings();
        }

        private void ApplyVolumeSettings()
        {
            if (musicSource != null) musicSource.volume = musicVolume * masterVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume * masterVolume;
            if (uiSource != null) uiSource.volume = sfxVolume * masterVolume;
            if (voiceSource != null) voiceSource.volume = sfxVolume * masterVolume;
        }

        #region PUBLIC API

        /// <summary>
        /// Set master volume (0-1) - Gọi từ SettingsPopup
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
            Debug.Log($"[AudioManager] Master volume set to {masterVolume:F2}");
        }

        /// <summary>
        /// Phát nhạc nền
        /// </summary>
        public void PlayMusic(AudioClip clip, bool fadeIn = false)
        {
            if (clip == null || musicSource == null) return;

            if (fadeIn)
            {
                StartCoroutine(FadeMusicRoutine(clip, 1f));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Dừng nhạc nền
        /// </summary>
        public void StopMusic(bool fadeOut = false)
        {
            if (musicSource == null) return;

            if (fadeOut)
            {
                StartCoroutine(FadeOutMusicRoutine(1f));
            }
            else
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Phát âm thanh click button
        /// </summary>
        public void PlayButtonClick()
        {
            PlayUISound(buttonClickSound);
        }

        /// <summary>
        /// Phát âm thanh countdown "3"
        /// </summary>
        public void PlayCountdown3()
        {
            PlayVoiceSound(countdown3Sound);
        }

        /// <summary>
        /// Phát âm thanh countdown "2"
        /// </summary>
        public void PlayCountdown2()
        {
            PlayVoiceSound(countdown2Sound);
        }

        /// <summary>
        /// Phát âm thanh countdown "1"
        /// </summary>
        public void PlayCountdown1()
        {
            PlayVoiceSound(countdown1Sound);
        }

        /// <summary>
        /// Phát âm thanh "Ready"
        /// </summary>
        public void PlayCountdownReady()
        {
            PlayVoiceSound(countdownReadySound);
        }

        /// <summary>
        /// Phát âm thanh "GO!"
        /// </summary>
        public void PlayCountdownGo()
        {
            PlayVoiceSound(countdownGoSound);
        }

        /// <summary>
        /// Phát âm thanh trả lời đúng
        /// </summary>
        public void PlayCorrectAnswer()
        {
            PlaySFX(correctAnswerSound);
        }

        /// <summary>
        /// Phát âm thanh trả lời sai
        /// </summary>
        public void PlayWrongAnswer()
        {
            PlaySFX(wrongAnswerSound);
        }

        #endregion

        #region PRIVATE METHODS

        private void PlayUISound(AudioClip clip)
        {
            if (clip != null && uiSource != null)
            {
                uiSource.PlayOneShot(clip);
            }
        }

        private void PlayVoiceSound(AudioClip clip)
        {
            if (clip != null && voiceSource != null)
            {
                voiceSource.PlayOneShot(clip);
            }
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        private IEnumerator FadeMusicRoutine(AudioClip newClip, float duration)
        {
            // Fade out
            float startVolume = musicSource.volume;
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0, t / (duration / 2));
                yield return null;
            }

            // Switch clip
            musicSource.Stop();
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in
            float targetVolume = musicVolume * masterVolume;
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0, targetVolume, t / (duration / 2));
                yield return null;
            }

            musicSource.volume = targetVolume;
        }

        private IEnumerator FadeOutMusicRoutine(float duration)
        {
            float startVolume = musicSource.volume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
                yield return null;
            }
            musicSource.Stop();
            musicSource.volume = musicVolume * masterVolume;
        }

        #endregion
    }
}
```

---

## 🔧 BƯỚC 2: Cập Nhật SettingsPopupController

Sửa file `Assets/Script/Script_multiplayer/1Code/CODE/SettingsPopupController.cs`:

### 2.1. Thêm using statement ở đầu file:

```csharp
using DoAnGame.Audio; // ← THÊM DÒNG NÀY
```

### 2.2. Sửa method `OnVolumeChanged`:

**TÌM:**
```csharp
private void OnVolumeChanged(float value)
{
    // Lưu volume
    PlayerPrefs.SetFloat(VOLUME_KEY, value);
    PlayerPrefs.Save();

    // Cập nhật text hiển thị
    UpdateVolumeText(value);

    // TODO: Áp dụng volume vào AudioListener
    // AudioListener.volume = value;

    Debug.Log($"[SettingsPopup] Volume changed: {value:F2} ({Mathf.RoundToInt(value * 100)}%)");
}
```

**THAY BẰNG:**
```csharp
private void OnVolumeChanged(float value)
{
    // Lưu volume
    PlayerPrefs.SetFloat(VOLUME_KEY, value);
    PlayerPrefs.Save();

    // Cập nhật text hiển thị
    UpdateVolumeText(value);

    // ✅ Áp dụng volume vào AudioManager
    if (AudioManager.Instance != null)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    Debug.Log($"[SettingsPopup] Volume changed: {value:F2} ({Mathf.RoundToInt(value * 100)}%)");
}
```

---

## 🎬 BƯỚC 3: Thêm Âm Thanh Vào Countdown

Sửa file `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerBattleController.cs`:

### 3.1. Thêm using statement:

```csharp
using DoAnGame.Audio; // ← THÊM DÒNG NÀY
```

### 3.2. Sửa method `CountdownRoutine`:

**TÌM:**
```csharp
private System.Collections.IEnumerator CountdownRoutine()
{
    // Delay nhỏ trước khi bắt đầu countdown
    yield return new WaitForSeconds(0.5f);

    // Countdown: 3, 2, 1
    for (int i = 3; i >= 1; i--)
    {
        if (battleStatusText != null)
        {
            battleStatusText.SetText(i.ToString());
        }
        Debug.Log($"[BattleController] Countdown: {i}");
        yield return new WaitForSeconds(1f);
    }

    // Ready
    if (battleStatusText != null)
    {
        battleStatusText.SetText("Ready");
    }
    Debug.Log("[BattleController] Countdown: Ready");
    yield return new WaitForSeconds(1f);

    // GO!
    if (battleStatusText != null)
    {
        battleStatusText.SetText("GO!");
    }
    Debug.Log("[BattleController] Countdown: GO!");
    yield return new WaitForSeconds(1f);

    // Hiển thị tất cả UI và bắt đầu timer
    ShowAllBattleUI();
    StartQuestionTimer();
    
    Debug.Log("[BattleController] ✅ Countdown complete, battle started!");
}
```

**THAY BẰNG:**
```csharp
private System.Collections.IEnumerator CountdownRoutine()
{
    // Delay nhỏ trước khi bắt đầu countdown
    yield return new WaitForSeconds(0.5f);

    // Countdown: 3, 2, 1
    for (int i = 3; i >= 1; i--)
    {
        if (battleStatusText != null)
        {
            battleStatusText.SetText(i.ToString());
        }
        
        // ✅ PHÁT ÂM THANH COUNTDOWN
        if (i == 3) AudioManager.Instance?.PlayCountdown3();
        else if (i == 2) AudioManager.Instance?.PlayCountdown2();
        else if (i == 1) AudioManager.Instance?.PlayCountdown1();
        
        Debug.Log($"[BattleController] Countdown: {i}");
        yield return new WaitForSeconds(1f);
    }

    // Ready
    if (battleStatusText != null)
    {
        battleStatusText.SetText("Ready");
    }
    AudioManager.Instance?.PlayCountdownReady(); // ✅ PHÁT ÂM THANH "READY"
    Debug.Log("[BattleController] Countdown: Ready");
    yield return new WaitForSeconds(1f);

    // GO!
    if (battleStatusText != null)
    {
        battleStatusText.SetText("GO!");
    }
    AudioManager.Instance?.PlayCountdownGo(); // ✅ PHÁT ÂM THANH "GO!"
    Debug.Log("[BattleController] Countdown: GO!");
    yield return new WaitForSeconds(1f);

    // Hiển thị tất cả UI và bắt đầu timer
    ShowAllBattleUI();
    StartQuestionTimer();
    
    Debug.Log("[BattleController] ✅ Countdown complete, battle started!");
}
```

### 3.3. Thêm âm thanh vào `HandleAnswerResult`:

**TÌM dòng này** (khoảng line 600-650):
```csharp
private void HandleAnswerResult(int winnerId, bool correct, long responseTimeMs)
{
    var net = NetworkManager.Singleton;
    string role = net != null && net.IsHost ? "HOST" : "CLIENT";
    
    Debug.Log($"[BattleController] [{role}] HandleAnswerResult CALLED: Winner={winnerId}, Correct={correct}, Time={responseTimeMs}ms");
```

**THÊM SAU DÒNG Debug.Log:**
```csharp
    // ✅ PHÁT ÂM THANH KẾT QUẢ
    bool isLocalWinner = net != null && ((net.IsHost && winnerId == 0) || (!net.IsHost && winnerId == 1));
    if (winnerId == -1)
    {
        // Cả 2 đều sai
        AudioManager.Instance?.PlayWrongAnswer();
    }
    else if (isLocalWinner)
    {
        // Bạn trả lời đúng
        AudioManager.Instance?.PlayCorrectAnswer();
    }
    else
    {
        // Đối thủ trả lời đúng
        AudioManager.Instance?.PlayWrongAnswer();
    }
```

---

## 🔘 BƯỚC 4: Thêm Âm Thanh Click Cho Buttons (Tự Động)

### 4.1. Tạo file mới
**Path**: `Assets/Script/Script_multiplayer/1Code/CODE/UIButtonAudioHelper.cs`

### 4.2. Copy code sau:

```csharp
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Audio;

namespace DoAnGame.UI
{
    /// <summary>
    /// Tự động thêm âm thanh click cho tất cả buttons trong Canvas
    /// Attach vào Canvas root
    /// </summary>
    public class UIButtonAudioHelper : MonoBehaviour
    {
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool includeInactiveButtons = true;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupAllButtons();
            }
        }

        /// <summary>
        /// Tự động tìm và setup tất cả buttons
        /// </summary>
        public void SetupAllButtons()
        {
            Button[] buttons = GetComponentsInChildren<Button>(includeInactiveButtons);
            
            int count = 0;
            foreach (Button button in buttons)
            {
                if (button == null) continue;

                // Xóa listener cũ (tránh duplicate)
                button.onClick.RemoveListener(PlayButtonClickSound);
                
                // Thêm listener mới
                button.onClick.AddListener(PlayButtonClickSound);
                count++;
            }

            Debug.Log($"[UIButtonAudioHelper] Setup {count} buttons with click sound in {name}");
        }

        private void PlayButtonClickSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }
    }
}
```

---

## 🎮 BƯỚC 5: Setup Trong Unity Editor

### 5.1. Tạo AudioManager GameObject

1. **Mở scene đầu tiên** (ví dụ: `GameUIPlay 1.unity` hoặc scene login)
2. **Hierarchy** → Right-click → **Create Empty**
3. Đặt tên: `AudioManager`
4. **Add Component** → Tìm `AudioManager` script → Add
5. **Inspector** sẽ hiển thị các field để gán audio clips

### 5.2. Tạo Thư Mục Audio (Tạm thời để trống)

```
Assets/
└── Audio/
    ├── Music/          (để trống - sẽ thêm sau)
    ├── SFX/
    │   ├── UI/         (để trống - sẽ thêm sau)
    │   ├── Countdown/  (để trống - sẽ thêm sau)
    │   └── Battle/     (để trống - sẽ thêm sau)
```

**Lưu ý**: Bạn có thể để trống các field trong Inspector, game vẫn chạy bình thường (chỉ không có âm thanh).

### 5.3. Setup UIButtonAudioHelper

1. **Hierarchy** → Chọn **Canvas** (hoặc GameUICanvas)
2. **Add Component** → Tìm `UIButtonAudioHelper` → Add
3. ✅ **XONG!** Tất cả buttons trong Canvas này sẽ tự động có click sound

**Lặp lại cho mỗi scene có buttons:**
- Scene Login → Attach vào Canvas
- Scene Menu → Attach vào Canvas
- Scene Lobby → Attach vào Canvas
- Scene Battle → Attach vào Canvas

---

## ✅ BƯỚC 6: Test Ngay (Không Cần Audio Files)

### 6.1. Compile Code

1. **Ctrl + S** (Save all)
2. Quay lại Unity Editor
3. Đợi compile xong (không có errors)

### 6.2. Test Volume Slider

1. **Play** scene
2. Mở **Settings Popup**
3. Kéo **Volume Slider**
4. Check Console: `[AudioManager] Master volume set to 0.XX`

### 6.3. Test Countdown (Không Có Âm Thanh)

1. Vào **Battle scene**
2. Xem countdown "3, 2, 1, Ready, GO!"
3. Check Console: Không có errors khi gọi `AudioManager.Instance?.PlayCountdownX()`

### 6.4. Test Button Click (Không Có Âm Thanh)

1. Click bất kỳ button nào
2. Check Console: `[UIButtonAudioHelper] Setup X buttons...`
3. Không có errors

---

## 🎵 BƯỚC 7: Thêm Audio Files (Sau Này)

Khi bạn có audio files, làm theo:

### 7.1. Import Audio Files

1. Kéo file audio vào thư mục `Assets/Audio/...`
2. Chọn file → **Inspector** → Set import settings:
   - **Music (.mp3)**: Load Type = Streaming, Compression = Vorbis
   - **SFX (.wav)**: Load Type = Decompress On Load, Compression = PCM

### 7.2. Gán Vào AudioManager

1. **Hierarchy** → Chọn `AudioManager`
2. **Inspector** → Kéo từng file vào từng field:
   ```
   Countdown Voice:
     Countdown 3 Sound: [Kéo countdown_3.wav vào đây]
     Countdown 2 Sound: [Kéo countdown_2.wav vào đây]
     Countdown 1 Sound: [Kéo countdown_1.wav vào đây]
     Countdown Ready Sound: [Kéo countdown_ready.wav vào đây]
     Countdown Go Sound: [Kéo countdown_go.wav vào đây]
   
   UI Sound Effects:
     Button Click Sound: [Kéo button_click.wav vào đây]
   
   Battle Sound Effects:
     Correct Answer Sound: [Kéo correct_answer.wav vào đây]
     Wrong Answer Sound: [Kéo wrong_answer.wav vào đây]
   ```

### 7.3. Test Lại

1. **Play** scene
2. Bây giờ sẽ có âm thanh thật!

---

## 📝 Checklist Hoàn Thành

### Code Files
- [ ] Tạo `AudioManager.cs`
- [ ] Tạo `UIButtonAudioHelper.cs`
- [ ] Sửa `SettingsPopupController.cs` (thêm `using DoAnGame.Audio` và sửa `OnVolumeChanged`)
- [ ] Sửa `UIMultiplayerBattleController.cs` (thêm audio calls vào `CountdownRoutine` và `HandleAnswerResult`)

### Unity Setup
- [ ] Tạo AudioManager GameObject trong scene đầu tiên
- [ ] Attach `UIButtonAudioHelper` vào Canvas của mỗi scene
- [ ] Tạo thư mục `Assets/Audio/` (có thể để trống)

### Testing
- [ ] Compile thành công (0 errors)
- [ ] Volume slider hoạt động (check Console)
- [ ] Countdown không có errors
- [ ] Button click không có errors

### Optional (Sau Này)
- [ ] Tìm/tạo audio files
- [ ] Import audio files với settings đúng
- [ ] Gán audio files vào AudioManager Inspector
- [ ] Test âm thanh thật

---

## 🎯 Tóm Tắt

### Những Gì Đã Làm:
1. ✅ Tạo AudioManager quản lý âm thanh
2. ✅ Tích hợp với SettingsPopup (slider âm lượng)
3. ✅ Thêm audio calls vào countdown
4. ✅ Thêm audio calls vào answer result
5. ✅ Tự động thêm click sound cho buttons

### Những Gì Chưa Làm (Không Bắt Buộc):
- ⏳ Tìm/tạo audio files
- ⏳ Gán audio files vào Inspector
- ⏳ Thêm nhạc nền
- ⏳ Thêm timer warning sound

### Kết Quả:
- 🎮 Game chạy bình thường (không có errors)
- 🔊 Volume slider hoạt động
- 🎵 Sẵn sàng thêm audio files bất cứ lúc nào
- 🔘 Buttons sẵn sàng phát click sound

---

## 🆘 Troubleshooting

### Lỗi: "AudioManager not found"
**Nguyên nhân**: Chưa tạo AudioManager GameObject
**Giải pháp**: Tạo GameObject với AudioManager component trong scene đầu tiên

### Lỗi: "Namespace DoAnGame.Audio not found"
**Nguyên nhân**: Chưa thêm `using DoAnGame.Audio;`
**Giải pháp**: Thêm dòng này ở đầu file

### Volume slider không hoạt động
**Nguyên nhân**: AudioManager chưa được khởi tạo
**Giải pháp**: Đảm bảo AudioManager GameObject tồn tại trong scene đầu tiên

### Buttons không có click sound
**Nguyên nhân**: Chưa attach UIButtonAudioHelper vào Canvas
**Giải pháp**: Add component UIButtonAudioHelper vào Canvas root

---

Chúc bạn setup thành công! 🎉
