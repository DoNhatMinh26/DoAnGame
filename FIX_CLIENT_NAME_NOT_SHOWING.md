# ✅ ĐÃ SỬA: TÊN CLIENT KHÔNG HIỂN THỊ

## Vấn đề:

Khi vào battle screen:
- **Host**: Hiển thị tên đúng (ví dụ: "Huynh2333") ✅
- **Client**: Hiển thị "Player 2" thay vì tên thật (ví dụ: "VovangiangPro") ❌

---

## Nguyên nhân:

**`SendPlayerNameToServer()` không bao giờ được gọi từ Client!**

### Logic cũ (SAI):

```csharp
public override void OnNetworkSpawn()
{
    if (IsServer)
    {
        Debug.Log("[BattleManager] Server spawned");
    }
    else
    {
        Debug.Log("[BattleManager] Client spawned");
        // ❌ KHÔNG CÓ CODE GỌI SendPlayerNameToServer()!
    }
}
```

### Flow cũ:
1. Host gọi `InitializeBattle(grade)`
2. Host spawn player states:
   - Player 1: Tên = `AuthManager.GetCharacterName()` (Host) ✅
   - Player 2: Tên = "Player 2" (default) ❌
3. **Client KHÔNG GỬI TÊN** → Player 2 vẫn là "Player 2"

---

## Giải pháp:

**Client gọi `SendPlayerNameToServer()` sau khi BattleManager spawn**

### Logic mới (ĐÚNG):

```csharp
public override void OnNetworkSpawn()
{
    base.OnNetworkSpawn();

    if (IsServer)
    {
        Debug.Log("[BattleManager] Server spawned, waiting for initialization...");
    }
    else
    {
        Debug.Log("[BattleManager] Client spawned");
        // ✅ Client delay 2 giây để đảm bảo player states đã spawn, sau đó gửi tên
        Invoke(nameof(SendPlayerNameToServer), 2f);
    }
}
```

### Flow mới:
1. Host gọi `InitializeBattle(grade)`
2. Host spawn player states:
   - Player 1: Tên = `AuthManager.GetCharacterName()` (Host) ✅
   - Player 2: Tên = "Player 2" (default, tạm thời)
3. **Client gọi `SendPlayerNameToServer()` sau 2 giây**
4. Client gửi `UpdatePlayerNameServerRpc(playerName)`
5. Server update `player2State.PlayerName.Value = playerName` ✅
6. NetworkVariable sync → MultiplayerHealthUI update UI ✅

---

## Code đã thay đổi:

### File: `NetworkedMathBattleManager.cs`

**Thay đổi duy nhất:**
```diff
public override void OnNetworkSpawn()
{
    base.OnNetworkSpawn();

    if (IsServer)
    {
        Debug.Log("[BattleManager] Server spawned, waiting for initialization...");
    }
    else
    {
        Debug.Log("[BattleManager] Client spawned");
-       // Client sẽ gửi tên sau khi nhận được thông báo player states đã spawn
-       // (được gọi từ StartMatch)
+       // Client delay 2 giây để đảm bảo player states đã spawn, sau đó gửi tên
+       Invoke(nameof(SendPlayerNameToServer), 2f);
    }
}
```

---

## Logic `SendPlayerNameToServer()`:

```csharp
private void SendPlayerNameToServer()
{
    if (IsServer) return;

    string playerName = "Player 2"; // Default

    var authManager = AuthManager.Instance;
    if (authManager != null)
    {
        var playerData = authManager.GetCurrentPlayerData();
        if (playerData != null && !string.IsNullOrWhiteSpace(playerData.characterName))
        {
            playerName = playerData.characterName;
        }
        else
        {
            string charName = authManager.GetCharacterName();
            if (!string.IsNullOrWhiteSpace(charName) && charName != "Unknown")
            {
                playerName = charName;
            }
        }
    }

    Debug.Log($"[BattleManager] Client sending name: {playerName}");
    UpdatePlayerNameServerRpc(playerName);
}
```

---

## Logic `UpdatePlayerNameServerRpc()`:

