# Profile UI — Tính năng mới: Xoá Tài Khoản & Đổi Độ Khó

## Tổng quan

Bổ sung 2 tính năng vào panel `Profile` (script `ProfileUI.cs`, scene `GameUIPlay 1`):

| # | Tính năng | Mô tả ngắn |
|---|---|---|
| 1 | **Xoá tài khoản** | Xoá toàn bộ dữ liệu Firebase + local, quay về WELCOMESCREEN |
| 2 | **Đổi độ khó (lớp)** | Chọn lại Lớp 1–5, reset tiến độ 3 chế độ về màn 1, giữ level/điểm/tiền |

---

## 1. Tính năng: Xoá Tài Khoản

### 1.1 Điều kiện áp dụng

| Trạng thái người chơi | Hành vi |
|---|---|
| Đã đăng nhập (có Firebase account) | Xoá Auth user + toàn bộ Firestore documents + local data → về WELCOMESCREEN |
| Chơi nhanh / Chơi mới (guest/anonymous) | Nút **ẩn** — không hiển thị (không có tài khoản để xoá) |

### 1.2 Flow chi tiết

```
Người dùng nhấn nút "Xoá Tài Khoản"
    ↓
Hiện DeleteAccountPopup (copy từ LoginRequiredPopup)
    Tiêu đề: "Xoá Tài Khoản"
    Nội dung: "Toàn bộ dữ liệu tài khoản sẽ bị xoá vĩnh viễn và không thể khôi phục.
               Bạn có chắc chắn muốn tiếp tục?"
    [Xác nhận Xoá]   [Huỷ]
    ↓ (nhấn Xác nhận)
Hiện LoadingIndicator (spinner)
    ↓
Bước 1: Xoá Firestore documents
    - playerData/{uid}
    - users/{uid}
    - gameModeProgress/{uid}_chonda_{1..5}
    - gameModeProgress/{uid}_keothada_{1..5}
    - gameModeProgress/{uid}_phithuyen_{1..5}
    - playerShop/{uid}_chonda_skin
    - playerShop/{uid}_keothada_skin
    - playerShop/{uid}_keothada_phao
    - playerShop/{uid}_phithuyen_ship
    ↓
Bước 2: Xoá Firebase Auth user (FirebaseUser.DeleteAsync())
    ↓
Bước 3: Xoá toàn bộ local data (PlayerPrefs.DeleteAll())
    ↓
Bước 4: Reset UIManager.SelectedGrade = 1
    ↓
Bước 5: AuthManager.Logout() (clear session, cache)
    ↓
Bước 6: Ẩn LoadingIndicator
    ↓
Bước 7: UIScreenRouter điều hướng về WELCOMESCREEN
```

### 1.3 Xử lý lỗi

| Lỗi | Hành vi |
|---|---|
| Firestore delete thất bại | Log warning, tiếp tục xoá Auth (không block) |
| Firebase Auth delete thất bại (requires-recent-login) | Hiện thông báo: "Phiên đăng nhập đã hết hạn. Vui lòng đăng xuất và đăng nhập lại để xoá tài khoản." |
| Lỗi mạng | Hiện thông báo: "Mất kết nối. Kiểm tra mạng và thử lại." |

> **Lưu ý Firebase Auth:** `DeleteAsync()` yêu cầu user đã đăng nhập gần đây (recent login). Nếu session cũ (> vài giờ), Firebase trả về lỗi `requires-recent-login`. Cần bắt `FirebaseException` và kiểm tra message.

---

## 2. Tính năng: Đổi Độ Khó (Lớp Học)

### 2.1 Điều kiện áp dụng

| Trạng thái người chơi | Hành vi |
|---|---|
| Đã đăng nhập | Đổi grade → reset tiến độ local + sync Firebase |
| Guest (chơi nhanh/chơi mới) | Đổi grade → chỉ reset tiến độ local, không đụng Firebase |

### 2.2 Dữ liệu bị reset vs giữ nguyên

