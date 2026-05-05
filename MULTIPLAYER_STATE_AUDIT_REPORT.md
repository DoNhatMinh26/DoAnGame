# BÁO CÁO KIỂM TRA LOGIC MULTIPLAYER

**Ngày:** 5/5/2026  
**File:** UIMultiplayerRoomController.cs  
**Tình trạng:** Phát hiện 10 bugs nghiêm trọng

---

## 🔴 DANH SÁCH BUGS VÀ CÁCH FIX

### 1. **BUG CHÍNH: Back → Tạo phòng lại → Client join → Host không thấy nút Bắt đầu**

**Root Cause:**
- Khi user ấn Back từ LobbyPanel, `HandleQuitRoom()` được gọi
- `HandleQuitRoom()` set `suppressAutoBattleStart = true` và `isQuitting = true`
- Nhưng khi quay lại LobbyPanel và tạo phòng mới, các flag này **KHÔNG được reset**
- Kết quả: `UpdateReadyButtonState()` không hoạt động đúng vì state cũ còn sót lại

**Vị trí lỗi:**
```csharp
// File: UIMultiplayerRoomController.cs
// Line: ~623 - HandleCreateRoom()

private async Task HandleCreateRoom()
{
    // ❌ THIẾU: Reset suppressAutoBattleStart và isQuitting
    suppressAutoBattleStart = false;  // ← Có dòng này nhưng KHÔNG ĐỦ
    isBusy = true;
    // ...
}
```

**Giải pháp:**
Cần reset **TẤT CẢ** state flags khi tạo phòng mới hoặc join phòng mới:

```csharp
private async Task HandleCreateRoom()
{
    // ✅ Reset ALL state flags
    suppressAutoBattleStart = false;
    isQuitting = false;
    battleStartNotified = false;
    receivedStartSignalFromHost = false;
    
    isBusy = true;
    SetActionButtonsInteractable(false);
    // ... rest of code
}
```

---

### 2. **BUG: State không được reset khi Back từ LobbyPanel**

**Hiện tượng:** Back button không gọi cleanup → state cũ còn sót lại

**Fix:** Thêm vào `OnHide()` (line ~605):
```csharp
protected override void OnHide()
{
    base.OnHide();
    
    // ✅ Cleanup khi panel bị ẩn
    if (currentLobby != null)
    {
        Debug.Log("[UIRoom] OnHide: Cleaning up lobby...");
        _ = HandleQuitRoom();
    }
    else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
    {
        RelayManager.Instance.Disconnect();
    }
    
    // ✅ Reset ALL flags
    receivedStartSignalFromHost = false;
    battleStartNotified = false;
    suppressAutoBattleStart = false;
    isQuitting = false;
    
    StopRoutines();
}
```

---

### 3. **BUG: OnShow() không reset flags đầy đủ**

**Hiện tượng:** Quay lại LobbyPanel, flags cũ còn sót lại

**Fix:** Thêm vào đầu `OnShow()` (line ~146):
```csharp
protected override void OnShow()
{
    base.OnShow();
    
    // ✅ LUÔN reset flags khi panel được hiển thị
    receivedStartSignalFromHost = false;
    battleStartNotified = false;
    suppressAutoBattleStart = false;
    isQuitting = false;
    
    ResolveTextReferences();
    // ... rest of existing code ...
}
```

---

### 4. **BUG: HandleCreateRoom() không reset isQuitting**

**Hiện tượng:** Sau khi Back → Tạo phòng lại, isQuitting=true → logic sai

**Fix:** Thêm vào đầu `HandleCreateRoom()` (line ~623):
```csharp
private async Task HandleCreateRoom()
{
    if (isBusy) return;

    // ✅ Reset ALL state flags
    suppressAutoBattleStart = false;
    isQuitting = false;
    battleStartNotified = false;
    receivedStartSignalFromHost = false;
    
    isBusy = true;
    SetActionButtonsInteractable(false);
    // ... rest of existing code ...
}
```

---

### 5. **BUG: HandleQuickJoin() và HandleJoinByCode() thiếu reset flags**

