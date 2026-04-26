# ✅ TODO: Unity Setup Checklist

## 🎯 Mục tiêu
Setup Returning User & Auto-Skip WELCOMESCREEN feature trong Unity Editor

**Thời gian ước tính:** 10-15 phút

---

## 📋 Checklist

### ✅ BƯỚC 1: Setup UIStartupController (3 phút)

**Mục tiêu:** Tạo controller tự động skip WELCOMESCREEN cho returning users

1. **Tìm GameObject chứa UI panels**
   - [ ] Mở scene chính (scene có WELCOMESCREEN, WellcomePanel)
   - [ ] Tìm GameObject: `GameUICanvas` hoặc `UIManager`

2. **Add UIStartupController**
   - [ ] Select GameObject
   - [ ] Inspector → **Add Component**
   - [ ] Search: `UIStartupController`
   - [ ] Click **Add**

3. **Gán References**
   - [ ] **Welcome Screen Panel:** Kéo `WELCOMESCREEN` vào đây
   - [ ] **Welcome Panel:** Kéo `WellcomePanel` vào đây
   - [ ] **Main Menu Panel:** Kéo `MainMenuPanel` vào đây (optional)
   - [ ] **Enable Auto Skip:** ✅ Tick checkbox

4. **Verify**
   - [ ] Tất cả references đã gán (không còn "None")
   - [ ] Enable Auto Skip đã tick

---

### ✅ BƯỚC 2: Update NhapTen_choiNhanh Panel (5 phút)

**Mục tiêu:** Thêm UI cho returning user (welcome message + continue button)

#### A. Tạo Continue Button

1. **Create Button**
   - [ ] Hierarchy → Right-click `NhapTen_choiNhanh`
   - [ ] **UI → Button - TextMeshPro**
   - [ ] Rename: `ContinueButton`

2. **Position Button**
   - [ ] Đặt bên cạnh button "Bắt đầu" (hoặc phía dưới)
   - [ ] Adjust size để cân đối với button "Bắt đầu"

3. **Style Button**
   - [ ] Select `ContinueButton`
   - [ ] Inspector → **Image** component
   - [ ] **Color:** Xanh lá (R: 76, G: 175, B: 80) hoặc #4CAF50
   - [ ] Select `ContinueButton/Text (TMP)`
   - [ ] **Text:** "Tiếp tục"
   - [ ] **Font Size:** 24-28 (match với button "Bắt đầu")
   - [ ] **Color:** Trắng

4. **Hide Initially**
   - [ ] Select `ContinueButton`
   - [ ] Inspector → Bỏ tick **Active** (script sẽ hiển thị khi cần)

#### B. Tạo Status Text

1. **Create Text**
   - [ ] Hierarchy → Right-click `NhapTen_choiNhanh`
   - [ ] **UI → Text - TextMeshPro**
   - [ ] Rename: `StatusText`

2. **Position Text**
   - [ ] Đặt phía trên input field (hoặc giữa title và input)
   - [ ] Adjust size: Width = 400-500, Height = 150-200

