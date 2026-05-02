# Implement Ready System for Start Button

## 📋 Yêu Cầu

**Host**:
- Nút "Bắt đầu" hiển thị nhưng bị xám (disabled)
- Sáng lên (enabled) khi client báo sẵn sàng

**Client**:
- Nút hiển thị text "Sẵn sàng"
- Khi bấm → Gửi tín hiệu sẵn sàng lên server
- Nút chuyển thành "Đã sẵn sàng" (disabled)

---

## 🔧 Implementation Plan

### 1. Thêm NetworkVariable để Track Ready State

**File**: `NetworkedPlayerState.cs`

```csharp
[Tooltip("Player đã sẵn sàng chưa")]
public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);
```

---

### 2. Thêm Ready Metadata vào Lobby

**File**: `UIMultiplayerRoomController.cs`

```csharp
private const string Player1ReadyKey = "Player1Ready";
private const string Player2ReadyKey = "Player2Ready";
```

---

### 3. Thêm Logic Xử Lý Ready State

**File**: `UIMultiplayerRoomController.cs`

```csharp
// Thêm method để handle Ready button click
private async Task HandleReadyButton()
{
    if (isHost)
    {
        // Host không cần bấm Ready, chỉ bấm Start
        return;
    }

    // Client bấm Ready
    isBusy = true;
    SetActionButtonsInteractable(false);

    try
    {
        // Update lobby metadata để báo sẵn sàng
        await LobbyService.Instance.UpdatePlayerAsync(
            currentLobby.Id,
            AuthenticationService.Instance.PlayerId,
            new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "1") }
                }
            });

        SetStatus("Bạn đã sẵn sàng. Chờ chủ phòng bắt đầu...");
        UpdateReadyButtonState(true);  // Disable button, change text
    }
    catch (Exception ex)
    {
        Debug.LogWarning($"[UIRoom] Failed to mark ready: {ex.Message}");
        SetStatus("Không thể báo sẵn sàng.");
    }
    finally
    {
        isBusy = false;
        SetActionButtonsInteractable(true);
    }
}

// Update button state dựa trên ready status
private void UpdateReadyButtonState(bool isReady)
{
    if (startMatchButton == null)
        return;

    if (isHost)
    {
        // Host: button text = "Bắt đầu"
        var buttonText = startMatchButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = "Bắt đầu";
        }
        
        // Enable/disable dựa trên client ready status
        startMatchButton.interactable = IsClientReady();
    }
    else
    {
        // Client: button text = "Sẵn sàng" hoặc "Đã sẵn sàng"
        var buttonText = startMatchButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = isReady ? "Đã sẵn sàng" : "Sẵn sàng";
        }
        
        // Disable nếu đã sẵn sàng
        startMatchButton.interactable = !isReady;
    }
}

// Check xem client có sẵn sàng không
private bool IsClientReady()
{
    if (currentLobby == null || currentLobby.Players == null || currentLobby.Players.Count < 2)
        return false;

    // Tìm client player (không phải host)
    for (int i = 0; i < currentLobby.Players.Count; i++)
    {
        var player = currentLobby.Players[i];
        if (player == null || string.IsNullOrWhiteSpace(player.Id))
            continue;

        // Skip host
        if (!string.IsNullOrWhiteSpace(currentLobby.HostId) && 
            string.Equals(player.Id, currentLobby.HostId, StringComparison.OrdinalIgnoreCase))
            continue;

        // Check ready status
        if (player.Data != null && player.Data.TryGetValue("Ready", out var readyData))
        {
            return readyData != null && readyData.Value == "1";
        }
    }

    return false;
}
```

---

### 4. Update HandleStartMatch để Check Ready Status

**File**: `UIMultiplayerRoomController.cs`

```csharp
private async Task HandleStartMatch()
{
    // ... existing validation ...

    // ← THÊM: Check xem client có sẵn sàng không
    if (!IsClientReady())
    {
        SetStatus("Chờ người chơi thứ 2 báo sẵn sàng.");
        isBusy = false;
        SetActionButtonsInteractable(true);
        return;
    }

    // ... rest of existing code ...
}
```

---

### 5. Update PollLobbyOnce để Refresh Ready State

**File**: `UIMultiplayerRoomController.cs`

