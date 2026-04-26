# 🔐 Hướng dẫn Setup Popup Yêu Cầu Đăng Nhập

## 📋 Tổng quan

Popup này hiển thị khi người chơi khách cố vào Multiplayer, với 2 button:
- **"Đăng nhập"** → Chuyển sang LoginPanel
- **"Hủy"** → Đóng popup, quay lại ModSelectionPanel

---

## 🎨 Tạo Popup UI trong Unity

### Bước 1: Tạo Popup GameObject

1. **Hierarchy:** Right-click **GameUICanvas** → **UI → Panel**
2. **Rename:** `LoginRequiredPopup`
3. **RectTransform:**
   ```
   Anchor Presets: Stretch (Alt+Shift+Click)
   Left: 0, Top: 0, Right: 0, Bottom: 0
   ```

---

### Bước 2: Tạo Background Overlay (Làm mờ phía sau)

1. **Hierarchy:** Right-click **LoginRequiredPopup** → **UI → Image**
2. **Rename:** `Overlay`
3. **RectTransform:**
   ```
   Anchor Presets: Stretch
   Left: 0, Top: 0, Right: 0, Bottom: 0
   ```
4. **Image Component:**
   ```
   Color: Black (R:0, G:0, B:0, A:180) ← Làm mờ 70%
   ```

---

### Bước 3: Tạo Popup Content Panel

1. **Hierarchy:** Right-click **LoginRequiredPopup** → **UI → Panel**
2. **Rename:** `ContentPanel`
3. **RectTransform:**
   ```
   Anchor Presets: Center
   Width: 600
   Height: 400
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
   Text: "Yêu Cầu Đăng Nhập"
   Font Size: 40
   Alignment: Center
   Color: Red hoặc màu nổi bật
   Font Style: Bold
   ```

---

### Bước 5: Tạo Message Text

1. **Hierarchy:** Right-click **ContentPanel** → **UI → Text - TextMeshPro**
2. **Rename:** `MessageText`
3. **RectTransform:**
   ```
   Anchor Presets: Center
   Width: 500
   Height: 150
   Pos X: 0, Pos Y: 0
   ```
4. **TextMeshPro:**
   ```
   Text: "Chế độ Multiplayer yêu cầu tài khoản.\nVui lòng đăng nhập hoặc đăng ký!"
   Font Size: 28
   Alignment: Center
   Color: Black hoặc màu chữ chính
   ```

---

### Bước 6: Tạo Button Container

1. **Hierarchy:** Right-click **ContentPanel** → **Create Empty**
2. **Rename:** `ButtonContainer`
3. **Add Component:** `RectTransform` (nếu chưa có)
4. **RectTransform:**
   ```
   Anchor Presets: Bottom Center
   Width: 500
   Height: 100
   Pos X: 0, Pos Y: 60
   ```
5. **Add Component:** `Horizontal Layout Group`
   ```
   Spacing: 20
   Child Alignment: Middle Center
   Child Force Expand: Width ✓, Height ✓
   ```

---

### Bước 7: Tạo Login Button

1. **Hierarchy:** Right-click **ButtonContainer** → **UI → Button - TextMeshPro**
2. **Rename:** `LoginButton`
3. **RectTransform:**
   ```
   Width: 220
   Height: 80
   ```
4. **Button Component:**
   ```
   Normal Color: Green (R:0, G:200, B:0)
   Highlighted Color: Light Green
   Pressed Color: Dark Green
   ```
5. **Text (child):**
   ```
   Text: "Đăng Nhập"
   Font Size: 32
   Alignment: Center
   Color: White
   Font Style: Bold
   ```

---

### Bước 8: Tạo Cancel Button

1. **Hierarchy:** Right-click **ButtonContainer** → **UI → Button - TextMeshPro**
2. **Rename:** `CancelButton`
3. **RectTransform:**
   ```
   Width: 220
   Height: 80
   ```
4. **Button Component:**
   ```
   Normal Color: Gray (R:150, G:150, B:150)
   Highlighted Color: Light Gray
   Pressed Color: Dark Gray
   ```
5. **Text (child):**
   ```
   Text: "Hủy"
   Font Size: 32
   Alignment: Center
   Color: White
   Font Style: Bold
   ```

---

## 🔧 Setup Script

### Bước 9: Gán UILoginRequiredPopupController

