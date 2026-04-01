# 📱 PHƯƠNG ÁN LƯU TRỮ FIREBASE CHO GAME MULTIPLAYER

**Mục tiêu**: Lưu tất cả dữ liệu trên Firebase → Người chơi đăng nhập máy khác vẫn có data

---

## 1️⃣ CẤU TRÚC FIREBASE REALTIME DATABASE

```
firebase_project/
├── users/                          # Thông tin cơ bản user
│   └── {uid}/
│       ├── email: "user@example.com"
│       ├── username: "Player123"
│       ├── age: 18
│       ├── avatarUrl: "https://..."
│       ├── createdAt: 1704067200000
│       ├── lastLogin: 1704067200000
│       └── isActive: true
│
├── playerData/                     # Statistics & progression
│   └── {uid}/
│       ├── username: "Player123"
│       ├── totalScore: 5000
│       ├── totalXp: 2500
│       ├── currentLevel: 5
│       ├── rank: 123
│       ├── gamesPlayed: 50
│       ├── gamesWon: 30
│       ├── winRate: 0.6
│       └── lastUpdated: 1704067200000
│
├── gameHistory/                    # Lưu lại lịch sử trận đấu
│   └── {uid}/
│       └── {gameId}/
│           ├── opponentId: "other_uid"
│           ├── opponentName: "Player456"
│           ├── playerScore: 500
│           ├── opponentScore: 350
│           ├── result: "WIN"
│           ├── duration: 300
│           ├── difficulty: "NORMAL"
│           ├── timestamp: 1704067200000
│           └── questions: [list của câu hỏi + đáp án]
│
├── leaderboard/                    # Cache cho top 100 (update định kỳ)
│   ├── global/
│   │   └── {rank}/
│   │       ├── uid: "uid_123"
│   │       ├── username: "Player123"
│   │       ├── score: 5000
│   │       └── level: 5
│   │
│   └── weekly/
│       └── {rank}/
│           └── ... (same structure)
│
└── userSettings/                   # Cài đặt game
    └── {uid}/
        ├── soundVolume: 0.8
        ├── musicVolume: 0.5
        ├── language: "vi"
        ├── difficulty: "NORMAL"
        └── notifications: true
```

---

## 2️⃣ DỮ LIỆU CẦN LƯU (Data Models)

### **A. UserData** (Thông tin tài khoản)
```csharp
[System.Serializable]
public class UserData
{
    public string uid;                    // Firebase UID
    public string email;                  // Email đăng nhập
    public string username;               // Tên người chơi
    public int age;                       // Tuổi
    public string avatarUrl;              // Link ảnh từ Firebase Storage hoặc URL
    public string bio;                    // Tiểu sử người chơi
    public long createdAt;                // Timestamp tạo tài khoản
    public long lastLogin;                // Lần đăng nhập cuối
    public bool isActive;                 // Trạng thái active
    public string country;                // Quốc gia
}
```

### **B. PlayerData** (Thống kê & Progression)
```csharp
[System.Serializable]
public class PlayerData
{
    public string uid;
    public string username;
    public int totalScore;               // Tổng điểm
    public int totalXp;                  // Tổng kinh nghiệm
    public int currentLevel;             // Level hiện tại
    public int rank;                     // Xếp hạng toàn cầu
    public int gamesPlayed;              // Tổng trận chơi
    public int gamesWon;                 // Trận thắng
    public float winRate;                // Tỷ lệ thắng (%)
    public int killStreak;               // Streak chiến thắng liên tiếp
    public int bestScore;                // Điểm cao nhất 1 trận
    public long lastUpdated;             // Lần cập nhật cuối
}
```

### **C. GameSession** (Trận đấu)
```csharp
[System.Serializable]
public class GameSession
{
    public string sessionId;             // ID trận đấu
    public string player1Id;             // UID player 1
    public string player1Name;           // Tên player 1
    public string player2Id;             // UID player 2
    public string player2Name;           // Tên player 2
    public int player1Score;             // Điểm player 1
    public int player2Score;             // Điểm player 2
    public int player1Hp;                // HP còn lại player 1
    public int player2Hp;                // HP còn lại player 2
    public string winner;                // UID người chiến thắng
    public int questionsCount;           // Số câu hỏi
    public long startTime;               // Bắt đầu
    public long endTime;                 // Kết thúc
    public int duration;                 // Thời lượng (giây)
    public string difficulty;            // Độ khó
    public List<QuestionRecord> questions; // Chi tiết câu hỏi
}
```

### **D. GameHistory** (Lịch sử trận)
```csharp
[System.Serializable]
public class GameHistory
{
    public string gameId;
    public string opponentId;
    public string opponentName;
    public int playerScore;
    public int opponentScore;
    public string result;                // "WIN" or "LOSE"
    public long timestamp;
    public string difficulty;
}
```

