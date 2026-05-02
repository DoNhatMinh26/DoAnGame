# Research & Problem Solving Guidelines

## Nguyên tắc chung
Trước khi đưa ra giải pháp, **luôn**:
1. Đọc file liên quan trước khi sửa
2. Tìm kiếm thông tin mới nhất nếu liên quan đến API bên ngoài
3. Compile check sau mỗi thay đổi
4. Không đoán mò — đọc code thực tế

---

## Khi nào phải tìm kiếm trên mạng

### Unity Lobby / Relay / Authentication
```
Unity Lobby Service UpdatePlayerAsync vs UpdateLobbyAsync permissions
Unity Lobby player data visibility options Member vs Public
Unity Relay client join authority error fix
Unity Lobby stale snapshot StartedKey
Unity Services Lobby rate limit handling
Unity Lobby QueryLobbiesAsync filters
```

### Unity Netcode for GameObjects (NGO)
```
Unity NGO NetworkVariable sync client host 1.12
Unity Netcode ServerRpc RequireOwnership false
Unity NGO ClientRpc send to specific client
Unity NGO NetworkObject spawn timing OnNetworkSpawn
Unity NGO player state not found race condition
Unity Netcode FastBufferReader writer named message
```

### Firebase Unity SDK
```
Firebase Unity Auth token refresh
Firebase Realtime Database rules read write
Firebase Unity SDK 13.x breaking changes
Firebase GetValueAsync null check Unity
```

### UI / Button / Panel
```
Unity TMP_Text SetText vs text property
Unity Button interactable disabled color
Unity UI Canvas RaycastTarget blocking clicks
Unity UI SetActive vs CanvasGroup alpha
Unity EventSystem click not detected
Unity UI panel show hide best practice
```

### Crash / NullReference
```
Unity NullReferenceException [ComponentName] on client
Unity multiplayer lobby snapshot stale data
Unity NGO object not spawned yet ServerRpc
Unity coroutine on inactive gameobject
```

---

## Quy trình xử lý vấn đề

```
1. Đọc lỗi console → xác định component/API
2. Đọc file liên quan (readCode / readFile)
3. Tìm kiếm web nếu cần (remote_web_search)
4. Áp dụng giải pháp
5. dotnet build → kiểm tra 0 errors
6. Tạo 1 .md duy nhất cho setup nếu có mới.
7. Báo cáo kết quả
```

---

## Nguồn tài liệu ưu tiên

| Nguồn | URL | Dùng cho |
|---|---|---|
| Unity Lobby Docs | https://docs.unity.com/ugs/en-us/manual/lobby/manual | Lobby API |
| Unity Relay Docs | https://docs.unity.com/ugs/en-us/manual/relay/manual | Relay API |
| NGO Docs | https://docs-multiplayer.unity3d.com/netcode/current/about | Netcode |
| Firebase Unity | https://firebase.google.com/docs/unity/setup | Firebase |
| Unity Forum | https://forum.unity.com | Bug reports |
| Unity Changelog | https://unity.com/releases/editor/whats-new | Version changes |

---

## Kiến thức dự án — Unity Lobby Permissions

| Method | Ai gọi được | Dùng để |
|---|---|---|
| `UpdateLobbyAsync` | **Chỉ host** | Cập nhật Lobby Data (StartedKey, JoinCode, Grade...) |
| `UpdatePlayerAsync` | **Mỗi player tự update mình** | Cập nhật Player Data (ready state, character name...) |
| `LobbyService.Instance.GetLobbyAsync` | Bất kỳ member | Lấy snapshot mới nhất |
| `QueryLobbiesAsync` | Bất kỳ ai | Tìm kiếm lobby công khai |

**Lobby Data** (`DataObject`) → Host quản lý, dùng `UpdateLobbyAsync`
**Player Data** (`PlayerDataObject`) → Mỗi player tự quản lý, dùng `UpdatePlayerAsync`

---

## Kiến thức dự án — Stale Lobby Snapshot

