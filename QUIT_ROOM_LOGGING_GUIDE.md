# Quit Room Flow - Logging Guide

## Mục Đích

Thêm log chi tiết vào quit room flow để debug các vấn đề:
1. Màn hình trắng khi quit
2. WinsPanel không cập nhật đúng
3. State không reset đúng

## Log Files Location

Logs được ghi vào:
```
Assets/Script/Script_multiplayer/1Code/Multiplay/
├── GameLog_HOST_<timestamp>.txt
└── GameLog_CLIENT_<timestamp>.txt
```

## Flow & Log Points

### 1. Người Chơi Ấn "Quit" Button

**File:** `UIBattleQuitConfirmPopup.cs`

**Log Points:**
```
[QuitPopup] [HOST/CLIENT] [ClientID:X] User CONFIRMED quit - starting forfeit flow
[QuitPopup] [HOST/CLIENT] Sending RequestForfeitServerRpc to server...
[QuitPopup] [HOST/CLIENT] ✅ RequestForfeitServerRpc sent successfully
[QuitPopup] [HOST/CLIENT] Starting 1s delay before ExecuteQuitToLobby...
[QuitPopup] [HOST/CLIENT] Waiting 1s for server to process forfeit...
[QuitPopup] [HOST/CLIENT] Delay complete - calling ExecuteQuitToLobby
```

**Hoặc nếu ấn "Huỷ":**
```
[QuitPopup] [HOST/CLIENT] User CANCELLED quit - continuing battle
```

### 2. Server Nhận Forfeit Request

**File:** `NetworkedMathBattleManager.cs`

**Log Points:**
```
[BattleManager] [SERVER] 🏳️ FORFEIT RECEIVED from ClientID:X (Player Y)
[BattleManager] [SERVER] Winner = Player Z
[BattleManager] [SERVER] ✅ Timer stopped
[BattleManager] [SERVER] ✅ Set IsAbandoned=true, AbandonedPlayerId=Y
[BattleManager] [SERVER] Starting 0.5s delay before EndMatchWithWinner...
[BattleManager] [SERVER] Waiting 0.5s for clients to read player states...
[BattleManager] [SERVER] Delay complete - calling EndMatchWithWinner(winnerId=Z)
```

### 3. Server Kết Thúc Trận

**File:** `NetworkedMathBattleManager.cs` → `EndMatchWithWinner()`

**Log Points:**
```
[BattleManager] [SERVER] EndMatchWithWinner START - winnerId=Z
[BattleManager] [SERVER] ✅ Set MatchEnded=true, WinnerId=Z, WinnerHealth=X
[BattleManager] [SERVER] Player1State: Name=ABC, Score=50, Health=3
[BattleManager] [SERVER] Player2State: Name=XYZ, Score=30, Health=0
[BattleManager] [SERVER] Syncing Firebase for winner: uid=..., score=50
[BattleManager] [SERVER] Invoking OnMatchEnded event (Host)...
[BattleManager] [SERVER] Sending ShowMatchResultClientRpc to clients...
[BattleManager] [SERVER] EndMatchWithWinner COMPLETE
```

**Nếu có lỗi:**
```
[BattleManager] [SERVER] ⚠️ Player1State is NULL
[BattleManager] [SERVER] ⚠️ Player2State is NULL
```

### 4. Client Nhận Match Ended

**File:** `UIMultiplayerBattleController.cs` → `HandleMatchEnded()`

**Log Points:**
```
[BattleController] [HOST/CLIENT] HandleMatchEnded RECEIVED - winnerId=Z, winnerHealth=X
[BattleController] [HOST/CLIENT] LocalPlayerId=Y, IsAbandoned=true, AbandonedPlayerId=Y
[BattleController] [HOST/CLIENT] Pushing match result to WinsController...
[BattleController] [HOST/CLIENT] ✅ PushMatchResult SUCCESS:
  - Winner: ABC (Score:50, HP:3)
  - Loser: XYZ (Score:30, HP:0)
  - IsAbandoned: true, AbandonedPlayerId: Y
[BattleController] [HOST/CLIENT] Syncing own match result to Firebase...
[BattleController] [HOST/CLIENT] IsLocalWinner=false, navigating to WinsPanel in 2s...
```