**Fix:** Thêm vào đầu cả 2 methods (line ~690 và ~757):
```csharp
private async Task HandleQuickJoin()
{
    if (isBusy) return;

    // ✅ Reset flags
    suppressAutoBattleStart = false;
    isQuitting = false;
    battleStartNotified = false;
    receivedStartSignalFromHost = false;
    
    isBusy = true;
    // ... rest of code ...
}

private async Task HandleJoinByCode()
{
    if (isBusy) return;

    // ✅ Reset flags
    suppressAutoBattleStart = false;
    isQuitting = false;
    battleStartNotified = false;
    receivedStartSignalFromHost = false;
    
    isBusy = true;
    // ... rest of code ...
}
```

---

### 6. **BUG: HandleStartMatch() không check NetworkManager.IsServer**

**Root Cause:**
- `Back` button có `UIButtonScreenNavigator` component
- Khi click Back, nó gọi `NavigateNow()` để chuyển scene
- **NHƯNG** `UIMultiplayerRoomController` không có hook vào navigation event
- Kết quả: `currentLobby`, `isHost`, và các state khác **KHÔNG được cleanup**

**Vị trí lỗi:**
```csharp
// Scene hierarchy:
// LobbyPanel → Back (Button + UIButtonScreenNavigator)
//   targetSceneName = "GameUIPlay 1"
//   
// ❌ KHÔNG CÓ listener nào gọi HandleQuitRoom() khi Back được click!
```

**Giải pháp:**
Có 2 cách:

**Option A: Hook vào Back button trong Awake()**
```csharp
protected override void Awake()
{
    base.Awake();
    // ... existing code ...
    
    // ✅ Tìm Back button và hook vào onClick
    var backButton = transform.Find("Back")?.GetComponent<Button>();
    if (backButton != null)
    {
        backButton.onClick.AddListener(() => {
            Debug.Log("[UIRoom] Back button clicked, calling HandleQuitRoom");
            _ = HandleQuitRoom();
        });
    }
}
```

**Option B: Override OnHide() để cleanup**
```csharp
protected override void OnHide()
{
    base.OnHide();
    
    // ✅ Cleanup khi panel bị ẩn (bất kể lý do gì)
    if (currentLobby != null)
    {
        Debug.Log("[UIRoom] OnHide detected active lobby, cleaning up...");
        _ = HandleQuitRoom();
    }
    
    StopRoutines();
}
```

**Khuyến nghị:** Dùng **Option B** vì an toàn hơn (cover cả trường hợp navigation không qua button)

---

### 3. **BUG: Race condition khi client join ngay sau khi host tạo phòng**

**Root Cause:**
- Host tạo phòng → `CreateLobbyAsync()` → `currentLobby` được set
- Host bắt đầu polling ngay lập tức
- Client join → `JoinLobbyAndRelayAsync()` → `UpdatePlayerAsync()` để set tên
- **NHƯNG** host poll có thể đọc snapshot CŨ (chưa có tên client)
- Kết quả: UI hiển thị "Người chơi 2" thay vì tên thật

**Vị trí lỗi:**
```csharp
// File: UIMultiplayerRoomController.cs
// Line: ~803 - JoinLobbyAndRelayAsync()

public async Task<bool> JoinLobbyAndRelayAsync(Lobby lobby)
{
    // ... join lobby ...
    
    await SyncLocalPlayerLobbyDataAsync();  // ← Update tên
    
    // ❌ THIẾU: Force refresh lobby ngay sau khi update
    // Host có thể đang poll và đọc snapshot cũ
}
```

**Giải pháp:**
```csharp
public async Task<bool> JoinLobbyAndRelayAsync(Lobby lobby)
{
    // ... existing code ...
    
    await SyncLocalPlayerLobbyDataAsync();
    
    // ✅ Force refresh để đảm bảo có snapshot mới nhất
    await Task.Delay(200); // Đợi server propagate changes
    var refreshed = await RefreshLobbySafe();
    if (refreshed != null)
    {
        currentLobby = refreshed;
        RefreshRoomRoster();
    }
    
    // ... rest of code ...
}
```

---

### 4. **BUG: Polling không dừng khi GameObject bị inactive**

