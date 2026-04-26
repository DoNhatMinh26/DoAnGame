# 📝 Changelog - Guest Mode Implementation

## 🎯 Tổng quan thay đổi

Thêm chế độ **Chơi Nhanh (Guest Mode)** cho phép người chơi vào game mà không cần đăng ký, nhưng **BẮT BUỘC đăng nhập** để chơi Multiplayer.

---

## 📅 Ngày: 2026-04-27 (Update 2)

### ✨ Tính năng mới - Returning User & Auto-Skip

#### 1. UIStartupController.cs ⭐ MỚI
**File:** `Assets/Script/Script_multiplayer/1Code/CODE/UIStartupController.cs`

**Chức năng:**
- Kiểm tra trạng thái user khi app khởi động
- Auto-skip WELCOMESCREEN cho returning users
- Auto-skip cho logged-in users
- Restore SelectedGrade từ PlayerPrefs

**Logic:**
```csharp
if (isLoggedIn)
    → ShowWellcomePanel() // Skip WELCOMESCREEN

else if (isGuest && hasName && hasGrade)
    → Restore UIManager.SelectedGrade
    → ShowWellcomePanel() // Skip WELCOMESCREEN

else
    → ShowWelcomeScreen() // New user
```

---

#### 2. UIQuickPlayNameController.cs (Cập nhật lớn)
**File:** `Assets/Script/Script_multiplayer/1Code/CODE/UIQuickPlayNameController.cs`

**Tính năng mới:**
- ✅ Phát hiện returning user (đã chơi trước đó)
- ✅ Hiển thị welcome back message với tên đã lưu
- ✅ Button "Tiếp tục" (giữ dữ liệu cũ)
- ✅ Button "Chơi mới" (xóa dữ liệu cũ)
- ✅ Tự động xóa dữ liệu khi nhập tên mới
- ✅ Lưu/lấy SelectedGrade vào PlayerPrefs

**New Methods:**
```csharp
CheckReturningUser() // Kiểm tra returning user
ShowWelcomeBackMessage(string name) // Hiển thị welcome message
OnContinueButtonClicked() // Xử lý button "Tiếp tục"
SaveSelectedGrade(int grade) // Lưu grade
GetSelectedGrade() → int // Lấy grade (0 = chưa chọn)
HasSelectedGrade() → bool // Kiểm tra đã chọn grade chưa
```

**New UI References:**
- `continueButton` (Button) - Button "Tiếp tục"
- `statusText` (TextMeshProUGUI) - Welcome back message

---

#### 3. UIManager.cs (Cập nhật)
**File:** `Assets/Script/Script_multiplayer/UIManager.cs`

**Thay đổi:**
- Lưu `SelectedGrade` vào PlayerPrefs khi user chọn lớp
- Gọi `UIQuickPlayNameController.SaveSelectedGrade()` trong `OnBirthYearSelectionChanged()`

---

### 📚 Documentation (Mới)

#### 4. SETUP_RETURNING_USER.md ⭐ MỚI
**File:** `Assets/Script/Script_multiplayer/1Code/SETUP_RETURNING_USER.md`

**Nội dung:**
- Hướng dẫn setup UIStartupController
- Hướng dẫn tạo Continue Button và Status Text
- Test cases chi tiết cho returning user flows
- Troubleshooting guide
- Script Execution Order setup

---

#### 5. RETURNING_USER_SUMMARY.md ⭐ MỚI
**File:** `Assets/Script/Script_multiplayer/1Code/RETURNING_USER_SUMMARY.md`

**Nội dung:**
- Tóm tắt implementation
- User flows (4 cases)
- Testing checklist
- Code examples

---

### 🔧 Setup yêu cầu trong Unity (Bổ sung)

**NhapTen_choiNhanh (Cập nhật):**
```
├─ NhapTen (TMP_InputField)
├─ BatDau (Button) → Text đổi thành "Chơi mới" khi returning
├─ ContinueButton (Button) ⭐ MỚI - "Tiếp tục"
├─ StatusText (TextMeshPro) ⭐ MỚI - Welcome message
└─ ErrorText (TextMeshPro)
```

**GameUICanvas (hoặc UIManager GameObject):**
- Add Component: `UIStartupController` ⭐ MỚI
- Gán references:
  - Welcome Screen Panel: WELCOMESCREEN
  - Welcome Panel: WellcomePanel
  - Main Menu Panel: MainMenuPanel (optional)
- Enable Auto Skip: ✅

**Script Execution Order:**
- `UIStartupController` = **-100** (chạy trước tất cả)

---

### 💾 PlayerPrefs Keys (Bổ sung)

| Key | Type | Mô tả | Xóa khi |
|-----|------|-------|---------|
| `SelectedGrade` | int | Lớp đã chọn (1-5) | Đăng xuất, nhập tên mới |

---

### 🧪 Test Cases (Bổ sung)

#### ✅ Test 4: New User → Returning User
1. Xóa PlayerPrefs
2. Play game → Chọn lớp → Nhập tên
3. Thoát game
4. Play lại → ✅ Auto-skip WELCOMESCREEN
5. Console log: `[Startup] Returning guest 'X' with grade Y`

