# Multiplayer Flow Guide - Test_FireBase_multi Scene

**Scene:** `Assets/Scenes/Test_FireBase_multi.unity`  
**Mục đích:** Multiplayer battle 1v1 với Unity Relay + Lobby + Firebase

---

## 📋 Tổng Quan

Scene này cung cấp trải nghiệm multiplayer hoàn chỉnh:
- **Tạo/Join phòng** qua Unity Lobby Service
- **Kết nối P2P** qua Unity Relay
- **Battle real-time** với câu hỏi toán học
- **Sync kết quả** lên Firebase
- **Quit/Forfeit** an toàn

---

## 🎯 Các Flow Chính

### 1. [Flow Tạo Phòng (HOST)](#flow-1-tạo-phòng-host)
### 2. [Flow Join Phòng (CLIENT)](#flow-2-join-phòng-client)
### 3. [Flow Bắt Đầu Trận (HOST)](#flow-3-bắt-đầu-trận-host)
### 4. [Flow Battle (HOST + CLIENT)](#flow-4-battle-host--client)
### 5. [Flow Kết Thúc Trận (Hết Máu)](#flow-5-kết-thúc-trận-hết-máu)
### 6. [Flow Quit Giữa Trận (Forfeit)](#flow-6-quit-giữa-trận-forfeit)
### 7. [Flow Quay Về Lobby](#flow-7-quay-về-lobby)

---

## Flow 1: Tạo Phòng (HOST)

### **Điểm Bắt Đầu:** LobbyPanel

### **Bước 1: User Click "Tạo Phòng"**
```
User → Click button "Tạo Phòng"
  ↓
UIMultiplayerRoomController.HandleCreateRoom()
  ↓
Set isBusy = true
Set status: "Đang tạo phòng..."
```

### **Bước 2: Tạo Relay Allocation**
```
RelayManager.CreateRelay()
  ↓
Unity Relay Service → Allocate server
  ↓
Nhận JoinCode (ví dụ: "ABCD12")
```

### **Bước 3: Tạo Lobby**
```
LobbyService.CreateLobbyAsync()
  ↓
Lobby Data:
  - JoinCode: "ABCD12"
  - Grade: "1" (từ dropdown)
  - Started: "0"
  - MaxPlayers: 2
  ↓
Nhận Lobby ID
```

### **Bước 4: Start NetworkManager (Host)**
```
NetworkManager.Singleton.StartHost()
  ↓
Relay transport kết nối
  ↓
isHost = true
```

### **Bước 5: Hiển thị Lobby**
```
Show LobbyPanel
  ↓
Status: "Chờ người chơi thứ 2..."
  ↓
Start PollLobbyRoutine() (refresh mỗi 1.5s)
  ↓
Hiển thị:
  - Mã phòng: "ABCD12"
  - Danh sách người chơi: 1/2
  - Button "Bắt Đầu": DISABLED (chờ client ready)
```

### **Kết Quả:**
- ✅ Phòng đã tạo
- ✅ HOST đang chờ CLIENT join
- ✅ Polling lobby để detect CLIENT join

---

## Flow 2: Join Phòng (CLIENT)

### **Điểm Bắt Đầu:** LobbyPanel

### **Bước 1: User Nhập Mã Phòng**
```
User → Nhập "ABCD12" vào input field
  ↓
Click button "Tham Gia"
  ↓
UIMultiplayerRoomController.HandleJoinRoom()
  ↓
Set isBusy = true
Set status: "Đang tham gia..."
```

### **Bước 2: Join Lobby**
```
LobbyService.JoinLobbyByCodeAsync("ABCD12")
  ↓
Nhận Lobby object
  ↓
Đọc JoinCode từ Lobby.Data["JoinCode"]
```

### **Bước 3: Join Relay**
```
RelayManager.JoinRelay(joinCode)
  ↓
Unity Relay Service → Join allocation
  ↓
Kết nối P2P với HOST
```

### **Bước 4: Start NetworkManager (Client)**
```
NetworkManager.Singleton.StartClient()
  ↓
Relay transport kết nối
  ↓
isHost = false
```