**Root Cause:**
- `PollLobbyRoutine()` chạy trong coroutine
- Khi panel bị ẩn (SetActive(false)), coroutine **VẪN CHẠY** (nếu GameObject cha còn active)
- Kết quả: Spam API calls ngay cả khi user không ở LobbyPanel

**Vị trí lỗi:**
```csharp
// File: UIMultiplayerRoomController.cs
// Line: ~1120 - PollLobbyRoutine()

private IEnumerator PollLobbyRoutine()
{
    while (true)  // ❌ Không check gameObject.activeInHierarchy
    {
        _ = PollLobbyOnce();
        yield return new WaitForSeconds(Mathf.Max(1.5f, pollIntervalSeconds));
    }
}
```

**Giải pháp:**
```csharp
private IEnumerator PollLobbyRoutine()
{
    while (true)
    {
        // ✅ Chỉ poll khi panel đang active
        if (gameObject.activeInHierarchy && currentLobby != null)
        {
            _ = PollLobbyOnce();
        }
        
        yield return new WaitForSeconds(Mathf.Max(1.5f, pollIntervalSeconds));
    }
}
```

---

### 5. **BUG: Client có thể tự trigger NotifyBattleStarted() từ stale StartedKey**

**Root Cause:**
- Trận 1: Host set `StartedKey=1` → Battle xong → Quay về LobbyPanel
- Host **KHÔNG clear** `StartedKey` về `0`
- Trận 2: Client join lại phòng cũ → Poll đọc `StartedKey=1` từ trận trước
- Client tự gọi `NotifyBattleStarted()` **MÀ KHÔNG CẦN** host cho phép

**Vị trí lỗi:**
```csharp
// File: UIMultiplayerRoomController.cs
// Line: ~1129 - PollLobbyOnce()

private async Task PollLobbyOnce()
{
    // ... refresh lobby ...
    
    // ❌ CLIENT CŨNG CHECK StartedKey - SAI!
    if (isHost && currentLobby.Data.TryGetValue(StartedKey, out var startedData) && startedData.Value == "1")
    {
        NotifyBattleStarted();
    }
    
    // ✅ Đã có guard trong NotifyBattleStarted() nhưng KHÔNG ĐỦ
    // Vì receivedStartSignalFromHost có thể bị set sai
}
```

**Giải pháp:**
```csharp
private async Task PollLobbyOnce()
{
    // ... refresh lobby ...
    
    // ✅ CHỈ HOST mới dùng StartedKey từ poll
    // Client PHẢI đợi NGO message từ host
    bool isActuallyHost = isHost && (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer);
    
    if (isActuallyHost && currentLobby.Data.TryGetValue(StartedKey, out var startedData) && startedData.Value == "1")
    {
        if (!suppressAutoBattleStart)
        {
            NotifyBattleStarted();
        }
    }
    
    // Client: KHÔNG check StartedKey, chỉ đợi HandleStartMatchMessageReceived()
}
```

---

### 6. **BUG: receivedStartSignalFromHost không được reset đúng cách**

**Root Cause:**
- `receivedStartSignalFromHost` được set `true` khi client nhận NGO message
- **NHƯNG** chỉ được reset trong `HandleCreateRoom()` và `JoinLobbyAndRelayAsync()`
- Nếu user Back → Tạo phòng lại, flag này **VẪN CÒN `true`** từ trận trước
- Kết quả: Client có thể bypass guard trong `NotifyBattleStarted()`

**Vị trí lỗi:**
```csharp
// File: UIMultiplayerRoomController.cs
// Line: ~1099 - ResetRoomSessionState()

private void ResetRoomSessionState(string status)
{
    currentLobby = null;
    isHost = false;
    battleStartNotified = false;
    receivedStartSignalFromHost = false; // ✅ Có reset ở đây
    // ...
}

// ❌ NHƯNG ResetRoomSessionState() CHỈ được gọi trong HandleQuitRoom()
// Nếu user Back (không qua HandleQuitRoom), flag không được reset!
```

