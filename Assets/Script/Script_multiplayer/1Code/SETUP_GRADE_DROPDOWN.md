# 🎓 Grade Dropdown - Hướng Dẫn Setup

## 📋 Tổng Quan

**Grade Dropdown** trong **LobbyPanel** cho phép người chơi chọn **độ khó** (Lớp 1-5) khi tạo phòng multiplayer. Độ khó này sẽ ảnh hưởng đến thuật toán sinh câu hỏi trong trận đấu.

### Mục Đích
- ✅ Chọn lớp (Grade 1-5) trước khi tạo phòng (Host)
- ✅ Độ khó được đồng bộ cho tất cả người chơi trong phòng
- ✅ Thuật toán sinh câu hỏi dựa trên lớp đã chọn
- ✅ Hiển thị lớp đã chọn trong phòng chờ

---

## 🎯 Cấu Trúc UI Hiện Tại

Theo file export `Test_FireBase_multi_Export.txt`, Dropdown đã được thêm vào:

```
Canvas
└── LobbyPanel
    ├── HostButton          (Tạo phòng)
    ├── JoinInputField      (Nhập mã phòng)
    ├── JoinButton          (Tham gia phòng)
    ├── QuickButton         (Tìm phòng nhanh)
    ├── Dropdown ⭐         (Chọn lớp 1-5)
    │   ├── Label           (Hiển thị lớp đã chọn)
    │   ├── Arrow           (Mũi tên dropdown)
    │   └── Template        (Danh sách options)
    │       └── Content
    │           └── Item    (Template cho mỗi option)
    ├── JoinCodeText        (Hiển thị mã phòng)
    ├── StatusText          (Trạng thái phòng)
    └── StartButton         (Bắt đầu trận đấu)
```

---

## 🛠️ Setup Dropdown (Unity Inspector)

### Bước 1: Cấu Hình TMP_Dropdown Component

Select `LobbyPanel/Dropdown` trong Hierarchy, sau đó cấu hình trong Inspector:

#### 1.1. Options (Danh Sách Lớp)
Thêm 5 options:

| Index | Text | Image |
|---|---|---|
| 0 | Lớp 1 | None |
| 1 | Lớp 2 | None |
| 2 | Lớp 3 | None |
| 3 | Lớp 4 | None |
| 4 | Lớp 5 | None |

**Cách thêm:**
1. Inspector → TMP_Dropdown → **Options**
2. Set **Size = 5**
3. Nhập text cho từng option

#### 1.2. Value (Giá Trị Mặc Định)
```
Value: 0  (Lớp 1 - mặc định)
```

#### 1.3. Template Settings
```
Template: Template (GameObject con)
Caption Text: Label (TextMeshProUGUI)
Item Text: Item Label (trong Template/Content/Item)
```

---

### Bước 2: Styling Dropdown

#### 2.1. Label (Text Hiển Thị)
```
Font Size: 24
Color: #FFFFFF (trắng)
Alignment: Center
Text: "Lớp 1" (sẽ tự động thay đổi khi chọn)
```

#### 2.2. Dropdown Background
```
Color: #2196F3 (xanh dương)
Size: 200 x 50
```

#### 2.3. Arrow Icon
```
Color: #FFFFFF (trắng)
Size: 20 x 20
Rotation: 0 (mũi tên xuống)
```

#### 2.4. Template (Dropdown Menu)
```
Background Color: #FFFFFF (trắng)
Border: 2px, #2196F3 (xanh dương)
Max Height: 200 (hiển thị tối đa 4 items, scroll nếu nhiều hơn)
```

#### 2.5. Item (Mỗi Option)
```
Normal Color: #FFFFFF (trắng)
Highlighted Color: #E3F2FD (xanh nhạt)
Selected Color: #2196F3 (xanh dương)
Text Color: #000000 (đen)
Font Size: 20
```

---

### Bước 3: Vị Trí Dropdown

Đặt Dropdown ở vị trí dễ thấy, gần các button tạo phòng:

```
Position: Trên HostButton hoặc bên cạnh HostButton
Anchor: Center hoặc Top-Center
Size: 200 x 50
```

**Gợi ý layout:**
```
┌─────────────────────────────┐
│      LOBBY PANEL            │
│                             │
│   [Dropdown: Lớp 1 ▼]      │  ← Dropdown
│                             │
│   [Tạo Phòng (Host)]       │  ← HostButton
│                             │
│   [Nhập mã phòng...]       │  ← JoinInputField
│   [Tham Gia]              │  ← JoinButton
│                             │
│   [Tìm Phòng Nhanh]        │  ← QuickButton
│                             │
└─────────────────────────────┘
```

---

## 🎨 Visual Design

### Màu Sắc Đề Xuất

| Element | Color | Hex |
|---|---|---|
| Dropdown Background | Xanh dương | #2196F3 |
| Label Text | Trắng | #FFFFFF |
| Arrow | Trắng | #FFFFFF |
| Template Background | Trắng | #FFFFFF |
| Item Normal | Trắng | #FFFFFF |
| Item Highlighted | Xanh nhạt | #E3F2FD |
| Item Selected | Xanh dương | #2196F3 |
| Item Text | Đen | #000000 |

---

## 📊 Dữ Liệu & Logic (Sẽ Code Sau)

### Grade Mapping