**Nếu có lỗi:**
```
[BattleController] PushMatchResult: battleManager is NULL - cannot push data
[BattleController] ⚠️ PushMatchResult: Player states NULL - p1=false, p2=false
```

### 5. Người Quit → ExecuteQuitToLobby

**File:** `UIBattleQuitConfirmPopup.cs` → `ExecuteQuitToLobby()`

**Log Points:**
```
[QuitPopup] [HOST/CLIENT] ExecuteQuitToLobby START
[QuitPopup] [HOST/CLIENT] ✅ Hidden GameplayPanel
[QuitPopup] [HOST/CLIENT] ✅ Hidden QuitPopup
[QuitPopup] [HOST/CLIENT] roomController found: true
[QuitPopup] [HOST/CLIENT] Showing LobbyPanel before quit...
[QuitPopup] [HOST/CLIENT] ✅ LobbyPanel shown
[QuitPopup] [HOST/CLIENT] Calling RequestQuitRoom...
[QuitPopup] [HOST/CLIENT] ✅ RequestQuitRoom called - ExecuteQuitToLobby COMPLETE
```

**Nếu có lỗi:**
```
[QuitPopup] [HOST/CLIENT] ⚠️ GameplayPanel NOT FOUND
[QuitPopup] [HOST/CLIENT] roomController is null, searching...
[QuitPopup] [HOST/CLIENT] ❌ ERROR: roomController NOT FOUND - cannot navigate to lobby!
```

### 6. HandleQuitRoom

**File:** `UIMultiplayerRoomController.cs` → `HandleQuitRoom()`

**Log Points:**
```
[UIRoom] [HOST/CLIENT] HandleQuitRoom START
[UIRoom] [HOST/CLIENT] Set isBusy=true, isQuitting=true, suppressAutoBattleStart=true
[UIRoom] [HOST/CLIENT] Stopped all routines
[UIRoom] [HOST] Is HOST - marking lobby as abandoned (lobbyId=...)
[UIRoom] [HOST] ✅ Lobby marked as abandoned successfully
[UIRoom] [CLIENT] Is CLIENT - calling LeaveLobbySafe
[UIRoom] [CLIENT] ✅ LeaveLobbySafe completed
[UIRoom] [HOST/CLIENT] Disconnecting Relay...
[UIRoom] [HOST/CLIENT] ✅ Relay disconnected
[UIRoom] [HOST/CLIENT] Calling ResetRoomSessionState...
[UIRoom] [HOST/CLIENT] ✅ ResetRoomSessionState completed
[UIRoom] [HOST/CLIENT] Calling quitRoomNavigator.NavigateNow()...
[UIRoom] [HOST/CLIENT] ✅ Navigation completed
[UIRoom] [HOST/CLIENT] HandleQuitRoom COMPLETE - isBusy=false, isQuitting=false
```

**Nếu có lỗi:**
```
[UIRoom] [HOST/CLIENT] HandleQuitRoom: isBusy=true, returning early
[UIRoom] [HOST] ⚠️ Failed to mark abandoned: ... - fallback to LeaveLobbySafe
[UIRoom] [HOST/CLIENT] ⚠️ quitRoomNavigator is NULL - cannot navigate
```

### 7. ResetRoomSessionState

**File:** `UIMultiplayerRoomController.cs` → `ResetRoomSessionState()`

