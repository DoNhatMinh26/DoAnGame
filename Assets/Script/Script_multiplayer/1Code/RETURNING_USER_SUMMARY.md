# 🎯 Returning User Feature - Implementation Summary

## ✅ Đã hoàn thành

### 1. Enhanced UIQuickPlayNameController
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
// Kiểm tra returning user
private void CheckReturningUser()

// Hiển thị welcome message
private void ShowWelcomeBackMessage(string name)

// Xử lý button "Tiếp tục"
private void OnContinueButtonClicked()

// Lưu/lấy grade
public static void SaveSelectedGrade(int grade)
public static int GetSelectedGrade()
public static bool HasSelectedGrade()
```

---

### 2. UIStartupController (MỚI)
**File:** `Assets/Script/Script_multiplayer/1Code/CODE/UIStartupController.cs`

**Chức năng:**
- ✅ Kiểm tra trạng thái user khi app khởi động
- ✅ Auto-skip WELCOMESCREEN cho returning users
- ✅ Auto-skip cho logged-in users
- ✅ Restore SelectedGrade từ PlayerPrefs

**Logic:**
```
if (isLoggedIn)
    → Skip to WellcomePanel

else if (isGuest && hasName && hasGrade)
    → Restore UIManager.SelectedGrade
    → Skip to WellcomePanel

else
    → Show WELCOMESCREEN (new user)
```

---

### 3. Updated UIManager
**File:** `Assets/Script/Script_multiplayer/UIManager.cs`

**Thay đổi:**
- ✅ Lưu SelectedGrade vào PlayerPrefs khi user chọn lớp
- ✅ Gọi `UIQuickPlayNameController.SaveSelectedGrade()` trong `OnBirthYearSelectionChanged()`

---

### 4. Documentation
**Files:**
- ✅ `SETUP_RETURNING_USER.md` - Hướng dẫn setup chi tiết
- ✅ `RETURNING_USER_SUMMARY.md` - Tóm tắt implementation

---

## 🎨 UI Changes Required

### NhapTen_choiNhanh Panel

**Cần thêm:**
1. **Continue Button**
   - Name: `ContinueButton`
   - Text: "Tiếp tục"
   - Color: Xanh lá (#4CAF50)
   - Position: Bên cạnh button "Bắt đầu"
   - Initially: Hidden (script sẽ hiển thị khi cần)

2. **Status Text**
   - Name: `StatusText`
   - Type: TextMeshProUGUI
   - Font Size: 18-20
   - Alignment: Center
   - Rich Text: Enabled
   - Initially: Hidden

**Gán vào UIQuickPlayNameController:**
```
Continue Button: [Kéo ContinueButton vào đây]
Status Text: [Kéo StatusText vào đây]
```

---

### GameUICanvas (hoặc UIManager GameObject)

**Cần thêm:**
1. **UIStartupController Component**
   - Add Component → UIStartupController
   - Gán references:
     - Welcome Screen Panel: WELCOMESCREEN
     - Welcome Panel: WellcomePanel
     - Main Menu Panel: MainMenuPanel (optional)
   - Enable Auto Skip: ✅

---

## 📊 User Flows

### Flow 1: New User
```
App Start
  → WELCOMESCREEN (chọn lớp)
  → WellcomePanel
  → Click "Chơi Nhanh"
  → NhapTen_choiNhanh (no welcome message)
  → Nhập tên → Click "Bắt đầu"
  → MainMenuPanel
```

### Flow 2: Returning Guest - Continue
```
App Start
  → WellcomePanel (AUTO-SKIP ✅)
  → Click "Chơi Nhanh"
  → NhapTen_choiNhanh (welcome message + 2 buttons)
  → Click "Tiếp tục"
  → MainMenuPanel (dữ liệu giữ nguyên)
```

### Flow 3: Returning Guest - New Game
```
App Start
  → WellcomePanel (AUTO-SKIP ✅)
  → Click "Chơi Nhanh"
  → NhapTen_choiNhanh (welcome message)
  → Nhập tên mới → Click "Chơi mới"
  → LocalProgressService.ClearAllData() ✅
  → MainMenuPanel (dữ liệu đã xóa)
```

### Flow 4: Logged-in User
```
App Start
  → WellcomePanel (AUTO-SKIP ✅)