#### ✅ Test 5: Returning User - Continue
1. Returning user → Click "Chơi Nhanh"
2. ✅ Hiển thị welcome message
3. ✅ Button "Tiếp tục" hiển thị
4. ✅ Button "Bắt đầu" → "Chơi mới"
5. Click "Tiếp tục" → ✅ Giữ dữ liệu

#### ✅ Test 6: Returning User - New Game
1. Returning user → Click "Chơi Nhanh"
2. Nhập tên mới → Click "Chơi mới"
3. Console log: `[QuickPlay] New name detected`
4. Console log: `[LocalProgress] Cleared all local data`
5. ✅ Dữ liệu đã xóa

---

### 📊 Code Statistics (Cập nhật)

**Files Created:** +2
- `UIStartupController.cs` (150 lines)
- `SETUP_RETURNING_USER.md` (600+ lines)
- `RETURNING_USER_SUMMARY.md` (300+ lines)

**Files Modified:** +2
- `UIQuickPlayNameController.cs` (thêm 150+ lines)
- `UIManager.cs` (thêm 5 lines)

**Total Lines Added:** ~1200 lines

---

## 📅 Ngày: 2026-04-27 (Update 1)

### ✨ Tính năng mới

#### 1. UIQuickPlayNameController.cs ⭐ MỚI (Version 1.0)
**File:** `Assets/Script/Script_multiplayer/1Code/CODE/UIQuickPlayNameController.cs`

**Chức năng:**
- Panel nhập tên cho người chơi khách
- Validate tên (3-20 ký tự)
- Lưu vào PlayerPrefs với keys:
  - `GuestPlayerName` (string)
  - `IsGuestMode` (int: 1 = guest, 0 = logged in)
- Navigation tự động sang MainMenuPanel

**Public API:**
```csharp
UIQuickPlayNameController.IsGuestMode() → bool
UIQuickPlayNameController.GetGuestName() → string
UIQuickPlayNameController.ClearGuestData() → void
```

---

#### 2. UILoginRequiredPopupController.cs ⭐ MỚI
**File:** `Assets/Script/Script_multiplayer/1Code/CODE/UILoginRequiredPopupController.cs`

**Chức năng:**
- Popup thông báo yêu cầu đăng nhập
- 2 buttons: "Đăng nhập" và "Hủy"
- Callback system cho xử lý sự kiện
- Auto-hide khi click button

**Public API:**
```csharp
Show(string title, string message, Action onLogin, Action onCancel)
Show(Action onLogin, Action onCancel) // Dùng default text
Hide()
FindInScene() → UILoginRequiredPopupController
```

---

#### 3. UIModSelectionPanelController.cs ⭐ MỚI
**File:** `Assets/Script/Script_multiplayer/1Code/CODE/UIModSelectionPanelController.cs`

**Chức năng:**
- Kiểm tra đăng nhập trước khi vào Multiplayer
- Hiển thị popup nếu là guest hoặc chưa đăng nhập
- Cho phép chơi đơn không cần đăng nhập
- Tự động disable `UIButtonScreenNavigator` trên multiplayer button

**Logic:**
```
OnMultiplayerButtonClicked():
  if (IsGuestMode())
    → ShowGuestLoginRequiredPopup()
  else if (!IsLoggedIn())
    → ShowLoginRequiredPopup()
  else
    → NavigateToMultiplayer()
```

---

### 🔄 Files đã cập nhật

#### 4. UIMainMenuController.cs (Cập nhật)
**File:** `Assets/Script/Script_multiplayer/1Code/CODE/UIMainMenuController.cs`

**Thay đổi:**
- Hiển thị tên khách: `"Khách: [tên]"` nếu `IsGuestMode() == true`
- Hiển thị tên Firebase: `"Tên Nhân Vật: [tên]"` nếu đã đăng nhập
- Xóa dữ liệu khách khi đăng xuất

---

### 📚 Documentation

#### 5. GUEST_MODE_GUIDE.md ⭐ MỚI
**File:** `Assets/Script/Script_multiplayer/1Code/GUEST_MODE_GUIDE.md`

**Nội dung:**
- Tổng quan chế độ khách
- Luồng hoạt động (flowchart)
- Hướng dẫn setup từng bước
- Test cases chi tiết
- Troubleshooting

---

#### 6. SETUP_LOGIN_REQUIRED_POPUP.md ⭐ MỚI
**File:** `Assets/Script/Script_multiplayer/1Code/SETUP_LOGIN_REQUIRED_POPUP.md`

**Nội dung:**
- Hướng dẫn tạo UI popup từ đầu
- Setup script và references
- Tùy chỉnh giao diện
- Test cases
- Troubleshooting

---

## 🔧 Setup yêu cầu trong Unity

### 1. Tạo UI Components

