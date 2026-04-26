# ⚙️ Hướng dẫn Setup Settings Popup

## 📋 Tổng quan

Settings Popup hiển thị khi người chơi nhấn button Settings, bao gồm:
- 🔊 **Volume Slider** - Điều chỉnh âm thanh (UI only, chưa áp dụng vào game)
- 🚪 **Exit Game** - Thoát game
- 🏠 **Back to Menu** - Quay về Main Menu
- ❌ **Close** - Đóng popup

---

## 🎨 Tạo Settings Popup UI

### Bước 1: Tạo SettingsPopup GameObject

1. **Hierarchy:** Right-click **GameUICanvas** → **UI → Panel**
2. **Rename:** `SettingsPopup`
3. **RectTransform:**
   ```
   Anchor Presets: Stretch (Alt+Shift+Click)
   Left: 0, Top: 0, Right: 0, Bottom: 0
   ```

---

### Bước 2: Tạo Overlay (Background mờ)

1. **Hierarchy:** Right-click **SettingsPopup** → **UI → Image**
2. **Rename:** `Overlay`
3. **RectTransform:**
   ```
   Anchor Presets: Stretch
   Left: 0, Top: 0, Right: 0, Bottom: 0
   ```
4. **Image Component:**
   ```
   Color: Black (R:0, G:0, B:0, A:180)
   ```

---

### Bước 3: Tạo ContentPanel

1. **Hierarchy:** Right-click **SettingsPopup** → **UI → Panel**
2. **Rename:** `ContentPanel`
3. **RectTransform:**
   ```
   Anchor Presets: Center
   Width: 600
   Height: 500
   Pos X: 0, Pos Y: 0
   ```
4. **Image Component:**
   ```
   Color: White hoặc màu nền popup
   ```

---

### Bước 4: Tạo Title Text

1. **Hierarchy:** Right-click **ContentPanel** → **UI → Text - TextMeshPro**
2. **Rename:** `TitleText`
3. **RectTransform:**
   ```
   Anchor Presets: Top Center
   Width: 500
   Height: 80
   Pos X: 0, Pos Y: -50
   ```
4. **TextMeshPro:**
   ```
   Text: "Cài Đặt"
   Font Size: 40
   Alignment: Center
   Color: Black
   Font Style: Bold
   ```

---

### Bước 5: Tạo Volume Section

#### 5.1: Volume Label

1. **Hierarchy:** Right-click **ContentPanel** → **UI → Text - TextMeshPro**
2. **Rename:** `VolumeLabel`
3. **RectTransform:**
   ```
   Anchor Presets: Top Left
   Width: 200
   Height: 40
   Pos X: 100, Pos Y: -150
   ```
4. **TextMeshPro:**
   ```
   Text: "Âm Thanh:"
   Font Size: 28
   Alignment: Left
   ```

#### 5.2: Volume Slider

1. **Hierarchy:** Right-click **ContentPanel** → **UI → Slider**
2. **Rename:** `VolumeSlider`
3. **RectTransform:**
   ```
   Anchor Presets: Top Center
   Width: 400
   Height: 40
   Pos X: 0, Pos Y: -200
   ```
4. **Slider Component:**
   ```
   Min Value: 0
   Max Value: 1
   Value: 1
   Whole Numbers: OFF
   ```

#### 5.3: Volume Value Text

1. **Hierarchy:** Right-click **ContentPanel** → **UI → Text - TextMeshPro**
2. **Rename:** `VolumeValueText`
3. **RectTransform:**
   ```
   Anchor Presets: Top Right
   Width: 100
   Height: 40
   Pos X: -80, Pos Y: -200
   ```
4. **TextMeshPro:**
   ```
   Text: "100%"
   Font Size: 28
   Alignment: Right
   ```

---

### Bước 6: Tạo Button Container

1. **Hierarchy:** Right-click **ContentPanel** → **Create Empty**
2. **Rename:** `ButtonContainer`
3. **RectTransform:**
   ```
   Anchor Presets: Bottom Center
   Width: 500
   Height: 200
   Pos X: 0, Pos Y: 80
   ```
4. **Add Component:** `Vertical Layout Group`
   ```
   Spacing: 15
   Child Alignment: Middle Center
   Child Force Expand: Width ✓, Height ✗
   Child Control Size: Height ✓
   ```

---

### Bước 7: Tạo Exit Game Button

1. **Hierarchy:** Right-click **ButtonContainer** → **UI → Button - TextMeshPro**
2. **Rename:** `ExitGameButton`
3. **RectTransform:**
   ```
   Height: 60
   ```
4. **Button Component:**
   ```
   Normal Color: Red (R:200, G:0, B:0)
   Highlighted Color: Light Red
   Pressed Color: Dark Red
   ```