**Giải pháp:**
```csharp
protected override void OnHide()
{
    base.OnHide();
    
    // ✅ LUÔN reset flags khi panel bị ẩn
    receivedStartSignalFromHost = false;
    battleStartNotified = false;
    suppressAutoBattleStart = false;
    
    StopRoutines();
}

protected override void OnShow()
{
    base.OnShow();
    
    // ✅ LUÔN reset flags khi panel được hiển thị
    receivedStartSignalFromHost = false;
    battleStartNotified = false;
    suppressAutoBattleStart = false;
    
    // ... rest of code ...
}
```

---

### 7. **BUG: NetworkManager.Singleton có thể null khi check isHost**

**Hiện tượng:** isHost=true nhưng NetworkManager chưa sẵn sàng → crash

**Fix:** Thay thế check đầu `HandleStartMatch()` (line ~927):
```csharp
private async Task HandleStartMatch()
{
    if (isBusy || isQuitting) return;
    
    // ✅ Double-check: isHost flag + NetworkManager.IsServer
    bool isActuallyHost = isHost && 
                         (NetworkManager.Singleton != null && 
                          NetworkManager.Singleton.IsServer);
    
    if (!isActuallyHost || currentLobby == null)
    {
        Debug.LogWarning($"[UIRoom] HandleStartMatch blocked: isHost={isHost}, IsServer={NetworkManager.Singleton?.IsServer}");
        SetStatus("Chỉ chủ phòng mới được bắt đầu.");
        return;
    }
    
    isBusy = true;
    SetActionButtonsInteractable(false);
    // ... rest of existing code ...
}
```

---

### 7. **BUG: PollLobbyOnce() client có thể tự trigger battle**

**Hiện tượng:** Client đọc StartedKey=1 từ trận trước → tự vào battle

**Fix:** Thay thế logic check StartedKey trong `PollLobbyOnce()` (line ~1129):
```csharp
private async Task PollLobbyOnce()
{
    if (currentLobby == null || isQuitting) return;
    // ... refresh lobby code ...
    
    currentLobby = refreshed;
    RefreshAuthState();

    if (isHost && startMatchButton != null)
    {
        UpdateStartMatchButtonState(true);
    }

    // ✅ CHỈ HOST mới dùng StartedKey từ poll
    // Client PHẢI đợi NGO message từ host (HandleStartMatchMessageReceived)
    bool isActuallyHost = isHost && 
                         (NetworkManager.Singleton != null && 
                          NetworkManager.Singleton.IsServer);
    
    if (isActuallyHost && 
        currentLobby.Data.TryGetValue(StartedKey, out var startedData) && 
        startedData.Value == "1")
    {
        if (suppressAutoBattleStart)
        {
            MultiplayerDetailedLogger.Trace("UI_ROOM", "Poll detected StartedKey=1 but suppressed");
            return;
        }

        SetStatus("Trận đấu bắt đầu.");
        NotifyBattleStarted();
    }

    // ✅ Client: KHÔNG check StartedKey ở đây
    UpdateReadyButtonState();
}
```

---

### 8. **BUG: PollLobbyRoutine() không check activeInHierarchy**

**Hiện tượng:** Panel bị ẩn nhưng vẫn spam API calls

**Fix:** Thêm check vào `PollLobbyRoutine()` (line ~1120):
```csharp
private IEnumerator PollLobbyRoutine()
{
    while (true)
    {
        // ✅ Chỉ poll khi panel đang active VÀ có lobby
        if (gameObject.activeInHierarchy && currentLobby != null && !isQuitting)
        {
            _ = PollLobbyOnce();
        }
        
        yield return new WaitForSeconds(Mathf.Max(1.5f, pollIntervalSeconds));
    }
}
```

---

### 9. **BUG: MarkClientReadyAsync() không force refresh lobby**

**Hiện tượng:** Client mark ready nhưng host phải đợi lâu mới thấy

