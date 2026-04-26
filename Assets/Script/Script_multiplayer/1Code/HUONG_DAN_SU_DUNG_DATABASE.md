# 📘 Hướng dẫn sử dụng Database API

## ✅ Đã cập nhật FirebaseManager

FirebaseManager đã được cập nhật để tự động tạo đầy đủ cơ sở dữ liệu khi người dùng đăng ký.

---

## 🎯 Khi đăng ký tài khoản

### Tự động tạo:
1. **users** (1 record) - Thông tin tài khoản
2. **playerData** (1 record) - Thống kê tổng
3. **gameModeProgress** (15 records) - Tiến độ 3 chế độ x 5 lớp

### Code:
```csharp
// Trong UIRegisterPanelController hoặc nơi xử lý đăng ký
bool success = await FirebaseManager.Instance.RegisterAsync(email, password, characterName, age);

if (success)
{
    // ✅ Đã tự động tạo:
    // - users/{uid}
    // - playerData/{uid}
    // - gameModeProgress/{uid}_chonda_1 đến {uid}_phithuyen_5 (15 records)
    
    Debug.Log("Đăng ký thành công! Database đã được tạo.");
}
```

**Lưu ý:** Phải bật `enablePlayerDataSync = true` trong Unity Inspector trên FirebaseManager component!

---

## 🎮 Khi chơi level

### 1. Kiểm tra level có mở khóa không

```csharp
string uid = FirebaseManager.Instance.GetCurrentUser().UserId;
string gameMode = "chonda";  // hoặc "keothada", "phithuyen"
int grade = 1;               // Lớp 1-5
int levelNumber = 5;         // Level muốn chơi

bool isUnlocked = await FirebaseManager.Instance.IsLevelUnlockedAsync(uid, gameMode, grade, levelNumber);

if (isUnlocked)
{
    // Cho phép chơi
    StartLevel(levelNumber);
}
else
{
    // Hiển thị "Level bị khóa"
    ShowLockedMessage();
}
```

---

### 2. Sau khi chơi xong level

```csharp
// Khi người chơi hoàn thành level
string uid = FirebaseManager.Instance.GetCurrentUser().UserId;
string gameMode = "chonda";
int grade = 1;
int levelNumber = 5;
int score = 950;  // Điểm đạt được

// Cập nhật toàn bộ database
await FirebaseManager.Instance.UpdateLevelProgressAsync(uid, gameMode, grade, levelNumber, score);

// ✅ Tự động cập nhật:
// 1. levelProgress/{uid}_{gameMode}_{grade}_{levelNumber}
//    - bestScore (nếu cao hơn)
//    - attempts += 1
//
// 2. gameModeProgress/{uid}_{gameMode}_{grade}
//    - currentLevel = levelNumber
//    - maxLevelUnlocked = levelNumber + 1 (nếu hoàn thành)
//    - totalScore += score
//    - bestScore (nếu cao hơn)
//    - lastPlayed = timestamp
//
// 3. playerData/{uid}
//    - totalScore += score
//    - totalXp += (score / 10)
//    - level = 1 + (totalXp / 100)
//    - gamesPlayed += 1
```

---

## 📊 Hiển thị màn hình chọn level

### 1. Load tiến độ chế độ game

```csharp
string uid = FirebaseManager.Instance.GetCurrentUser().UserId;
string gameMode = "chonda";
int grade = 1;

// Load tiến độ
GameModeProgressData progress = await FirebaseManager.Instance.LoadGameModeProgressAsync(uid, gameMode, grade);

if (progress != null)
{
    Debug.Log($"Max level unlocked: {progress.maxLevelUnlocked}");
    Debug.Log($"Total score: {progress.totalScore}");
    Debug.Log($"Best score: {progress.bestScore}");
    
    // Hiển thị UI
    maxLevelText.text = $"Level {progress.maxLevelUnlocked}/100";
    totalScoreText.text = $"Tổng điểm: {progress.totalScore}";
}
```

---

### 2. Load điểm từng level

```csharp
string uid = FirebaseManager.Instance.GetCurrentUser().UserId;
string gameMode = "chonda";
int grade = 1;

// Load điểm tất cả level đã chơi
Dictionary<int, LevelProgressData> levelScores = await FirebaseManager.Instance.LoadLevelScoresAsync(uid, gameMode, grade);

// Hiển thị 100 level buttons
for (int i = 1; i <= 100; i++)
{
    GameObject levelButton = levelButtons[i - 1];
    
    // Kiểm tra unlock
    if (i <= progress.maxLevelUnlocked)
    {
        // Level mở khóa
        levelButton.GetComponent<Button>().interactable = true;
        levelButton.GetComponent<Image>().color = Color.white;
        
        // Hiển thị điểm (nếu đã chơi)
        if (levelScores.ContainsKey(i))
        {
            LevelProgressData levelData = levelScores[i];
            levelButton.GetComponentInChildren<Text>().text = $"Level {i}\n{levelData.bestScore} điểm";
        }
        else
        {
            levelButton.GetComponentInChildren<Text>().text = $"Level {i}\nChưa chơi";
        }
    }
    else
    {
        // Level bị khóa
        levelButton.GetComponent<Button>().interactable = false;
        levelButton.GetComponent<Image>().color = Color.gray;
        levelButton.GetComponentInChildren<Text>().text = $"Level {i}\n🔒";
    }
}
```