5. **Text (child):**
   ```
   Text: "🚪 Thoát Game"
   Font Size: 28
   Alignment: Center
   Color: White
   Font Style: Bold
   ```

---

### Bước 8: Tạo Back to Menu Button

1. **Hierarchy:** Right-click **ButtonContainer** → **UI → Button - TextMeshPro**
2. **Rename:** `BackToMenuButton`
3. **RectTransform:**
   ```
   Height: 60
   ```
4. **Button Component:**
   ```
   Normal Color: Blue (R:0, G:100, B:200)
   Highlighted Color: Light Blue
   Pressed Color: Dark Blue
   ```
5. **Text (child):**
   ```
   Text: "🏠 Về Menu"
   Font Size: 28
   Alignment: Center
   Color: White
   Font Style: Bold
   ```

---

### Bước 9: Tạo Close Button (X)

1. **Hierarchy:** Right-click **ContentPanel** → **UI → Button - TextMeshPro**
2. **Rename:** `CloseButton`
3. **RectTransform:**
   ```
   Anchor Presets: Top Right
   Width: 60
   Height: 60
   Pos X: -20, Pos Y: -20
   ```
4. **Button Component:**
   ```
   Normal Color: Gray (R:150, G:150, B:150)
   ```
5. **Text (child):**
   ```
   Text: "✕"
   Font Size: 40
   Alignment: Center
   Color: White
   Font Style: Bold
   ```

---

## 🔧 Setup Script

### Bước 10: Gán SettingsPopupController

1. **Hierarchy:** Chọn **SettingsPopup**
2. **Inspector → Add Component:** `SettingsPopupController`
3. **Gán references:**
   ```
   Title Text: [Kéo TitleText vào đây]
   Volume Slider: [Kéo VolumeSlider vào đây]
   Volume Value Text: [Kéo VolumeValueText vào đây]
   Exit Game Button: [Kéo ExitGameButton vào đây]
   Back To Menu Button: [Kéo BackToMenuButton vào đây]
   Close Button: [Kéo CloseButton vào đây]
   ```
4. **Settings:**
   ```
   Menu Scene Name: "GameUIPlay 1"
   ```

**Lưu ý:** Tên class là `SettingsPopupController` (không có "UI" ở đầu)

---

### Bước 11: Ẩn Popup ban đầu và đặt thứ tự

1. **Hierarchy:** Chọn **SettingsPopup**
2. **Inspector:** Bỏ tick ✓ (để inactive)
3. **Hierarchy:** Kéo **SettingsPopup** xuống **CUỐI CÙNG** trong GameUICanvas
   ```
   GameUICanvas
   ├─ EventSystem
   ├─ WellcomePanel
   ├─ LoginPanel
   ├─ MainMenuPanel
   ├─ ModSelectionPanel
   ├─ LoginRequiredPopup
   └─ SettingsPopup ← CUỐI CÙNG
   ```

---

## 🎮 Cách mở Popup

### Option 1: Từ Button với Script

```csharp
using DoAnGame.UI;

public class SomeController : MonoBehaviour
{
    private void OnSettingsButtonClicked()
    {
        var popup = SettingsPopupController.FindInScene();
        if (popup != null)
        {
            popup.Show();
            // Hoặc: popup.Open();
            // Hoặc: popup.Toggle();
        }
    }
}
```

### Option 2: Từ Button với UnityEvent

1. **Hierarchy:** Chọn button Settings
2. **Inspector → Button → OnClick():**
   - Click **+**
   - Kéo **SettingsPopup** vào ô Object
   - Function: `SettingsPopupController → Show()` hoặc `Open()` hoặc `Toggle()`

### Option 3: Dùng UISettingsOpenButton (Khuyến nghị)

**Đây là cách tốt nhất** - script có sẵn trong project:

1. **Hierarchy:** Chọn button Settings
2. **Inspector → Add Component:** `UISettingsOpenButton`
3. **Gán:**
   ```
   Popup Controller: [Kéo SettingsPopup vào đây]
   Use Toggle: ✓ (nếu muốn bật/tắt) hoặc ✗ (chỉ mở)
   Enable Debug Logs: ✓ (để debug)
   ```

**Ưu điểm:**
- Tự động gán button onClick
- Hỗ trợ Toggle mode
- Có debug logs
- Dễ setup

---

## 📝 API Methods

### `Show()` / `Open()`
Hiển thị popup (2 methods giống nhau)

```csharp
SettingsPopupController.FindInScene().Show();
// Hoặc
SettingsPopupController.FindInScene().Open();
```

### `Hide()`
Ẩn popup

```csharp
SettingsPopupController.FindInScene().Hide();
```

### `Toggle()`
Bật/tắt popup

