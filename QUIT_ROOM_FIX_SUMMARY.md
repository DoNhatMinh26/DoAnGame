# Fix: Quit Room Issues - Màn Hình Trắng & WinsPanel Không Cập Nhật

## Vấn Đề

### 1. Người Quit → Màn Hình Trắng
- Khi người chơi ấn "Quit" trong popup, họ không quay về được LobbyPanel
- Màn hình chỉ hiển thị trắng (blank screen)

### 2. Người Còn Lại → WinsPanel Không Cập Nhật Đúng
- Người còn lại được tính thắng nhưng WinsPanel không hiển thị tên/điểm/máu đúng
- Trạng thái UI không được reset sau khi quit

## Nguyên Nhân

### 1. Màn Hình Trắng
- `UIBattleQuitConfirmPopup.ExecuteQuitToLobby()` ẩn GameplayPanel và gọi `RequestQuitRoom()`
- `RequestQuitRoom()` gọi `quitRoomNavigator.NavigateNow()` nhưng **không hiển thị LobbyPanel trước**
- Kết quả: Tất cả panels đều bị ẩn → màn hình trắng

### 2. WinsPanel Không Cập Nhật
- Khi người quit gọi `RequestForfeitServerRpc()`, server kết thúc trận **NGAY LẬP TỨC**
- `NetworkedPlayerState` của người quit có thể bị destroy trước khi người còn lại đọc được data
- `UIMultiplayerBattleController.PushMatchResultToWinsController()` không đọc được tên/điểm/máu

### 3. State Không Reset
- `ResetRoomSessionState()` chỉ reset state của LobbyPanel (dropdown, buttons, status)
- **KHÔNG** reset state của WinsPanel và BattleManager
- Kết quả: Data cũ vẫn còn khi tạo trận mới

## Giải Pháp

### 1. Fix Màn Hình Trắng

#### File: `UIBattleQuitConfirmPopup.cs`

**Thay đổi:**
- Thêm `roomController.Show()` TRƯỚC KHI gọi `RequestQuitRoom()`
- Đảm bảo LobbyPanel hiển thị trước khi quit

```csharp
private void ExecuteQuitToLobby()
{
    // Ẩn GameplayPanel
    var gameplayPanel = FindObjectOfType<UIMultiplayerBattleController>(true);
    if (gameplayPanel != null)
    {
        gameplayPanel.Hide();
    }

    // Ẩn popup
    Hide();

    // Resolve roomController
    if (roomController == null)
    {
        roomController = FindObjectOfType<UIMultiplayerRoomController>(true);
    }

    if (roomController != null)
    {
        // ✅ FIX: Hiển thị LobbyPanel TRƯỚC KHI quit
        roomController.Show();
        
        // Sau đó quit room
        roomController.RequestQuitRoom();
    }
}
```

### 2. Fix WinsPanel Không Cập Nhật

#### File: `NetworkedMathBattleManager.cs`

**Thay đổi:**
- Delay 0.5 giây trước khi kết thúc trận
- Cho client thời gian đọc `NetworkedPlayerState` trước khi disconnect

```csharp
[ServerRpc(RequireOwnership = false)]
public void RequestForfeitServerRpc(ServerRpcParams rpcParams = default)
{
    // ... existing code ...

    // ✅ FIX: Delay để client có thời gian đọc data
    StartCoroutine(EndMatchAfterDelay(winnerId, 0.5f));
}

private IEnumerator EndMatchAfterDelay(int winnerId, float delay)
{
    yield return new WaitForSeconds(delay);
    EndMatchWithWinner(winnerId);
}
```

#### File: `UIBattleQuitConfirmPopup.cs`

**Thay đổi:**
- Delay 1 giây sau khi gửi `RequestForfeitServerRpc()`
- Cho server thời gian xử lý và gửi thông báo cho người còn lại

```csharp
private void HandleConfirmQuit()
{
    Hide();

    // Gửi forfeit signal
    var battleManager = NetworkedMathBattleManager.Instance;
    if (battleManager != null)
    {
        battleManager.RequestForfeitServerRpc();
    }

    // ✅ FIX: Delay 1 giây trước khi quit
    StartCoroutine(QuitAfterDelay(1f));
}

private IEnumerator QuitAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    ExecuteQuitToLobby();
}
```