---

## 🏆 Bảng xếp hạng

Bảng xếp hạng đã có sẵn trong `UILeaderboardPanelController.cs`, tự động load từ `playerData` collection.

```csharp
// Trong UILeaderboardPanelController
private async void LoadLeaderboard()
{
    var query = firestore.Collection("playerData")
        .OrderByDescending("totalScore")
        .Limit(50);
    
    var snapshot = await query.GetSnapshotAsync();
    
    // Hiển thị top 50 players
    foreach (var doc in snapshot.Documents)
    {
        // ...
    }
}
```

---

## 📝 Ví dụ hoàn chỉnh: LevelManager

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class LevelManager : MonoBehaviour
{
    [Header("Settings")]
    public string gameMode = "chonda";  // chonda, keothada, phithuyen
    public int grade = 1;               // 1-5
    public int currentLevel = 1;
    
    [Header("UI")]
    public Text levelText;
    public Text scoreText;
    public Button playButton;
    
    private string uid;
    
    async void Start()
    {
        // Lấy UID
        var user = FirebaseManager.Instance.GetCurrentUser();
        if (user == null)
        {
            Debug.LogError("User chưa đăng nhập!");
            return;
        }
        uid = user.UserId;
        
        // Load tiến độ
        await LoadProgress();
    }
    
    private async Task LoadProgress()
    {
        // Load gameModeProgress
        var progress = await FirebaseManager.Instance.LoadGameModeProgressAsync(uid, gameMode, grade);
        
        if (progress != null)
        {
            currentLevel = progress.currentLevel;
            levelText.text = $"Level {currentLevel}/100";
            scoreText.text = $"Tổng điểm: {progress.totalScore}";
        }
    }
    
    public async void OnPlayButtonClick()
    {
        // Kiểm tra unlock
        bool isUnlocked = await FirebaseManager.Instance.IsLevelUnlockedAsync(uid, gameMode, grade, currentLevel);
        
        if (!isUnlocked)
        {
            Debug.LogWarning("Level bị khóa!");
            return;
        }
        
        // Chơi level
        StartLevel();
    }
    
    private void StartLevel()
    {
        // Load scene level
        Debug.Log($"Bắt đầu chơi level {currentLevel}");
        // SceneManager.LoadScene("GamePlay");
    }
    
    public async void OnLevelComplete(int score)
    {
        // Cập nhật database
        await FirebaseManager.Instance.UpdateLevelProgressAsync(uid, gameMode, grade, currentLevel, score);
        
        // Reload tiến độ
        await LoadProgress();
        
        Debug.Log($"Hoàn thành level {currentLevel} với {score} điểm!");
    }
}
```

---

## 🔧 Firestore Rules

Nhớ cập nhật Firestore Rules:

```javascript
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    
    // users - chỉ user tự đọc/ghi
    match /users/{uid} {
      allow read, write: if request.auth != null && request.auth.uid == uid;
    }
    
    // playerData - public read (leaderboard), chỉ user tự ghi
    match /playerData/{uid} {
      allow read: if true;
      allow write: if request.auth != null && request.auth.uid == uid;
    }
    
    // gameModeProgress - chỉ user tự đọc/ghi
    match /gameModeProgress/{progressId} {
      allow read, write: if request.auth != null && 
                           resource.data.uid == request.auth.uid;
    }
    
    // levelProgress - chỉ user tự đọc/ghi
    match /levelProgress/{progressId} {
      allow read, write: if request.auth != null && 
                           resource.data.uid == request.auth.uid;
    }
  }
}
```

---

## ⚠️ Lưu ý quan trọng

### 1. Bật enablePlayerDataSync
Trong Unity Editor, chọn GameObject có FirebaseManager component, bật checkbox `Enable Player Data Sync`.

### 2. 3 Chế độ game
- `chonda` - Chọn đáp án (Scene: ChonDA.unity)
- `keothada` - Kéo thả (Scene: KeoThaDA.unity)
- `phithuyen` - Phi thuyền (Scene: PhiThuyen.unity)

### 3. Level unlock
- Người chơi chỉ có thể chơi level <= `maxLevelUnlocked`
- Khi hoàn thành level, `maxLevelUnlocked` tự động tăng lên

### 4. Không dùng hệ thống sao
Game chỉ tính điểm (score), không có sao (0-3 sao).

### 5. XP tự động tính
XP = score / 10 (ví dụ: 950 điểm = 95 XP)

---

## 📁 Files liên quan

- `FirebaseManager.cs` - Code chính (đã cập nhật)
- `firestore_schema_optimized.sql` - Schema database
- `DATABASE_TOI_UU.md` - Tóm tắt cấu trúc
- `GIAI_THICH_DATABASE.md` - Giải thích chi tiết
- `HUONG_DAN_SU_DUNG_DATABASE.md` - File này

---

✅ **Đã sẵn sàng sử dụng! Chỉ cần gọi các method trong FirebaseManager.**