**Log Points:**
```
[UIRoom] [HOST/CLIENT] ResetRoomSessionState START
[UIRoom] [HOST/CLIENT] Reset flags: currentLobby=null, isHost=false, ...
[UIRoom] [HOST/CLIENT] ✅ Unlocked grade dropdown
[UIRoom] [HOST/CLIENT] Calling HideAllBattlePanels...
[UIRoom] [HOST/CLIENT] ✅ Hidden GameplayPanel
[UIRoom] [HOST/CLIENT] ✅ Hidden WinsPanel
[UIRoom] [HOST/CLIENT] ✅ Hidden LoadingPanel
[UIRoom] [HOST/CLIENT] ✅ Hidden QuitPopup
[UIRoom] [HOST/CLIENT] ✅ Shown LobbyPanel
[UIRoom] [HOST/CLIENT] Calling ResetWinsPanelState...
[UIRoom] [HOST/CLIENT] ✅ Reset WinsPanel.LastResult (IsValid=false)
[UIRoom] [HOST/CLIENT] Calling ResetBattleManagerState...
[UIRoom] [HOST/CLIENT] ✅ Reset BattleManager NetworkVariables (Server)
[UIRoom] [HOST/CLIENT] Calling ApplyIdleVisualState...
[UIRoom] [HOST/CLIENT] ResetRoomSessionState COMPLETE
```

**Nếu có lỗi:**
```
[UIRoom] [HOST/CLIENT] GameplayPanel not found
[UIRoom] [HOST/CLIENT] WinsPanel not found
[UIRoom] [HOST/CLIENT] ❌ ERROR in HideAllBattlePanels: ...
[UIRoom] [HOST/CLIENT] ❌ ERROR in ResetWinsPanelState: ...
[UIRoom] [HOST/CLIENT] ⚠️ Not server, skipping BattleManager NetworkVariable reset
```

## Cách Đọc Logs

### 1. Kiểm Tra Flow Đúng

**Người Quit (ví dụ CLIENT):**
```
[QuitPopup] [CLIENT] User CONFIRMED quit
→ [QuitPopup] [CLIENT] RequestForfeitServerRpc sent
→ [QuitPopup] [CLIENT] Waiting 1s
→ [QuitPopup] [CLIENT] ExecuteQuitToLobby START
→ [QuitPopup] [CLIENT] LobbyPanel shown
→ [UIRoom] [CLIENT] HandleQuitRoom START
→ [UIRoom] [CLIENT] LeaveLobbySafe completed
→ [UIRoom] [CLIENT] Relay disconnected
→ [UIRoom] [CLIENT] ResetRoomSessionState completed
→ [UIRoom] [CLIENT] Navigation completed
→ [UIRoom] [CLIENT] HandleQuitRoom COMPLETE
```

**Người Còn Lại (ví dụ HOST):**
```
[BattleManager] [SERVER] FORFEIT RECEIVED from ClientID:1
→ [BattleManager] [SERVER] Winner = Player 0
→ [BattleManager] [SERVER] Waiting 0.5s
→ [BattleManager] [SERVER] EndMatchWithWinner START
→ [BattleManager] [SERVER] Player1State: Name=..., Score=..., Health=...
→ [BattleManager] [SERVER] Player2State: Name=..., Score=..., Health=...
→ [BattleManager] [SERVER] Sending ShowMatchResultClientRpc
→ [BattleController] [HOST] HandleMatchEnded RECEIVED
→ [BattleController] [HOST] PushMatchResult SUCCESS
→ [BattleController] [HOST] navigating to WinsPanel in 2s
```

### 2. Debug Màn Hình Trắng

**Tìm trong log:**
```
[QuitPopup] ExecuteQuitToLobby START
```

**Kiểm tra:**
- ✅ `LobbyPanel shown` → OK
- ❌ `roomController NOT FOUND` → Lỗi: không tìm thấy UIMultiplayerRoomController
- ❌ `quitRoomNavigator is NULL` → Lỗi: chưa gán navigator trong Inspector

### 3. Debug WinsPanel Không Cập Nhật

**Tìm trong log:**
```
[BattleController] HandleMatchEnded RECEIVED
```

**Kiểm tra:**
- ✅ `PushMatchResult SUCCESS` với tên/điểm/máu đúng → OK
- ❌ `Player states NULL` → Lỗi: player states bị destroy quá sớm
- ❌ `battleManager is NULL` → Lỗi: không tìm thấy BattleManager

### 4. Debug State Không Reset

