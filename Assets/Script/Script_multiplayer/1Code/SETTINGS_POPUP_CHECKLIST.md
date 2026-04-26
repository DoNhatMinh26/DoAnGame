# ✅ Settings Popup - Checklist Kiểm Tra

## 📋 Kiểm tra từng bước

### ✅ Bước 1: Hierarchy Structure

Kiểm tra cấu trúc trong Hierarchy:

```
GameUICanvas
└─ SettingsPopup (inactive)
    ├─ Overlay
    └─ ContentPanel
        ├─ TitleText
        ├─ VolumeLabel (optional)
        ├─ VolumeSlider
        ├─ VolumeValueText
        ├─ CloseButton
        └─ ButtonContainer
            ├─ ExitGameButton
            └─ BackToMenuButton
```

**Kiểm tra:**
- [ ] SettingsPopup tồn tại trong GameUICanvas
- [ ] SettingsPopup ở **CUỐI CÙNG** trong Hierarchy
- [ ] SettingsPopup **INACTIVE** (không có tick)
- [ ] Tất cả children **ACTIVE** (có tick)

---

### ✅ Bước 2: RectTransform Settings

#### SettingsPopup:
```
Anchor: Stretch
Left: 0, Top: 0, Right: 0, Bottom: 0
```

#### Overlay:
```
Anchor: Stretch
Left: 0, Top: 0, Right: 0, Bottom: 0
Color: Black (A: 180)
```

#### ContentPanel:
```
Anchor: Center
Width: 600
Height: 500
Pos X: 0, Pos Y: 0
```

**Kiểm tra:**
- [ ] SettingsPopup stretch full-screen
- [ ] Overlay stretch full-screen
- [ ] ContentPanel ở giữa màn hình

---

### ✅ Bước 3: Component Script

**Chọn SettingsPopup → Inspector:**

**Kiểm tra:**
- [ ] Có component **UISettingsPopupController (Script)**
- [ ] **KHÔNG** có component này trên GameUICanvas
- [ ] Tất cả references đã gán:
  - [ ] Title Text
  - [ ] Volume Slider
  - [ ] Volume Value Text
  - [ ] Exit Game Button
  - [ ] Back To Menu Button
  - [ ] Close Button
- [ ] Menu Scene Name = "GameUIPlay 1"

---

### ✅ Bước 4: Button Settings

#### ExitGameButton:
```
Text: "🚪 Thoát Game" hoặc "Thoát Game"
Color: Red (R:200, G:0, B:0)
```

#### BackToMenuButton:
```
Text: "🏠 Về Menu" hoặc "Về Menu"
Color: Blue (R:0, G:100, B:200)
```

#### CloseButton:
```
Text: "✕" hoặc "X"
Position: Top Right của ContentPanel
```

**Kiểm tra:**
- [ ] Tất cả buttons có component **Button**
- [ ] Tất cả buttons có **Text (TMP)** child
- [ ] Text hiển thị đúng

---

### ✅ Bước 5: Volume Slider

**VolumeSlider settings:**
```
Min Value: 0
Max Value: 1
Value: 1
Whole Numbers: OFF
```

**Kiểm tra:**
- [ ] Slider có component **Slider**
- [ ] Min/Max values đúng
- [ ] VolumeValueText hiển thị "100%"

---

### ✅ Bước 6: Layout Groups

**ButtonContainer:**
```
Component: Vertical Layout Group
Spacing: 15
Child Alignment: Middle Center
Child Force Expand: Width ✓, Height ✗
```

**Kiểm tra:**
- [ ] ButtonContainer có **Vertical Layout Group**
- [ ] Buttons xếp theo chiều dọc
- [ ] Có khoảng cách giữa buttons

---

## 🧪 Test Cases

### Test 1: Mở Popup thủ công

1. **Hierarchy:** Chọn **SettingsPopup**
2. **Inspector:** Tick checkbox (active popup)
3. **Game View:** Popup hiển thị?
   - [ ] ✅ Background mờ đen
   - [ ] ✅ ContentPanel ở giữa
   - [ ] ✅ Title "Cài Đặt"
   - [ ] ✅ Volume slider
   - [ ] ✅ 3 buttons

---

### Test 2: Volume Slider

1. **Active popup** (tick checkbox)
2. **Scene View:** Click vào **VolumeSlider**
3. **Inspector → Slider → Value:** Thay đổi từ 0 → 1
4. **Kiểm tra:**
   - [ ] VolumeValueText thay đổi (0% → 100%)

---

### Test 3: Buttons Click (Play Mode)

1. **Play game**
2. **Hierarchy:** Chọn **SettingsPopup** → Tick checkbox
3. **Game View:** Click từng button:
   - [ ] **Close (X):** Popup đóng
   - [ ] **Exit Game:** Play mode stop (Editor) hoặc app đóng (Build)
   - [ ] **Back to Menu:** Load scene "GameUIPlay 1"

---

### Test 4: Console Logs

**Play game → Active popup → Click buttons:**

**Expected logs:**
```
[SettingsPopup] Awake() - GameObject: SettingsPopup
[SettingsPopup] Show()
[SettingsPopup] Volume changed: 1.00
[SettingsPopup] Close clicked
[SettingsPopup] Hide()
```

**Kiểm tra:**
- [ ] Awake() được gọi
- [ ] Show() hoạt động
- [ ] Volume slider log ra giá trị
- [ ] Buttons log ra khi click

---

## 🐛 Common Issues

### ❌ Issue 1: Popup không hiển thị

**Nguyên nhân:**
- SettingsPopup không ở cuối cùng trong Hierarchy
- ContentPanel bị stretch thay vì center

**Fix:**
- Kéo SettingsPopup xuống cuối cùng
- Set ContentPanel anchor = Center

---

### ❌ Issue 2: Buttons không click được

**Nguyên nhân:**
- Overlay che mất buttons
- Không có EventSystem

**Fix:**
- Bỏ tick **Raycast Target** ở Overlay
- Thêm EventSystem vào scene

---

### ❌ Issue 3: Volume không lưu

**Nguyên nhân:**
- Slider không gọi onValueChanged
- PlayerPrefs.Save() không được gọi

**Fix:**
- Kiểm tra Slider component
- Code đã có PlayerPrefs.Save()

---

### ❌ Issue 4: Script không tìm thấy references

**Nguyên nhân:**
- Script gán vào GameUICanvas thay vì SettingsPopup
- References chưa được kéo vào

**Fix:**
- Xóa script trên GameUICanvas
- Gán script vào SettingsPopup
- Kéo tất cả UI elements vào Inspector

---

## 🎯 Final Checklist

**Trước khi test:**
- [ ] Hierarchy structure đúng
- [ ] SettingsPopup ở cuối cùng
- [ ] Script gán vào SettingsPopup (KHÔNG phải GameUICanvas)
- [ ] Tất cả references đã gán
- [ ] Menu Scene Name = "GameUIPlay 1"
- [ ] SettingsPopup inactive ban đầu
- [ ] Tất cả children active

**Test:**
- [ ] Popup hiển thị đúng (active thủ công)
- [ ] Volume slider hoạt động
- [ ] Close button đóng popup
- [ ] Exit button thoát game
- [ ] Back to Menu load scene
- [ ] Console logs đúng

---

## 📞 Nếu vẫn có vấn đề:

**Gửi thông tin:**
1. Screenshot Hierarchy (expand SettingsPopup)
2. Screenshot Inspector của SettingsPopup
3. Console logs khi click buttons
4. Mô tả vấn đề cụ thể

---

✅ **Nếu tất cả đều pass → Settings Popup hoạt động hoàn hảo!**