3. **Style Text**
   - [ ] Select `StatusText`
   - [ ] Inspector → **TextMeshPro** component
   - [ ] **Font Size:** 18-20
   - [ ] **Alignment:** Center (Horizontal + Vertical)
   - [ ] **Color:** Trắng hoặc Vàng (#FFC107)
   - [ ] **Wrapping:** Enabled
   - [ ] **Rich Text:** ✅ Enabled (quan trọng!)
   - [ ] **Overflow:** Overflow (để text không bị cắt)

4. **Hide Initially**
   - [ ] Select `StatusText`
   - [ ] Inspector → Bỏ tick **Active**

#### C. Gán vào UIQuickPlayNameController

1. **Select Panel**
   - [ ] Hierarchy → Select `NhapTen_choiNhanh`

2. **Gán References**
   - [ ] Inspector → **UIQuickPlayNameController** component
   - [ ] **Continue Button:** Kéo `ContinueButton` vào đây
   - [ ] **Status Text:** Kéo `StatusText` vào đây

3. **Verify Existing References**
   - [ ] **Name Input Field:** Đã gán (từ trước)
   - [ ] **Start Button:** Đã gán (từ trước)
   - [ ] **Error Text:** Đã gán (optional)
   - [ ] **Target Panel Name:** "MainMenuPanel"

---

### ✅ BƯỚC 3: Set Script Execution Order (2 phút)

**Mục tiêu:** Đảm bảo UIStartupController chạy trước tất cả UI controllers

1. **Open Settings**
   - [ ] Menu → **Edit → Project Settings**
   - [ ] Chọn **Script Execution Order** (bên trái)

2. **Add UIStartupController**
   - [ ] Click **"+"** (dưới cùng)
   - [ ] Search: `UIStartupController`
   - [ ] Select script

3. **Set Order**
   - [ ] Nhập order: **-100**
   - [ ] Press Enter

4. **Verify**
   - [ ] `UIStartupController` hiển thị với order `-100`
   - [ ] Click **Apply** (nếu có)

---

### ✅ BƯỚC 4: Test (5 phút)

**Mục tiêu:** Kiểm tra tất cả flows hoạt động đúng

#### Test 1: New User Flow

1. **Clear Data**
   - [ ] Menu → **Edit → Clear All PlayerPrefs**
   - [ ] Confirm

2. **Play Game**
   - [ ] Click **Play** button
   - [ ] **Expected:** Hiển thị WELCOMESCREEN (chọn lớp)

3. **Select Grade**
   - [ ] Chọn lớp: "2018 - Lớp 1"
   - [ ] Click "Chơi"
   - [ ] **Expected:** Hiển thị WellcomePanel

4. **Quick Play**
   - [ ] Click "Chơi Nhanh"
   - [ ] **Expected:** Hiển thị NhapTen_choiNhanh
   - [ ] **Expected:** Không có welcome message
   - [ ] **Expected:** Chỉ có button "Bắt đầu"

5. **Enter Name**
   - [ ] Nhập tên: `TestUser`
   - [ ] Click "Bắt đầu"
   - [ ] **Expected:** Vào MainMenuPanel

6. **Check Console**
   - [ ] Console log: `[QuickPlay] Saved selected grade: 1`
   - [ ] Console log: `[QuickPlay] Saved guest name: TestUser`

#### Test 2: Returning User - Auto-Skip

1. **Exit & Play Again**
   - [ ] Click **Stop** button
   - [ ] Click **Play** button

2. **Verify Auto-Skip**
   - [ ] **Expected:** WELCOMESCREEN bị skip
   - [ ] **Expected:** Hiển thị WellcomePanel ngay lập tức
   - [ ] Console log: `[Startup] Returning guest 'TestUser' with grade 1`

#### Test 3: Returning User - Continue

1. **Quick Play**
   - [ ] Click "Chơi Nhanh"

2. **Verify Welcome Message**
   - [ ] **Expected:** StatusText hiển thị: "Chào mừng trở lại, TestUser!"
   - [ ] **Expected:** Input field pre-filled: `TestUser`
   - [ ] **Expected:** Button "Tiếp tục" hiển thị (màu xanh)
   - [ ] **Expected:** Button "Bắt đầu" đổi thành "Chơi mới" (màu cam)

3. **Continue**
   - [ ] Click "Tiếp tục"
   - [ ] **Expected:** Vào MainMenuPanel
   - [ ] Console log: `[QuickPlay] User chose to continue with: TestUser`

#### Test 4: Returning User - New Game

1. **Exit & Play Again**
   - [ ] Click **Stop** → Click **Play**
   - [ ] Click "Chơi Nhanh"

2. **Enter New Name**
   - [ ] Xóa tên cũ
   - [ ] Nhập tên mới: `NewUser`
   - [ ] Click "Chơi mới"

3. **Verify Data Cleared**
   - [ ] Console log: `[QuickPlay] New name detected: 'NewUser' (old: 'TestUser')`
   - [ ] Console log: `[LocalProgress] Cleared all local data`
   - [ ] StatusText: "Đã xóa dữ liệu cũ!"
   - [ ] **Expected:** Vào MainMenuPanel

---

## 🐛 Troubleshooting

### Problem 1: Không auto-skip WELCOMESCREEN

**Symptoms:**
- Vẫn hiển thị WELCOMESCREEN dù đã chơi trước đó

**Check:**
- [ ] UIStartupController đã được gán vào GameObject?
- [ ] Enable Auto Skip đã tick?
- [ ] Script Execution Order = -100?
- [ ] Console log: `[Startup] Status - LoggedIn: ..., Guest: ..., HasGrade: ...`

**Fix:**
1. Verify tất cả checkboxes trên
2. Restart Unity Editor
3. Clear PlayerPrefs và test lại từ đầu

---

### Problem 2: Welcome message không hiển thị

**Symptoms:**
- Không thấy "Chào mừng trở lại" message
- Button "Tiếp tục" không hiển thị

**Check:**
- [ ] StatusText đã được gán vào UIQuickPlayNameController?
- [ ] ContinueButton đã được gán?
- [ ] StatusText có component TextMeshProUGUI (không phải Text)?
- [ ] Rich Text đã enabled?

**Fix:**
1. Select NhapTen_choiNhanh
2. Inspector → UIQuickPlayNameController
3. Verify tất cả references đã gán
4. Check Console log: `[QuickPlay] Returning user detected: ...`

---

### Problem 3: Button không click được

**Symptoms:**
- Click button không có phản ứng

**Check:**
- [ ] Button có component Button?
- [ ] Button có Raycast Target enabled?
- [ ] EventSystem có trong scene?
- [ ] Button không bị che bởi UI khác?

**Fix:**
1. Hierarchy → Check thứ tự: Button phải ở sau (render trên) StatusText
2. Select Button → Inspector → Image → Tick Raycast Target
3. Hierarchy → Tìm EventSystem (nếu không có, tạo mới: GameObject → UI → Event System)

---

### Problem 4: Dữ liệu không bị xóa

**Symptoms:**
- Nhập tên mới nhưng dữ liệu cũ vẫn còn

**Check:**
- [ ] Console log: `[LocalProgress] Cleared all local data`?
- [ ] LocalProgressService có trong scene?

**Fix:**
1. Check Console logs
2. Nếu không thấy log, LocalProgressService chưa được khởi tạo
3. Thử gọi thủ công: `LocalProgressService.Instance.LogStats()`

---

### Problem 5: Script Execution Order không apply

**Symptoms:**
- UIStartupController chạy sau UIManager

**Check:**
- [ ] Script Execution Order đã set -100?
- [ ] Đã click Apply?
- [ ] Đã restart Unity?

**Fix:**
1. Edit → Project Settings → Script Execution Order
2. Remove UIStartupController
3. Add lại với order -100
4. Restart Unity Editor

---

## 📊 Expected Console Logs

### New User
```
[Startup] Status - LoggedIn: False, Guest: False, HasGrade: False, Name: Guest
[Startup] New user or no grade selected → Showing WELCOMESCREEN
[UIManager] Năm sinh: 2018 - Lớp 1 -> GradeIndex đã lưu: 1
[QuickPlay] Saved selected grade: 1
[QuickPlay] Saved guest name: TestUser
```

### Returning User
```
[Startup] Status - LoggedIn: False, Guest: True, HasGrade: True, Name: TestUser
[Startup] Returning guest 'TestUser' with grade 1 → Navigating to WellcomePanel
[QuickPlay] Returning user detected: TestUser
[QuickPlay] User chose to continue with: TestUser
```

### New Game
```
[QuickPlay] Returning user detected: TestUser
[QuickPlay] New name detected: 'NewUser' (old: 'TestUser') - Clearing old data
[LocalProgress] Cleared all local data
[QuickPlay] Saved guest name: NewUser
```

---

## ✅ Final Checklist

### Setup Complete:
- [ ] UIStartupController added and configured
- [ ] ContinueButton created and styled
- [ ] StatusText created and styled
- [ ] All references assigned
- [ ] Script Execution Order set to -100

### Testing Complete:
- [ ] New user flow works
- [ ] Auto-skip works for returning users
- [ ] Welcome message displays correctly
- [ ] Continue button works (keeps data)
- [ ] New game button works (clears data)
- [ ] Console logs are correct

### Optional Enhancements:
- [ ] Add animation for welcome message
- [ ] Add sound effects for buttons
- [ ] Add confirmation dialog for "Chơi mới"
- [ ] Customize button colors/styles

---

## 📚 Documentation

**Đã đọc:**
- [ ] `SETUP_RETURNING_USER.md` - Full setup guide
- [ ] `RETURNING_USER_SUMMARY.md` - Implementation summary
- [ ] `QUICK_REFERENCE_RETURNING_USER.md` - Quick reference
- [ ] `RETURNING_USER_FLOW_DIAGRAM.md` - Visual diagrams

**Nếu cần hỗ trợ:**
- Check Console logs với filter: `[Startup]`, `[QuickPlay]`, `[LocalProgress]`
- Check PlayerPrefs: `GuestPlayerName`, `IsGuestMode`, `SelectedGrade`
- Đọc Troubleshooting section trong docs

---

✅ **Setup Complete! Ready to test!**

**Estimated time:** 10-15 minutes  
**Difficulty:** Easy  
**Status:** Ready for implementation