### 3. Fix State Reset

#### File: `UIMultiplayerRoomController.cs`

**Thay đổi:**
- Thêm `HideAllBattlePanels()` để ẩn tất cả battle panels
- Thêm `ResetWinsPanelState()` để reset static cache của WinsPanel
- Thêm `ResetBattleManagerState()` để reset NetworkVariables

```csharp
private void ResetRoomSessionState(string status)
{
    currentLobby = null;
    isHost = false;
    battleStartNotified = false;
    receivedStartSignalFromHost = false;
    nextLobbyReadAt = 0f;
    
    if (gradeDropdown != null)
    {
        gradeDropdown.interactable = true;
    }
    
    // ✅ FIX: Ẩn tất cả battle panels
    HideAllBattlePanels();
    
    // ✅ FIX: Reset WinsPanel state
    ResetWinsPanelState();
    
    // ✅ FIX: Reset BattleManager state
    ResetBattleManagerState();
    
    ApplyIdleVisualState(status);
    roomCodeInput?.SetTextWithoutNotify(string.Empty);
}

private void HideAllBattlePanels()
{
    // Ẩn GameplayPanel, WinsPanel, LoadingPanel, QuitPopup
    // Hiển thị LobbyPanel
}

private void ResetWinsPanelState()
{
    // Reset UIWinsController.LastResult
}

private void ResetBattleManagerState()
{
    // Reset NetworkVariables nếu là server
}
```

## Flow Sau Khi Fix

### Người Quit:
1. Ấn "Quit" trong popup
2. Popup gửi `RequestForfeitServerRpc()` → server kết thúc trận
3. **Delay 1 giây** để server xử lý
4. Ẩn GameplayPanel và popup
5. **Hiển thị LobbyPanel** (fix màn hình trắng)
6. Gọi `RequestQuitRoom()` → disconnect relay, reset state
7. Quay về LobbyPanel với state sạch

### Người Còn Lại:
1. Server nhận `RequestForfeitServerRpc()`
2. Server set `IsAbandoned=true`, `AbandonedPlayerId=forfeitPlayerId`
3. **Server delay 0.5 giây** để client đọc player states
4. Server gọi `EndMatchWithWinner()` → gửi `ShowMatchResultClientRpc()`
5. Client nhận `OnMatchEnded` event
6. `UIMultiplayerBattleController.HandleMatchEnded()` push data vào `UIWinsController.LastResult`
7. Navigate đến WinsPanel với data đầy đủ (tên, điểm, máu, "(Đã Rời Trận)")
8. Ấn "Tiếp tục" → `RequestQuitRoom()` → reset state → quay về LobbyPanel

## Testing Checklist

- [ ] Người quit thấy LobbyPanel (không còn màn hình trắng)
- [ ] Người còn lại thấy WinsPanel với tên/điểm/máu đúng
- [ ] WinsPanel hiển thị "(Đã Rời Trận)" cho người quit
- [ ] Cả 2 người đều quay về LobbyPanel với state sạch
- [ ] Dropdown lớp học được unlock khi quit
- [ ] Buttons (Host, Join, Quick Join) hoạt động bình thường sau quit
- [ ] Tạo trận mới không bị conflict với data cũ

## Files Changed

1. `Assets/Script/Script_multiplayer/1Code/Multiplay/UIBattleQuitConfirmPopup.cs`
   - Thêm `using System.Collections`
   - Thêm delay 1 giây trước khi quit
   - Hiển thị LobbyPanel trước khi quit

2. `Assets/Script/Script_multiplayer/1Code/Multiplay/NetworkedMathBattleManager.cs`
   - Thêm delay 0.5 giây trước khi kết thúc trận
   - Cho client thời gian đọc player states

3. `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerRoomController.cs`
   - Thêm `HideAllBattlePanels()` method
   - Thêm `ResetWinsPanelState()` method
   - Thêm `ResetBattleManagerState()` method
   - Gọi 3 methods trên trong `ResetRoomSessionState()`

## Notes

- **Timing is critical**: Delays đảm bảo NetworkVariables được sync trước khi disconnect
- **State cleanup**: Reset tất cả panels và managers để tránh conflict
- **UI visibility**: Luôn hiển thị target panel trước khi navigate để tránh màn hình trắng
