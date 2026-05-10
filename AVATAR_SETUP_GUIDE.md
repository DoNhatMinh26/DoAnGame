# Hướng dẫn Setup Avatar System trong Unity

## Bước 1: Tạo AvatarData ScriptableObject

1. Mở Unity, vào `Assets/Resources/Avatars/`
2. Right-click → **Create → Game → AvatarData**
3. Tạo 4 assets:

| Asset name | avatarId | avatarName | isDefault |
|---|---|---|---|
| `Avatar_0` | 0 | Mèo 1 | ✅ true |
| `Avatar_1` | 1 | Mèo 2 | false |
| `Avatar_2` | 2 | Mèo 3 | false |
| `Avatar_3` | 3 | Mèo 4 | false |

4. Gán sprites cho mỗi asset từ `Assets/TaiNguyen/Character/Avatar/`:
   - `thumbnail` và `fullAvatar` → dùng cùng 1 ảnh cũng được
   - `AVA_M1.png` → Avatar_0, `AVA_M2.png` → Avatar_1, v.v.

> **Không có field `animatorController`.**
> Lý do: 3 PSB có 3 bộ xương khác nhau → mỗi PSB phải có controller riêng, và controller đó đã gán sẵn trong PSB khi import — không cần lưu hay swap từ AvatarData.

---

## Bước 2: Tạo AvatarItem Prefab

1. Trong Hierarchy của scene `GameUIPlay 1`, right-click → **UI → Button - TextMeshPro**
   - Đặt tên root GameObject là `AvatarItem`
   - Xóa child `Text (TMP)` mặc định
2. Thêm 3 child vào `AvatarItem`:
   - **UI → Image** → đặt tên `AvatarThumbnail`
   - **UI → Text - TextMeshPro** → đặt tên `AvatarNameText`
   - **UI → Image** → đặt tên `SelectedIndicator` (set Inactive mặc định)
3. Gán script **AvatarItemUI** lên root `AvatarItem`
4. Trong Inspector của `AvatarItemUI`, gán:
   - `Thumbnail Image` → `AvatarThumbnail`
   - `Name Text` → `AvatarNameText`
   - `Selected Indicator` → `SelectedIndicator`
   - `Button` → Button component trên root
5. Drag `AvatarItem` vào `Assets/TaiNguyen/prefap/AvatarItem.prefab`
6. Xóa khỏi Hierarchy

---

## Bước 3: Thêm AvatarManager vào scene

1. Mở scene `GameUIPlay 1`
2. Tạo Empty GameObject ở root, đặt tên `AvatarManager`
3. Gán script **AvatarManager** lên đó
4. `DontDestroyOnLoad` — chỉ cần có trong scene đầu tiên

---

## Bước 4: Tạo UI trong Profile panel

Mở scene `GameUIPlay 1` → tìm `GameUICanvas/Profile`

### 4a. Tạo AvatarSection

1. Tạo Empty GameObject con của `Profile`, đặt tên `AvatarSection`
2. Thêm con `AvatarImage` (Image component) — ảnh avatar lớn ở Profile
3. Thêm con `ChonNhanVatBtn` (Button) với text "Chọn Nhân Vật"

### 4b. Tạo AvatarSelectionPopup

1. Tạo Empty GameObject con của `Profile`, đặt tên `AvatarSelectionPopup`
2. Thêm component **Canvas** → RenderMode: **Screen Space - Overlay**
3. Thêm component **GraphicRaycaster**
4. Thêm con `Overlay` (Image, màu đen bán trong suốt, alpha ~0.7)
5. Thêm con `ContentPanel` (Image, nền trắng/xám):
   - Con `TitleText` (TMP_Text: "Chọn Nhân Vật")
   - Con `ScrollView` (ScrollRect) → Content có **GridLayoutGroup**:
     - Cell Size: 120 × 140, Spacing: 10 × 10, Fixed Column Count = 3
   - Con `CloseBtn` (Button, text "Đóng")
6. Set `AvatarSelectionPopup` **Inactive** mặc định

---

## Bước 5: Gán references trong Inspector của ProfileUI

Chọn GameObject `Profile` → Component **ProfileUI**:

| Field | Gán vào |
|---|---|
| `Avatar Display Image` | `Profile/AvatarSection/AvatarImage` |
| `Chon Nhan Vat Btn` | `Profile/AvatarSection/ChonNhanVatBtn` |
| `Avatar Selection Popup` | `Profile/AvatarSelectionPopup` |
| `Avatar Item Container` | `Profile/AvatarSelectionPopup/ContentPanel/ScrollView/Viewport/Content` |
| `Avatar Item Prefab` | `Assets/TaiNguyen/prefap/AvatarItem.prefab` |
| `Close Avatar Popup Btn` | `Profile/AvatarSelectionPopup/ContentPanel/CloseBtn` |

---

## Bước 6: Gán Avatar Image trong MainMenuPanel

Chọn `MainMenuPanel` → Component **UIMainMenuController**:

| Field | Gán vào |
|---|---|
| `Avatar Image` | `MainMenuPanel/Avatar` |

---

## Kết quả sau khi setup

- Mở Profile → thấy ảnh avatar hiện tại
- Nhấn "Chọn Nhân Vật" → popup mở, hiển thị 4 avatar
- Chọn avatar → `AvatarManager.SelectAvatar(id)` → skin tương ứng bật trên cả 3 PSB
- Guest: lưu PlayerPrefs
- Logged-in: lưu PlayerPrefs + sync `users/{uid}/avatarId` lên Firebase
- Khi login lại: `CloudSyncService.RestoreAvatar()` tự restore từ Firebase