- Lobby snapshot từ API **có thể chứa data cũ** (ví dụ `StartedKey=1` từ trận trước)
- Sau khi `UpdatePlayerAsync`, snapshot trả về có thể vẫn còn `StartedKey=1`
- **Luôn guard** trước khi trigger `NotifyBattleStarted()` từ poll
- Dùng flag `suppressNextPollBattleStart` để bỏ qua 1 cycle sau khi client mark ready

---

## Kiến thức dự án — NGO Spawn Timing

- `NetworkedPlayerState` chỉ spawn sau `InitializeBattle()`, **không phải** khi join lobby
- Không gửi `ServerRpc` trong `OnNetworkSpawn()` nếu target object chưa spawn
- Client gửi tên trong `InitializeBattle()`, không phải `OnNetworkSpawn()`

---

## Kiến thức dự án — UI Panel System

### BasePanelController
- Tất cả UI panel kế thừa từ `BasePanelController`
- Dùng `Show()` / `Hide()` để hiển thị/ẩn panel — **không dùng `SetActive` trực tiếp**
- Override `OnShow()` / `OnHide()` để xử lý logic khi panel hiện/ẩn
- `UIScreenRouter` quản lý navigation giữa các panel

### Button Logic
- Dùng `button.interactable = false` để disable button (giữ nguyên visible)
- Dùng `button.gameObject.SetActive(false)` để ẩn hoàn toàn
- **Không ẩn/hiện button liên tục** trong polling loop — chỉ update `interactable` và text
- Text của button lấy qua `button.GetComponentInChildren<TMP_Text>()`

### TMP_Text
- Dùng `text.SetText("...")` thay vì `text.text = "..."` (tránh allocation)
- Kiểm tra null trước khi gọi: `text?.SetText(...)`

### Canvas / Raycast
- `Image` component có `Raycast Target` — tắt nếu không cần click để tránh block
- `CanvasGroup.blocksRaycasts = false` để disable toàn bộ panel
- Kiểm tra `EventSystem.current` trước khi xử lý input

---

## Kiến thức dự án — Ready System (UIMultiplayerRoomController)

### Pattern: 2 button riêng biệt (ĐÚNG — không dùng 1 button chung)
- `startMatchButton` → **chỉ hiển thị cho HOST** — bắt đầu trận
- `readyButton` → **chỉ hiển thị cho CLIENT** — báo sẵn sàng, KHÔNG chuyển UI

```
isHost = true  → startMatchButton.SetActive(true),  readyButton.SetActive(false)
isHost = false → startMatchButton.SetActive(false), readyButton.SetActive(true)
```

**Tại sao không dùng 1 button chung:**
- 1 button chung → logic phức tạp, dễ nhầm lẫn host/client
- Client click "Sẵn sàng" có thể vô tình trigger battle start
- 2 button riêng → mỗi button chỉ làm 1 việc, không thể nhầm

**Inspector setup:**
- Gán `startMatchButton` → Button "Bắt đầu" (chỉ host thấy)
- Gán `readyButton` → Button "Sẵn sàng" (chỉ client thấy)
- Cả 2 đều bắt đầu ở `SetActive(false)`, code tự show/hide theo `isHost`

### Client Guard: receivedStartSignalFromHost
**Vấn đề:** Client có thể tự trigger `NotifyBattleStarted()` từ polling hoặc stale data, dẫn đến crash khi chưa được host cho phép.

**Giải pháp:** Thêm flag `receivedStartSignalFromHost` (client-only):
- `false` khi join phòng, tạo phòng, hoặc rời phòng
- `true` chỉ khi `HandleStartMatchMessageReceived()` nhận NGO message từ host
- `NotifyBattleStarted()` có guard: nếu `!isHost && !receivedStartSignalFromHost` → return early

**Code pattern:**
```csharp
// In NotifyBattleStarted()
if (!isHost && !receivedStartSignalFromHost)
{
    Debug.LogWarning("[UIRoom] CLIENT chưa nhận start signal từ host!");
    return;
}

// In HandleStartMatchMessageReceived()
receivedStartSignalFromHost = true;
NotifyBattleStarted();

// Reset khi join/create/quit
receivedStartSignalFromHost = false;
```