1. **Hierarchy:** Chọn **LoginRequiredPopup**
2. **Inspector → Add Component:** `UILoginRequiredPopupController`
3. **Gán references:**
   ```
   Title Text: [Kéo TitleText vào đây]
   Message Text: [Kéo MessageText vào đây]
   Login Button: [Kéo LoginButton vào đây]
   Cancel Button: [Kéo CancelButton vào đây]
   ```
4. **Default Text (Optional):**
   ```
   Default Title: "Yêu Cầu Đăng Nhập"
   Default Message: "Chế độ Multiplayer yêu cầu tài khoản.\nVui lòng đăng nhập hoặc đăng ký!"
   ```

---

### Bước 10: Gán Popup vào ModSelectionPanel

1. **Hierarchy:** Chọn **ModSelectionPanel**
2. **Inspector → UIModSelectionPanelController:**
   ```
   Login Required Popup: [Kéo LoginRequiredPopup vào đây]
   ```

---

### Bước 11: Ẩn Popup ban đầu và đặt thứ tự hiển thị

1. **Hierarchy:** Chọn **LoginRequiredPopup**
2. **Inspector:** Bỏ tick ✓ ở checkbox bên cạnh tên GameObject (để inactive)
3. **Hierarchy:** Kéo **LoginRequiredPopup** xuống **CUỐI CÙNG** trong GameUICanvas
   ```
   GameUICanvas
   ├─ EventSystem
   ├─ WellcomePanel
   ├─ LoginPanel
   ├─ MainMenuPanel
   ├─ ModSelectionPanel
   └─ LoginRequiredPopup ← PHẢI Ở CUỐI CÙNG
   ```

**LƯU Ý QUAN TRỌNG:** Popup phải ở cuối cùng trong Hierarchy để hiển thị trên tất cả panels khác!

---

## 🧪 Test

### Test Case 1: Popup hiển thị đúng

1. **Play game**
2. Click **"Chơi Nhanh"** → Nhập tên → **"Bắt Đầu"**
3. Click **"Play"** → Click **"Multiplayer"**
4. **Popup hiển thị:**
   - ✅ Background mờ đen
   - ✅ Title: "Yêu Cầu Đăng Nhập"
   - ✅ Message: "Chế độ Multiplayer yêu cầu tài khoản..."
   - ✅ 2 buttons: "Đăng Nhập" (xanh) và "Hủy" (xám)

---

### Test Case 2: Click "Đăng Nhập"

1. **Popup hiển thị**
2. Click **"Đăng Nhập"**
3. **Kết quả:**
   - ✅ Popup đóng
   - ✅ Chuyển sang **LoginPanel**
   - ✅ Console log: `[ModSelection] User chose to login`

---

### Test Case 3: Click "Hủy"

1. **Popup hiển thị**
2. Click **"Hủy"**
3. **Kết quả:**
   - ✅ Popup đóng
   - ✅ Vẫn ở **ModSelectionPanel**
   - ✅ Console log: `[ModSelection] User cancelled login`

---

## 🎨 Tùy chỉnh giao diện

### Thay đổi màu sắc

**LoginButton (Xanh lá):**
```
Normal Color: RGB(0, 200, 0)
Highlighted Color: RGB(50, 255, 50)
Pressed Color: RGB(0, 150, 0)
```

**CancelButton (Xám):**
```
Normal Color: RGB(150, 150, 150)
Highlighted Color: RGB(200, 200, 200)
Pressed Color: RGB(100, 100, 100)
```

---

### Thay đổi kích thước

**Popup nhỏ hơn:**
```
ContentPanel:
  Width: 500
  Height: 350
```

**Popup lớn hơn:**
```
ContentPanel:
  Width: 700
  Height: 450
```

---

### Thêm icon

1. **Hierarchy:** Right-click **ContentPanel** → **UI → Image**
2. **Rename:** `IconImage`
3. **RectTransform:**
   ```
   Anchor Presets: Top Center
   Width: 80
   Height: 80
   Pos X: 0, Pos Y: -120
   ```
4. **Image Component:**
   ```
   Source Image: [Kéo icon lock/warning vào đây]
   ```

---

## 🐛 Troubleshooting

### Vấn đề 1: Popup hiển thị nhưng bị che bởi panel khác ⚠️ PHỔ BIẾN

