# 🔊 Hướng Dẫn Thiết Kế Hệ Thống Âm Thanh

## 📋 Mục Lục
1. [Kiến trúc hệ thống](#kiến-trúc-hệ-thống)
2. [Loại âm thanh cần có](#loại-âm-thanh-cần-có)
3. [Implementation chi tiết](#implementation-chi-tiết)
4. [Inspector setup](#inspector-setup)
5. [Best practices](#best-practices)

---

## 🏗️ Kiến Trúc Hệ Thống

### 1. AudioManager (Singleton)
Quản lý tất cả âm thanh trong game - **DontDestroyOnLoad**

```csharp
namespace DoAnGame.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;      // Nhạc nền
        [SerializeField] private AudioSource sfxSource;        // Sound effects
        [SerializeField] private AudioSource uiSource;         // UI sounds (button clicks)
        [SerializeField] private AudioSource voiceSource;      // Voice countdown (3, 2, 1...)

        [Header("Music Clips")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip battleMusic;
        [SerializeField] private AudioClip victoryMusic;
        [SerializeField] private AudioClip defeatMusic;

        [Header("UI Sound Effects")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip panelOpenSound;
        [SerializeField] private AudioClip panelCloseSound;

        [Header("Countdown Voice")]
        [SerializeField] private AudioClip countdown3Sound;
        [SerializeField] private AudioClip countdown2Sound;
        [SerializeField] private AudioClip countdown1Sound;
        [SerializeField] private AudioClip countdownReadySound;
        [SerializeField] private AudioClip countdownGoSound;

        [Header("Battle Sound Effects")]
        [SerializeField] private AudioClip correctAnswerSound;
        [SerializeField] private AudioClip wrongAnswerSound;
        [SerializeField] private AudioClip timerTickSound;
        [SerializeField] private AudioClip timerWarningSound;  // Khi còn 5s
        [SerializeField] private AudioClip healthLostSound;
        [SerializeField] private AudioClip matchEndSound;

        [Header("Volume Settings")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
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

            ApplyVolumeSettings();
        }

        private void ApplyVolumeSettings()
        {
            if (musicSource != null) musicSource.volume = musicVolume;
            if (sfxSource != null) sfxSource.volume = sfxVolume;
            if (uiSource != null) uiSource.volume = uiVolume;
            if (voiceSource != null) voiceSource.volume = voiceVolume;
        }

        #region MUSIC CONTROL

        public void PlayMusic(AudioClip clip, bool fadeIn = true)
        {
            if (clip == null || musicSource == null) return;

            if (fadeIn)
            {
                StartCoroutine(FadeMusic(clip, 1f));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }

        public void StopMusic(bool fadeOut = true)
        {
            if (musicSource == null) return;

            if (fadeOut)
            {
                StartCoroutine(FadeOutMusic(1f));
            }
            else
            {
                musicSource.Stop();
            }
        }

        private System.Collections.IEnumerator FadeMusic(AudioClip newClip, float duration)
        {
            // Fade out current music
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

            // Fade in new music
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0, musicVolume, t / (duration / 2));
                yield return null;
            }

            musicSource.volume = musicVolume;
        }

        private System.Collections.IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = musicSource.volume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
                yield return null;
            }
            musicSource.Stop();
            musicSource.volume = musicVolume;
        }

        #endregion

        #region UI SOUNDS

        public void PlayButtonClick()
        {
            PlayUISound(buttonClickSound);
        }

        public void PlayButtonHover()
        {
            PlayUISound(buttonHoverSound);
        }

        public void PlayPanelOpen()
        {
            PlayUISound(panelOpenSound);
        }

        public void PlayPanelClose()
        {
            PlayUISound(panelCloseSound);
        }

        private void PlayUISound(AudioClip clip)
        {
            if (clip != null && uiSource != null)
            {
                uiSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region COUNTDOWN SOUNDS

        public void PlayCountdown3()
        {
            PlayVoiceSound(countdown3Sound);
        }

        public void PlayCountdown2()
        {
            PlayVoiceSound(countdown2Sound);
        }

        public void PlayCountdown1()
        {
            PlayVoiceSound(countdown1Sound);
        }

        public void PlayCountdownReady()
        {
            PlayVoiceSound(countdownReadySound);
        }

        public void PlayCountdownGo()
        {
            PlayVoiceSound(countdownGoSound);
        }

        private void PlayVoiceSound(AudioClip clip)
        {
            if (clip != null && voiceSource != null)
            {
                voiceSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region BATTLE SOUNDS

        public void PlayCorrectAnswer()
        {
            PlaySFX(correctAnswerSound);
        }

        public void PlayWrongAnswer()
        {
            PlaySFX(wrongAnswerSound);
        }

        public void PlayTimerTick()
        {
            PlaySFX(timerTickSound);
        }

        public void PlayTimerWarning()
        {
            PlaySFX(timerWarningSound);
        }

        public void PlayHealthLost()
        {
            PlaySFX(healthLostSound);
        }

        public void PlayMatchEnd()
        {
            PlaySFX(matchEndSound);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region VOLUME CONTROL

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }
        }

        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            if (uiSource != null)
            {
                uiSource.volume = uiVolume;
            }
        }

        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
            if (voiceSource != null)
            {
                voiceSource.volume = voiceVolume;
            }
        }

        #endregion
    }
}
```

---

## 🎵 Loại Âm Thanh Cần Có

### 1. **Nhạc Nền (Background Music)**
| Tên File | Khi Nào Phát | Đặc Điểm |
|---|---|---|
| `menu_music.mp3` | Menu chính, Lobby | Nhẹ nhàng, thư giãn |/////////
| `battle_music.mp3` | Trong trận đấu | Sôi động, căng thẳng |///////////
| `victory_music.mp3` | Thắng trận | Vui tươi, phấn khích |////////
| `defeat_music.mp3` | Thua trận | Buồn bã nhưng động viên |/////////

### 2. **UI Sound Effects**
| Tên File | Khi Nào Phát | Độ Dài |
|---|---|---|
| `button_click.wav` | Click button | ~0.1s |/////////////
| `button_hover.wav` | Hover button | ~0.05s |
| `panel_open.wav` | Mở panel | ~0.3s |
| `panel_close.wav` | Đóng panel | ~0.2s |

### 3. **Countdown Voice (Quan Trọng!)**
| Tên File | Khi Nào Phát | Độ Dài |
|---|---|---|
| `countdown_3.wav` | Hiển thị "3" | ~0.5s |////////////
| `countdown_2.wav` | Hiển thị "2" | ~0.5s |//////////
| `countdown_1.wav` | Hiển thị "1" | ~0.5s |//////////
| `countdown_ready.wav` | Hiển thị "Ready" | ~0.7s |////////////
| `countdown_go.wav` | Hiển thị "GO!" | ~0.5s |///////////

**Lưu ý**: Phát âm thanh **NGAY KHI** hiển thị text, không delay!

### 4. **Battle Sound Effects**
| Tên File | Khi Nào Phát | Đặc Điểm |
|---|---|---|
| `correct_answer.wav` | Trả lời đúng | Vui tươi, tích cực |/////////////
| `wrong_answer.wav` | Trả lời sai | Tiêu cực nhưng không quá nặng |///////////////
| `timer_tick.wav` | Mỗi giây đếm ngược | Nhẹ, không gây khó chịu |
| `timer_warning.wav` | Còn 5s | Căng thẳng, cảnh báo |
| `health_lost.wav` | Mất máu | Đau đớn nhưng ngắn |/////////////
| `match_end.wav` | Kết thúc trận | Trang trọng |////////////////

---

## 💻 Implementation Chi Tiết

### 1. Tích Hợp Vào UIMultiplayerBattleController

```csharp
// Trong CountdownRoutine()
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
        
        // ✅ PHÁT ÂM THANH NGAY KHI HIỂN THỊ SỐ
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

### 2. Tích Hợp Vào HandleAnswerResult

```csharp
private void HandleAnswerResult(int winnerId, bool correct, long responseTimeMs)
{
    // ... existing code ...

    // ✅ PHÁT ÂM THANH KẾT QUẢ
    if (winnerId == -1)
    {
        // Cả 2 đều sai
        AudioManager.Instance?.PlayWrongAnswer();
    }
    else
    {
        bool isLocalWinner = net != null && ((net.IsHost && winnerId == 0) || (!net.IsHost && winnerId == 1));
        
        if (isLocalWinner)
        {
            // Bạn trả lời đúng
            AudioManager.Instance?.PlayCorrectAnswer();
        }
        else
        {
            // Đối thủ trả lời đúng
            AudioManager.Instance?.PlayWrongAnswer();
        }
    }

    // ... rest of code ...
}
```

### 3. Tích Hợp Vào AnswerSummaryUI (Timer Warning)

```csharp
// Trong Update() của AnswerSummaryUI
private void Update()
{
    if (!isTimerRunning) return;

    timeRemaining -= Time.deltaTime;

    // ✅ PHÁT ÂM THANH CẢNH BÁO KHI CÒN 5S
    if (timeRemaining <= 5f && timeRemaining > 4.9f && !hasPlayedWarning)
    {
        AudioManager.Instance?.PlayTimerWarning();
        hasPlayedWarning = true;
    }

    // Update UI
    if (timerText != null)
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.SetText($"{seconds}s");
    }

    // Hết giờ
    if (timeRemaining <= 0f)
    {
        timeRemaining = 0f;
        isTimerRunning = false;
        OnTimerExpired();
    }
}

private bool hasPlayedWarning = false; // Thêm field này

// Reset flag khi bắt đầu câu hỏi mới
public void StartQuestionTimer()
{
    hasPlayedWarning = false; // Reset flag
    // ... rest of code ...
}
```

### 4. Tích Hợp Button Click Sound (Global)

Tạo component `UIButtonAudioHelper.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Audio;

namespace DoAnGame.UI
{
    /// <summary>
    /// Tự động thêm âm thanh click cho tất cả buttons trong scene
    /// Attach vào Canvas root
    /// </summary>
    public class UIButtonAudioHelper : MonoBehaviour
    {
        [SerializeField] private bool autoSetupOnStart = true;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupAllButtons();
            }
        }

        public void SetupAllButtons()
        {
            // Tìm tất cả buttons trong Canvas này
            Button[] buttons = GetComponentsInChildren<Button>(true);
            
            foreach (Button button in buttons)
            {
                if (button == null) continue;

                // Xóa listener cũ (tránh duplicate)
                button.onClick.RemoveListener(PlayButtonClickSound);
                
                // Thêm listener mới
                button.onClick.AddListener(PlayButtonClickSound);
            }

            Debug.Log($"[UIButtonAudioHelper] Setup {buttons.Length} buttons with click sound");
        }

        private void PlayButtonClickSound()
        {
            AudioManager.Instance?.PlayButtonClick();
        }
    }
}
```

**Cách dùng**: Attach `UIButtonAudioHelper` vào **Canvas root** của mỗi scene → Tự động setup cho TẤT CẢ buttons!

---

## 🎮 Inspector Setup

### 1. Tạo AudioManager GameObject

1. **Hierarchy** → Right-click → Create Empty → Đặt tên `AudioManager`
2. Add Component → `AudioManager` script
3. **Gán AudioClips** vào các field trong Inspector:
   - Music Clips: Kéo file `.mp3` vào
   - UI Sound Effects: Kéo file `.wav` vào
   - Countdown Voice: Kéo file `.wav` vào
   - Battle Sound Effects: Kéo file `.wav` vào

### 2. Cấu Trúc Thư Mục Assets

```
Assets/
├── Audio/
│   ├── Music/
│   │   ├── menu_music.mp3
│   │   ├── battle_music.mp3
│   │   ├── victory_music.mp3
│   │   └── defeat_music.mp3
│   ├── SFX/
│   │   ├── UI/
│   │   │   ├── button_click.wav
│   │   │   ├── button_hover.wav
│   │   │   ├── panel_open.wav
│   │   │   └── panel_close.wav
│   │   ├── Countdown/
│   │   │   ├── countdown_3.wav
│   │   │   ├── countdown_2.wav
│   │   │   ├── countdown_1.wav
│   │   │   ├── countdown_ready.wav
│   │   │   └── countdown_go.wav
│   │   └── Battle/
│   │       ├── correct_answer.wav
│   │       ├── wrong_answer.wav
│   │       ├── timer_tick.wav
│   │       ├── timer_warning.wav
│   │       ├── health_lost.wav
│   │       └── match_end.wav
```

### 3. Import Settings (Quan Trọng!)

**Music (`.mp3`):**
- Load Type: **Streaming**
- Compression Format: **Vorbis**
- Quality: **70-80%**
- Sample Rate: **44100 Hz**

**SFX (`.wav`):**
- Load Type: **Decompress On Load**
- Compression Format: **PCM** (không nén)
- Force To Mono: **✅** (tiết kiệm dung lượng)
- Sample Rate: **22050 Hz** (đủ cho SFX)

**Countdown Voice (`.wav`):**
- Load Type: **Decompress On Load**
- Compression Format: **ADPCM** (nén nhẹ)
- Force To Mono: **✅**
- Sample Rate: **44100 Hz** (giữ chất lượng giọng nói)

---

## 🎯 Best Practices

### 1. **Timing Âm Thanh Countdown**

```csharp
// ❌ SAI - Phát âm thanh SAU khi hiển thị text
battleStatusText.SetText("3");
yield return new WaitForSeconds(0.5f);
AudioManager.Instance?.PlayCountdown3(); // Muộn 0.5s!

// ✅ ĐÚNG - Phát âm thanh NGAY KHI hiển thị text
battleStatusText.SetText("3");
AudioManager.Instance?.PlayCountdown3(); // Đồng bộ!
yield return new WaitForSeconds(1f);
```

### 2. **Fade Music Khi Chuyển Scene**

```csharp
// Trong UIMultiplayerRoomController
protected override void OnShow()
{
    base.OnShow();
    AudioManager.Instance?.PlayMusic(AudioManager.Instance.menuMusic, fadeIn: true);
}

// Trong UIMultiplayerBattleController
private void StartCountdown()
{
    // Chuyển sang nhạc battle với fade
    AudioManager.Instance?.PlayMusic(AudioManager.Instance.battleMusic, fadeIn: true);
    
    // ... rest of code ...
}
```

### 3. **Không Phát Âm Thanh Trùng Lặp**

```csharp
// Trong AnswerSummaryUI
private bool hasPlayedWarning = false;

private void Update()
{
    // ...
    
    // Chỉ phát 1 lần duy nhất khi còn 5s
    if (timeRemaining <= 5f && !hasPlayedWarning)
    {
        AudioManager.Instance?.PlayTimerWarning();
        hasPlayedWarning = true; // Đánh dấu đã phát
    }
}

public void StartQuestionTimer()
{
    hasPlayedWarning = false; // Reset cho câu hỏi mới
    // ...
}
```

### 4. **Volume Control Trong Settings**

```csharp
// Trong SettingsPopupController
[SerializeField] private Slider musicVolumeSlider;
[SerializeField] private Slider sfxVolumeSlider;

private void Start()
{
    // Load saved volume
    musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
    sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

    // Listen to slider changes
    musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
    sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
}

private void OnMusicVolumeChanged(float value)
{
    AudioManager.Instance?.SetMusicVolume(value);
    PlayerPrefs.SetFloat("MusicVolume", value);
}

private void OnSFXVolumeChanged(float value)
{
    AudioManager.Instance?.SetSFXVolume(value);
    PlayerPrefs.SetFloat("SFXVolume", value);
}
```

### 5. **Tối Ưu Performance**

- **Dùng Object Pooling** cho SFX phát nhiều lần (timer tick)
- **Giới hạn số AudioSource** đồng thời (max 10-15)
- **Unload unused audio** khi chuyển scene
- **Compress audio** đúng cách (xem Import Settings)

---

## 📝 Checklist Khi Implement

- [ ] Tạo `AudioManager.cs` trong `Assets/Script/Script_multiplayer/1Code/CODE/`
- [ ] Tạo thư mục `Assets/Audio/` với cấu trúc như trên
- [ ] Import audio files với settings đúng
- [ ] Tạo AudioManager GameObject trong scene đầu tiên (Login/Menu)
- [ ] Gán tất cả AudioClips vào AudioManager Inspector
- [ ] Thêm audio calls vào `UIMultiplayerBattleController.CountdownRoutine()`
- [ ] Thêm audio calls vào `HandleAnswerResult()`
- [ ] Thêm timer warning vào `AnswerSummaryUI.Update()`
- [ ] Tạo `UIButtonAudioHelper.cs` và attach vào Canvas
- [ ] Test âm thanh trong Unity Editor
- [ ] Test âm thanh trên Android device
- [ ] Thêm volume sliders vào Settings panel
- [ ] Save/load volume settings với PlayerPrefs

---

## 🎨 Nguồn Âm Thanh Miễn Phí

### Websites
- **Freesound.org** - SFX miễn phí, chất lượng cao
- **Incompetech.com** - Nhạc nền miễn phí (Kevin MacLeod)
- **Zapsplat.com** - SFX và music
- **Mixkit.co** - SFX và music hiện đại

### Unity Asset Store
- **Free Sound Effects Pack** by Olivier Girardot
- **Casual Game BGM** by Cyberwave Orchestra
- **UI Sound Effects** by Little Robot Sound Factory

### Tự Ghi Âm (Countdown Voice)
- Dùng **Audacity** (miễn phí) để ghi và edit
- Ghi giọng nói rõ ràng, nhiệt tình
- Export dạng `.wav`, 44100 Hz, Mono
- Normalize volume về -3dB

---

## 🚀 Roadmap Phát Triển

### Phase 1: Basic (Ưu tiên cao)
- ✅ Button click sounds
- ✅ Countdown voice (3, 2, 1, Ready, GO!)
- ✅ Battle music
- ✅ Correct/wrong answer sounds

### Phase 2: Enhanced
- Timer tick sound (mỗi giây)
- Timer warning sound (còn 5s)
- Health lost sound
- Match end sound
- Panel open/close sounds

### Phase 3: Polish
- Music fade in/out
- Volume control trong Settings
- Mute button
- Sound variations (không lặp lại âm thanh giống nhau)
- Spatial audio cho multiplayer (nghe thấy đối thủ click)

### Phase 4: Advanced
- Dynamic music (thay đổi theo tình huống)
- Voice acting cho characters
- Combo sounds (trả lời đúng liên tiếp)
- Crowd cheering sounds
- Localized voice (tiếng Việt/English)

---

## ⚠️ Lưu Ý Quan Trọng

1. **Đồng Bộ Âm Thanh Với Animation**
   - Phát âm thanh **NGAY KHI** hiển thị text/animation
   - Không delay âm thanh sau animation

2. **Không Làm Phiền Người Chơi**
   - Timer tick sound phải nhẹ nhàng, không gây khó chịu
   - Không phát quá nhiều âm thanh cùng lúc
   - Cho phép tắt âm thanh trong Settings

3. **Test Trên Thiết Bị Thật**
   - Âm thanh trên Editor khác với Android
   - Test với headphones và loa ngoài
   - Kiểm tra latency (độ trễ âm thanh)

4. **Tối Ưu Dung Lượng**
   - Music: Vorbis compression (70-80%)
   - SFX: PCM hoặc ADPCM
   - Mono cho SFX (tiết kiệm 50% dung lượng)

5. **Multiplayer Sync**
   - Countdown sounds phát **local only** (không sync qua network)
   - Battle music phát **local only**
   - Chỉ sync game events, không sync audio

---

Chúc bạn thành công! 🎉
