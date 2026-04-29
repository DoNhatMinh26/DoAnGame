# 🎨 Thêm Độ Khó Vào LobbyRowTemplate

## 📋 Tổng Quan

Hướng dẫn thêm **Grade Text** (Độ khó: Lớp X) vào `LobbyRowTemplate` prefab để hiển thị trong danh sách phòng.

---

## 🎯 Có 2 Cách

### Cách 1: Gộp Grade Vào Status Text (Đã Làm) ✅
- **Ưu điểm**: Không cần sửa UI, không cần sửa code widget
- **Nhược điểm**: Status text dài hơn
- **Hiện tại**: `"Đang chờ người chơi | Lớp 3"`

### Cách 2: Thêm Grade Text Riêng (Tùy Chọn) ⭐
- **Ưu điểm**: Rõ ràng hơn, dễ styling
- **Nhược điểm**: Cần sửa UI + code
- **Kết quả**: Text riêng hiển thị "Lớp 3"

---

## 🛠️ Cách 2: Thêm Grade Text Riêng (Nếu Muốn)

### Bước 1: Mở Prefab LobbyRowTemplate

1. Project → `Assets/TaiNguyen/prefab/`
2. Double-click `LobbyRowTemplate` để mở Prefab Mode
3. Hoặc: Hierarchy → Right-click `LobbyRowTemplate` → Open Prefab

---

### Bước 2: Thêm Grade Text (TextMeshPro)

#### 2.1. Tạo Text Object
```
LobbyRowTemplate (Root)
├── TenPhong (Lobby Name)
├── Chuphong (Host Name)
├── SoNguoi (Player Count)
├── TrangThai (Status)
├── DoKho ⭐ (NEW - Grade Text)
└── Join (Button)
```

#### 2.2. Cách Tạo
1. Right-click `LobbyRowTemplate` root
2. UI → Text - TextMeshPro
3. Rename: `DoKho`

#### 2.3. Setup RectTransform
```
Anchor: Middle-Right (hoặc Bottom-Center)
Pos X: -120 (bên trái button Join)
Pos Y: -10
Width: 80
Height: 30
```

#### 2.4. Setup TextMeshPro Component
```
Text: "Lớp 1" (placeholder)
Font: LiberationSans SDF
Font Size: 18
Color: #FFC107 (vàng) hoặc #2196F3 (xanh)
Alignment: Center
Overflow: Ellipsis
Wrapping: Disabled
```

#### 2.5. Vị Trí Gợi Ý

**Layout A: Bên cạnh Status**
```
┌─────────────────────────────────────┐
│ Phòng Math        Chủ phòng: Player1│
│ Người chơi: 1/2   Đang chờ | Lớp 3  │
│                          [Join]     │
└─────────────────────────────────────┘
```

**Layout B: Dòng riêng**
```
┌─────────────────────────────────────┐
│ Phòng Math        Chủ phòng: Player1│
│ Người chơi: 1/2   Đang chờ người chơi│
│ Độ khó: Lớp 3              [Join]  │
└─────────────────────────────────────┘
```

**Layout C: Góc trên phải (Badge style)**
```
┌─────────────────────────────────────┐
│ Phòng Math              [Lớp 3]    │
│ Chủ phòng: Player1                  │
│ Người chơi: 1/2   Đang chờ [Join]  │
└─────────────────────────────────────┘
```

---

### Bước 3: Gán Vào Widget Script

1. Select `LobbyRowTemplate` root
2. Inspector → `LobbyBrowserEntryWidget` component
3. Sẽ thấy section mới: **Grade Text** (sau khi sửa code)
4. Drag `DoKho` text vào field **Grade Text**
5. Save Prefab (Ctrl+S)

---

### Bước 4: Sửa Code Widget

#### File: `LobbyBrowserEntryWidget.cs`

**Thêm SerializeField:**
```csharp
[SerializeField] private TMP_Text lobbyNameText;
[SerializeField] private TMP_Text hostText;
[SerializeField] private TMP_Text playerCountText;
[SerializeField] private TMP_Text statusText;
[SerializeField] private TMP_Text gradeText;  // ← THÊM
[SerializeField] private Button joinButton;
```

**Sửa Awake():**
```csharp
private void Awake()
{
    rectTransform = transform as RectTransform;
    layoutElement = GetComponent<LayoutElement>();
    if (layoutElement == null)
    {
        layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    ApplyCompactTextStyle(lobbyNameText);
    ApplyCompactTextStyle(hostText);
    ApplyCompactTextStyle(playerCountText);
    ApplyCompactTextStyle(statusText);
    ApplyCompactTextStyle(gradeText);  // ← THÊM
}
```

**Sửa Bind() - Thêm tham số gradeLabel:**
```csharp
public void Bind(string lobbyName, string hostName, string countText, string lobbyStatus, string gradeLabel, Action onJoinClicked)
{
    if (lobbyNameText != null)
    {
        lobbyNameText.SetText(lobbyName);
    }

    if (hostText != null)
    {
        hostText.SetText(hostName);
    }

    if (playerCountText != null)
    {
        playerCountText.SetText(countText);
    }

    if (statusText != null)
    {
        statusText.SetText(lobbyStatus);
    }

    if (gradeText != null)  // ← THÊM
    {
        gradeText.SetText(gradeLabel);
    }

    joinAction = onJoinClicked;
    if (joinButton != null)
    {
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(HandleJoinClicked);
        joinButton.interactable = onJoinClicked != null;
    }

    EnsureValidRowHeight();
}
```