**Tại sao cần guard này:**
- `PollLobbyOnce()` đã có `if (isHost && ...)` guard, nhưng không đủ
- ParrelSync (multi-client testing) có thể gây race condition
- Stale lobby snapshot có thể có `StartedKey=1` từ trận trước
- Client PHẢI chỉ vào battle khi nhận NGO message từ host, không được tự trigger

### Flow
```
Client join lobby
  → receivedStartSignalFromHost = false
  → readyButton hiển thị: "Sẵn sàng" (enabled)
  → Click → HandleReadyButtonClick() → MarkClientReadyAsync()
  → UpdatePlayerAsync(Player2ReadyKey = "1")
  → Button: "Đã sẵn sàng ✓" (disabled)
  → Status: "Đã sẵn sàng! Chờ chủ phòng bắt đầu..."

Host poll lobby
  → IsClientReady() đọc player.Data[Player2ReadyKey]
  → startMatchButton: "Bắt đầu" (enabled)
  → Status: "Người chơi đã sẵn sàng! Có thể bắt đầu."
  → Click → HandleStartMatch() → UpdateLobbyAsync(StartedKey = "1")
  → SendStartMatchSignalToClients() → NGO message to client
  → NotifyBattleStarted() (host can proceed immediately)

Client receives NGO message
  → HandleStartMatchMessageReceived()
  → receivedStartSignalFromHost = true
  → NotifyBattleStarted() (guard passes)
  → InitializeMultiplayerBattleImmediate()
  → Navigate to GameplayPanel
```

### Constants
```csharp
private const string Player2ReadyKey = "Player2Ready";  // Player Data key
private const string StartedKey = "Started";             // Lobby Data key
private const string JoinCodeKey = "JoinCode";           // Lobby Data key
private const string GradeKey = "Grade";                 // Lobby Data key
```

### Permissions
- `Player2ReadyKey` → lưu trong **Player Data** (`PlayerDataObject`) → client tự update
- `StartedKey` → lưu trong **Lobby Data** (`DataObject`) → chỉ host update

---

## Kiến thức dự án — Polling

### UIMultiplayerRoomController
- Poll interval: `pollIntervalSeconds` (default 1.5s)
- `PollLobbyOnce()` → refresh lobby → `RefreshAuthState()` → `RefreshRoomRoster()` → `UpdateReadyButtonState()`
- Guard: `suppressNextPollBattleStart` để tránh trigger battle sớm

### UILobbyBrowserController
- Auto-refresh interval: `autoRefreshIntervalSeconds` (default 2s)
- `AutoRefreshRoutine()` chạy khi panel visible, dừng khi ẩn
- Dùng để host thấy client join phòng mà không cần click Refresh

---

## Kiến thức dự án — Battle Flow

```
Host tạo phòng → CreateRoom → Relay allocation → JoinCode lưu vào Lobby Data
Client join → JoinLobbyAndRelayAsync → TryJoinRelay(joinCode)
Cả 2 vào UIMultiplayerRoomController
Client mark ready → UpdatePlayerAsync
Host thấy ready → click Bắt đầu → UpdateLobbyAsync(StartedKey=1)
Host → NotifyBattleStarted() → InitializeMultiplayerBattleImmediate()
Client → Poll detect StartedKey=1 → NotifyBattleStarted()
NetworkedMathBattleManager.InitializeBattle(grade) → SpawnPlayerStates()
```

---

## Kiến thức dự án — AnswerSummaryUI & Timer

### Timer phases
- **Question Time (10s):** TimerText "10s"→"0s", TrangThaiText "Thời gian trả lời câu hỏi"
- **Summary Time (3s):** TimerText "3s"→"0s", TrangThaiText "Thời gian thống kê đáp án"
- TextTrangThaiDapAn1/2: **HIDDEN** trong Question Time, **VISIBLE** trong Summary Time