### **E. UserSettings** (Cài đặt người chơi)
```csharp
[System.Serializable]
public class UserSettings
{
    public float soundVolume;
    public float musicVolume;
    public string language;
    public string difficulty;
    public bool notifications;
    public bool emailNotifications;
}
```

---

## 3️⃣ AVATAR LƯU TRỮ - 3 PHƯƠNG ÁN

### **Phương án A: Firebase Storage (RECOMMENDED)**
✅ **Ưu điểm**: 
- Lưu ảnh trực tiếp, CDN tự động
- Nhanh, an toàn, có quyền kiểm soát

❌ **Nhược điểm**: 
- Chi phí lưu trữ lớn hơn

```csharp
// Upload avatar to Firebase Storage
public async Task<string> UploadAvatarAsync(string uid, Texture2D avatarTexture)
{
    try
    {
        byte[] bytes = avatarTexture.EncodeToPNG();
        string fileName = $"avatars/{uid}/avatar.png";
        
        FirebaseStorage.DefaultInstance
            .GetReference(fileName)
            .PutBytesAsync(bytes)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    // Lấy download URL
                    task.Result.Reference.GetDownloadUrlAsync()
                        .ContinueWithOnMainThread(urlTask =>
                        {
                            if (urlTask.IsCompleted)
                            {
                                string downloadUrl = urlTask.Result.ToString();
                                // Lưu URL vào Realtime DB
                                SaveAvatarUrlToDatabase(uid, downloadUrl);
                            }
                        });
                }
            });
    }
    catch (Exception ex)
    {
        Debug.LogError($"Upload avatar failed: {ex.Message}");
    }
}
```

### **Phương án B: Gravatar URL (FREE)**
✅ **Ưu điểm**: 
- Miễn phí, không cần lưu trữ
- Chuẩn quốc tế

❌ **Nhược điểm**: 
- Phụ thuộc bên thứ ba
- Hạn chế tùy chỉnh

```csharp
// Generate Gravatar URL from email
public static string GetGravatarUrl(string email)
{
    string hash = MD5Hash(email.ToLower());
    return $"https://www.gravatar.com/avatar/{hash}?s=200&d=identicon";
}
```

### **Phương án C: Player-chọn avatar từ preset**
✅ **Ưu điểm**: 
- Nhanh, không cần upload
- Đơn giản

❌ **Nhược điểm**: 
- Ít cá nhân hóa

```csharp
[System.Serializable]
public class AvatarOption
{
    public int id;
    public string name;
    public string resourcePath;  // Resources/Avatars/avatar_1.png
}

// Lưu ID avatar được chọn
public async Task SelectAvatarAsync(string uid, int avatarId)
{
    await dbRef.Child("users").Child(uid).Child("avatarId").SetValueAsync(avatarId);
}
```

---

## 4️⃣ FLOW CỬ THỂ: ĐĂNG NHẬP TRÊN MÁY KHÁC

```
User đăng nhập trên điện thoại B
↓
Firebase Auth xác thực email/password
↓
AuthManager gọi FirebaseManager.LoadPlayerDataAsync(uid)
↓
Firebase Realtime DB trả về toàn bộ data:
  - UserData (avatar, tên, tuổi)
  - PlayerData (score, level, rank)
  - UserSettings (âm thanh, ngôn ngữ)
↓
Game load Avatar từ URL hoặc Resources
↓
UI hiển thị Main Menu với data đúng
↓
Người chơi có thể tiếp tục chơi từ điểm dừng
```

---

## 5️⃣ CODE IMPLEMENTATION

### **Bước 1: Cập nhật FirebaseManager.cs**