| Dữ liệu | Sau khi đổi lớp |
|---|---|
| `Class_HighestLevel` (ChonDA) | Reset về **1** |
| `HighestLevelReached` (KeoThaDA) | Reset về **1** |
| `Space_HighestLevel` (PhiThuyen) | Reset về **1** |
| `UserScore` (tổng điểm) | **Giữ nguyên** |
| `UserLevel` (level nhân vật) | **Giữ nguyên** |
| `TotalCoins` (tiền) | **Giữ nguyên** |
| Skin đã mua | **Giữ nguyên** |
| `UIManager.SelectedGrade` | Cập nhật thành grade mới |

### 2.3 Firebase — dữ liệu bị reset (chỉ khi đã đăng nhập)

```
gameModeProgress/{uid}_chonda_{grade_mới}    → currentLevel=1, maxLevelUnlocked=1
gameModeProgress/{uid}_keothada_{grade_mới}  → currentLevel=1, maxLevelUnlocked=1
gameModeProgress/{uid}_phithuyen_{grade_mới} → currentLevel=1, maxLevelUnlocked=1
users/{uid}                                  → grade = grade_mới (MergeAll)
```

> Chỉ reset gameModeProgress của **grade mới được chọn**. Tiến độ các lớp khác giữ nguyên.

### 2.4 Flow chi tiết

```
Người dùng chọn lớp từ Dropdown (Lớp 1 / Lớp 2 / ... / Lớp 5)
    ↓
Nhấn nút "Áp Dụng" (hoặc "Chọn")
    ↓
Kiểm tra: grade mới có khác grade hiện tại không?
    → Nếu giống → không làm gì (hoặc thông báo "Đây là lớp hiện tại của bạn")
    ↓
Hiện ChangeDifficultyPopup
    Tiêu đề: "Đổi Độ Khó"
    Nội dung: "Chuyển sang Lớp {X}?
               Tiến độ các chế độ ChonDA, KeoThaDA, PhiThuyen sẽ được làm mới về Màn 1.
               Level, điểm và tiền của bạn vẫn được giữ nguyên."
    [Xác nhận]   [Huỷ]
    ↓ (nhấn Xác nhận)
Bước 1: Cập nhật UIManager.SelectedGrade = grade mới
Bước 2: UIQuickPlayNameController.SaveSelectedGrade(grade mới)
Bước 3: Reset PlayerPrefs tiến độ
    PlayerPrefs.SetInt("Class_HighestLevel", 1)
    PlayerPrefs.SetInt("HighestLevelReached", 1)
    PlayerPrefs.SetInt("Space_HighestLevel", 1)
    PlayerPrefs.Save()
Bước 4 (chỉ khi đã đăng nhập):
    Sync grade mới lên users/{uid}
    Reset gameModeProgress 3 chế độ cho grade mới trên Firebase
Bước 5: Cập nhật ProfileUI.UpdateProfileDisplay()
Bước 6: Hiện thông báo thành công: "Đã chuyển sang Lớp {X}. Chúc bạn chơi vui!"
```

---

## 3. Cấu trúc UI cần thêm vào Profile panel

### 3.1 Các GameObject mới (con của `Profile`)

```
Profile
├── ... (các UI hiện có)
│
├── DeleteAccountBtn          ← Button "Xoá Tài Khoản" (chỉ hiện khi đã đăng nhập)
│   └── Text (TMP)
│
├── DifficultySection         ← Container nhóm đổi độ khó
│   ├── DifficultyLabel       ← TMP_Text "Độ khó (Lớp học):"
│   ├── GradeDropdown         ← TMP_Dropdown (copy từ RegisterPanel.Dropdown)
│   └── ApplyDifficultyBtn    ← Button "Áp Dụng"
│       └── Text (TMP)
│
├── DeleteAccountPopup        ← Popup xác nhận xoá (copy từ LoginRequiredPopup)
│   ├── Overlay               ← Image bán trong suốt
│   └── ContentPanel
│       ├── TitleText         ← "Xoá Tài Khoản"
│       ├── MessageText       ← Nội dung cảnh báo
│       └── ButtonContainer   ← HorizontalLayoutGroup
│           ├── ConfirmDeleteBtn  ← Button "Xác nhận Xoá"
│           └── CancelBtn         ← Button "Huỷ"
│
└── ChangeDifficultyPopup     ← Popup xác nhận đổi lớp (copy từ LoginRequiredPopup)
    ├── Overlay
    └── ContentPanel
        ├── TitleText         ← "Đổi Độ Khó"
        ├── MessageText       ← Nội dung thông báo (có {X} = lớp mới)
        └── ButtonContainer
            ├── ConfirmChangeBtn  ← Button "Xác nhận"
            └── CancelBtn         ← Button "Huỷ"
```