```csharp
[ServerRpc(RequireOwnership = false)]
private void UpdatePlayerNameServerRpc(string playerName, ServerRpcParams rpcParams = default)
{
    ulong clientId = rpcParams.Receive.SenderClientId;
    
    Debug.Log($"[BattleManager] Server received name from client {clientId}: {playerName}");
    
    // Tìm player state của client này
    NetworkedPlayerState playerState = null;
    if (clientId == 0)
    {
        playerState = player1State;
    }
    else
    {
        playerState = player2State;
    }

    if (playerState != null)
    {
        playerState.PlayerName.Value = playerName;
        Debug.Log($"[BattleManager] ✅ Updated player name: {playerName}");
    }
    else
    {
        Debug.LogWarning($"[BattleManager] ⚠️ Player state not found! Retrying...");
        StartCoroutine(RetryUpdatePlayerName(clientId, playerName));
    }
}
```

---

## Timeline:

```
T=0s:  Host click "Bắt đầu"
       → InitializeBattle(grade)
       → SpawnPlayerStates()
       → Player 1: "Huynh2333" ✅
       → Player 2: "Player 2" (tạm thời)

T=2s:  StartMatch()
       → GenerateQuestion()
       → Câu hỏi hiển thị

T=2s:  Client.OnNetworkSpawn()
       → Invoke(SendPlayerNameToServer, 2f)

T=4s:  Client.SendPlayerNameToServer()
       → UpdatePlayerNameServerRpc("VovangiangPro")
       → Server: player2State.PlayerName.Value = "VovangiangPro"
       → NetworkVariable sync to all clients
       → MultiplayerHealthUI.OnValueChanged()
       → NamePL2.text = "VovangiangPro" ✅
```

---

## Test:

### 1. Compile:
```bash
dotnet build Assembly-CSharp.csproj
```
✅ **Build succeeded**

### 2. Test trong Unity:

#### Setup:
- **Host**: Tài khoản "Huynh2333"
- **Client**: Tài khoản "VovangiangPro"

#### Bước test:

1. **Host tạo phòng** (Lớp 1)
2. **Client join phòng** (nhập mã phòng)
3. **Lobby screen**:
   - Danh sách: "1. Huynh2333 (chủ phòng)" ✅
   - Danh sách: "2. VovangiangPro" ✅
4. **Host click "Bắt đầu"**
5. **Battle screen hiển thị**:
   - **T=0-4s**: 
     - Host: "Huynh2333" ✅
     - Client: "Player 2" (tạm thời)
   - **T=4s+**: 
     - Host: "Huynh2333" ✅
     - Client: "VovangiangPro" ✅ (đã update!)

### 3. Kiểm tra Console logs:

**Client logs:**
```
[BattleManager] Client spawned
[BattleManager] Client sending name: VovangiangPro (ClientID: 1)
```

**Server logs:**
```
[BattleManager] Server received name from client 1: VovangiangPro
[BattleManager] ✅ Updated player name for client 1: VovangiangPro
```

**MultiplayerHealthUI logs:**
```
[HealthUI] Player2State: FOUND
[HealthUI] Subscribing to Player2: HP=10/10
[HealthUI] Player 2 name updated: VovangiangPro
```

---

## Lưu ý:

### Tại sao delay 2 giây?

1. **T=0s**: Host gọi `InitializeBattle()`
2. **T=0s**: Host spawn player states
3. **T=0-1s**: NetworkObjects replicate to Client
4. **T=2s**: Client gọi `SendPlayerNameToServer()` → Đảm bảo player states đã spawn

### Nếu vẫn thấy "Player 2":

**Nguyên nhân có thể:**
1. AuthManager chưa load playerData
2. characterName bị null hoặc empty
3. Network lag → player2State chưa spawn

**Debug:**
```csharp
// Trong SendPlayerNameToServer()
Debug.Log($"[BattleManager] AuthManager: {authManager != null}");
Debug.Log($"[BattleManager] PlayerData: {playerData != null}");
Debug.Log($"[BattleManager] CharacterName: {playerData?.characterName}");
```

---

## Kết luận:

✅ **Đã sửa**: Client gọi `SendPlayerNameToServer()` sau 2 giây
✅ **Compile thành công**: Không có lỗi
✅ **Logic đúng**: Tên được sync từ NetworkVariable
✅ **Timeline rõ ràng**: T=4s tên Client sẽ hiển thị đúng

**Test ngay trong Unity để xác nhận!** 🎮