```csharp
/// <summary>
/// Lưu toàn bộ user profile (bao gồm avatar URL)
/// </summary>
public async Task SaveUserProfileAsync(string uid, UserData userData)
{
    try
    {
        string json = JsonUtility.ToJson(userData);
        await dbRef.Child("users").Child(uid).SetRawJsonValueAsync(json);
        Debug.Log("[Firebase] ✅ Lưu user profile thành công (có avatar)");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi lưu user profile: {ex.Message}");
    }
}

/// <summary>
/// Lưu game history khi trận đấu kết thúc
/// </summary>
public async Task SaveGameSessionAsync(GameSession session)
{
    try
    {
        // Lưu cho player 1
        string json1 = JsonUtility.ToJson(session);
        await dbRef.Child("gameHistory")
            .Child(session.player1Id)
            .Child(session.sessionId)
            .SetRawJsonValueAsync(json1);
        
        // Lưu cho player 2
        await dbRef.Child("gameHistory")
            .Child(session.player2Id)
            .Child(session.sessionId)
            .SetRawJsonValueAsync(json1);
        
        Debug.Log("[Firebase] ✅ Lưu game session thành công");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi lưu game session: {ex.Message}");
    }
}

/// <summary>
/// Tải toàn bộ user data (một lần)
/// </summary>
public async Task<UserData> LoadUserProfileAsync(string uid)
{
    try
    {
        var snapshot = await dbRef.Child("users").Child(uid).GetValueAsync();
        if (snapshot.Exists)
        {
            string json = snapshot.GetRawJsonValue();
            UserData data = JsonUtility.FromJson<UserData>(json);
            Debug.Log("[Firebase] ✅ Tải user profile thành công");
            return data;
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi tải user profile: {ex.Message}");
    }
    return null;
}

/// <summary>
/// Cập nhật avatar URL riêng
/// </summary>
public async Task UpdateAvatarUrlAsync(string uid, string avatarUrl)
{
    try
    {
        await dbRef.Child("users").Child(uid).Child("avatarUrl").SetValueAsync(avatarUrl);
        Debug.Log("[Firebase] ✅ Cập nhật avatar URL thành công");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi cập nhật avatar: {ex.Message}");
    }
}

/// <summary>
/// Lưu user settings
/// </summary>
public async Task SaveUserSettingsAsync(string uid, UserSettings settings)
{
    try
    {
        string json = JsonUtility.ToJson(settings);
        await dbRef.Child("userSettings").Child(uid).SetRawJsonValueAsync(json);
        Debug.Log("[Firebase] ✅ Lưu settings thành công");
    }
    catch (Exception ex)
    {
        Debug.LogError($"[Firebase] ❌ Lỗi lưu settings: {ex.Message}");
    }
}
```

### **Bước 2: Cập nhật AuthManager.cs để load tất cả data**

```csharp
public async Task<bool> Login(string email, string password)
{
    Debug.Log($"[Auth] 🔑 Đăng nhập: {email}");

    bool success = await firebaseManager.LoginAsync(email, password);
    
    if (success)
    {
        var user = firebaseManager.GetCurrentUser();
        
        // Tải USER PROFILE (bao gồm avatar)
        var userData = await firebaseManager.LoadUserProfileAsync(user.UserId);
        
        // Tải PLAYER DATA (score, level, rank)
        var playerData = await firebaseManager.LoadPlayerDataAsync(user.UserId);
        
        // Tải SETTINGS
        var settings = await LoadUserSettingsAsync(user.UserId);
        
        // Cache vào memory
        currentPlayerData = playerData;
        
        // Trigger event để UI cập nhật
        OnPlayerDataLoaded?.Invoke(userData, playerData, settings);
    }

    return success;
}

private async Task<UserSettings> LoadUserSettingsAsync(string uid)
{
    // Implementation để tải settings từ Firebase
    // ...
}
```

---

## 6️⃣ CHECKLIST IMPLEMENTATION

- [ ] Cập nhật UserData class thêm `avatarUrl`, `bio`, `country`
- [ ] Cập nhật FirebaseManager với methods: SaveUserProfileAsync, LoadUserProfileAsync, UpdateAvatarUrlAsync, SaveGameSessionAsync
- [ ] Tạo class UserSettings để lưu cài đặt âm thanh, ngôn ngữ, v.v.
- [ ] Cập nhật AuthManager để load toàn bộ data khi đăng nhập
- [ ] Tạo AvatarLoader script để tải avatar từ URL (async)
- [ ] Tạo GameSessionManager để lưu kết quả trận đấu
- [ ] Cập nhật UI16 để hiển thị Avatar (load async từ URL)
- [ ] Test: Đăng nhập, chơi game, kiểm tra Firebase Console để xác nhận data được lưu
- [ ] Test: Đăng xuất, đăng nhập trên máy khác, xác nhận data vẫn có

---

## 7️⃣ FIREBASE SECURITY RULES (Bảo vệ dữ liệu)

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid",
        ".validate": "newData.hasChildren(['email', 'username', 'age'])"
      }
    },
    "playerData": {
      "$uid": {
        ".read": true,
        ".write": "$uid === auth.uid"
      }
    },
    "gameHistory": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "leaderboard": {
      ".read": true,
      ".write": false
    }
  }
}
```

---

## 8️⃣ TỔNG KẾT

✅ **Người chơi sẽ có**:
- Tên & Avatar trên tất cả máy
- Điểm số, Level, Rank không mất
- Lịch sử trận đấu toàn bộ
- Cài đặt game được lưu (âm thanh, ngôn ngữ)
- Top leaderboard update tự động

✅ **Security**:
- Mỗi user chỉ có quyền đọc/ghi dữ liệu của mình
- Dữ liệu leaderboard công khai
- Timestamp ghi lại tất cả cập nhật
