# 🎓 Implement Grade Selection - Hướng Dẫn & Code

## 📋 Tổng Quan

Tài liệu này hướng dẫn **thêm tính năng chọn độ khó (Grade 1-5)** vào multiplayer room, bao gồm:

1. ✅ Host chọn grade từ Dropdown trong LobbyPanel
2. ✅ **Mặc định Grade = 1** nếu không chọn
3. ✅ Lưu grade vào Lobby metadata
4. ✅ Hiển thị grade trong LobbyBrowserPanel (danh sách phòng)
5. ✅ Đồng bộ grade cho tất cả players
6. ✅ **Giữ nguyên logic hiện tại**, chỉ thêm tính năng mới

---

## 🎯 Yêu Cầu

### 1. LobbyPanel (Tạo Phòng)
- Dropdown chọn Lớp 1-5
- Nếu không chọn → Mặc định Lớp 1
- Lưu grade vào Lobby Data khi tạo phòng

### 2. LobbyBrowserPanel (Danh Sách Phòng)
- Hiển thị "Độ khó: Lớp X" cho mỗi phòng
- Format: `"Độ khó: Lớp 3"` hoặc `"Lớp 3"`

### 3. Network Sync
- Grade được lưu trong Lobby metadata
- Tất cả players đọc grade từ Lobby
- Không cần NetworkVariable (dùng Lobby Data)

---

## 📝 Các Thay Đổi Cần Thực Hiện

### File 1: UIMultiplayerRoomController.cs

#### Thêm Constants
```csharp
private const string GradeKey = "Grade";  // ← Thêm key cho grade
```

#### Thêm SerializeField
```csharp
[Header("Grade Selection")]
[SerializeField] private TMP_Dropdown gradeDropdown;  // ← Gán Dropdown trong Inspector
```

#### Thêm Helper Method
```csharp
/// <summary>
/// Lấy grade đã chọn từ Dropdown (1-5)
/// Mặc định = 1 nếu dropdown null hoặc không chọn
/// </summary>
private int GetSelectedGrade()
{
    if (gradeDropdown == null)
    {
        Debug.LogWarning("[UIRoom] Grade dropdown chưa gán, dùng mặc định Lớp 1");
        return 1;
    }

    // Dropdown value: 0→Lớp 1, 1→Lớp 2, ..., 4→Lớp 5
    int grade = gradeDropdown.value + 1;
    
    // Clamp để đảm bảo trong khoảng 1-5
    grade = Mathf.Clamp(grade, 1, 5);
    
    Debug.Log($"[UIRoom] Grade đã chọn: Lớp {grade}");
    return grade;
}
```

#### Sửa HandleCreateRoom() - Thêm Grade Vào Lobby Data
Tìm đoạn code:
```csharp
var options = new CreateLobbyOptions
{
    IsPrivate = false,
    Data = new Dictionary<string, DataObject>
    {
        { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode, DataObject.IndexOptions.S1) },
        { StartedKey, new DataObject(DataObject.VisibilityOptions.Public, "0", DataObject.IndexOptions.S2) },
        { ModeKey, new DataObject(DataObject.VisibilityOptions.Public, "MathDuel") },
        { HostNameKey, new DataObject(DataObject.VisibilityOptions.Public, GetCurrentPlayerDisplayName()) }
    },
    // ...
};
```

**Thay bằng:**
```csharp
int selectedGrade = GetSelectedGrade();  // ← Lấy grade từ dropdown

var options = new CreateLobbyOptions
{
    IsPrivate = false,
    Data = new Dictionary<string, DataObject>
    {
        { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode, DataObject.IndexOptions.S1) },
        { StartedKey, new DataObject(DataObject.VisibilityOptions.Public, "0", DataObject.IndexOptions.S2) },
        { ModeKey, new DataObject(DataObject.VisibilityOptions.Public, "MathDuel") },
        { HostNameKey, new DataObject(DataObject.VisibilityOptions.Public, GetCurrentPlayerDisplayName()) },
        { GradeKey, new DataObject(DataObject.VisibilityOptions.Public, selectedGrade.ToString()) }  // ← THÊM GRADE
    },
    // ...
};
```

