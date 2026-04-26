# 🔄 Hướng dẫn Setup Returning User & Auto-Skip WELCOMESCREEN

## 📋 Tổng quan

Tính năng này cho phép:
1. **Returning Guest Users**: Hiển thị welcome back message, cho phép tiếp tục hoặc chơi mới
2. **Auto-Skip WELCOMESCREEN**: Tự động bỏ qua màn hình chọn lớp cho user đã chọn trước đó

---

## 🔄 Luồng hoạt động

### Case 1: New User (Lần đầu vào game)
```
App Start
  ↓
UIStartupController kiểm tra
  ↓ (Không có dữ liệu)
WELCOMESCREEN (Chọn lớp)
  ↓ Chọn lớp → Lưu grade vào PlayerPrefs
WellcomePanel
  ↓ Click "Chơi Nhanh"
NhapTen_choiNhanh
  ↓ Nhập tên → Lưu tên vào PlayerPrefs
MainMenuPanel
```

### Case 2: Returning Guest User (Đã chơi trước đó)
```
App Start
  ↓
UIStartupController kiểm tra
  ↓ (Có tên + có grade)
WellcomePanel (Auto-skip WELCOMESCREEN) ✅
  ↓ Click "Chơi Nhanh"
NhapTen_choiNhanh
  ↓ (Tự động hiển thị welcome back message)
  ├─ Click "Tiếp tục" → Giữ nguyên dữ liệu
  └─ Nhập tên mới + Click "Chơi mới" → Xóa dữ liệu cũ
MainMenuPanel
```

### Case 3: Logged-in User
```
App Start
  ↓
UIStartupController kiểm tra
  ↓ (Đã đăng nhập)
WellcomePanel (Auto-skip WELCOMESCREEN) ✅
```

---

## 📁 Files đã tạo/cập nhật

### 1. UIQuickPlayNameController.cs (Đã cập nhật)
**Đường dẫn:** `Assets/Script/Script_multiplayer/1Code/CODE/UIQuickPlayNameController.cs`

**Thay đổi:**
- ✅ Thêm `continueButton` (Button "Tiếp tục")
- ✅ Thêm `statusText` (Welcome back message)
- ✅ Thêm `CheckReturningUser()` - Kiểm tra returning user
- ✅ Thêm `ShowWelcomeBackMessage()` - Hiển thị welcome message
- ✅ Thêm `OnContinueButtonClicked()` - Xử lý button "Tiếp tục"
- ✅ Cập nhật `OnStartButtonClicked()` - Xóa dữ liệu nếu nhập tên mới
- ✅ Thêm `SaveSelectedGrade()`, `GetSelectedGrade()`, `HasSelectedGrade()`

**New Public Methods:**
```csharp
// Lưu lớp đã chọn
public static void SaveSelectedGrade(int grade)

// Lấy lớp đã chọn (0 = chưa chọn)
public static int GetSelectedGrade()

// Kiểm tra xem user đã chọn lớp chưa
public static bool HasSelectedGrade()
```

---

### 2. UIStartupController.cs ⭐ MỚI
**Đường dẫn:** `Assets/Script/Script_multiplayer/1Code/CODE/UIStartupController.cs`

**Chức năng:**
- Kiểm tra trạng thái user khi app khởi động
- Auto-skip WELCOMESCREEN cho returning users
- Route đến panel phù hợp

**Logic:**
```csharp
if (isLoggedIn)
    → ShowWellcomePanel() // Skip WELCOMESCREEN

else if (isGuest && hasName && hasGrade)
    → ShowWellcomePanel() // Skip WELCOMESCREEN
    → Restore UIManager.SelectedGrade

else
    → ShowWelcomeScreen() // New user
```

---

### 3. UIManager.cs (Đã cập nhật)
**Đường dẫn:** `Assets/Script/Script_multiplayer/UIManager.cs`

**Thay đổi:**
- ✅ Lưu `SelectedGrade` vào PlayerPrefs khi user chọn lớp
- ✅ Gọi `UIQuickPlayNameController.SaveSelectedGrade()` trong `OnBirthYearSelectionChanged()`

---

### 4. LocalProgressService.cs (Không thay đổi)
**Đường dẫn:** `Assets/Script/Script_multiplayer/1Code/CODE/LocalProgressService.cs`

**Chức năng:**
- Lưu tiến trình chơi local (score, avatar, progress)
- `ClearAllData()` được gọi khi user nhập tên mới

---

## 🔧 Setup trong Unity

### Bước 1: Setup UIStartupController ⭐ QUAN TRỌNG