**Tìm trong log:**
```
[UIRoom] ResetRoomSessionState START
```

**Kiểm tra:**
- ✅ `Hidden GameplayPanel` → OK
- ✅ `Hidden WinsPanel` → OK
- ✅ `Reset WinsPanel.LastResult` → OK
- ✅ `Reset BattleManager NetworkVariables` → OK
- ❌ `ERROR in HideAllBattlePanels` → Lỗi khi ẩn panels
- ❌ `Not server, skipping BattleManager reset` → Bình thường (client không reset NetworkVariables)

## Timing Issues

### Delay Quan Trọng

1. **Người Quit: 1 giây delay**
   - Cho server thời gian xử lý forfeit
   - Cho người còn lại nhận được thông báo

2. **Server: 0.5 giây delay**
   - Cho client thời gian đọc player states
   - Trước khi NetworkVariables bị reset

**Nếu thấy lỗi timing:**
```
[BattleController] ⚠️ PushMatchResult: Player states NULL
```
→ Tăng delay trong `EndMatchAfterDelay()` từ 0.5s lên 1s

## Common Issues & Solutions

### Issue 1: Màn Hình Trắng
**Log:**
```
[QuitPopup] ❌ ERROR: roomController NOT FOUND
```
**Solution:** Gán `UIMultiplayerRoomController` vào `roomController` field trong Inspector

### Issue 2: Player States NULL
**Log:**
```
[BattleController] ⚠️ PushMatchResult: Player states NULL
```
**Solution:** Tăng delay trong `RequestForfeitServerRpc()` hoặc `QuitAfterDelay()`

### Issue 3: WinsPanel Hiển thị Data Cũ
**Log:**
```
[UIRoom] GameplayPanel not found
[UIRoom] WinsPanel not found
```
**Solution:** Đảm bảo `HideAllBattlePanels()` tìm được các panels (dùng `includeInactive=true`)

### Issue 4: Navigation Không Hoạt Động
**Log:**
```
[UIRoom] ⚠️ quitRoomNavigator is NULL
```
**Solution:** Gán `quitRoomNavigator` trong Inspector của `UIMultiplayerRoomController`

## Testing Checklist

Khi test, kiểm tra log có đầy đủ các dòng sau:

**Người Quit:**
- [ ] `User CONFIRMED quit`
- [ ] `RequestForfeitServerRpc sent`
- [ ] `ExecuteQuitToLobby START`
- [ ] `LobbyPanel shown`
- [ ] `HandleQuitRoom START`
- [ ] `ResetRoomSessionState completed`
- [ ] `HandleQuitRoom COMPLETE`

**Người Còn Lại:**
- [ ] `FORFEIT RECEIVED`
- [ ] `EndMatchWithWinner START`
- [ ] `Player1State: Name=...` (có tên đúng)
- [ ] `Player2State: Name=...` (có tên đúng)
- [ ] `HandleMatchEnded RECEIVED`
- [ ] `PushMatchResult SUCCESS` (có tên/điểm/máu đúng)
- [ ] `navigating to WinsPanel`

## Files Changed

1. `UIBattleQuitConfirmPopup.cs` - Thêm log cho quit flow
2. `NetworkedMathBattleManager.cs` - Thêm log cho server forfeit handling
3. `UIMultiplayerBattleController.cs` - Thêm log cho HandleMatchEnded
4. `UIMultiplayerRoomController.cs` - Thêm log cho HandleQuitRoom và reset state
   - Thêm `using DoAnGame.Multiplayer;` để dùng GameLogger

## Log Format

Tất cả logs sử dụng format:
```
[Component] [ROLE] Message
```

Ví dụ:
```
[QuitPopup] [HOST] User CONFIRMED quit
[BattleManager] [SERVER] FORFEIT RECEIVED
[BattleController] [CLIENT] HandleMatchEnded RECEIVED
[UIRoom] [HOST] HandleQuitRoom START
```

**Symbols:**
- ✅ = Success
- ⚠️ = Warning
- ❌ = Error
- 🏳️ = Forfeit/Abandon