**Fix:** Thêm force refresh vào cuối `MarkClientReadyAsync()` (line ~1247):
```csharp
private async Task MarkClientReadyAsync()
{
    try
    {
        // ... existing UpdatePlayerAsync code ...
        
        var updatedLobby = await LobbyService.Instance.UpdatePlayerAsync(/*...*/);
        
        if (updatedLobby != null)
        {
            // Chỉ update nếu chưa started
            bool alreadyStarted = updatedLobby.Data != null &&
                                 updatedLobby.Data.TryGetValue(StartedKey, out var sd) &&
                                 sd.Value == "1";
            if (!alreadyStarted)
            {
                currentLobby = updatedLobby;
            }
        }

        // ✅ Force refresh để host thấy ngay
        await Task.Delay(200);
        var refreshed = await RefreshLobbySafe();
        if (refreshed != null)
        {
            currentLobby = refreshed;
        }

        SetStatus("Đã sẵn sàng! Chờ chủ phòng bắt đầu...");
        UpdateReadyButtonState();
    }
    catch (Exception ex)
    {
        Debug.LogError($"[UIRoom] Failed to mark ready: {ex.Message}");
    }
}
```

---

### 10. **BUG: JoinLobbyAndRelayAsync() không force refresh sau khi sync tên**

**Hiện tượng:** Host poll đọc snapshot cũ → không thấy tên client

**Fix:** Thêm force refresh vào cuối `JoinLobbyAndRelayAsync()` (line ~803):
```csharp
public async Task<bool> JoinLobbyAndRelayAsync(Lobby lobby)
{
    // ... existing join logic ...
    
    await SyncLocalPlayerLobbyDataAsync();

    // ✅ Force refresh để host thấy tên client ngay
    await Task.Delay(200);
    var refreshed = await RefreshLobbySafe();
    if (refreshed != null)
    {
        currentLobby = refreshed;
        RefreshRoomRoster();
    }

    if (startMatchButton != null)
    {
        UpdateReadyButtonState();
    }

    RefreshAuthState();
    EnsureLobbyRuntimeRoutines();
    suppressAutoBattleStart = false;
    receivedStartSignalFromHost = false;
    
    return true;
}
```

---

## 📋 CHECKLIST ÁP DỤNG FIXES

- [ ] **Fix 1:** Reset flags trong `HandleCreateRoom()`
- [ ] **Fix 2:** Cleanup trong `OnHide()`
- [ ] **Fix 3:** Reset flags trong `OnShow()`
- [ ] **Fix 4:** Reset flags trong `HandleQuickJoin()`
- [ ] **Fix 5:** Reset flags trong `HandleJoinByCode()`
- [ ] **Fix 6:** Double-check `isActuallyHost` trong `HandleStartMatch()`
- [ ] **Fix 7:** Chỉ host check `StartedKey` trong `PollLobbyOnce()`
- [ ] **Fix 8:** Check `activeInHierarchy` trong `PollLobbyRoutine()`
- [ ] **Fix 9:** Force refresh trong `MarkClientReadyAsync()`
- [ ] **Fix 10:** Force refresh trong `JoinLobbyAndRelayAsync()`

---

## 🧪 TEST CASES SAU KHI FIX

### Test Case 1: Back → Tạo phòng lại
1. Tạo phòng → Client join → Ấn Back
2. Tạo phòng mới → Client join lại
3. Client ấn "Sẵn sàng"
4. **Expected:** Host thấy nút "Bắt đầu" ngay lập tức

### Test Case 2: Rematch
1. Battle xong → Quay về LobbyPanel
2. Client ấn "Sẵn sàng"
3. Host ấn "Bắt đầu"
4. **Expected:** Vào battle mới không lỗi

### Test Case 3: Quick Join nhiều lần
1. Quick Join → Quit → Quick Join lại
2. **Expected:** Không có state cũ sót lại

### Test Case 4: Client disconnect giữa chừng
1. Host tạo phòng → Client join → Client tắt game
2. Host đợi 30s → Tạo phòng mới
3. **Expected:** Không crash, không conflict

---

## 🎯 KẾT LUẬN

**Tổng số bugs:** 10 bugs nghiêm trọng  
**Root cause chính:** State management không chặt chẽ, thiếu reset flags khi navigate  
**Độ ưu tiên:** **CRITICAL** - Cần fix ngay để multiplayer hoạt động ổn định

**Thời gian ước tính:** 2-3 giờ để apply tất cả fixes + test