### **Bước 5: Hiển thị Lobby**
```
Show LobbyPanel
  ↓
Status: "Nhấn Sẵn sàng để báo hiệu cho chủ phòng."
  ↓
Start PollLobbyRoutine()
  ↓
Hiển thị:
  - Danh sách người chơi: 2/2
  - Button "Sẵn Sàng": ENABLED
  - Dropdown Grade: LOCKED (theo HOST)
```

### **Bước 6: HOST Detect CLIENT Join**
```
HOST PollLobbyRoutine() detect 2 players
  ↓
OnClientConnectedCallback() fired
  ↓
Force refresh lobby
  ↓
Update roster: 2/2 players
  ↓
Status: "Chờ người chơi thứ 2 sẵn sàng..."
  ↓
Button "Bắt Đầu": DISABLED (chờ client ready)
```

### **Kết Quả:**
- ✅ CLIENT đã join phòng
- ✅ HOST thấy CLIENT trong roster
- ✅ Cả 2 đang ở LobbyPanel

---

## Flow 3: Bắt Đầu Trận (HOST)

### **Điểm Bắt Đầu:** LobbyPanel (2/2 players)

### **Bước 1: CLIENT Click "Sẵn Sàng"**
```
CLIENT → Click button "Sẵn Sàng"
  ↓
UIMultiplayerRoomController.HandleReadyButtonClick()
  ↓
MarkClientReadyAsync()
  ↓
UpdatePlayerAsync(Player2Ready = "1")
  ↓
Button text: "Đã sẵn sàng ✓"
Button interactable: FALSE
  ↓
Status: "Đã sẵn sàng! Chờ chủ phòng bắt đầu..."
```

### **Bước 2: HOST Detect CLIENT Ready**
```
HOST PollLobbyRoutine() detect Player2Ready = "1"
  ↓
IsClientReady() returns true
  ↓
Button "Bắt Đầu": ENABLED
  ↓
Status: "Người chơi đã sẵn sàng! Có thể bắt đầu."
```

### **Bước 3: HOST Click "Bắt Đầu"**
```
HOST → Click button "Bắt Đầu"
  ↓
UIMultiplayerRoomController.HandleStartMatch()
  ↓
Set isBusy = true
Button "Bắt Đầu": DISABLED
  ↓
UpdateLobbyAsync(Started = "1")
  ↓
SendStartMatchSignalToClients() → NGO named message
  ↓
NotifyBattleStarted() (HOST)
```

### **Bước 4: CLIENT Nhận Start Signal**
```
CLIENT → HandleStartMatchMessageReceived()
  ↓
receivedStartSignalFromHost = true
  ↓
NotifyBattleStarted() (CLIENT)
```

### **Bước 5: Cả 2 Vào Battle**
```
HOST + CLIENT → InitializeMultiplayerBattleImmediate()
  ↓
Hide LobbyPanel
Show LoadingPanel
  ↓
NetworkedMathBattleManager.InitializeBattle(grade)
  ↓
Spawn NetworkedPlayerState × 2
  ↓
Generate first question (no timer)
  ↓
LoadingPanel: Check network + player states
  ↓
Navigate to GameplayPanel
```

### **Kết Quả:**
- ✅ Cả 2 đã vào GameplayPanel
- ✅ Player states đã spawn
- ✅ Câu hỏi đầu tiên đã generate

---

## Flow 4: Battle (HOST + CLIENT)

### **Điểm Bắt Đầu:** GameplayPanel

### **Bước 1: Countdown "3, 2, 1, Ready, GO!"**
```
UIMultiplayerBattleController.StartCountdown()
  ↓
Hide all UI (question, answers, timer)
  ↓
Show countdown text: "3" → "2" → "1" → "Ready" → "GO!"
  ↓
Duration: 5 seconds
```

### **Bước 2: Hiển thị Câu Hỏi**
```
After countdown:
  ↓
Show question text: "1 + 2 = ?"
Show 4 answer choices: [1, 3, 6, 2]
Show timer: "6s"
Show health bars: P1 (2/2), P2 (2/2)
  ↓
Start question timer (6s)
  ↓
Unlock drag-drop (isLocked = false)
```