#### Thêm Method Đọc Grade Từ Lobby
```csharp
/// <summary>
/// Đọc grade từ Lobby metadata
/// Trả về 1 nếu không tìm thấy (mặc định)
/// </summary>
private int GetLobbyGrade(Lobby lobby)
{
    if (lobby == null || lobby.Data == null)
    {
        return 1;  // Mặc định Lớp 1
    }

    if (!lobby.Data.TryGetValue(GradeKey, out var gradeData) || gradeData == null)
    {
        return 1;  // Mặc định Lớp 1
    }

    if (int.TryParse(gradeData.Value, out int grade))
    {
        return Mathf.Clamp(grade, 1, 5);  // Đảm bảo 1-5
    }

    return 1;  // Fallback
}
```

#### Hiển Thị Grade Trong Status (Optional)
Trong `RefreshRoomRoster()` hoặc `SetStatus()`, có thể thêm:
```csharp
if (currentLobby != null)
{
    int grade = GetLobbyGrade(currentLobby);
    SetStatus($"Phòng đã tạo. Độ khó: Lớp {grade}. Chờ người chơi thứ 2...");
}
```

---

### File 2: UILobbyBrowserController.cs

#### Thêm Constants
```csharp
private const string GradeKey = "Grade";  // ← Thêm key cho grade
```

#### Thêm Method BuildGradeLabel
```csharp
/// <summary>
/// Tạo label hiển thị độ khó (grade) của phòng
/// </summary>
private string BuildGradeLabel(Lobby lobby)
{
    if (lobby == null || lobby.Data == null)
    {
        return "Độ khó: Lớp 1";  // Mặc định
    }

    if (!lobby.Data.TryGetValue(GradeKey, out var gradeData) || gradeData == null)
    {
        return "Độ khó: Lớp 1";  // Mặc định
    }

    if (int.TryParse(gradeData.Value, out int grade))
    {
        grade = Mathf.Clamp(grade, 1, 5);
        return $"Độ khó: Lớp {grade}";
    }

    return "Độ khó: Lớp 1";  // Fallback
}
```

#### Sửa RenderLobbyList() - Thêm Grade Vào Widget
Tìm đoạn code:
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

**Kiểm tra xem `LobbyBrowserEntryWidget.Bind()` có bao nhiêu tham số:**

**Trường hợp 1: Widget có 5 tham số (như hiện tại)**
```csharp
// Giữ nguyên, KHÔNG SỬA
var widget = Instantiate(entryPrefab, contentRoot);
widget.Bind(
    BuildLobbyName(lobby),
    BuildHostLabel(lobby),
    BuildPlayerCount(lobby),
    BuildLobbyStatus(lobby),
    canJoin ? (() => _ = JoinLobbyAsync(lobby)) : null);
spawnedEntries.Add(widget);
```

**Trường hợp 2: Muốn thêm grade vào widget**

Cần sửa `LobbyBrowserEntryWidget.cs` để thêm tham số thứ 6:

```csharp
// Trong LobbyBrowserEntryWidget.cs
public void Bind(string lobbyName, string hostLabel, string playerCount, string status, string gradeLabel, System.Action onJoinClicked)
{
    // ... existing code ...
    
    // Thêm TMP_Text gradeText trong widget
    if (gradeText != null)
    {
        gradeText.SetText(gradeLabel);
    }
}
```

Sau đó trong `UILobbyBrowserController.cs`:
```csharp
var widget = Instantiate(entryPrefab, contentRoot);
widget.Bind(
    BuildLobbyName(lobby),
    BuildHostLabel(lobby),
    BuildPlayerCount(lobby),
    BuildLobbyStatus(lobby),
    BuildGradeLabel(lobby),  // ← THÊM GRADE
    canJoin ? (() => _ = JoinLobbyAsync(lobby)) : null);
spawnedEntries.Add(widget);
```

**⚠️ LƯU Ý:** Nếu không muốn sửa `LobbyBrowserEntryWidget`, có thể **gộp grade vào status**:

```csharp
private string BuildLobbyStatus(Lobby lobby)
{
    if (lobby == null || lobby.Data == null)
        return "Đang chờ vào phòng";

    // Lấy grade
    string gradeLabel = BuildGradeLabel(lobby);

    if (lobby.Data.TryGetValue(StartedKey, out var startedData) && startedData != null && startedData.Value == "1")
        return $"Đang trong trận | {gradeLabel}";

    if (!HasAvailableSlots(lobby))
        return $"Phòng đã đầy | {gradeLabel}";

    return $"Đang chờ người chơi | {gradeLabel}";
}
```

---

### File 3: LobbyBrowserEntryWidget.cs (Nếu Muốn Hiển Thị Grade Riêng)

#### Thêm SerializeField
```csharp
[SerializeField] private TMP_Text gradeText;  // ← Thêm text hiển thị grade
```

#### Sửa Bind Method
```csharp
public void Bind(string lobbyName, string hostLabel, string playerCount, string status, string gradeLabel, System.Action onJoinClicked)
{
    if (lobbyNameText != null)
        lobbyNameText.SetText(lobbyName);

    if (hostText != null)
        hostText.SetText(hostLabel);

    if (playerCountText != null)
        playerCountText.SetText(playerCount);

    if (statusText != null)
        statusText.SetText(status);

    if (gradeText != null)  // ← THÊM
        gradeText.SetText(gradeLabel);

    if (joinButton != null)
    {
        joinButton.onClick.RemoveAllListeners();
        if (onJoinClicked != null)
        {
            joinButton.onClick.AddListener(() => onJoinClicked());
            joinButton.interactable = true;
        }
        else
        {
            joinButton.interactable = false;
        }
    }
}
```

---

## 🛠️ Setup Unity Inspector

### LobbyPanel
1. Select `LobbyPanel` trong Hierarchy
2. Inspector → `UIMultiplayerRoomController`
3. Tìm section **Grade Selection**
4. Drag `Dropdown` GameObject vào field **Grade Dropdown**

### LobbyRowTemplate (Nếu Thêm Grade Text)
1. Mở Prefab `LobbyRowTemplate` (hoặc `LobbyBrowserEntry`)
2. Thêm `TextMeshProUGUI` mới tên `GradeText`
3. Vị trí: Dưới `StatusText` hoặc bên cạnh `PlayerCountText`
4. Font size: 18-20
5. Color: #FFC107 (vàng) hoặc #2196F3 (xanh)
6. Text: "Độ khó: Lớp 1" (placeholder)
7. Select Prefab root → Inspector → `LobbyBrowserEntryWidget`
8. Drag `GradeText` vào field **Grade Text**
9. Save Prefab

---

## 🧪 Test Checklist

### Test 1: Tạo Phòng Với Grade
- [ ] Chọn Lớp 3 từ Dropdown
- [ ] Click "Tạo Phòng"
- [ ] Phòng được tạo thành công
- [ ] Console log: `"Grade đã chọn: Lớp 3"`

### Test 2: Tạo Phòng Không Chọn Grade
- [ ] KHÔNG chọn gì từ Dropdown (để mặc định)
- [ ] Click "Tạo Phòng"
- [ ] Phòng được tạo với Grade = 1
- [ ] Console log: `"Grade đã chọn: Lớp 1"`

### Test 3: Hiển Thị Grade Trong Browser
- [ ] Tạo 3 phòng với Lớp 1, 3, 5
- [ ] Mở LobbyBrowserPanel
- [ ] Click "Refresh"
- [ ] Mỗi phòng hiển thị đúng độ khó:
  - Phòng 1: "Độ khó: Lớp 1"
  - Phòng 2: "Độ khó: Lớp 3"
  - Phòng 3: "Độ khó: Lớp 5"

### Test 4: Join Phòng Và Đọc Grade
- [ ] Player 2 join phòng Lớp 3
- [ ] Player 2 đọc grade từ Lobby
- [ ] Console log: `"Lobby grade: 3"`

### Test 5: Backward Compatibility
- [ ] Tạo phòng từ code cũ (không có GradeKey)
- [ ] Phòng vẫn hoạt động bình thường
- [ ] Grade mặc định = 1

---

## 📊 Dữ Liệu Lobby

### Lobby Data Structure (Sau Khi Thêm Grade)

