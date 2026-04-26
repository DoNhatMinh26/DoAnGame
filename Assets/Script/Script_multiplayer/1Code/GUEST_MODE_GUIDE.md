# 🎮 Hướng dẫn Chế độ Chơi Nhanh (Guest Mode)

## 📋 Tổng quan

Chế độ **Chơi Nhanh** cho phép người chơi vào game mà không cần đăng ký tài khoản. Tuy nhiên, họ **BẮT BUỘC phải đăng nhập** để chơi Multiplayer.

---

## 🔄 Luồng hoạt động

### 1. Chơi Nhanh (Guest Mode)

```
WellcomePanel
  ↓ Click "Chơi Nhanh"
NhapTen_choiNhanh
  ↓ Nhập tên → Click "Bắt Đầu"
MainMenuPanel (Chế độ khách)
  ↓ Click "Play"
ModSelectionPanel
  ├─ Click "Chơi Đơn" → OK ✅
  └─ Click "Multiplayer" → ❌ YÊU CẦU ĐĂNG NHẬP
```

### 2. Đăng nhập từ Guest Mode

```
ModSelectionPanel
  ↓ Click "Multiplayer"
LoginRequiredPopup (Popup với 2 buttons)
  ├─ Click "Đăng nhập" → LoginPanel
  └─ Click "Hủy" → Đóng popup, vẫn ở ModSelectionPanel
LoginPanel
  ↓ Đăng nhập thành công
MainMenuPanel (Đã đăng nhập)
  ↓ Click "Play"
ModSelectionPanel
  ↓ Click "Multiplayer"
Multiplayer Scene ✅
```

---

## 📁 Files đã tạo

### 1. UIQuickPlayNameController.cs
**Đường dẫn:** `Assets/Script/Script_multiplayer/1Code/CODE/UIQuickPlayNameController.cs`

**Chức năng:**
- Xử lý panel nhập tên chơi nhanh
- Validate tên người chơi (3-20 ký tự)
- Lưu tên vào PlayerPrefs
- Đánh dấu chế độ khách

**Public Methods:**
```csharp
// Kiểm tra xem có đang ở chế độ khách không
public static bool IsGuestMode()

// Lấy tên người chơi khách
public static string GetGuestName()

// Xóa dữ liệu khách (khi đăng nhập)
public static void ClearGuestData()
```

---

### 2. UIModSelectionPanelController.cs
**Đường dẫn:** `Assets/Script/Script_multiplayer/1Code/CODE/UIModSelectionPanelController.cs`

**Chức năng:**
- Kiểm tra đăng nhập trước khi vào Multiplayer
- Hiển thị popup yêu cầu đăng nhập nếu là khách
- Cho phép chơi đơn không cần đăng nhập

---

### 3. UILoginRequiredPopupController.cs ⭐ MỚI
**Đường dẫn:** `Assets/Script/Script_multiplayer/1Code/CODE/UILoginRequiredPopupController.cs`

**Chức năng:**
- Popup thông báo yêu cầu đăng nhập
- 2 buttons: "Đăng nhập" và "Hủy"
- Callback system cho xử lý sự kiện

**Public Methods:**
```csharp
// Hiển thị popup với message tùy chỉnh
public void Show(string title, string message, System.Action onLogin, System.Action onCancel)

// Hiển thị popup với message mặc định
public void Show(System.Action onLogin, System.Action onCancel)

// Ẩn popup
public void Hide()

// Tìm popup trong scene
public static UILoginRequiredPopupController FindInScene()
```

---

### 4. UIMainMenuController.cs (Đã cập nhật)
**Đường dẫn:** `Assets/Script/Script_multiplayer/1Code/CODE/UIMainMenuController.cs`

**Thay đổi:**
- Hiển thị tên khách nếu ở chế độ khách
- Xóa dữ liệu khách khi đăng xuất

---

## 🔧 Setup trong Unity

### Bước 1: Setup NhapTen_choiNhanh Panel

1. **Hierarchy:** Chọn **NhapTen_choiNhanh**
2. **Add Component:** `UIQuickPlayNameController`
3. **Inspector:**
   ```
   Name Input Field: [Kéo NhapTen InputField vào đây]
   Start Button: [Kéo BatDau Button vào đây]
   Error Text: [Tạo TextMeshPro cho error, hoặc để trống]
   Target Panel Name: "MainMenuPanel"
   ```

---