| Dropdown Value | Grade | Độ Khó |
|---|---|---|
| 0 | 1 | Cộng/Trừ 0-10 |
| 1 | 2 | Cộng/Trừ 0-20, Nhân 0-5 |
| 2 | 3 | Cộng/Trừ 0-100, Nhân/Chia 0-10 |
| 3 | 4 | Cộng/Trừ 0-1000, Nhân/Chia 0-12 |
| 4 | 5 | Tất cả phép toán, số thập phân |

### Flow Hoạt Động (Sẽ Code)

```
1. User chọn lớp từ Dropdown (vd: Lớp 3)
   ↓
2. Click "Tạo Phòng" (HostButton)
   ↓
3. UIMultiplayerRoomController lấy giá trị Dropdown
   ↓
4. Tạo Relay room với metadata: { "grade": 3 }
   ↓
5. Khi player khác join → Nhận metadata grade = 3
   ↓
6. Khi bắt đầu trận đấu → DragQuizManager dùng grade = 3
   ↓
7. Thuật toán sinh câu hỏi theo độ khó Lớp 3
```

---

## 🔧 Các Thành Phần Cần Code (Bước Tiếp Theo)

### 1. UIMultiplayerRoomController.cs
```csharp
[SerializeField] private TMP_Dropdown gradeDropdown;  // ← Gán Dropdown

private int GetSelectedGrade()
{
    return gradeDropdown.value + 1;  // 0→1, 1→2, ..., 4→5
}

// Khi Host:
int selectedGrade = GetSelectedGrade();
// Lưu vào Relay metadata hoặc NetworkVariable
```

### 2. DragQuizManager.cs (Hoặc MathManager.cs)
```csharp
public void SetGrade(int grade)
{
    currentGrade = grade;  // 1-5
    // Điều chỉnh thuật toán sinh câu hỏi
}
```

### 3. Network Sync (Netcode)
```csharp
// Trong UIMultiplayerBattleController hoặc NetworkManager
private NetworkVariable<int> selectedGrade = new NetworkVariable<int>(1);

// Host set:
selectedGrade.Value = GetSelectedGrade();

// Client read:
int grade = selectedGrade.Value;
```

---

## 🧪 Test Checklist (Sau Khi Code)

### Test 1: UI Interaction
- [ ] Click Dropdown → Hiển thị 5 options (Lớp 1-5)
- [ ] Chọn Lớp 3 → Label hiển thị "Lớp 3"
- [ ] Chọn Lớp 5 → Label hiển thị "Lớp 5"

### Test 2: Host Room
- [ ] Chọn Lớp 2 → Click "Tạo Phòng"
- [ ] Phòng được tạo thành công
- [ ] Grade = 2 được lưu vào metadata

### Test 3: Join Room
- [ ] Player 2 join phòng
- [ ] Player 2 nhận được grade = 2 từ Host
- [ ] Hiển thị "Độ khó: Lớp 2" trong phòng chờ

### Test 4: Gameplay
- [ ] Bắt đầu trận đấu
- [ ] Câu hỏi sinh ra theo độ khó Lớp 2
- [ ] Cả 2 player nhận câu hỏi cùng độ khó

---

## 📁 Files Liên Quan

- **Scene**: `Assets/Scenes/Test_FireBase_multi.unity`
- **UI Structure**: `Assets/Editor/Test_FireBase_multi_Export.txt`
- **Controller**: `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerRoomController.cs` (sẽ code)
- **Math Manager**: `Assets/Script/MathManager.cs` (sẽ điều chỉnh)
- **Setup Guide**: `Assets/Script/Script_multiplayer/1Code/SETUP_GRADE_DROPDOWN.md` (file này)

---

## 🎯 Mục Tiêu Cuối Cùng

Sau khi hoàn thành:

1. ✅ Host chọn lớp trước khi tạo phòng
2. ✅ Tất cả players trong phòng chơi cùng độ khó
3. ✅ Câu hỏi sinh ra phù hợp với lớp đã chọn
4. ✅ Hiển thị rõ ràng độ khó trong UI

---

**✅ Setup UI hoàn tất!** Sẵn sàng cho bước code tiếp theo.

---

## 💡 Lưu Ý

### Dropdown vs UIManager.SelectedGrade

Dự án đã có `UIManager.SelectedGrade` (dùng cho single-player). Cần phân biệt:

| Mode | Grade Source |
|---|---|
| **Single-player** | `UIManager.SelectedGrade` (chọn từ màn hình chính) |
| **Multiplayer** | `Dropdown trong LobbyPanel` (Host chọn) |

Trong multiplayer, **KHÔNG dùng** `UIManager.SelectedGrade`, mà dùng giá trị từ Dropdown.

---

## 🔄 Tích Hợp Với Hệ Thống Hiện Có

### LevelDataConfig (ScriptableObject)

Dự án đã có `LevelDataConfig` cho từng lớp:

```
Assets/Resources/LevelData/
├── Grade1_LevelData.asset
├── Grade2_LevelData.asset
├── Grade3_LevelData.asset
├── Grade4_LevelData.asset
└── Grade5_LevelData.asset
```

**Cách dùng:**
```csharp
int selectedGrade = gradeDropdown.value + 1;  // 1-5
LevelDataConfig config = Resources.Load<LevelDataConfig>($"LevelData/Grade{selectedGrade}_LevelData");
// Dùng config.minNumber, config.maxNumber, config.allowedOperators
```

---

**🚀 Sẵn sàng code!** Hãy cho tôi biết khi bạn muốn bắt đầu bước tiếp theo.