```json
{
  "JoinCode": "ABC123",
  "Started": "0",
  "Mode": "MathDuel",
  "HostName": "Player1",
  "Grade": "3"  // ← MỚI THÊM
}
```

### Grade Mapping

| Dropdown Value | Grade | Lobby Data |
|---|---|---|
| 0 | 1 | `"Grade": "1"` |
| 1 | 2 | `"Grade": "2"` |
| 2 | 3 | `"Grade": "3"` |
| 3 | 4 | `"Grade": "4"` |
| 4 | 5 | `"Grade": "5"` |
| (null/missing) | 1 | `"Grade": "1"` (mặc định) |

---

## 🔄 Tích Hợp Với Gameplay

Sau khi có grade trong Lobby, cần truyền vào gameplay:

### Trong UIMultiplayerBattleController.cs (Hoặc DragQuizManager.cs)

```csharp
protected override void OnShow()
{
    base.OnShow();
    
    // Lấy grade từ UIMultiplayerRoomController
    var roomController = FindObjectOfType<UIMultiplayerRoomController>();
    if (roomController != null)
    {
        int grade = roomController.GetCurrentLobbyGrade();  // ← Cần thêm method public
        SetGameGrade(grade);
    }
}

private void SetGameGrade(int grade)
{
    // Truyền grade vào MathManager hoặc DragQuizManager
    var mathManager = MathManager.Instance;
    if (mathManager != null)
    {
        mathManager.SetGrade(grade);
    }
    
    Debug.Log($"[Battle] Độ khó trận đấu: Lớp {grade}");
}
```

### Thêm Public Method Trong UIMultiplayerRoomController.cs

```csharp
/// <summary>
/// Lấy grade của lobby hiện tại (public để các controller khác đọc)
/// </summary>
public int GetCurrentLobbyGrade()
{
    return GetLobbyGrade(currentLobby);
}
```

---

## ⚠️ Lưu Ý Quan Trọng

### 1. Backward Compatibility
- ✅ Phòng cũ (không có GradeKey) vẫn hoạt động
- ✅ Mặc định Grade = 1 nếu không tìm thấy
- ✅ Không crash khi GradeKey missing

### 2. Validation
- ✅ Grade luôn trong khoảng 1-5 (dùng `Mathf.Clamp`)
- ✅ Parse an toàn với `int.TryParse`
- ✅ Fallback về 1 nếu parse thất bại

### 3. Network Sync
- ✅ Dùng Lobby Data (không cần NetworkVariable)
- ✅ Tất cả players đọc từ cùng 1 source
- ✅ Không cần đồng bộ thủ công

### 4. UI/UX
- ✅ Dropdown mặc định chọn Lớp 1 (value = 0)
- ✅ Hiển thị rõ ràng grade trong browser
- ✅ Không làm rối UI hiện tại

---

## 📁 Files Cần Sửa

| File | Thay Đổi | Bắt Buộc |
|---|---|---|
| `UIMultiplayerRoomController.cs` | Thêm grade selection logic | ✅ Bắt buộc |
| `UILobbyBrowserController.cs` | Hiển thị grade trong danh sách | ✅ Bắt buộc |
| `LobbyBrowserEntryWidget.cs` | Thêm grade text (nếu muốn hiển thị riêng) | ⚠️ Tùy chọn |
| `UIMultiplayerBattleController.cs` | Đọc grade và truyền vào gameplay | ⚠️ Sau này |

---

## 🚀 Thứ Tự Thực Hiện

1. ✅ **Bước 1**: Sửa `UIMultiplayerRoomController.cs` (thêm grade vào CreateLobby)
2. ✅ **Bước 2**: Sửa `UILobbyBrowserController.cs` (hiển thị grade)
3. ✅ **Bước 3**: Setup Inspector (gán Dropdown)
4. ✅ **Bước 4**: Test tạo phòng + hiển thị grade
5. ⚠️ **Bước 5** (Sau): Tích hợp grade vào gameplay

---

**✅ Hoàn thành!** Tính năng chọn độ khó đã sẵn sàng implement.

**🔥 Bắt đầu code ngay?** Hãy cho tôi biết!