### **Bước 3: Player Trả Lời**
```
Player → Kéo đáp án vào slot
  ↓
MultiplayerDragAndDrop.OnEndDrag()
  ↓
UIMultiplayerBattleController.OnAnswerDropped(answer)
  ↓
Lock drag-drop (isLocked = true)
  ↓
NetworkedMathBattleManager.SubmitAnswerServerRpc(answer)
  ↓
Server ghi nhận: playerAnswers[clientId] = (answer, timestamp)
```

### **Bước 4: Đánh Giá Đáp Án**

#### **Case 1: Cả 2 Đúng**
```
Server → EvaluateAnswers()
  ↓
P1: answer=3, correct=true, time=2549ms
P2: answer=3, correct=true, time=5181ms
  ↓
Winner: P1 (nhanh hơn)
  ↓
P1 +10 điểm, P2 +5 điểm (khuyến khích)
  ↓
Không mất máu
  ↓
Tăng difficulty +1
```

#### **Case 2: 1 Đúng, 1 Sai**
```
Winner: người đúng
  ↓
Winner +10 điểm
Loser -1 HP
  ↓
Reset difficulty về 1
```

#### **Case 3: Cả 2 Sai**
```
Winner: -1 (không có)
  ↓
Cả 2 -1 HP
  ↓
Reset difficulty về 1
```

### **Bước 5: Hiển thị Kết Quả (Summary Time)**
```
Server → ShowAnswerResultClientRpc()
  ↓
Cả 2 nhận kết quả
  ↓
AnswerSummaryUI:
  - State: "Summary Time"
  - Timer: "3s" → "0s"
  - Show đáp án đúng (màu xanh)
  - Show đáp án sai (màu đỏ)
  - Show text: "Người chơi 1: 3 (2.5490s)"
  - Show text: "Người chơi 2: 3 (5.1810s)"
  - Show result: "Cả 2 đều đúng! Người chơi 1 nhanh hơn!"
  ↓
Duration: 3 seconds
```

### **Bước 6: Câu Hỏi Tiếp Theo**
```
After summary time:
  ↓
Server → GenerateQuestion()
  ↓
Reset answer states
Generate new question: "2 + 16 = ?"
Generate choices: [22, 16, 20, 18]
  ↓
Broadcast to clients
  ↓
Unlock drag-drop
Start timer (6s)
  ↓
Lặp lại từ Bước 3
```

### **Kết Quả:**
- ✅ Battle đang diễn ra
- ✅ Điểm số và máu được cập nhật real-time
- ✅ Câu hỏi mới được generate sau mỗi round

---

## Flow 5: Kết Thúc Trận (Hết Máu)

### **Điểm Bắt Đầu:** GameplayPanel (1 player hết máu)

### **Bước 1: Detect Hết Máu**
```
Server → CheckMatchEnd()
  ↓
P1.CurrentHealth = 0 OR P2.CurrentHealth = 0
  ↓
MatchEnded = true
WinnerId = (player còn máu)
```

### **Bước 2: Stop Timer**
```
Server → EndMatch()
  ↓
Stop timerRoutine
Cancel all Invoke
  ↓
IsAbandoned = false (trận kết thúc bình thường)
```

### **Bước 3: Sync Firebase**
```
Server → SyncMatchResultToFirebase(winnerId)
  ↓
Update winner:
  - totalScore += score
  - gamesPlayed += 1
  - gamesWon += 1
  - winRate = gamesWon / gamesPlayed
  ↓
Update loser:
  - totalScore += score
  - gamesPlayed += 1
  - winRate = gamesWon / gamesPlayed
```

### **Bước 4: Notify Clients**
```
Server → ShowMatchResultClientRpc(winnerId, winnerHealth)
  ↓
Cả 2 nhận kết quả
  ↓
UIMultiplayerBattleController.HandleMatchEnded()
  ↓
Push data to UIWinsController.LastResult:
  - WinnerId, LocalPlayerId
  - WinnerName, WinnerScore, WinnerHealth
  - LoserName, LoserScore, LoserHealth
  - IsAbandoned = false
```

### **Bước 5: Navigate to WinsPanel**
```
Delay 2s
  ↓
matchEndNavigator.NavigateNow()
  ↓
Hide GameplayPanel
Show WinsPanel
  ↓
UIWinsController.DisplayMatchResult()
  ↓
Hiển thị:
  - Title: "CHIẾN THẮNG!" (winner) / "THUA CUỘC!" (loser)
  - Winner section: Tên, Điểm, Máu
  - Loser section: Tên, Điểm, Máu
  - Button "Tiếp Tục"
```