### Bước 2: Setup LoginRequiredPopup ⭐ MỚI

**Xem hướng dẫn chi tiết tại:** `SETUP_LOGIN_REQUIRED_POPUP.md`

**Tóm tắt:**
1. Tạo **LoginRequiredPopup** trong **GameUICanvas**
2. Tạo UI: Overlay + ContentPanel + TitleText + MessageText + 2 Buttons
3. Gán **UILoginRequiredPopupController** vào popup
4. Gán references (TitleText, MessageText, LoginButton, CancelButton)
5. Ẩn popup ban đầu (inactive)

---

### Bước 3: Setup ModSelectionPanel

1. **Hierarchy:** Chọn **ModSelectionPanel**
2. **Add Component:** `UIModSelectionPanelController`
3. **Inspector:**
   ```
   Multiplayer Button: [Kéo multiplayerBtn vào đây]
   Single Player Button: [Kéo ChoiDonBtn vào đây] (optional)
   Login Required Popup: [Kéo LoginRequiredPopup vào đây] ⭐ MỚI
   ```

**Lưu ý:** Nếu button đã có `UIButtonScreenNavigator`, script sẽ tự động disable nó.

---

### Bước 4: Setup WellcomePanel Button

1. **Hierarchy:** Chọn **WellcomePanel → ChoiNhanh**
2. **Inspector → UIButtonScreenNavigator:**
   ```
   Target Panel Name: "NhapTen_choiNhanh"
   ```

---

## 🧪 Test

### Test Case 1: Chơi Nhanh → Chơi Đơn

1. **Play game**
2. Click **"Chơi Nhanh"**
3. Nhập tên: `TestGuest`
4. Click **"Bắt Đầu"**
5. **MainMenuPanel** hiển thị: `Khách: TestGuest`
6. Click **"Play"**
7. Click **"Chơi Đơn"** → ✅ Vào game bình thường

---

### Test Case 2: Chơi Nhanh → Multiplayer (Bị chặn) ⭐ CẬP NHẬT

1. **Play game**
2. Click **"Chơi Nhanh"**
3. Nhập tên: `TestGuest`
4. Click **"Bắt Đầu"**
5. Click **"Play"**
6. Click **"Multiplayer"**
7. **Popup hiển thị:**
   - Title: "Yêu Cầu Đăng Nhập"
   - Message: "Chế độ Multiplayer yêu cầu tài khoản..."
   - 2 buttons: "Đăng nhập" (xanh) và "Hủy" (xám)
8. **Test 2 trường hợp:**
   - Click **"Đăng nhập"** → Chuyển sang **LoginPanel** ✅
   - Click **"Hủy"** → Đóng popup, vẫn ở **ModSelectionPanel** ✅

---

### Test Case 3: Đăng nhập sau khi Chơi Nhanh

1. **Play game**
2. Click **"Chơi Nhanh"**
3. Nhập tên: `TestGuest`
4. Click **"Bắt Đầu"**
5. Click **"Đăng Xuất"** (hoặc Back)
6. Click **"Đăng Nhập"**
7. Đăng nhập thành công
8. **MainMenuPanel** hiển thị: `Tên Nhân Vật: [Tên từ Firebase]` (không còn "Khách")
9. Click **"Play"** → **"Multiplayer"** → ✅ Vào multiplayer scene

---

## 💾 PlayerPrefs Keys

| Key | Type | Mô tả |
|-----|------|-------|
| `GuestPlayerName` | string | Tên người chơi khách |
| `IsGuestMode` | int | 1 = Chế độ khách, 0 = Đã đăng nhập |

**Lưu ý:** Dữ liệu này sẽ bị xóa khi:
- Đăng nhập thành công
- Đăng xuất
- Gọi `UIQuickPlayNameController.ClearGuestData()`

---

## 🔒 Bảo mật

### Chế độ khách KHÔNG thể:
- ❌ Chơi Multiplayer
- ❌ Xem Bảng Xếp Hạng (nếu yêu cầu đăng nhập)
- ❌ Lưu tiến trình lên Firebase
- ❌ Đồng bộ dữ liệu cross-device

### Chế độ khách CÓ THỂ:
- ✅ Chơi Đơn (Single Player)
- ✅ Chơi tất cả các mode: Chọn Đáp Án, Kéo Thả, Phi Thuyền
- ✅ Lưu tiến trình local (PlayerPrefs)