```csharp
SettingsPopupController.FindInScene().Toggle();
```

### `FindInScene()`
Tìm popup trong scene (static method)

```csharp
var popup = SettingsPopupController.FindInScene();
if (popup != null)
{
    popup.Show();
}
```

---

## 🧪 Test

### Test Case 1: Mở/Đóng Popup

1. **Play game**
2. Click button **Settings**
3. **Popup hiển thị:**
   - ✅ Background mờ đen
   - ✅ Title: "Cài Đặt"
   - ✅ Volume slider
   - ✅ 3 buttons: Exit, Back to Menu, Close
4. Click **X** (Close) → Popup đóng

---

### Test Case 2: Volume Slider

1. **Mở popup**
2. **Kéo slider** từ trái sang phải
3. **Kiểm tra:**
   - ✅ Text hiển thị % thay đổi (0% → 100%)
   - ✅ Console log: `Volume changed: 0.XX`
4. **Đóng popup và mở lại**
5. **Kiểm tra:** Slider vẫn giữ giá trị cũ ✅

---

### Test Case 3: Exit Game

1. **Mở popup**
2. Click **"Thoát Game"**
3. **Kết quả:**
   - Unity Editor: Play mode stop
   - Build: App đóng

---

### Test Case 4: Back to Menu

1. **Mở popup** (trong game scene)
2. Click **"Về Menu"**
3. **Kết quả:**
   - ✅ Popup đóng
   - ✅ Load scene "GameUIPlay 1"
   - ✅ Hiển thị MainMenuPanel

---

## 🎨 Tùy chỉnh giao diện

### Thay đổi màu buttons

**Exit Button (Đỏ):**
```
Normal Color: RGB(200, 0, 0)
Highlighted Color: RGB(255, 50, 50)
Pressed Color: RGB(150, 0, 0)
```

**Back to Menu Button (Xanh dương):**
```
Normal Color: RGB(0, 100, 200)
Highlighted Color: RGB(50, 150, 255)
Pressed Color: RGB(0, 70, 150)
```

---

### Thêm icon cho buttons

Thay text bằng icon:
- 🚪 → Sprite icon cửa
- 🏠 → Sprite icon nhà
- ✕ → Sprite icon X

---

## 📊 Hierarchy Structure

```
GameUICanvas
└─ SettingsPopup ← MỚI
    ├─ Overlay (Image - Black, Alpha 180)
    └─ ContentPanel (Panel - White)
        ├─ TitleText (TextMeshPro - "Cài Đặt")
        ├─ VolumeLabel (TextMeshPro - "Âm Thanh:")
        ├─ VolumeSlider (Slider)
        ├─ VolumeValueText (TextMeshPro - "100%")
        ├─ CloseButton (Button - "✕")
        └─ ButtonContainer (Empty + Vertical Layout Group)
            ├─ ExitGameButton (Button - "🚪 Thoát Game")
            └─ BackToMenuButton (Button - "🏠 Về Menu")
```

---

## 🐛 Troubleshooting

### Vấn đề 1: Popup không hiển thị

**Giải pháp:** Xem `SETUP_LOGIN_REQUIRED_POPUP.md` → Troubleshooting

---

### Vấn đề 2: Volume không lưu

**Kiểm tra:**
- Slider có gọi `onValueChanged` không?
- `PlayerPrefs.Save()` đã được gọi chưa?

---

### Vấn đề 3: Back to Menu không hoạt động

**Kiểm tra:**
- Scene name đúng chưa? (phải là "GameUIPlay 1")
- Scene đã được add vào Build Settings chưa?

---

## ✅ Checklist

### Setup UI:
- [ ] Tạo **SettingsPopup** trong **GameUICanvas**
- [ ] Tạo **Overlay** (background mờ)
- [ ] Tạo **ContentPanel** (popup chính)
- [ ] Tạo **TitleText**
- [ ] Tạo **VolumeSlider** và **VolumeValueText**
- [ ] Tạo **ButtonContainer** với Vertical Layout Group
- [ ] Tạo **ExitGameButton**, **BackToMenuButton**, **CloseButton**

### Setup Script:
- [ ] Gán **SettingsPopupController** vào **SettingsPopup**
- [ ] Gán tất cả references
- [ ] Set **Menu Scene Name**
- [ ] Ẩn popup ban đầu (inactive)
- [ ] Đặt popup ở cuối cùng trong Hierarchy

### Test:
- [ ] Popup mở/đóng đúng
- [ ] Volume slider hoạt động
- [ ] Volume được lưu và load lại
- [ ] Exit game hoạt động
- [ ] Back to menu hoạt động
- [ ] Toggle mode hoạt động (nếu dùng UISettingsOpenButton)

---

✅ **Hoàn thành! Settings Popup sẵn sàng!**