### 3.2 Dropdown options (GradeDropdown)

```
Index 0 → "Lớp 1"
Index 1 → "Lớp 2"
Index 2 → "Lớp 3"
Index 3 → "Lớp 4"
Index 4 → "Lớp 5"
```

Khi mở Profile, set `GradeDropdown.value = UIManager.SelectedGrade - 1` để hiển thị lớp hiện tại.

---

## 4. Script: ProfileUI.cs — thay đổi cần làm

### 4.1 Thêm các field mới

```csharp
[Header("Xoá Tài Khoản")]
public Button deleteAccountBtn;
public GameObject deleteAccountPopup;
public Button confirmDeleteBtn;
public Button cancelDeleteBtn;
// (MessageText không cần field vì nội dung cố định)

[Header("Đổi Độ Khó")]
public TMP_Dropdown gradeDropdown;
public Button applyDifficultyBtn;
public GameObject changeDifficultyPopup;
public TMP_Text changeDifficultyMessageText;
public Button confirmChangeBtn;
public Button cancelChangeBtn;
```

### 4.2 Thêm vào OnEnable() / Start()

```csharp
private void Start()
{
    // Gán sự kiện buttons
    deleteAccountBtn?.onClick.AddListener(() => _ = HandleDeleteAccountClick());
    confirmDeleteBtn?.onClick.AddListener(() => _ = HandleConfirmDelete());
    cancelDeleteBtn?.onClick.AddListener(HideDeletePopup);

    applyDifficultyBtn?.onClick.AddListener(HandleApplyDifficulty);
    confirmChangeBtn?.onClick.AddListener(() => _ = HandleConfirmChangeDifficulty());
    cancelChangeBtn?.onClick.AddListener(HideChangeDifficultyPopup);

    // Ẩn popup mặc định
    deleteAccountPopup?.SetActive(false);
    changeDifficultyPopup?.SetActive(false);
}

private void OnEnable()
{
    UpdateProfileDisplay();
    RefreshDeleteButtonVisibility();
    SyncDropdownToCurrentGrade();
}
```

### 4.3 Logic xoá tài khoản

```csharp
private bool isBusy = false;

private async Task HandleDeleteAccountClick()
{
    if (isBusy) return;
    // Hiện popup xác nhận
    deleteAccountPopup?.SetActive(true);
}

private void HideDeletePopup()
{
    deleteAccountPopup?.SetActive(false);
}

private async Task HandleConfirmDelete()
{
    if (isBusy) return;
    isBusy = true;
    HideDeletePopup();

    // Hiện loading
    UILoadingIndicator.Instance?.Show("Đang xoá tài khoản...");

    try
    {
        string uid = AuthManager.Instance?.GetCurrentUser()?.UserId;

        // Bước 1: Xoá Firestore documents
        if (!string.IsNullOrEmpty(uid))
        {
            await DeleteFirestoreDataAsync(uid);
        }

        // Bước 2: Xoá Firebase Auth user
        var firebaseUser = AuthManager.Instance?.GetCurrentUser();
        if (firebaseUser != null)
        {
            await firebaseUser.DeleteAsync();
        }

        // Bước 3: Xoá local data
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Bước 4: Reset grade
        UIManager.SelectedGrade = 1;

        // Bước 5: Logout + clear session
        AuthManager.Instance?.Logout();

        // Bước 6: Ẩn loading
        UILoadingIndicator.Instance?.Hide();

        // Bước 7: Về WELCOMESCREEN
        UIScreenRouter.Instance?.NavigateTo("WELCOMESCREEN");
    }
    catch (Firebase.FirebaseException ex)
    {
        UILoadingIndicator.Instance?.Hide();
        isBusy = false;

        if (ex.Message.Contains("requires-recent-login"))
        {
            SetStatusText("Phiên đăng nhập đã hết hạn. Vui lòng đăng xuất và đăng nhập lại để xoá tài khoản.");
        }
        else
        {
            SetStatusText("Có lỗi xảy ra. Vui lòng thử lại.");
        }
        Debug.LogError($"[Profile] Xoá tài khoản thất bại: {ex.Message}");
    }
    catch (Exception ex)
    {
        UILoadingIndicator.Instance?.Hide();
        isBusy = false;
        SetStatusText("Mất kết nối. Kiểm tra mạng và thử lại.");
        Debug.LogError($"[Profile] Xoá tài khoản thất bại: {ex.Message}");
    }
    finally
    {
        isBusy = false;
    }
}

private async Task DeleteFirestoreDataAsync(string uid)
{
    var fs = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
    if (fs == null) return;

    try
    {
        // playerData
        await fs.Collection("playerData").Document(uid).DeleteAsync();

        // users
        await fs.Collection("users").Document(uid).DeleteAsync();

        // gameModeProgress (15 records)
        string[] modes  = { "chonda", "keothada", "phithuyen" };
        int[]    grades = { 1, 2, 3, 4, 5 };
        foreach (var mode in modes)
            foreach (var grade in grades)
                await fs.Collection("gameModeProgress")
                        .Document($"{uid}_{mode}_{grade}").DeleteAsync();

        // playerShop (4 records)
        string[] shopTypes = { "chonda_skin", "keothada_skin", "keothada_phao", "phithuyen_ship" };
        foreach (var shopType in shopTypes)
            await fs.Collection("playerShop")
                    .Document($"{uid}_{shopType}").DeleteAsync();

        Debug.Log("[Profile] ✅ Đã xoá toàn bộ Firestore data");
    }
    catch (Exception ex)
    {
        // Không block — tiếp tục xoá Auth dù Firestore lỗi
        Debug.LogWarning($"[Profile] ⚠️ Xoá Firestore thất bại (tiếp tục): {ex.Message}");
    }
}

private void RefreshDeleteButtonVisibility()
{
    bool isLoggedIn = AuthManager.Instance != null
                      && AuthManager.Instance.HasEmail(); // HasEmail() = có tài khoản thật
    deleteAccountBtn?.gameObject.SetActive(isLoggedIn);
}
```