---

## 🐛 Troubleshooting

### Vấn đề 1: Popup không hiển thị

**Nguyên nhân:** Không tìm thấy `UILoginRequiredPopupController`

**Giải pháp:**
1. Kiểm tra `LoginRequiredPopup` có trong **GameUICanvas** không
2. Kiểm tra đã gán **UILoginRequiredPopupController** vào popup chưa
3. Kiểm tra đã gán popup vào **UIModSelectionPanelController** chưa
4. Xem Console log: `[ModSelection] UILoginRequiredPopupController not found in scene!`
5. **Fallback:** Nếu không tìm thấy popup, script sẽ tự động chuyển thẳng sang LoginPanel

---

### Vấn đề 2: Vẫn vào được Multiplayer dù là khách

**Nguyên nhân:** `UIModSelectionPanelController` chưa được gán vào ModSelectionPanel

**Giải pháp:**
1. Chọn **ModSelectionPanel** trong Hierarchy
2. **Add Component** → `UIModSelectionPanelController`
3. Gán **multiplayerBtn** và **loginRequiredPopup** vào Inspector

---

### Vấn đề 3: Tên khách không hiển thị trong MainMenuPanel

**Nguyên nhân:** `UIMainMenuController` chưa được cập nhật

**Giải pháp:**
1. Đảm bảo file `UIMainMenuController.cs` đã được cập nhật
2. Recompile script (Ctrl+R hoặc restart Unity)

---

### Vấn đề 4: Buttons trong popup không click được

**Nguyên nhân:** Overlay che mất buttons

**Giải pháp:**
1. Kiểm tra thứ tự Hierarchy: Overlay phải ở dưới ContentPanel
2. Bỏ tick **Raycast Target** ở Overlay Image component
3. Xem chi tiết tại `SETUP_LOGIN_REQUIRED_POPUP.md`

---

## 📊 Luồng dữ liệu

```
┌─────────────────────────────────────────────────────┐
│ NhapTen_choiNhanh                                   │
│ ↓                                                   │
│ PlayerPrefs.SetString("GuestPlayerName", name)     │
│ PlayerPrefs.SetInt("IsGuestMode", 1)               │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│ MainMenuPanel                                       │
│ ↓                                                   │
│ if (IsGuestMode())                                  │
│   → Hiển thị: "Khách: [name]"                      │
│ else                                                │
│   → Hiển thị: "Tên Nhân Vật: [Firebase name]"     │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│ ModSelectionPanel                                   │
│ ↓                                                   │
│ Click "Multiplayer"                                 │
│ ↓                                                   │
│ if (IsGuestMode() || !IsLoggedIn())                │
│   → Popup: "Yêu cầu đăng nhập"                     │
│ else                                                │
│   → Load multiplayer scene                         │
└─────────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────────┐
│ LoginPanel                                          │
│ ↓                                                   │
│ Đăng nhập thành công                                │
│ ↓                                                   │
│ ClearGuestData()                                    │
│ → Xóa "GuestPlayerName", "IsGuestMode"            │
└─────────────────────────────────────────────────────┘
```

---

## ✅ Checklist

### Setup:
- [ ] Gán `UIQuickPlayNameController` vào `NhapTen_choiNhanh`
- [ ] Tạo `LoginRequiredPopup` trong `GameUICanvas` (xem `SETUP_LOGIN_REQUIRED_POPUP.md`)
- [ ] Gán `UILoginRequiredPopupController` vào popup
- [ ] Gán `UIModSelectionPanelController` vào `ModSelectionPanel`
- [ ] Gán references trong Inspector (InputField, Buttons, Popup)
- [ ] Setup `ChoiNhanh` button → Navigate to `NhapTen_choiNhanh`

### Test:
- [ ] Chơi nhanh → Nhập tên → Vào MainMenuPanel
- [ ] MainMenuPanel hiển thị "Khách: [tên]"
- [ ] Chơi đơn hoạt động bình thường
- [ ] Multiplayer bị chặn → Hiển thị popup với 2 buttons
- [ ] Click "Đăng nhập" → Chuyển sang LoginPanel
- [ ] Click "Hủy" → Đóng popup, vẫn ở ModSelectionPanel
- [ ] Đăng nhập → Xóa dữ liệu khách → Vào multiplayer được

---

✅ **Hoàn thành! Chế độ Chơi Nhanh đã sẵn sàng!**