### **Kết Quả:**
- ✅ Trận đấu kết thúc
- ✅ Kết quả đã sync Firebase
- ✅ Cả 2 thấy WinsPanel

---

## Flow 6: Quit Giữa Trận (Forfeit)

### **Điểm Bắt Đầu:** GameplayPanel (đang battle)

### **Bước 1: Player Click Quit Button**
```
Player → Click button "Quit" (trong GameplayPanel)
  ↓
UI16ButtonActionHub.OnClickQuitRoom()
  ↓
Show UIBattleQuitConfirmPopup
  ↓
Popup text: "Bạn có chắc muốn rời trận không?
             Đối thủ sẽ được tính là chiến thắng.
             Thời gian trận vẫn tiếp tục chạy."
  ↓
Buttons: "Huỷ" | "Quit"
```

### **Bước 2: Player Xác Nhận Quit**
```
Player → Click "Quit"
  ↓
UIBattleQuitConfirmPopup.HandleConfirmQuit()
  ↓
NetworkedMathBattleManager.RequestForfeitServerRpc()
  ↓
Delay 1s (chờ server xử lý)
```

### **Bước 3: Server Xử Lý Forfeit**
```
Server → RequestForfeitServerRpc()
  ↓
Xác định:
  - forfeitPlayerId = (sender)
  - winnerId = (đối thủ)
  ↓
CancelInvoke() (cancel pending GenerateQuestion)
Stop timerRoutine
  ↓
Set IsAbandoned = true
Set AbandonedPlayerId = forfeitPlayerId
  ↓
Delay 0.5s (cho client đọc player states)
  ↓
EndMatchWithWinner(winnerId)
```

### **Bước 4: Người Quit Về Lobby**
```
Người quit → ExecuteQuitToLobby()
  ↓
CancelInvoke("NavigateToWinsPanel") (tránh navigate lại)
Hide GameplayPanel
Hide QuitPopup
  ↓
Show LobbyPanel (trước khi disconnect)
  ↓
RequestQuitRoom()
  ↓
LeaveLobbySafe()
Disconnect Relay
Reset state
  ↓
Status: "Mời tạo phòng để chơi."
```

### **Bước 5: Người Còn Lại Thắng**
```
Người còn lại → HandleMatchEnded()
  ↓
Push data to WinsController.LastResult:
  - IsAbandoned = true
  - AbandonedPlayerId = (người quit)
  ↓
Delay 2s
  ↓
Navigate to WinsPanel
  ↓
Hiển thị:
  - Title: "CHIẾN THẮNG!"
  - Winner: Tên, Điểm, Máu
  - Loser: Tên (Đã Rời Trận), Điểm, Máu
  - Button "Tiếp Tục"
```

### **Kết Quả:**
- ✅ Người quit đã về LobbyPanel
- ✅ Người còn lại thấy WinsPanel
- ✅ Không có crash
- ✅ Firebase không sync (IsAbandoned = true)

---

## Flow 7: Quay Về Lobby

### **Điểm Bắt Đầu:** WinsPanel

### **Bước 1: Player Click "Tiếp Tục"**
```
Player → Click button "Tiếp Tục"
  ↓
UIWinsController.NavigateBackToLobby()
  ↓
Hide WinsPanel
Show LoadingPanel (Simple mode)
  ↓
Text: "Đã hoàn thành trận đấu... Đang rời phòng"
Duration: 1.5s
```

### **Bước 2: Quit Room**
```
After loading:
  ↓
UIMultiplayerRoomController.RequestQuitRoom()
  ↓
Stop all routines (polling, etc.)
  ↓
LeaveLobbySafe() (nếu CLIENT)
  hoặc
UpdateLobbyAsync(Abandoned=true) (nếu HOST)
  ↓
Disconnect Relay
NetworkManager.Shutdown()
```