1. **Hierarchy:** Tìm GameObject chứa các UI panels (thường là **GameUICanvas** hoặc **UIManager**)
2. **Add Component:** `UIStartupController`
3. **Inspector:**
   ```
   Welcome Screen Panel: [Kéo WELCOMESCREEN vào đây]
   Welcome Panel: [Kéo WellcomePanel vào đây]
   Main Menu Panel: [Kéo MainMenuPanel vào đây] (optional)
   Enable Auto Skip: ✅ (Tick để bật auto-skip)
   ```

**Lưu ý:** 
- `UIStartupController` phải chạy **TRƯỚC** tất cả UI controllers khác
- Đặt **Script Execution Order** cao hơn (Edit → Project Settings → Script Execution Order)

---

### Bước 2: Setup NhapTen_choiNhanh Panel (Cập nhật)

1. **Hierarchy:** Chọn **NhapTen_choiNhanh**
2. **Inspector → UIQuickPlayNameController:**
   ```
   Name Input Field: [Kéo NhapTen InputField vào đây]
   Start Button: [Kéo BatDau Button vào đây]
   Continue Button: [Kéo button "Tiếp tục" vào đây] ⭐ MỚI
   Status Text: [Kéo TextMeshPro cho welcome message vào đây] ⭐ MỚI
   Error Text: [Kéo TextMeshPro cho error vào đây]
   Target Panel Name: "MainMenuPanel"
   ```

**Tạo UI mới:**

#### A. Tạo Continue Button
1. **Hierarchy:** Right-click **NhapTen_choiNhanh** → **UI → Button - TextMeshPro**
2. **Rename:** `ContinueButton`
3. **Position:** Đặt bên cạnh button "Bắt đầu"
4. **Text:** "Tiếp tục"
5. **Color:** Xanh lá (để phân biệt với "Chơi mới")

#### B. Tạo Status Text
1. **Hierarchy:** Right-click **NhapTen_choiNhanh** → **UI → Text - TextMeshPro**
2. **Rename:** `StatusText`
3. **Position:** Đặt phía trên input field
4. **Settings:**
   - Font Size: 18-20
   - Alignment: Center
   - Color: Trắng hoặc vàng
   - Rich Text: ✅ (Bật để hỗ trợ `<b>`, `<color>`)
5. **Ẩn ban đầu:** Bỏ tick **Active** (script sẽ tự hiển thị khi cần)

---

### Bước 3: Kiểm tra UIManager

1. **Hierarchy:** Chọn **UIManager** (hoặc GameObject chứa UIManager script)
2. **Inspector:** Đảm bảo `birthYearDropdown` đã được gán
3. **Không cần thay đổi gì thêm** - script đã tự động lưu grade

---

## 🧪 Test

### Test Case 1: New User Flow

1. **Xóa PlayerPrefs:** Unity Editor → **Edit → Clear All PlayerPrefs**
2. **Play game**
3. **Kết quả:** Hiển thị **WELCOMESCREEN** (chọn lớp) ✅
4. Chọn lớp: **"2018 - Lớp 1"**
5. Click **"Chơi"**
6. **Kết quả:** Hiển thị **WellcomePanel** ✅
7. Click **"Chơi Nhanh"**
8. **Kết quả:** Hiển thị **NhapTen_choiNhanh** (không có welcome message) ✅
9. Nhập tên: `TestUser`
10. Click **"Bắt đầu"**
11. **Kết quả:** Vào **MainMenuPanel** ✅

---

### Test Case 2: Returning User - Continue

1. **Sau Test Case 1, thoát game và Play lại**
2. **Kết quả:** **Auto-skip WELCOMESCREEN** → Hiển thị **WellcomePanel** ngay ✅
3. **Console log:** `[Startup] Returning guest 'TestUser' with grade 1 → Navigating to WellcomePanel`
4. Click **"Chơi Nhanh"**
5. **Kết quả:** Hiển thị **NhapTen_choiNhanh** với:
   - ✅ Welcome back message: "Chào mừng trở lại, TestUser!"
   - ✅ Input field pre-filled: `TestUser`
   - ✅ Button "Tiếp tục" hiển thị (màu xanh)
   - ✅ Button "Bắt đầu" đổi thành "Chơi mới" (màu cam)
6. Click **"Tiếp tục"**
7. **Kết quả:** Vào **MainMenuPanel** (dữ liệu giữ nguyên) ✅

---

### Test Case 3: Returning User - New Game