### Display format
```
TextTrangThaiDapAn1: "Đáp án người chơi 1 chọn là: 32 (3.2450s)"
```

### Scoring khi cả 2 đúng
- Winner (nhanh hơn): +10 điểm
- Loser (chậm hơn): +5 điểm (khuyến khích)
- Không mất HP

---

## Kiến thức dự án — Multiplayer Health UI

### Inspector assignment
- `MultiplayerHealthUI → Timer Text → Timertext` (hiển thị "10s", "9s"...)
- `AnswerSummaryUI → Timer Text → Timertext` (shared)
- `AnswerSummaryUI → Trang Thai Text → TimerState` (hiển thị trạng thái)
- Timer Text trong MultiplayerHealthUI là **optional** (có thể để None)

---

## Kiến thức dự án — Player Names

- Host (Player 1): lấy tên từ `AuthManager.GetCharacterName()` trong `SpawnPlayerStates()`
- Client (Player 2): gửi tên qua `UpdatePlayerNameServerRpc()` trong `InitializeBattle()`
- `NetworkedPlayerState.PlayerName` là `NetworkVariable<FixedString64Bytes>` — tự sync
- `MultiplayerHealthUI` subscribe `PlayerName.OnValueChanged` để update UI

---

## Anti-patterns cần tránh

| Tránh | Dùng thay thế |
|---|---|
| `SetActive(false/true)` trực tiếp trên panel | `Show()` / `Hide()` từ BasePanelController |
| `UpdateLobbyAsync` từ client | `UpdatePlayerAsync` cho player data |
| `ServerRpc` trong `OnNetworkSpawn()` | Gọi trong `InitializeBattle()` |
| Ẩn/hiện button trong polling loop | Chỉ update `interactable` và text |
| `.Result` hoặc `.Wait()` trên Task | `await` |
| `text.text = "..."` | `text.SetText("...")` |
| `FindObjectOfType` trong Update/polling | Cache reference trong Awake/Start |
| Ghi đè `currentLobby` với snapshot có `StartedKey=1` | Guard check trước khi assign |

---

## Defensive Coding — Ngăn chặn crash

### Nguyên tắc bắt buộc
**Mọi** operation có thể fail đều phải được bọc trong try/catch và hiển thị thông báo lỗi thân thiện thay vì crash.

---

### 1. Button Click Handler
Mọi button click gọi async method phải có try/catch:

```csharp
// ❌ SAI — crash nếu có exception
button.onClick.AddListener(() => _ = HandleSomeAction());

// ✅ ĐÚNG — bọc trong try/catch, hiển thị lỗi
button.onClick.AddListener(() => _ = SafeHandleAction());

private async Task SafeHandleAction()
{
    try
    {
        // logic
    }
    catch (Exception ex)
    {
        Debug.LogError($"[UIRoom] Lỗi: {ex.Message}");
        SetStatus("Có lỗi xảy ra. Vui lòng thử lại.");
    }
    finally
    {
        isBusy = false;
        SetActionButtonsInteractable(true);
    }
}
```

---

### 2. Firebase / Lobby / Relay calls
Mọi call đến service bên ngoài phải có try/catch riêng:

```csharp
// ✅ Pattern chuẩn cho Firebase/Lobby/Relay
try
{
    var result = await SomeServiceCall();
    // xử lý result
}
catch (LobbyServiceException ex)
{
    // Lỗi Lobby cụ thể
    SetStatus($"Lỗi phòng chơi: {ex.Message}");
    Debug.LogWarning($"[Lobby] {ex.Reason}: {ex.Message}");
}
catch (Exception ex)
{
    // Lỗi chung
    SetStatus("Không thể kết nối. Kiểm tra mạng và thử lại.");
    Debug.LogError($"[Service] Unexpected error: {ex.Message}");
}
```

---

### 3. Null check trước khi dùng
Luôn null-check các reference có thể null:

```csharp
// ❌ SAI
AuthManager.Instance.GetCurrentUser().Email

// ✅ ĐÚNG
var user = AuthManager.Instance?.GetCurrentUser();
if (user == null)
{
    SetStatus("Chưa đăng nhập.");
    return;
}
string email = user.Email ?? "Không có email";
```

---

### 4. Navigation / Panel chuyển màn
Trước khi navigate, luôn kiểm tra target tồn tại:

```csharp
// ❌ SAI — crash nếu navigator null
startBattleNavigator.NavigateNow();

// ✅ ĐÚNG
if (startBattleNavigator != null)
{
    startBattleNavigator.NavigateNow();
}
else
{
    Debug.LogWarning("[UIRoom] startBattleNavigator chưa được gán trong Inspector!");
    SetStatus("Lỗi điều hướng. Liên hệ hỗ trợ.");
}
```

---

### 5. Input validation (Login / Register)
Validate input trước khi gửi lên server:

```csharp
// ✅ Pattern chuẩn
private bool ValidateLoginInput(string email, string password)
{
    if (string.IsNullOrWhiteSpace(email))
    {
        SetStatus("Vui lòng nhập email.");
        return false;
    }
    if (!email.Contains("@"))
    {
        SetStatus("Email không hợp lệ.");
        return false;
    }
    if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
    {
        SetStatus("Mật khẩu phải có ít nhất 6 ký tự.");
        return false;
    }
    return true;
}
```

---

### 6. isBusy guard — tránh double-click crash
Mọi button action tốn thời gian phải có isBusy guard:

```csharp
private async Task HandleSomeAction()
{
    if (isBusy) return;  // ← tránh double-click
    isBusy = true;
    SetActionButtonsInteractable(false);

    try
    {
        // logic
    }
    catch (Exception ex)
    {
        SetStatus("Có lỗi xảy ra.");
        Debug.LogError(ex.Message);
    }
    finally
    {
        isBusy = false;                    // ← luôn unlock dù có lỗi
        SetActionButtonsInteractable(true);
    }
}
```

---

### 7. Coroutine trên inactive GameObject
Coroutine crash nếu GameObject bị inactive:

```csharp
// ❌ SAI — crash nếu gameObject inactive
StartCoroutine(SomeRoutine());

// ✅ ĐÚNG
if (isActiveAndEnabled && gameObject.activeInHierarchy)
{
    StartCoroutine(SomeRoutine());
}
```

---

### 8. NetworkManager / NGO null check
Luôn check NetworkManager trước khi dùng:

```csharp
// ✅ ĐÚNG
var nm = NetworkManager.Singleton;
if (nm == null || !nm.IsListening)
{
    Debug.LogWarning("[Network] NetworkManager chưa sẵn sàng.");
    return;
}
```

---

### 9. SetStatus — thông báo lỗi thân thiện (tiếng Việt)
Khi hiển thị lỗi cho người dùng, dùng tiếng Việt rõ ràng:

| Tình huống | Message hiển thị |
|---|---|
| Mạng lỗi | "Mất kết nối. Kiểm tra mạng và thử lại." |
| Sai mật khẩu | "Email hoặc mật khẩu không đúng." |
| Chưa đăng nhập | "Vui lòng đăng nhập để tiếp tục." |
| Phòng đầy | "Phòng đã đủ người chơi." |
| Relay lỗi | "Không thể kết nối relay. Thử lại sau." |
| Lobby không tồn tại | "Phòng không còn tồn tại." |
| Lỗi không xác định | "Có lỗi xảy ra. Vui lòng thử lại." |

---

### 10. Checklist khi viết code mới

Trước khi hoàn thành bất kỳ method nào, kiểm tra:
- [ ] Có try/catch bao quanh service calls không?
- [ ] Có null check cho tất cả reference không?
- [ ] Có isBusy guard cho button actions không?
- [ ] finally block có unlock isBusy và buttons không?
- [ ] Thông báo lỗi có bằng tiếng Việt, thân thiện không?
- [ ] Navigator/panel target có được kiểm tra null không?
- [ ] Coroutine có check gameObject.activeInHierarchy không?