```

---

## 💾 PlayerPrefs Keys

| Key | Type | Value | Purpose |
|-----|------|-------|---------|
| `GuestPlayerName` | string | Tên người chơi | Lưu tên guest |
| `IsGuestMode` | int | 1 hoặc 0 | Đánh dấu guest mode |
| `SelectedGrade` | int | 1-5 | Lớp đã chọn |

**Khi nào bị xóa:**
- Đăng nhập → Xóa tất cả
- Đăng xuất → Xóa tất cả
- Nhập tên mới → Xóa `GuestPlayerName`, `SelectedGrade`, và dữ liệu LocalProgress

---

## 🧪 Testing Checklist

### Test 1: New User
- [ ] Xóa PlayerPrefs (Edit → Clear All PlayerPrefs)
- [ ] Play game
- [ ] Hiển thị WELCOMESCREEN ✅
- [ ] Chọn lớp → Console log: `[QuickPlay] Saved selected grade: X`
- [ ] Click "Chơi Nhanh"
- [ ] Không có welcome message ✅
- [ ] Nhập tên → Click "Bắt đầu"
- [ ] Vào MainMenuPanel ✅

### Test 2: Returning User - Continue
- [ ] Thoát và Play lại
- [ ] Auto-skip WELCOMESCREEN → WellcomePanel ✅
- [ ] Console log: `[Startup] Returning guest 'X' with grade Y`
- [ ] Click "Chơi Nhanh"
- [ ] Hiển thị welcome message ✅
- [ ] Button "Tiếp tục" hiển thị ✅
- [ ] Button "Bắt đầu" → "Chơi mới" ✅
- [ ] Click "Tiếp tục"
- [ ] Vào MainMenuPanel (dữ liệu giữ nguyên) ✅

### Test 3: Returning User - New Game
- [ ] Thoát và Play lại
- [ ] Auto-skip WELCOMESCREEN ✅
- [ ] Click "Chơi Nhanh"
- [ ] Hiển thị welcome message ✅
- [ ] Xóa tên cũ, nhập tên mới
- [ ] Click "Chơi mới"
- [ ] Console log: `[QuickPlay] New name detected`
- [ ] Console log: `[LocalProgress] Cleared all local data`
- [ ] Status text: "Đã xóa dữ liệu cũ!" ✅
- [ ] Vào MainMenuPanel ✅
- [ ] Kiểm tra dữ liệu đã xóa (score = 0) ✅

### Test 4: Logged-in User
- [ ] Đăng nhập vào game
- [ ] Thoát và Play lại
- [ ] Auto-skip WELCOMESCREEN → WellcomePanel ✅
- [ ] Console log: `[Startup] User logged in`

### Test 5: Disable Auto-Skip
- [ ] Bỏ tick "Enable Auto Skip" trong UIStartupController
- [ ] Play game
- [ ] Luôn hiển thị WELCOMESCREEN ✅

---

## 🔧 Script Execution Order

**QUAN TRỌNG:** `UIStartupController` phải chạy trước các UI controllers khác

**Setup:**
1. **Edit → Project Settings → Script Execution Order**
2. Click **"+"** → Chọn `UIStartupController`
3. Set order: **-100** (chạy sớm)
4. Click **Apply**

---

## 🐛 Common Issues

### Issue 1: Không auto-skip
**Fix:** Kiểm tra `UIStartupController` đã được gán và `Enable Auto Skip` đã tick

### Issue 2: Welcome message không hiển thị
**Fix:** Kiểm tra `statusText` và `continueButton` đã được gán trong Inspector

### Issue 3: Dữ liệu không bị xóa
**Fix:** Kiểm tra Console log `[LocalProgress] Cleared all local data`

### Issue 4: SelectedGrade = 0
**Fix:** Kiểm tra `UIManager.OnBirthYearSelectionChanged()` có gọi `SaveSelectedGrade()`

---

## 📝 Code Examples

### Kiểm tra returning user
```csharp
bool isReturning = UIQuickPlayNameController.IsGuestMode() 
                && !string.IsNullOrEmpty(UIQuickPlayNameController.GetGuestName())
                && UIQuickPlayNameController.HasSelectedGrade();
```

### Lấy dữ liệu đã lưu
```csharp
string name = UIQuickPlayNameController.GetGuestName();
int grade = UIQuickPlayNameController.GetSelectedGrade();
Debug.Log($"Returning user: {name}, Grade: {grade}");
```

### Xóa dữ liệu khi đăng nhập
```csharp
// Trong AuthManager hoặc LoginController
UIQuickPlayNameController.ClearGuestData();
LocalProgressService.Instance.ClearAllData();
```

---

## 🎯 Next Steps

### Để hoàn thành tính năng:

1. **Setup UI trong Unity:**
   - [ ] Tạo Continue Button
   - [ ] Tạo Status Text
   - [ ] Gán UIStartupController
   - [ ] Gán references

2. **Set Script Execution Order:**
   - [ ] UIStartupController = -100

3. **Test tất cả flows:**
   - [ ] New user
   - [ ] Returning user - Continue
   - [ ] Returning user - New game
   - [ ] Logged-in user
   - [ ] Disable auto-skip

4. **Optional enhancements:**
   - [ ] Animation cho welcome message
   - [ ] Sound effects cho buttons
   - [ ] Confirmation dialog cho "Chơi mới"

---

## 📚 Related Files

- `UIQuickPlayNameController.cs` - Main controller
- `UIStartupController.cs` - Auto-skip logic
- `UIManager.cs` - Grade selection
- `LocalProgressService.cs` - Data storage
- `SETUP_RETURNING_USER.md` - Setup guide
- `GUEST_MODE_GUIDE.md` - Guest mode overview

---

✅ **Implementation Complete!**

Tất cả code đã sẵn sàng. Chỉ cần setup UI trong Unity Editor theo hướng dẫn trong `SETUP_RETURNING_USER.md`.