1. **Sau Test Case 2, thoát game và Play lại**
2. **Kết quả:** **Auto-skip WELCOMESCREEN** → Hiển thị **WellcomePanel** ✅
3. Click **"Chơi Nhanh"**
4. **Kết quả:** Hiển thị welcome back message ✅
5. **Xóa tên cũ**, nhập tên mới: `NewUser`
6. Click **"Chơi mới"**
7. **Kết quả:**
   - ✅ Console log: `[QuickPlay] New name detected: 'NewUser' (old: 'TestUser') - Clearing old data`
   - ✅ Console log: `[LocalProgress] Cleared all local data`
   - ✅ Status text: "Đã xóa dữ liệu cũ! Bắt đầu chơi mới với tên: NewUser"
   - ✅ Vào **MainMenuPanel**
8. **Kiểm tra dữ liệu:** Tất cả progress đã bị xóa (score = 0, level = 1)

---

### Test Case 4: Logged-in User Auto-Skip

1. **Đăng nhập vào game** (qua LoginPanel)
2. **Thoát game và Play lại**
3. **Kết quả:** **Auto-skip WELCOMESCREEN** → Hiển thị **WellcomePanel** ✅
4. **Console log:** `[Startup] User logged in → Navigating to WellcomePanel`

---

### Test Case 5: Disable Auto-Skip

1. **Hierarchy:** Chọn GameObject chứa **UIStartupController**
2. **Inspector:** Bỏ tick **Enable Auto Skip**
3. **Play game**
4. **Kết quả:** Luôn hiển thị **WELCOMESCREEN** (không skip) ✅

---

## 💾 PlayerPrefs Keys

| Key | Type | Mô tả | Khi nào bị xóa |
|-----|------|-------|----------------|
| `GuestPlayerName` | string | Tên người chơi khách | Đăng nhập, đăng xuất, nhập tên mới |
| `IsGuestMode` | int | 1 = Khách, 0 = Đã đăng nhập | Đăng nhập, đăng xuất |
| `SelectedGrade` | int | Lớp đã chọn (1-5) | Đăng xuất, nhập tên mới |
| `SessionToken` | string | Token đăng nhập 24h | Đăng xuất, hết hạn |
| `SessionExpiry` | long | Thời gian hết hạn token | Đăng xuất |

---

## 🎨 UI Design Recommendations

### Welcome Back Message (StatusText)
```
<b>Chào mừng trở lại, [Tên]!</b>

• Nhấn <color=green><b>Tiếp tục</b></color> để chơi với tên này
  (Dữ liệu chơi vẫn được giữ nguyên)

• Nhấn <color=orange><b>Chơi mới</b></color> và nhập tên khác
  để bắt đầu lại từ đầu
```

