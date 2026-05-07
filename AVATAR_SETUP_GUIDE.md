# Hướng dẫn Setup Avatar System trong Unity

## Bước 1: Tạo AvatarData ScriptableObject

1. Mở Unity, vào `Assets/Resources/Avatars/`
2. Right-click → **Create → Game → AvatarData**
3. Tạo 1 asset cho mỗi nhân vật, ví dụ:

| Asset name | avatarId | avatarName | isDefault |
|---|---|---|---|
| `Avatar_0_MeoTrang` | 0 | Mèo Trắng | ✅ true |
| `Avatar_1_MeoVang` | 1 | Mèo Vàng | false |
| `Avatar_2_MeoXam` | 2 | Mèo Xám | false |

4. Gán sprites cho mỗi asset:
   - Nếu chỉ có 1 ảnh cho mỗi nhân vật → gán **cùng sprite** cho cả `thumbnail` và `fullAvatar` — hoàn toàn ổn, Unity chỉ scale khác nhau theo kích thước Image component
   - Nếu muốn tối ưu sau này → tạo thêm ảnh nhỏ riêng cho `thumbnail`
5. Gán **animatorController** nếu có (có thể để trống trước)

---

## Bước 2: Tạo AvatarItem Prefab

1. Trong Hierarchy của scene `GameUIPlay 1`, right-click → **UI → Button - TextMeshPro**
   - Unity tự tạo Button có sẵn Image + Button component
   - Đặt tên root GameObject là `AvatarItem`
   - Xóa child `Text (TMP)` mặc định đi (không cần)
2. Thêm 3 child vào `AvatarItem` (right-click AvatarItem → UI → ...):
   - **UI → Image** → đặt tên `AvatarThumbnail` (hiển thị ảnh avatar)
   - **UI → Text - TextMeshPro** → đặt tên `AvatarNameText` (tên avatar)
   - **UI → Image** → đặt tên `SelectedIndicator` (viền vàng khi đang chọn, set Inactive mặc định)
3. Gán script **AvatarItemUI** lên root `AvatarItem`
4. Trong Inspector của `AvatarItemUI`, gán:
   - `Thumbnail Image` → `AvatarThumbnail`
   - `Name Text` → `AvatarNameText`
   - `Selected Indicator` → `SelectedIndicator`
   - `Button` → Button component trên root (thường tự tìm được, nhưng gán tay cho chắc)
5. Drag `AvatarItem` từ Hierarchy vào `Assets/TaiNguyen/prefap/AvatarItem.prefab`
6. Xóa `AvatarItem` khỏi Hierarchy (đã lưu thành prefab rồi)

---

## Bước 3: Thêm AvatarManager vào scene

1. Mở scene `GameUIPlay 1`
2. Tìm GameObject `AuthServices` (root level)
3. Tạo Empty GameObject mới ở root, đặt tên `AvatarManager`
4. Gán script **AvatarManager** lên đó
5. Vì `DontDestroyOnLoad`, chỉ cần có trong scene đầu tiên

---

## Bước 4: Tạo UI trong Profile panel

Mở scene `GameUIPlay 1` → tìm `GameUICanvas/Profile`

### 4a. Tạo AvatarSection

1. Tạo Empty GameObject con của `Profile`, đặt tên `AvatarSection`
2. Thêm con `AvatarImage` (Image component) — đây là ảnh avatar lớn hiển thị ở Profile
   - Set sprite mặc định = fullAvatar của Avatar_0
3. Thêm con `ChonNhanVatBtn` (Button) với text "Chọn Nhân Vật"

### 4b. Tạo AvatarSelectionPopup

1. Tạo Empty GameObject con của `Profile`, đặt tên `AvatarSelectionPopup`
2. Thêm component **Canvas** → RenderMode: **Screen Space - Overlay**
3. Thêm component **GraphicRaycaster**
4. Thêm con `Overlay` (Image, màu đen bán trong suốt, alpha ~0.7)
5. Thêm con `ContentPanel` (Image, nền trắng/xám)
   - Thêm con `TitleText` (TMP_Text: "Chọn Nhân Vật")
   - Thêm con `ScrollView` (ScrollRect):
     - Viewport → Mask
     - Content → **GridLayoutGroup**:
       - Cell Size: 120 × 140
       - Spacing: 10 × 10
       - Constraint: Fixed Column Count = 3
   - Thêm con `CloseBtn` (Button, text "Đóng")
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

Chọn GameObject `MainMenuPanel` → Component **UIMainMenuController**:

| Field | Gán vào |
|---|---|
| `Avatar Image` | `MainMenuPanel/Avatar` (đã có sẵn trong scene) |

> Nếu không gán, code sẽ tự tìm bằng `transform.Find("Avatar")` — nhưng gán tay thì chắc hơn.

---

## Kết quả sau khi setup

- Mở Profile → thấy ảnh avatar hiện tại ở `AvatarSection/AvatarImage`
- Nhấn "Chọn Nhân Vật" → popup mở, hiển thị danh sách tất cả avatars
- Chọn 1 avatar → ảnh cập nhật ngay ở Profile và MainMenuPanel
- Guest: lưu PlayerPrefs, không gọi Firebase
- Logged-in: lưu PlayerPrefs + sync `users/{uid}/avatarId` lên Firebase
- Khi login lại: `CloudSyncService.RestoreAvatar()` tự restore từ Firebase