**GameUICanvas:**
```
├─ NhapTen_choiNhanh (Panel)
│   ├─ NhapTen (TMP_InputField)
│   ├─ BatDau (Button)
│   └─ ErrorText (TextMeshPro - optional)
│
├─ LoginRequiredPopup (Panel)
│   ├─ Overlay (Image - Black, Alpha 180)
│   └─ ContentPanel (Panel)
│       ├─ TitleText (TextMeshPro)
│       ├─ MessageText (TextMeshPro)
│       └─ ButtonContainer (Empty + Horizontal Layout Group)
│           ├─ LoginButton (Button - Green)
│           └─ CancelButton (Button - Gray)
│
└─ ModSelectionPanel (Panel)
    ├─ multiplayerBtn (Button)
    └─ ChoiDonBtn (Button)
```

---

### 2. Gán Scripts

**NhapTen_choiNhanh:**
- Add Component: `UIQuickPlayNameController`
- Gán: nameInputField, startButton, errorText (optional)
- Set: targetPanelName = "MainMenuPanel"

**LoginRequiredPopup:**
- Add Component: `UILoginRequiredPopupController`
- Gán: titleText, messageText, loginButton, cancelButton
- Ẩn popup ban đầu (inactive)

**ModSelectionPanel:**
- Add Component: `UIModSelectionPanelController`
- Gán: multiplayerButton, loginRequiredPopup
- Script sẽ tự động disable UIButtonScreenNavigator trên multiplayerButton

---

## 🧪 Test Cases

### ✅ Test 1: Guest Mode Flow
1. Click "Chơi Nhanh"
2. Nhập tên → Click "Bắt Đầu"
3. MainMenuPanel hiển thị: "Khách: [tên]"
4. Click "Play" → Click "Chơi Đơn" → ✅ Vào game

### ✅ Test 2: Multiplayer Blocked
1. Guest mode → Click "Play" → Click "Multiplayer"
2. Popup hiển thị với 2 buttons
3. Click "Hủy" → Popup đóng, vẫn ở ModSelectionPanel
4. Click "Multiplayer" lại → Click "Đăng nhập" → Chuyển sang LoginPanel

### ✅ Test 3: Login After Guest
1. Guest mode → Click "Đăng Xuất"
2. Click "Đăng Nhập" → Đăng nhập thành công
3. MainMenuPanel hiển thị: "Tên Nhân Vật: [Firebase name]"
4. Click "Play" → Click "Multiplayer" → ✅ Vào multiplayer scene

---

## 🔒 Security & Data Flow

### PlayerPrefs Keys
| Key | Type | Mô tả | Xóa khi |
|-----|------|-------|---------|
| `GuestPlayerName` | string | Tên người chơi khách | Đăng nhập / Đăng xuất |
| `IsGuestMode` | int | 1 = guest, 0 = logged in | Đăng nhập / Đăng xuất |

### Guest Mode Restrictions
- ❌ Không thể chơi Multiplayer
- ❌ Không lưu dữ liệu lên Firebase
- ❌ Không đồng bộ cross-device
- ✅ Có thể chơi tất cả single-player modes
- ✅ Lưu tiến trình local (PlayerPrefs)

---

## 📊 Code Statistics

### Files Created: 3
- `UIQuickPlayNameController.cs` (180 lines)
- `UILoginRequiredPopupController.cs` (120 lines)
- `UIModSelectionPanelController.cs` (220 lines)

### Files Modified: 1
- `UIMainMenuController.cs` (thêm guest name display logic)

### Documentation: 3
- `GUEST_MODE_GUIDE.md`
- `SETUP_LOGIN_REQUIRED_POPUP.md`
- `CHANGELOG_GUEST_MODE.md` (this file)

---

## 🐛 Known Issues & Limitations

### Issue 1: Popup không tìm thấy
**Workaround:** Script có fallback tự động chuyển thẳng sang LoginPanel

### Issue 2: UIButtonScreenNavigator conflict
**Solution:** Script tự động disable UIButtonScreenNavigator trên multiplayer button

---

## 🚀 Future Improvements

### Có thể thêm:
1. **Guest data migration:** Chuyển tiến trình guest sang tài khoản khi đăng ký
2. **Guest leaderboard:** Bảng xếp hạng riêng cho guest (local only)
3. **Guest tutorial:** Hướng dẫn đăng ký để unlock multiplayer
4. **Guest time limit:** Giới hạn thời gian chơi guest để khuyến khích đăng ký
5. **Social features:** Cho phép guest xem (nhưng không tương tác) với social features

---

## 📞 Support

**Nếu gặp vấn đề:**
1. Đọc `GUEST_MODE_GUIDE.md` → Troubleshooting section
2. Đọc `SETUP_LOGIN_REQUIRED_POPUP.md` → Troubleshooting section
3. Kiểm tra Console logs với filter: `[QuickPlay]`, `[ModSelection]`, `[LoginRequiredPopup]`
4. Kiểm tra PlayerPrefs: `GuestPlayerName`, `IsGuestMode`

---

✅ **Guest Mode Implementation Complete!**

**Version:** 1.0.0  
**Date:** 2026-04-27  
**Status:** Ready for Testing