### Button Colors
- **"Tiếp tục"**: Xanh lá (#4CAF50) - Hành động an toàn
- **"Chơi mới"**: Cam (#FF9800) - Cảnh báo xóa dữ liệu

---

## 🐛 Troubleshooting

### Vấn đề 1: Không auto-skip WELCOMESCREEN

**Nguyên nhân:** `UIStartupController` chưa được gán hoặc chạy sau các UI controllers khác

**Giải pháp:**
1. Kiểm tra `UIStartupController` đã được gán vào GameObject chưa
2. Kiểm tra **Enable Auto Skip** đã tick chưa
3. Kiểm tra **Script Execution Order**: `UIStartupController` phải chạy trước `UIManager`
   - **Edit → Project Settings → Script Execution Order**
   - Thêm `UIStartupController` với order `-100`
4. Xem Console log: `[Startup] Status - LoggedIn: ..., Guest: ..., HasGrade: ...`

---

### Vấn đề 2: Welcome back message không hiển thị

**Nguyên nhân:** `statusText` hoặc `continueButton` chưa được gán

**Giải pháp:**
1. Kiểm tra **Inspector → UIQuickPlayNameController**
2. Đảm bảo `statusText` và `continueButton` đã được gán
3. Kiểm tra `statusText` có component **TextMeshProUGUI** (không phải Text)
4. Xem Console log: `[QuickPlay] Returning user detected: [tên]`

---

### Vấn đề 3: Dữ liệu không bị xóa khi nhập tên mới

**Nguyên nhân:** `LocalProgressService` không được khởi tạo

**Giải pháp:**
1. Kiểm tra Console log: `[LocalProgress] Cleared all local data`
2. Nếu không thấy log, kiểm tra `LocalProgressService.Instance` có null không
3. Thử gọi `LocalProgressService.Instance.LogStats()` để kiểm tra dữ liệu

---

### Vấn đề 4: Auto-skip nhưng SelectedGrade = 0

**Nguyên nhân:** Grade chưa được lưu khi chọn lớp

**Giải pháp:**
1. Kiểm tra `UIManager.OnBirthYearSelectionChanged()` có gọi `SaveSelectedGrade()` không
2. Xem Console log: `[UIManager] Năm sinh: ... -> GradeIndex đã lưu: ...`
3. Xem Console log: `[QuickPlay] Saved selected grade: ...`
4. Kiểm tra PlayerPrefs: `Debug.Log(PlayerPrefs.GetInt("SelectedGrade", 0));`

---

### Vấn đề 5: Button "Tiếp tục" không click được

**Nguyên nhân:** Button bị che bởi StatusText hoặc không có EventSystem

**Giải pháp:**
1. Kiểm tra **Hierarchy:** Button phải ở **sau** StatusText (render trên cùng)
2. Kiểm tra **EventSystem** có trong scene không
3. Kiểm tra Button có component **Button** và **Raycast Target** đã tick
4. Thử click vào vùng khác của button

---

## 📊 Luồng dữ liệu

```
┌─────────────────────────────────────────────────────┐
│ App Start                                           │
│ ↓                                                   │
│ UIStartupController.Start()                         │
│ ↓                                                   │
│ CheckAndRoute()                                     │
│   ├─ CheckIfLoggedIn()                             │
│   ├─ UIQuickPlayNameController.IsGuestMode()       │
│   ├─ UIQuickPlayNameController.HasSelectedGrade()  │
│   └─ UIQuickPlayNameController.GetGuestName()      │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│ if (isLoggedIn)                                     │
│   → ShowWellcomePanel() ✅ SKIP                     │
│                                                     │
│ else if (isGuest && hasName && hasGrade)           │
│   → Restore UIManager.SelectedGrade                │
│   → ShowWellcomePanel() ✅ SKIP                     │
│                                                     │
│ else                                                │
│   → ShowWelcomeScreen() (WELCOMESCREEN)            │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│ WELCOMESCREEN                                       │
│ ↓                                                   │
│ User chọn lớp                                       │
│ ↓                                                   │
│ UIManager.OnBirthYearSelectionChanged()            │
│ ↓                                                   │
│ UIQuickPlayNameController.SaveSelectedGrade()      │
│ → PlayerPrefs.SetInt("SelectedGrade", grade)       │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│ WellcomePanel → Click "Chơi Nhanh"                 │
│ ↓                                                   │
│ NhapTen_choiNhanh                                   │
│ ↓                                                   │
│ UIQuickPlayNameController.CheckReturningUser()     │
│   ├─ if (hasName && hasGrade)                      │
│   │   → ShowWelcomeBackMessage()                   │
│   │   → Show "Tiếp tục" button                     │
│   │   → Change "Bắt đầu" to "Chơi mới"            │
│   └─ else                                           │
│       → Normal flow (new user)                     │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│ User action:                                        │
│   ├─ Click "Tiếp tục"                              │
│   │   → OnContinueButtonClicked()                  │
│   │   → NavigateToMainMenu() (giữ dữ liệu)        │
│   │                                                 │
│   └─ Nhập tên mới + Click "Chơi mới"              │
│       → OnStartButtonClicked()                     │
│       → if (newName != oldName)                    │
│           → LocalProgressService.ClearAllData()    │
│       → NavigateToMainMenu()                       │
└─────────────────────────────────────────────────────┘
```

---

## ✅ Checklist

### Setup:
- [ ] Tạo `UIStartupController` trong scene
- [ ] Gán references: `welcomeScreenPanel`, `welcomePanel`, `mainMenuPanel`
- [ ] Tick **Enable Auto Skip**
- [ ] Set **Script Execution Order** cho `UIStartupController` (-100)
- [ ] Tạo **Continue Button** trong `NhapTen_choiNhanh`
- [ ] Tạo **Status Text** trong `NhapTen_choiNhanh`
- [ ] Gán references trong `UIQuickPlayNameController`

### Test:
- [ ] New user → Hiển thị WELCOMESCREEN
- [ ] Chọn lớp → Lưu grade vào PlayerPrefs
- [ ] Returning guest → Auto-skip WELCOMESCREEN
- [ ] Returning guest → Hiển thị welcome back message
- [ ] Click "Tiếp tục" → Giữ dữ liệu
- [ ] Nhập tên mới + "Chơi mới" → Xóa dữ liệu
- [ ] Logged-in user → Auto-skip WELCOMESCREEN
- [ ] Disable auto-skip → Luôn hiển thị WELCOMESCREEN

---

✅ **Hoàn thành! Returning User & Auto-Skip đã sẵn sàng!**
