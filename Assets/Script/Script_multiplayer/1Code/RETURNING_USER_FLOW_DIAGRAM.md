# 🔄 Returning User Flow Diagram

## 📊 Complete Flow Chart

```
┌─────────────────────────────────────────────────────────────────┐
│                         APP START                               │
│                            ↓                                    │
│                  UIStartupController                            │
│                   CheckAndRoute()                               │
└─────────────────────────────────────────────────────────────────┘
                            ↓
        ┌───────────────────┼───────────────────┐
        ↓                   ↓                   ↓
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│  Logged In?   │   │ Guest + Name  │   │   New User    │
│     YES       │   │   + Grade?    │   │               │
│               │   │     YES       │   │               │
└───────┬───────┘   └───────┬───────┘   └───────┬───────┘
        │                   │                   │
        │                   │                   │
        ↓                   ↓                   ↓
┌───────────────┐   ┌───────────────┐   ┌───────────────┐
│ WellcomePanel │   │ WellcomePanel │   │ WELCOMESCREEN │
│  (SKIP ✅)    │   │  (SKIP ✅)    │   │  (Chọn lớp)   │
│               │   │ + Restore     │   │               │
│               │   │   Grade       │   │               │
└───────┬───────┘   └───────┬───────┘   └───────┬───────┘
        │                   │                   │
        │                   │                   ↓
        │                   │           ┌───────────────┐
        │                   │           │ Chọn lớp      │
        │                   │           │ → Save Grade  │
        │                   │           └───────┬───────┘
        │                   │                   │
        ↓                   ↓                   ↓
┌─────────────────────────────────────────────────────────────────┐
│                      WellcomePanel                              │
│                  (Menu chính game)                              │
└─────────────────────────────────────────────────────────────────┘
                            ↓
                   Click "Chơi Nhanh"
                            ↓
┌─────────────────────────────────────────────────────────────────┐
│                   NhapTen_choiNhanh                             │
│                 CheckReturningUser()                            │
└─────────────────────────────────────────────────────────────────┘
                            ↓
        ┌───────────────────┼───────────────────┐
        ↓                                       ↓
┌───────────────────────────┐         ┌───────────────────────────┐
│   Returning User          │         │      New User             │
│   (Has Name + Grade)      │         │   (No saved data)         │
└───────────────────────────┘         └───────────────────────────┘
        ↓                                       ↓
┌───────────────────────────┐         ┌───────────────────────────┐
│ Show Welcome Message:     │         │ Normal Input:             │
│ "Chào mừng trở lại!"      │         │ - Empty input field       │
│                           │         │ - Button "Bắt đầu"        │
│ UI Changes:               │         │ - No welcome message      │
│ ✅ Pre-fill name          │         └───────────┬───────────────┘
│ ✅ Show "Tiếp tục" button │                     │
│ ✅ "Bắt đầu" → "Chơi mới" │                     │
│ ✅ Show status text       │                     │
└───────────────────────────┘                     │
        ↓                                         │
┌───────┴───────┐                                 │
│ User Choice:  │                                 │
└───────┬───────┘                                 │
        │                                         │
    ┌───┴───┐                                     │
    ↓       ↓                                     ↓
┌─────┐ ┌─────────┐                      ┌───────────────┐
│Tiếp │ │Chơi mới │                      │  Nhập tên     │
│tục  │ │+ Tên mới│                      │  → Bắt đầu    │
└──┬──┘ └────┬────┘                      └───────┬───────┘
   │         │                                   │
   │         ↓                                   │
   │  ┌──────────────┐                          │
   │  │ Clear Data:  │                          │
   │  │ - LocalProg  │                          │
   │  │ - Name       │                          │
   │  │ - Grade      │                          │
   │  └──────┬───────┘                          │
   │         │                                   │
   └─────────┴───────────────────────────────────┘
                     ↓
┌─────────────────────────────────────────────────────────────────┐
│                      MainMenuPanel                              │
│                   (Bắt đầu chơi)                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎯 Decision Points

### 1. UIStartupController Decision Tree

```
CheckAndRoute()
│
├─ if (Firebase.Auth.CurrentUser != null)
│   → isLoggedIn = TRUE
│   → ShowWellcomePanel() ✅ SKIP
│
├─ else if (SessionToken valid)
│   → isLoggedIn = TRUE
│   → ShowWellcomePanel() ✅ SKIP
│
├─ else if (IsGuestMode() && HasName() && HasGrade())
│   → isReturningGuest = TRUE
│   → Restore UIManager.SelectedGrade
│   → ShowWellcomePanel() ✅ SKIP
│
└─ else
    → isNewUser = TRUE
    → ShowWelcomeScreen() (WELCOMESCREEN)
```

---

### 2. UIQuickPlayNameController Decision Tree

```
CheckReturningUser()
│
├─ if (IsGuestMode() && HasName() && HasGrade())
│   → isReturningUser = TRUE
│   → ShowWelcomeBackMessage()
│   → Show "Tiếp tục" button
│   → Change "Bắt đầu" to "Chơi mới"
│   → Pre-fill name
│
└─ else
    → isReturningUser = FALSE
    → Normal flow (empty input)
```

---

### 3. OnStartButtonClicked Decision Tree

```
OnStartButtonClicked()
│
├─ if (isReturningUser && newName != oldName)
│   → LocalProgressService.ClearAllData()
│   → Show "Đã xóa dữ liệu cũ!"
│   → Save new name
│   → NavigateToMainMenu()
│
├─ else if (isReturningUser && newName == oldName)
│   → Keep data
│   → NavigateToMainMenu()
│
└─ else (new user)
    → Validate name
    → Save name
    → NavigateToMainMenu()