### 4.4 Logic đổi độ khó

```csharp
private int pendingGrade = -1;

private void SyncDropdownToCurrentGrade()
{
    if (gradeDropdown == null) return;
    int currentGrade = UIManager.SelectedGrade;
    if (currentGrade < 1 || currentGrade > 5) currentGrade = 1;
    gradeDropdown.value = currentGrade - 1; // Index 0 = Lớp 1
}

private void HandleApplyDifficulty()
{
    if (gradeDropdown == null) return;

    int selectedGrade = gradeDropdown.value + 1; // Convert index → grade (1–5)
    int currentGrade  = UIManager.SelectedGrade;

    if (selectedGrade == currentGrade)
    {
        SetStatusText("Đây là lớp hiện tại của bạn.");
        return;
    }

    pendingGrade = selectedGrade;

    // Cập nhật nội dung popup
    if (changeDifficultyMessageText != null)
    {
        changeDifficultyMessageText.SetText(
            $"Chuyển sang Lớp {selectedGrade}?\n\n" +
            $"Tiến độ các chế độ ChonDA, KeoThaDA, PhiThuyen sẽ được làm mới về Màn 1.\n\n" +
            $"Level, điểm và tiền của bạn vẫn được giữ nguyên."
        );
    }

    changeDifficultyPopup?.SetActive(true);
}

private void HideChangeDifficultyPopup()
{
    changeDifficultyPopup?.SetActive(false);
    pendingGrade = -1;
    SyncDropdownToCurrentGrade(); // Reset dropdown về giá trị hiện tại
}

private async Task HandleConfirmChangeDifficulty()
{
    if (isBusy || pendingGrade < 1 || pendingGrade > 5) return;
    isBusy = true;
    HideChangeDifficultyPopup();

    try
    {
        int newGrade = pendingGrade;

        // Bước 1: Cập nhật grade
        UIManager.SelectedGrade = newGrade;
        DoAnGame.UI.UIQuickPlayNameController.SaveSelectedGrade(newGrade);

        // Bước 2: Reset tiến độ local
        PlayerPrefs.SetInt("Class_HighestLevel", 1);
        PlayerPrefs.SetInt("HighestLevelReached", 1);
        PlayerPrefs.SetInt("Space_HighestLevel", 1);
        PlayerPrefs.Save();

        // Bước 3: Sync Firebase (chỉ khi đã đăng nhập)
        if (AuthManager.Instance != null && AuthManager.Instance.HasEmail())
        {
            await SyncGradeChangeToFirebase(newGrade);
        }

        // Bước 4: Refresh UI
        UpdateProfileDisplay();
        SyncDropdownToCurrentGrade();

        SetStatusText($"Đã chuyển sang Lớp {newGrade}. Chúc bạn chơi vui!");
        Debug.Log($"[Profile] ✅ Đổi độ khó thành công: Lớp {newGrade}");
    }
    catch (Exception ex)
    {
        SetStatusText("Có lỗi xảy ra khi đổi độ khó. Vui lòng thử lại.");
        Debug.LogError($"[Profile] Đổi độ khó thất bại: {ex.Message}");
    }
    finally
    {
        isBusy = false;
        pendingGrade = -1;
    }
}

private async Task SyncGradeChangeToFirebase(int newGrade)
{
    var fs = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
    string uid = AuthManager.Instance?.GetCurrentUser()?.UserId;
    if (fs == null || string.IsNullOrEmpty(uid)) return;

    try
    {
        // Cập nhật grade trong users/{uid}
        var gradeUpdate = new Dictionary<string, object> { { "grade", newGrade } };
        await fs.Collection("users").Document(uid)
                .SetAsync(gradeUpdate, Firebase.Firestore.SetOptions.MergeAll);

        // Reset gameModeProgress cho grade mới (3 chế độ)
        string[] modes = { "chonda", "keothada", "phithuyen" };
        foreach (var mode in modes)
        {
            string progressId = $"{uid}_{mode}_{newGrade}";
            var resetData = new Dictionary<string, object>
            {
                { "currentLevel",     1 },
                { "maxLevelUnlocked", 1 },
                { "totalScore",       0 },
                { "bestScore",        0 },
                { "lastPlayed",       null }
            };
            await fs.Collection("gameModeProgress").Document(progressId)
                    .SetAsync(resetData, Firebase.Firestore.SetOptions.MergeAll);
        }

        Debug.Log($"[Profile] ✅ Firebase synced: grade={newGrade}, gameModeProgress reset");
    }
    catch (Exception ex)
    {
        // Không block UI — local đã cập nhật rồi
        Debug.LogWarning($"[Profile] ⚠️ Firebase sync thất bại (local đã cập nhật): {ex.Message}");
    }
}
```