```csharp
private async Task PollLobbyOnce()
{
    if (currentLobby == null || isQuitting) return;

    if (Time.unscaledTime < nextLobbyReadAt)
        return;

    var refreshed = await RefreshLobbySafe();
    
    if (refreshed == null) 
    {
        // ... existing code ...
        return;
    }

    currentLobby = refreshed;
    
    // ← THÊM: Update ready button state
    if (isHost)
    {
        UpdateReadyButtonState(false);  // Host button always shows "Bắt đầu"
    }
    
    // ... rest of existing code ...
}
```

---

### 6. Update Button Click Handler

**File**: `UIMultiplayerRoomController.cs`

```csharp
protected override void Awake()
{
    base.Awake();
    
    // ... existing code ...
    
    // ← THÊM: Update start match button handler
    startMatchButton?.onClick.AddListener(() => _ = HandleStartMatchButtonClick());
}

private async Task HandleStartMatchButtonClick()
{
    if (isHost)
    {
        // Host: bấm "Bắt đầu"
        await HandleStartMatch();
    }
    else
    {
        // Client: bấm "Sẵn sàng"
        await HandleReadyButton();
    }
}
```

---

## 🎯 Flow

### Host Side
```
1. Host tạo phòng
   ↓
2. Nút "Bắt đầu" hiển thị nhưng xám (disabled)
   ↓
3. Client join phòng
   ↓
4. Nút vẫn xám (chờ client sẵn sàng)
   ↓
5. Client bấm "Sẵn sàng"
   ↓
6. Nút "Bắt đầu" sáng lên (enabled)
   ↓
7. Host bấm "Bắt đầu"
   ↓
8. Battle bắt đầu
```

### Client Side
```
1. Client join phòng
   ↓
2. Nút "Sẵn sàng" hiển thị (enabled)
   ↓
3. Client bấm "Sẵn sàng"
   ↓
4. Nút chuyển thành "Đã sẵn sàng" (disabled)
   ↓
5. Chờ host bấm "Bắt đầu"
   ↓
6. Battle bắt đầu
```

---

## 📝 Code Changes Summary

### Files to Modify

1. **NetworkedPlayerState.cs**
   - Thêm `IsReady` NetworkVariable

2. **UIMultiplayerRoomController.cs**
   - Thêm `Player1ReadyKey`, `Player2ReadyKey` constants
   - Thêm `HandleReadyButton()` method
   - Thêm `UpdateReadyButtonState()` method
   - Thêm `IsClientReady()` method
   - Thêm `HandleStartMatchButtonClick()` method
   - Update `HandleStartMatch()` để check ready status
   - Update `PollLobbyOnce()` để refresh ready state
   - Update `Awake()` để gán button handler

---

## 🧪 Test Cases

### Test 1: Host Waits for Client Ready
1. Host tạo phòng
2. Nút "Bắt đầu" xám (disabled)
3. Client join phòng
4. Nút vẫn xám
5. Client bấm "Sẵn sàng"
6. Nút "Bắt đầu" sáng lên (enabled)
7. Host bấm "Bắt đầu"
8. **Kết quả**: ✅ Battle bắt đầu

### Test 2: Client Ready State
1. Client join phòng
2. Nút "Sẵn sàng" hiển thị (enabled)
3. Client bấm "Sẵn sàng"
4. Nút chuyển thành "Đã sẵn sàng" (disabled)
5. **Kết quả**: ✅ Nút disabled, text đổi

### Test 3: Host Tries to Start Before Client Ready
1. Host tạo phòng
2. Client join phòng
3. Host bấm "Bắt đầu" (client chưa sẵn sàng)
4. **Kết quả**: ✅ Thông báo "Chờ người chơi thứ 2 báo sẵn sàng"

---

## ✅ Checklist

- [ ] Thêm `IsReady` NetworkVariable vào NetworkedPlayerState.cs
- [ ] Thêm Ready constants vào UIMultiplayerRoomController.cs
- [ ] Implement `HandleReadyButton()` method
- [ ] Implement `UpdateReadyButtonState()` method
- [ ] Implement `IsClientReady()` method
- [ ] Implement `HandleStartMatchButtonClick()` method
- [ ] Update `HandleStartMatch()` để check ready status
- [ ] Update `PollLobbyOnce()` để refresh ready state
- [ ] Update button click handler
- [ ] Test all scenarios

---

## 📌 Notes

- Ready state lưu trong Lobby metadata (không cần NetworkVariable)
- Host không cần bấm Ready, chỉ bấm Start
- Client bấm Ready → Gửi tín hiệu lên server → Host thấy button sáng
- Polling sẽ refresh ready state mỗi 1.5s