```

---

## 📊 State Diagram

```
┌─────────────┐
│  New User   │
│  (No data)  │
└──────┬──────┘
       │ Chọn lớp + Nhập tên
       ↓
┌─────────────┐
│   Guest     │
│  (Has data) │◄─────────┐
└──────┬──────┘          │
       │                 │
       │ Thoát & Vào lại │
       ↓                 │
┌─────────────┐          │
│ Returning   │          │
│   Guest     │──────────┘
│ (Auto-skip) │  Tiếp tục
└──────┬──────┘
       │
       │ Nhập tên mới
       ↓
┌─────────────┐
│   Guest     │
│ (New data)  │
└──────┬──────┘
       │
       │ Đăng nhập
       ↓
┌─────────────┐
│ Logged-in   │
│    User     │
│ (Auto-skip) │
└─────────────┘
```

---

## 🔄 Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        PlayerPrefs                              │
│  ┌──────────────┬──────────────┬──────────────┐                │
│  │GuestPlayerName│ IsGuestMode  │SelectedGrade │                │
│  │   (string)    │    (int)     │    (int)     │                │
│  └──────┬────────┴──────┬───────┴──────┬───────┘                │
└─────────┼───────────────┼──────────────┼────────────────────────┘
          ↓               ↓              ↓
┌─────────────────────────────────────────────────────────────────┐
│                  UIStartupController                            │
│  - CheckIfLoggedIn()                                            │
│  - IsGuestMode()                                                │
│  - HasSelectedGrade()                                           │
│  - GetGuestName()                                               │
└─────────────────────────────────────────────────────────────────┘
          ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Route Decision                               │
│  → WELCOMESCREEN (new user)                                     │
│  → WellcomePanel (returning/logged-in)                          │
└─────────────────────────────────────────────────────────────────┘
          ↓
┌─────────────────────────────────────────────────────────────────┐
│              UIQuickPlayNameController                          │
│  - CheckReturningUser()                                         │
│  - ShowWelcomeBackMessage()                                     │
│  - OnContinueButtonClicked() → Keep data                        │
│  - OnStartButtonClicked() → Clear if new name                   │
└─────────────────────────────────────────────────────────────────┘
          ↓
┌─────────────────────────────────────────────────────────────────┐
│                 LocalProgressService                            │
│  - GetAllData() → Read progress                                 │
│  - ClearAllData() → Delete if new name                          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎨 UI State Changes

### NhapTen_choiNhanh Panel States

#### State 1: New User
```
┌─────────────────────────────────────┐
│      Nhập Tên Chơi Nhanh            │
├─────────────────────────────────────┤
│                                     │
│  Tên: [____________]                │
│                                     │
│       [  Bắt đầu  ]                 │
│                                     │
└─────────────────────────────────────┘
```

#### State 2: Returning User
```
┌─────────────────────────────────────┐
│      Nhập Tên Chơi Nhanh            │
├─────────────────────────────────────┤
│  ┌─────────────────────────────┐   │
│  │ Chào mừng trở lại, TestUser!│   │
│  │                             │   │
│  │ • Tiếp tục → Giữ dữ liệu    │   │
│  │ • Chơi mới → Xóa dữ liệu    │   │
│  └─────────────────────────────┘   │
│                                     │
│  Tên: [TestUser____]                │
│                                     │
│  [ Tiếp tục ]  [ Chơi mới ]         │
│    (Green)       (Orange)           │
│                                     │
└─────────────────────────────────────┘
```

---

## 📝 Console Log Flow

### New User Flow
```
[Startup] Status - LoggedIn: False, Guest: False, HasGrade: False, Name: Guest
[Startup] New user or no grade selected → Showing WELCOMESCREEN
[UIManager] Năm sinh: 2018 - Lớp 1 -> GradeIndex đã lưu: 1
[QuickPlay] Saved selected grade: 1
[QuickPlay] Saved guest name: TestUser
[QuickPlay] Navigating to MainMenuPanel
```

### Returning User Flow
```
[Startup] Status - LoggedIn: False, Guest: True, HasGrade: True, Name: TestUser
[Startup] Returning guest 'TestUser' with grade 1 → Navigating to WellcomePanel
[QuickPlay] Returning user detected: TestUser
[QuickPlay] User chose to continue with: TestUser
[QuickPlay] Navigating to MainMenuPanel
```

### New Game Flow
```
[QuickPlay] Returning user detected: TestUser
[QuickPlay] New name detected: 'NewUser' (old: 'TestUser') - Clearing old data
[LocalProgress] Cleared all local data
[QuickPlay] Saved guest name: NewUser
[QuickPlay] Navigating to MainMenuPanel
```

---

## ✅ Validation Checklist

### Startup Phase
- [ ] UIStartupController runs first (Script Execution Order = -100)
- [ ] CheckAndRoute() called in Start()
- [ ] Correct panel shown based on user state

### Returning User Detection
- [ ] CheckReturningUser() called in Start()
- [ ] Welcome message shown if has name + grade
- [ ] Continue button visible
- [ ] Start button text changed to "Chơi mới"

### Data Management
- [ ] Grade saved when selected in WELCOMESCREEN
- [ ] Name saved when "Bắt đầu" clicked
- [ ] Data cleared when new name entered
- [ ] Data kept when "Tiếp tục" clicked

---

✅ **Visual guide complete!**