### **Bước 3: Reset State**
```
ResetRoomSessionState()
  ↓
currentLobby = null
isHost = false
battleStartNotified = false
receivedStartSignalFromHost = false
  ↓
HideAllBattlePanels()
  ↓
ResetWinsPanelState()
UIWinsController.LastResult.IsValid = false
  ↓
ResetBattleManagerState() (nếu HOST)
```

### **Bước 4: Hiển thị Lobby**
```
Show LobbyPanel
  ↓
Status: "Mời tạo phòng để chơi."
  ↓
Buttons:
  - "Tạo Phòng": ENABLED
  - "Tham Gia": ENABLED
  - "Xem DS Room": ENABLED
  ↓
Dropdown Grade: UNLOCKED
```

### **Kết Quả:**
- ✅ Player đã về LobbyPanel
- ✅ State đã reset sạch
- ✅ Sẵn sàng tạo/join phòng mới

---

## 🔧 Components Chính

### **NetworkedMathBattleManager**
- **Vai trò:** Server-authoritative battle logic
- **Chức năng:**
  - Generate câu hỏi
  - Đánh giá đáp án
  - Quản lý timer
  - Sync điểm số và máu
  - Detect kết thúc trận
  - Xử lý forfeit

### **UIMultiplayerRoomController**
- **Vai trò:** Quản lý lobby và room state
- **Chức năng:**
  - Tạo/join phòng
  - Polling lobby
  - Ready system
  - Start match
  - Quit room
  - Reset state

### **UIMultiplayerBattleController**
- **Vai trò:** UI controller cho battle
- **Chức năng:**
  - Hiển thị câu hỏi và đáp án
  - Countdown
  - Subscribe battle events
  - Handle answer result
  - Navigate to WinsPanel

### **UIWinsController**
- **Vai trò:** Hiển thị kết quả trận
- **Chức năng:**
  - Đọc data từ cache (LastResult)
  - Hiển thị winner/loser
  - Navigate back to lobby

### **RelayManager**
- **Vai trò:** Quản lý Unity Relay
- **Chức năng:**
  - Create relay allocation (HOST)
  - Join relay (CLIENT)
  - Disconnect

### **NetworkedPlayerState**
- **Vai trò:** Sync player data qua network
- **Chức năng:**
  - PlayerName (NetworkVariable)
  - Score (NetworkVariable)
  - CurrentHealth (NetworkVariable)
  - Answer state

---

## 🛡️ Safety Guards

### **1. Duplicate HandleMatchEnded**
**Vấn đề:** HOST nhận event 2 lần (local + ClientRpc)  
**Giải pháp:** Flag `hasHandledMatchEnd` trong `UIMultiplayerBattleController`

### **2. Client Navigate Lại Về WinsPanel**
**Vấn đề:** Client quit nhưng `Invoke(NavigateToWinsPanel)` vẫn fire  
**Giải pháp:** `CancelInvoke("NavigateToWinsPanel")` trong `ExecuteQuitToLobby()`

### **3. HOST Crash Khi Client Quit**
**Vấn đề:** `GenerateQuestion()` access destroyed `NetworkedPlayerState`  
**Giải pháp:**
- `CancelInvoke()` trong `RequestForfeitServerRpc()`
- Check `MatchEnded.Value` trong `GenerateQuestion()`
- Null-safe check `player1State.IsSpawned` trước `ResetAnswerState()`

### **4. Stale Lobby Data**
**Vấn đề:** Lobby snapshot có thể chứa `StartedKey=1` từ trận trước  
**Giải pháp:** Guard `receivedStartSignalFromHost` (CLIENT chỉ vào battle khi nhận NGO message)

---

## 📊 State Diagram

```
┌─────────────┐
│ LobbyPanel  │ ◄─── Entry point
└──────┬──────┘
       │
       ├─► Tạo Phòng (HOST) ──► Chờ CLIENT
       │                          │
       └─► Join Phòng (CLIENT) ───┘
                                  │
                                  ▼
                          ┌───────────────┐
                          │ LobbyPanel    │
                          │ (2/2 players) │
                          └───────┬───────┘
                                  │
                          CLIENT Ready
                          HOST Bắt Đầu
                                  │
                                  ▼
                          ┌───────────────┐
                          │ LoadingPanel  │
                          └───────┬───────┘
                                  │
                          Spawn player states
                          Generate question
                                  │
                                  ▼
                          ┌───────────────┐
                          │ GameplayPanel │◄──┐
                          │ (Battle)      │   │
                          └───────┬───────┘   │
                                  │           │
                          ┌───────┴───────┐   │
                          │               │   │
                    Hết máu          Quit │   │
                          │               │   │
                          ▼               ▼   │
                    ┌──────────┐   ┌─────────┴──┐
                    │WinsPanel │   │ LobbyPanel │
                    └────┬─────┘   └────────────┘
                         │
                    Tiếp Tục
                         │
                         ▼
                    ┌──────────┐
                    │LobbyPanel│
                    └──────────┘
```