**Triệu chứng:** 
- Console có log: `[LoginRequiredPopup] Showing popup: Yêu Cầu Đăng Nhập`
- Nhưng không thấy popup trên màn hình

**Nguyên nhân:** Popup không ở cuối cùng trong Hierarchy, bị ModSelectionPanel che mất

**Giải pháp:**
1. **Hierarchy:** Kéo **LoginRequiredPopup** xuống **CUỐI CÙNG** trong GameUICanvas
   ```
   GameUICanvas
   ├─ EventSystem
   ├─ WellcomePanel
   ├─ LoginPanel
   ├─ MainMenuPanel
   ├─ ModSelectionPanel
   └─ LoginRequiredPopup ← PHẢI Ở CUỐI CÙNG
   ```
2. Script đã tự động gọi `SetAsLastSibling()` khi hiển thị
3. Nhưng vị trí ban đầu trong Hierarchy vẫn quan trọng

---

### Vấn đề 2: Popup không hiển thị (không tìm thấy)

**Nguyên nhân:** Popup bị inactive hoặc không tìm thấy

**Giải pháp:**
1. Kiểm tra **LoginRequiredPopup** có trong **GameUICanvas** không
2. Kiểm tra **UIModSelectionPanelController** đã gán popup chưa
3. Xem Console log: `[ModSelection] UILoginRequiredPopupController not found in scene!`

---

### Vấn đề 3: Buttons không click được

**Nguyên nhân:** Overlay che mất buttons

**Giải pháp:**
1. **Hierarchy:** Đảm bảo thứ tự:
   ```
   LoginRequiredPopup
   ├─ Overlay (phía dưới)
   └─ ContentPanel (phía trên)
       ├─ TitleText
       ├─ MessageText
       └─ ButtonContainer
           ├─ LoginButton
           └─ CancelButton
   ```
2. **Overlay Image:** Bỏ tick **Raycast Target** (để click xuyên qua)

---

### Vấn đề 3: Popup bị lệch vị trí

**Nguyên nhân:** RectTransform không đúng

**Giải pháp:**
1. **LoginRequiredPopup:**
   ```
   Anchor: Stretch
   Left: 0, Top: 0, Right: 0, Bottom: 0
   ```
2. **ContentPanel:**
   ```
   Anchor: Center
   Pos X: 0, Pos Y: 0
   ```

---

## 📊 Hierarchy Structure

```
GameUICanvas
├─ ModSelectionPanel
├─ LoginPanel
├─ MainMenuPanel
└─ LoginRequiredPopup ← MỚI
    ├─ Overlay (Image - Black, Alpha 180)
    └─ ContentPanel (Panel - White)
        ├─ TitleText (TextMeshPro)
        ├─ MessageText (TextMeshPro)
        └─ ButtonContainer (Empty + Horizontal Layout Group)
            ├─ LoginButton (Button - Green)
            │   └─ Text (TextMeshPro - "Đăng Nhập")
            └─ CancelButton (Button - Gray)
                └─ Text (TextMeshPro - "Hủy")
```

---

## ✅ Checklist

### Setup UI:
- [ ] Tạo **LoginRequiredPopup** trong **GameUICanvas**
- [ ] Tạo **Overlay** (background mờ)
- [ ] Tạo **ContentPanel** (popup chính)
- [ ] Tạo **TitleText** và **MessageText**
- [ ] Tạo **ButtonContainer** với Horizontal Layout Group
- [ ] Tạo **LoginButton** (xanh) và **CancelButton** (xám)

### Setup Script:
- [ ] Gán **UILoginRequiredPopupController** vào **LoginRequiredPopup**
- [ ] Gán references (TitleText, MessageText, LoginButton, CancelButton)
- [ ] Gán **LoginRequiredPopup** vào **UIModSelectionPanelController**
- [ ] Ẩn popup ban đầu (inactive)

### Test:
- [ ] Popup hiển thị khi guest click Multiplayer
- [ ] Click "Đăng Nhập" → Chuyển sang LoginPanel
- [ ] Click "Hủy" → Đóng popup, vẫn ở ModSelectionPanel
- [ ] Popup đóng đúng cách (không còn hiển thị)

---

✅ **Hoàn thành! Popup yêu cầu đăng nhập đã sẵn sàng!**
