# 🚀 Quick Reference - Returning User Feature

## 📋 Checklist Setup (5 phút)

### 1. Add UIStartupController
```
GameObject (GameUICanvas hoặc UIManager)
  → Add Component → UIStartupController
  → Gán: welcomeScreenPanel, welcomePanel, mainMenuPanel
  → Tick: Enable Auto Skip
```

### 2. Update NhapTen_choiNhanh
```
Tạo:
  - ContinueButton (Button) - Text: "Tiếp tục", Color: Green
  - StatusText (TextMeshPro) - Hidden ban đầu

Gán vào UIQuickPlayNameController:
  - Continue Button: [ContinueButton]
  - Status Text: [StatusText]
```

### 3. Set Script Execution Order
```
Edit → Project Settings → Script Execution Order
  → Add UIStartupController
  → Set order: -100
```

---

## 🎯 User Flows

### New User
```
WELCOMESCREEN → WellcomePanel → NhapTen → MainMenu
```

### Returning Guest
```
WellcomePanel (SKIP ✅) → NhapTen (Welcome!) → MainMenu
```

### Logged-in User
```
WellcomePanel (SKIP ✅)
```

---

## 💻 Code Snippets

### Check if returning user
```csharp
bool isReturning = UIQuickPlayNameController.IsGuestMode() 
                && !string.IsNullOrEmpty(UIQuickPlayNameController.GetGuestName())
                && UIQuickPlayNameController.HasSelectedGrade();
```

### Get saved data
```csharp
string name = UIQuickPlayNameController.GetGuestName();
int grade = UIQuickPlayNameController.GetSelectedGrade();
```

### Clear guest data (on login)
```csharp
UIQuickPlayNameController.ClearGuestData();
LocalProgressService.Instance.ClearAllData();
```

---

## 🧪 Quick Test

1. **Clear PlayerPrefs** → Play → Chọn lớp → Nhập tên
2. **Thoát & Play lại** → ✅ Auto-skip WELCOMESCREEN
3. **Click "Chơi Nhanh"** → ✅ Welcome message + 2 buttons
4. **Click "Tiếp tục"** → ✅ Giữ dữ liệu
5. **Nhập tên mới + "Chơi mới"** → ✅ Xóa dữ liệu

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| Không auto-skip | Check `Enable Auto Skip` + Script Execution Order |
| Welcome message không hiện | Check `statusText` và `continueButton` đã gán |
| Dữ liệu không xóa | Check Console log `[LocalProgress] Cleared all data` |
| Grade = 0 | Check `UIManager` có gọi `SaveSelectedGrade()` |

---

## 📚 Full Docs

- **Setup:** `SETUP_RETURNING_USER.md`
- **Summary:** `RETURNING_USER_SUMMARY.md`
- **Changelog:** `CHANGELOG_GUEST_MODE.md`

---

✅ **Done in 5 minutes!**