---

## 🐛 Common Issues & Solutions

### **Issue 1: "Màn hình trắng khi quit"**
**Nguyên nhân:** Không show LobbyPanel trước khi disconnect  
**Giải pháp:** `roomController.Show()` trước `RequestQuitRoom()`

### **Issue 2: "WinsPanel không hiển thị tên/điểm"**
**Nguyên nhân:** UI references chưa gán trong Inspector  
**Giải pháp:** Kiểm tra Inspector → WinsPanel → UIWinsController → gán tất cả TMP_Text

### **Issue 3: "HOST crash khi CLIENT quit"**
**Nguyên nhân:** `GenerateQuestion()` access destroyed object  
**Giải pháp:** Đã fix (xem Safety Guards #3)

### **Issue 4: "Client tự vào battle khi chưa ready"**
**Nguyên nhân:** Stale lobby data có `StartedKey=1`  
**Giải pháp:** Guard `receivedStartSignalFromHost` (CLIENT chỉ vào khi nhận NGO message)

---

## 📝 Testing Checklist

### **Test 1: Happy Path**
- [ ] HOST tạo phòng thành công
- [ ] CLIENT join phòng thành công
- [ ] CLIENT click "Sẵn sàng" → HOST thấy button "Bắt Đầu" enabled
- [ ] HOST click "Bắt Đầu" → Cả 2 vào GameplayPanel
- [ ] Countdown "3, 2, 1, Ready, GO!" hiển thị đúng
- [ ] Câu hỏi và đáp án hiển thị đúng
- [ ] Cả 2 trả lời → kết quả hiển thị đúng
- [ ] Điểm số và máu cập nhật real-time
- [ ] 1 player hết máu → Cả 2 vào WinsPanel
- [ ] WinsPanel hiển thị đầy đủ tên/điểm/máu
- [ ] Click "Tiếp Tục" → Về LobbyPanel

### **Test 2: Forfeit Flow**
- [ ] Đang battle → Click Quit → Popup hiển thị
- [ ] Click "Huỷ" → Popup đóng, tiếp tục chơi
- [ ] Click "Quit" → Người quit về LobbyPanel
- [ ] Người còn lại vào WinsPanel
- [ ] WinsPanel hiển thị "(Đã Rời Trận)"
- [ ] HOST không crash

### **Test 3: Edge Cases**
- [ ] CLIENT disconnect đột ngột → HOST vào WinsPanel
- [ ] HOST quit → CLIENT về LobbyPanel
- [ ] Cả 2 trả lời sai → Cả 2 mất máu
- [ ] Cả 2 trả lời đúng → Người nhanh hơn +10, người chậm +5
- [ ] Timeout (không trả lời) → Mất máu

---

## 🔗 Related Files

### **Scripts**
- `Assets/Script/Script_multiplayer/1Code/Multiplay/NetworkedMathBattleManager.cs`
- `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerRoomController.cs`
- `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerBattleController.cs`
- `Assets/Script/Script_multiplayer/1Code/CODE/UIWinsController.cs`
- `Assets/Script/Script_multiplayer/1Code/Multiplay/UIBattleQuitConfirmPopup.cs`
- `Assets/Script/Script_multiplayer/1Code/CODE/RelayManager.cs`

### **Scenes**
- `Assets/Scenes/Test_FireBase_multi.unity`

### **Documentation**
- `.kiro/steering/scenes.md` - Scene structure
- `.kiro/steering/tech.md` - Tech stack
- `.kiro/steering/research.md` - Research guidelines

---

**Last Updated:** 2026-05-06  
**Version:** 1.0