---

## 5. Hướng dẫn Setup trong Unity Inspector

### Bước 1: Tạo UI trong scene `GameUIPlay 1`

1. Mở scene `GameUIPlay 1`
2. Tìm GameObject `Profile` trong `GameUICanvas`
3. Thêm các child GameObjects theo cấu trúc mục 3.1

**Tạo `DeleteAccountBtn`:**
- Duplicate button `ReSet` (đã có trong Profile) → đổi tên thành `DeleteAccountBtn`
- Đổi text thành "Xoá Tài Khoản"
- Đặt màu đỏ để phân biệt (Color: #FF4444)

**Tạo `DifficultySection`:**
- Tạo Empty GameObject tên `DifficultySection`
- Thêm TMP_Text `DifficultyLabel` với text "Độ khó (Lớp học):"
- Copy `Dropdown` từ `RegisterPanel` → paste vào `DifficultySection` → đổi tên thành `GradeDropdown`
  - Xoá các options cũ (tuổi), thêm: Lớp 1, Lớp 2, Lớp 3, Lớp 4, Lớp 5
- Thêm Button `ApplyDifficultyBtn` với text "Áp Dụng"

**Tạo `DeleteAccountPopup`:**
- Copy toàn bộ `LoginRequiredPopup` → paste vào `Profile` → đổi tên thành `DeleteAccountPopup`
- Đổi text:
  - TitleText: "Xoá Tài Khoản"
  - MessageText: "Toàn bộ dữ liệu tài khoản sẽ bị xoá vĩnh viễn và không thể khôi phục. Bạn có chắc chắn muốn tiếp tục?"
  - LoginButton → đổi tên thành `ConfirmDeleteBtn`, text: "Xác nhận Xoá"
  - CancelButton giữ nguyên, text: "Huỷ"
- Set `DeleteAccountPopup` Inactive mặc định

**Tạo `ChangeDifficultyPopup`:**
- Copy `DeleteAccountPopup` → đổi tên thành `ChangeDifficultyPopup`
- Đổi text:
  - TitleText: "Đổi Độ Khó"
  - MessageText: "(sẽ được set bằng code khi mở popup)"
  - ConfirmDeleteBtn → đổi tên thành `ConfirmChangeBtn`, text: "Xác nhận"
- Set `ChangeDifficultyPopup` Inactive mặc định

### Bước 2: Gán references trong Inspector của `ProfileUI`

Chọn GameObject `Profile` → Component `ProfileUI`:

| Field | Gán vào |
|---|---|
| `deleteAccountBtn` | `Profile/DeleteAccountBtn` |
| `deleteAccountPopup` | `Profile/DeleteAccountPopup` |
| `confirmDeleteBtn` | `Profile/DeleteAccountPopup/ContentPanel/ButtonContainer/ConfirmDeleteBtn` |
| `cancelDeleteBtn` (delete) | `Profile/DeleteAccountPopup/ContentPanel/ButtonContainer/CancelButton` |
| `gradeDropdown` | `Profile/DifficultySection/GradeDropdown` |
| `applyDifficultyBtn` | `Profile/DifficultySection/ApplyDifficultyBtn` |
| `changeDifficultyPopup` | `Profile/ChangeDifficultyPopup` |
| `changeDifficultyMessageText` | `Profile/ChangeDifficultyPopup/ContentPanel/MessageText` |
| `confirmChangeBtn` | `Profile/ChangeDifficultyPopup/ContentPanel/ButtonContainer/ConfirmChangeBtn` |
| `cancelChangeBtn` (change) | `Profile/ChangeDifficultyPopup/ContentPanel/ButtonContainer/CancelButton` |

### Bước 3: Thêm StatusText vào Profile (nếu chưa có)

- Thêm TMP_Text `StatusText` vào `Profile`
- Gán vào field `statusText` trong `ProfileUI`
- Dùng để hiển thị thông báo kết quả ("Đã chuyển sang Lớp X...", lỗi, v.v.)

### Bước 4: Kiểm tra UIScreenRouter

Đảm bảo `UIScreenRouter` có route đến `WELCOMESCREEN`:
- Tìm `UIScreenRouter` trong scene
- Kiểm tra mapping `"WELCOMESCREEN"` → `WELCOMESCREEN` GameObject

---

## 6. Checklist trước khi build

- [ ] `DeleteAccountBtn` ẩn khi guest (kiểm tra `AuthManager.HasEmail()`)
- [ ] Popup xác nhận hiện đúng nội dung trước khi thực hiện
- [ ] Xoá tài khoản: Firestore xoá trước, Auth xoá sau
- [ ] Xoá tài khoản: bắt lỗi `requires-recent-login` và hiện thông báo thân thiện
- [ ] Đổi độ khó: dropdown hiển thị đúng lớp hiện tại khi mở Profile
- [ ] Đổi độ khó: nếu chọn cùng lớp → không mở popup, hiện thông báo nhẹ
- [ ] Đổi độ khó: level/điểm/tiền không bị reset
- [ ] Đổi độ khó: Firebase chỉ sync khi `HasEmail()` = true
- [ ] Guest: đổi độ khó chỉ thay đổi PlayerPrefs, không gọi Firebase
- [ ] `isBusy` guard ngăn double-click
- [ ] `finally` block luôn unlock `isBusy`
- [ ] Tất cả async method có try/catch

---

## 7. Firestore Security Rules (cần thêm)

Để `DeleteAsync()` hoạt động, Firestore rules phải cho phép user xoá document của chính mình:

```javascript
// playerData
match /playerData/{uid} {
  allow delete: if request.auth != null && request.auth.uid == uid;
}

// users
match /users/{uid} {
  allow delete: if request.auth != null && request.auth.uid == uid;
}

// gameModeProgress
match /gameModeProgress/{docId} {
  allow delete: if request.auth != null && docId.matches(request.auth.uid + "_.*");
}

// playerShop
match /playerShop/{docId} {
  allow delete: if request.auth != null && docId.matches(request.auth.uid + "_.*");
}
```

> Thêm vào Firebase Console → Firestore → Rules.