---

### Bước 5: Sửa UILobbyBrowserController

**Tìm đoạn code:**
```csharp
var widget = Instantiate(entryPrefab, contentRoot);
widget.Bind(
    BuildLobbyName(lobby),
    BuildHostLabel(lobby),
    BuildPlayerCount(lobby),
    BuildLobbyStatus(lobby),
    canJoin ? (() => _ = JoinLobbyAsync(lobby)) : null);
spawnedEntries.Add(widget);
```

**Thay bằng:**
```csharp
var widget = Instantiate(entryPrefab, contentRoot);
widget.Bind(
    BuildLobbyName(lobby),
    BuildHostLabel(lobby),
    BuildPlayerCount(lobby),
    BuildLobbyStatus(lobby),
    BuildGradeLabel(lobby),  // ← THÊM tham số thứ 5
    canJoin ? (() => _ = JoinLobbyAsync(lobby)) : null);
spawnedEntries.Add(widget);
```

**Và sửa lại BuildLobbyStatus() (bỏ grade ra):**
```csharp
private string BuildLobbyStatus(Lobby lobby)
{
    if (lobby == null || lobby.Data == null)
        return "Đang chờ vào phòng";

    if (lobby.Data.TryGetValue(StartedKey, out var startedData) && startedData != null && startedData.Value == "1")
        return "Đang trong trận";

    if (!HasAvailableSlots(lobby))
        return "Phòng đã đầy";

    return "Đang chờ người chơi";
}
```

---

## 🎨 Styling Gợi Ý

### Style 1: Badge (Nổi bật)
```
Background: #FFC107 (vàng)
Text Color: #000000 (đen)
Border Radius: 12px
Padding: 4px 8px
Font Size: 16
Font Style: Bold
```

### Style 2: Subtle (Nhẹ nhàng)
```
Background: Transparent
Text Color: #9E9E9E (xám)
Font Size: 16
Font Style: Regular
```

### Style 3: Colorful (Theo lớp)
```
Lớp 1: #4CAF50 (xanh lá)
Lớp 2: #2196F3 (xanh dương)
Lớp 3: #FFC107 (vàng)
Lớp 4: #FF9800 (cam)
Lớp 5: #F44336 (đỏ)
```

---

## 🧪 Test

### Test 1: Hiển Thị Grade
1. Tạo 3 phòng với Lớp 1, 3, 5
2. Mở LobbyBrowserPanel
3. Kiểm tra mỗi row hiển thị đúng grade

### Test 2: Layout
1. Kiểm tra grade text không bị che bởi button
2. Kiểm tra text không bị cắt (ellipsis)
3. Kiểm tra alignment đẹp

### Test 3: Backward Compatibility
1. Join phòng cũ (không có GradeKey)
2. Kiểm tra hiển thị "Lớp 1" (mặc định)
3. Không crash

---

## 📊 So Sánh 2 Cách

| Tiêu Chí | Cách 1 (Gộp vào Status) | Cách 2 (Text riêng) |
|---|---|---|
| **Dễ implement** | ✅ Rất dễ (đã xong) | ⚠️ Cần sửa UI + code |
| **Rõ ràng** | ⚠️ Status text dài | ✅ Rõ ràng hơn |
| **Styling** | ⚠️ Khó tùy chỉnh | ✅ Dễ styling |
| **Backward compatible** | ✅ Hoàn toàn | ✅ Hoàn toàn |
| **Khuyến nghị** | ✅ Dùng nếu đủ | ⭐ Dùng nếu muốn đẹp hơn |

---

## 💡 Khuyến Nghị

### Nếu Status Text Đủ Rõ → Dùng Cách 1 (Hiện Tại)
```
"Đang chờ người chơi | Lớp 3"
```
- ✅ Không cần sửa gì thêm
- ✅ Đã hoạt động
- ✅ Đủ rõ ràng

### Nếu Muốn UI Đẹp Hơn → Dùng Cách 2
```
Status: "Đang chờ người chơi"
Grade: "Lớp 3" (text riêng, màu vàng, nổi bật)
```
- ⭐ Rõ ràng hơn
- ⭐ Dễ styling
- ⚠️ Cần sửa thêm code

---

## 📁 Files Cần Sửa (Cách 2)

| File | Thay Đổi |
|---|---|
| `LobbyRowTemplate.prefab` | Thêm DoKho text |
| `LobbyBrowserEntryWidget.cs` | Thêm gradeText field + Bind() |
| `UILobbyBrowserController.cs` | Thêm tham số gradeLabel vào Bind() |

---

## ✅ Kết Luận

- **Cách 1 (Hiện tại)**: Đã hoạt động, không cần làm gì thêm
- **Cách 2 (Tùy chọn)**: Làm theo hướng dẫn trên nếu muốn UI đẹp hơn

**🎯 Khuyến nghị: Dùng Cách 1 trước, nếu thấy cần thiết mới làm Cách 2.**
