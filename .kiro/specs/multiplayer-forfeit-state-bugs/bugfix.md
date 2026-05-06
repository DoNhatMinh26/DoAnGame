# Bugfix Requirements Document

## Introduction

This document specifies the requirements for fixing two critical state management bugs in the multiplayer forfeit flow (Flow 6) that were discovered through ParrelSync multi-client testing. These bugs affect the user experience after a CLIENT quits during battle:

1. **Bug 1:** CLIENT returns to LobbyPanel with incorrect ready state ("Đã sẵn sàng!" instead of "Sẵn sàng")
2. **Bug 2:** HOST's WinsPanel displays empty/missing player information after CLIENT quits

Both bugs passed the initial audit (MULTIPLAYER_FLOW_AUDIT_REPORT.md) but were revealed through actual gameplay testing with logs from `GameLog_CLIENT_20260506_065204.txt` and `GameLog_HOST_20260506_065201.txt`.

---

## Bug Analysis

### Current Behavior (Defect)

#### Bug 1: CLIENT Ready State Not Reset After Quit

1.1 WHEN CLIENT quits during battle (Flow 6) and returns to LobbyPanel THEN the system displays "Đã sẵn sàng!" (ready state) with disabled button

1.2 WHEN CLIENT's `UIMultiplayerRoomController.OnEnable()` executes after quit THEN the system reads stale lobby data showing `Player2ReadyKey=1` from the previous session

1.3 WHEN CLIENT's `OnEnable()` force refresh completes THEN the system correctly shows "Sẵn sàng" button (not ready state) because CLIENT is no longer in the lobby

#### Bug 2: HOST WinsPanel Empty After CLIENT Quits

2.1 WHEN CLIENT quits during battle and HOST navigates to WinsPanel THEN the system displays empty/missing player names, scores, and health values

2.2 WHEN HOST's `UIWinsController.DisplayMatchResult()` executes THEN the system fails to populate UI text fields with winner/loser information

2.3 WHEN HOST's WinsPanel attempts to read match result data THEN the system either has destroyed player states, unpopulated cache, or missing UI references

### Expected Behavior (Correct)

#### Bug 1: CLIENT Ready State Reset

3.1 WHEN CLIENT quits during battle and returns to LobbyPanel THEN the system SHALL display "Sẵn sàng" button (not ready state) immediately without showing stale "Đã sẵn sàng!" state

3.2 WHEN CLIENT's `UIMultiplayerRoomController.OnEnable()` executes after quit THEN the system SHALL skip the initial stale lobby read or immediately override it with correct state

3.3 WHEN CLIENT's ready button state is determined after quit THEN the system SHALL recognize that CLIENT is no longer in any lobby and show the default "Sẵn sàng" state

#### Bug 2: HOST WinsPanel Populated

4.1 WHEN CLIENT quits during battle and HOST navigates to WinsPanel THEN the system SHALL display complete winner/loser information including names, scores, and health values

4.2 WHEN HOST's `UIWinsController.DisplayMatchResult()` executes THEN the system SHALL successfully populate all UI text fields with cached match result data

4.3 WHEN HOST's WinsPanel reads match result data THEN the system SHALL use the cached `UIWinsController.LastResult` populated by `PushMatchResultToWinsController()` before player states are destroyed

4.4 WHEN HOST's WinsPanel displays loser information for a quit player THEN the system SHALL append "(Đã Rời Trận)" indicator to the loser's name

### Unchanged Behavior (Regression Prevention)

#### General Forfeit Flow Preservation

5.1 WHEN CLIENT quits during battle THEN the system SHALL CONTINUE TO send `RequestForfeitServerRpc` to server

5.2 WHEN server receives forfeit request THEN the system SHALL CONTINUE TO set `IsAbandoned=true` and `AbandonedPlayerId` correctly

5.3 WHEN CLIENT executes quit flow THEN the system SHALL CONTINUE TO cancel pending `NavigateToWinsPanel` invoke to prevent navigation

5.4 WHEN CLIENT quits THEN the system SHALL CONTINUE TO show LobbyPanel before disconnecting to prevent white screen

5.5 WHEN CLIENT quits THEN the system SHALL CONTINUE TO call `LeaveLobbySafe()`, disconnect Relay, and reset session state

#### HOST Winner Flow Preservation

5.6 WHEN HOST becomes winner due to CLIENT forfeit THEN the system SHALL CONTINUE TO navigate to WinsPanel after 2-second delay

5.7 WHEN HOST's `HandleMatchEnded()` executes THEN the system SHALL CONTINUE TO call `PushMatchResultToWinsController()` to cache match result data

5.8 WHEN match is abandoned (forfeit) THEN the system SHALL CONTINUE TO skip Firebase sync for both players

#### Ready System Preservation (Non-Quit Scenarios)

5.9 WHEN CLIENT joins a new room and clicks "Sẵn sàng" THEN the system SHALL CONTINUE TO update `Player2ReadyKey=1` and display "Đã sẵn sàng!" correctly

5.10 WHEN HOST sees CLIENT ready in normal flow THEN the system SHALL CONTINUE TO enable "Bắt Đầu" button

5.11 WHEN CLIENT is in a valid lobby session with ready state THEN the system SHALL CONTINUE TO display "Đã sẵn sàng!" correctly

#### WinsPanel Display Preservation (Normal End)

5.12 WHEN match ends normally (player runs out of HP) THEN the system SHALL CONTINUE TO display complete winner/loser information in WinsPanel

5.13 WHEN WinsPanel displays match result THEN the system SHALL CONTINUE TO show winner name, score, health and loser name, score, health

5.14 WHEN WinsPanel cannot read from cache THEN the system SHALL CONTINUE TO use `TryBuildResultFromBattleManager()` as fallback

---

## Bug Condition Methodology

### Bug 1: CLIENT Ready State Race Condition

**Bug Condition Function:**
```pascal
FUNCTION isBugCondition1(X)
  INPUT: X of type ClientQuitContext
  OUTPUT: boolean
  
  // X.isClientQuit: CLIENT just quit from battle
  // X.onEnableFired: OnEnable() executed on LobbyPanel
  // X.lobbySnapshot: Lobby data snapshot read in OnEnable()
  // X.player2ReadyKey: Value of Player2ReadyKey in snapshot
  
  RETURN X.isClientQuit AND 
         X.onEnableFired AND 
         X.lobbySnapshot != null AND
         X.player2ReadyKey = "1"
END FUNCTION
```

**Property: Fix Checking**
```pascal
// Property: CLIENT Ready State Correct After Quit
FOR ALL X WHERE isBugCondition1(X) DO
  uiState ← GetReadyButtonState'(X)
  ASSERT uiState.buttonText = "Sẵn sàng" AND
         uiState.buttonInteractable = true AND
         uiState.statusText != "Đã sẵn sàng! Chờ chủ phòng bắt đầu..."
END FOR
```

**Preservation Goal:**
```pascal
// Property: Ready State Preserved in Normal Scenarios
FOR ALL X WHERE NOT isBugCondition1(X) DO
  ASSERT GetReadyButtonState(X) = GetReadyButtonState'(X)
END FOR
```

**Counterexample:**
```
Input: CLIENT quits battle → OnEnable() reads lobby with Player2ReadyKey=1
Current Output: "Đã sẵn sàng!" (incorrect)
Expected Output: "Sẵn sàng" (correct)
```

---

### Bug 2: HOST WinsPanel Empty Data

**Bug Condition Function:**
```pascal
FUNCTION isBugCondition2(X)
  INPUT: X of type HostWinsPanelContext
  OUTPUT: boolean
  
  // X.isHost: Player is HOST
  // X.clientForfeited: CLIENT quit during battle
  // X.navigatedToWinsPanel: HOST navigated to WinsPanel
  // X.lastResultCache: UIWinsController.LastResult data
  
  RETURN X.isHost AND 
         X.clientForfeited AND 
         X.navigatedToWinsPanel AND
         (X.lastResultCache.IsValid = false OR
          X.lastResultCache.WinnerName = "" OR
          X.lastResultCache.LoserName = "")
END FOR
```

**Property: Fix Checking**
```pascal
// Property: WinsPanel Data Complete After Forfeit
FOR ALL X WHERE isBugCondition2(X) DO
  winsData ← DisplayMatchResult'(X)
  ASSERT winsData.winnerName != "" AND
         winsData.winnerScore >= 0 AND
         winsData.winnerHealth >= 0 AND
         winsData.loserName != "" AND
         winsData.loserScore >= 0 AND
         winsData.loserHealth >= 0 AND
         winsData.loserName.Contains("(Đã Rời Trận)")
END FOR
```

**Preservation Goal:**
```pascal
// Property: WinsPanel Display Preserved in Normal End
FOR ALL X WHERE NOT isBugCondition2(X) DO
  ASSERT DisplayMatchResult(X) = DisplayMatchResult'(X)
END FOR
```

**Counterexample:**
```
Input: CLIENT forfeits → HOST navigates to WinsPanel
Current Output: Empty name/score/health fields
Expected Output: Complete winner/loser data with "(Đã Rời Trận)" indicator
```

---

## Root Cause Analysis

### Bug 1 Root Cause: Race Condition in OnEnable()

**Timeline from CLIENT log:**
```
[06:52:57.416] OnEnable: Panel active lại, force refresh lobby...
[06:52:57.420] IsLocalPlayerReady: hasKey=true, value=1, isReady=True
[06:52:57.420] Đã sẵn sàng! Chờ chủ phòng bắt đầu...  ← WRONG (stale data)
[06:52:57.793] ✅ OnEnable refresh: 1 players in lobby
[06:52:57.796] IsLocalPlayerReady: local player not found in lobby
[06:52:57.797] Nhấn Sẵn sàng để báo hiệu cho chủ phòng.  ← CORRECT (after refresh)
```

**Problem:** `OnEnable()` reads `currentLobby` (which still contains 2 players with `Player2ReadyKey=1`) BEFORE `LeaveLobbySafe()` completes. The force refresh at the end of `OnEnable()` eventually corrects the state, but the user sees the incorrect "Đã sẵn sàng!" state first.

**Solution Direction:** Either:
1. Skip the initial ready state check in `OnEnable()` if `isQuitting=true` or `currentLobby` is stale
2. Immediately set `currentLobby=null` in `ExecuteQuitToLobby()` before showing LobbyPanel
3. Add a flag to suppress ready state display until after force refresh completes

---

### Bug 2 Root Cause: Cache Not Populated or UI References Missing

**Timeline from HOST log:**
```
[06:52:56.984] [BattleController] Pushing match result to WinsController...
[06:52:56.984] ✅ PushMatchResult SUCCESS:
[06:52:56.984]   - Winner: VVG_liv (Score:15, HP:2)
[06:52:56.984]   - Loser: Vovangiang22222 (Score:10, HP:1)
[06:52:56.984]   - IsAbandoned: True, AbandonedPlayerId: 1
[06:52:56.984] IsLocalWinner=True, navigating to WinsPanel in 2s...
```

**Log shows:** `PushMatchResultToWinsController()` is called and reports success. However, WinsPanel still displays empty data.

**Possible Causes:**
1. **Cache populated but UI references not assigned:** `UIWinsController` has `LastResult` populated, but TMP_Text references (winnerNameText, winnerScoreText, etc.) are `null` or not assigned in Inspector
2. **Cache not persisting:** `LastResult` is a struct that gets reset or overwritten before `DisplayMatchResult()` reads it
3. **DisplayMatchResult() not called:** Navigation to WinsPanel succeeds but `DisplayMatchResult()` is not triggered in `OnShow()` or `Start()`
4. **Player states destroyed too early:** `PushMatchResultToWinsController()` tries to read from `NetworkedPlayerState` objects that are already destroyed

**Solution Direction:** 
1. Verify all TMP_Text UI references are assigned in Inspector for WinsPanel
2. Ensure `DisplayMatchResult()` is called in `UIWinsController.OnShow()` or `Start()`
3. Add null checks and fallback logic in `DisplayMatchResult()`
4. Ensure `PushMatchResultToWinsController()` reads player data BEFORE any destroy operations

---

## Test Evidence Summary

### Bug 1 Evidence (CLIENT Log)

**Stale Ready State Displayed:**
- `[06:52:57.420] IsLocalPlayerReady: hasKey=true, value=1, isReady=True`
- `[06:52:57.420] Đã sẵn sàng! Chờ chủ phòng bắt đầu...` ← **WRONG**

**Correct State After Refresh:**
- `[06:52:57.796] IsLocalPlayerReady: local player not found in lobby`
- `[06:52:57.797] Nhấn Sẵn sàng để báo hiệu cho chủ phòng.` ← **CORRECT**

### Bug 2 Evidence (HOST Log)

**Cache Populated Successfully:**
- `[06:52:56.984] ✅ PushMatchResult SUCCESS:`
- `[06:52:56.984]   - Winner: VVG_liv (Score:15, HP:2)`
- `[06:52:56.984]   - Loser: Vovangiang22222 (Score:10, HP:1)`

**User Report:** WinsPanel displays empty name/score/health fields despite cache being populated.

---

## Key Files Involved

### Bug 1: CLIENT Ready State
- `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerRoomController.cs`
  - `OnEnable()` - Reads stale lobby data
  - `IsLocalPlayerReady()` - Determines ready button state
  - `ExecuteQuitToLobby()` - Shows LobbyPanel before quit
  - `HandleQuitRoom()` - Calls `LeaveLobbySafe()`

### Bug 2: HOST WinsPanel Empty
- `Assets/Script/Script_multiplayer/1Code/CODE/UIWinsController.cs`
  - `DisplayMatchResult()` - Displays winner/loser info
  - `LastResult` - Cached match result data
- `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerBattleController.cs`
  - `PushMatchResultToWinsController()` - Populates cache
  - `HandleMatchEnded()` - Calls push method
- `Assets/Script/Script_multiplayer/1Code/Multiplay/NetworkedMathBattleManager.cs`
  - `EndMatchWithWinner()` - Triggers match end flow
  - `RequestForfeitServerRpc()` - Handles forfeit

---

## Acceptance Criteria

### Bug 1 Fix Verification

**AC1.1:** WHEN CLIENT quits during battle and returns to LobbyPanel THEN ready button SHALL display "Sẵn sàng" (not "Đã sẵn sàng!") immediately without flicker

**AC1.2:** WHEN CLIENT's `OnEnable()` executes after quit THEN status text SHALL NOT display "Đã sẵn sàng! Chờ chủ phòng bắt đầu..." at any point

**AC1.3:** WHEN CLIENT quits and returns to lobby THEN ready button SHALL be interactable (enabled)

### Bug 2 Fix Verification

**AC2.1:** WHEN CLIENT quits during battle and HOST navigates to WinsPanel THEN winner name SHALL be displayed (not empty)

**AC2.2:** WHEN HOST's WinsPanel displays after forfeit THEN winner score and health SHALL be displayed with correct values

**AC2.3:** WHEN HOST's WinsPanel displays after forfeit THEN loser name SHALL be displayed with "(Đã Rời Trận)" appended

**AC2.4:** WHEN HOST's WinsPanel displays after forfeit THEN loser score and health SHALL be displayed with correct values

### Regression Prevention

**AC3.1:** WHEN CLIENT joins a new room and clicks "Sẵn sàng" in normal flow THEN ready state SHALL display "Đã sẵn sàng!" correctly

**AC3.2:** WHEN match ends normally (HP depletion) THEN WinsPanel SHALL display complete winner/loser information for both HOST and CLIENT

**AC3.3:** WHEN CLIENT quits THEN forfeit flow SHALL complete without crashes or white screens

---

## Testing Strategy

### Manual Testing with ParrelSync

1. **Setup:** Use ParrelSync to create 2 Unity Editor instances (HOST + CLIENT)
2. **Test Bug 1:**
   - HOST creates room
   - CLIENT joins and clicks "Sẵn sàng"
   - HOST starts match
   - During battle, CLIENT clicks Quit → Confirm
   - **Verify:** CLIENT's LobbyPanel shows "Sẵn sàng" button immediately (not "Đã sẵn sàng!")
3. **Test Bug 2:**
   - Same setup as Bug 1
   - CLIENT quits during battle
   - **Verify:** HOST's WinsPanel shows complete winner/loser info with "(Đã Rời Trận)"
4. **Regression Test:**
   - Normal match flow (no quit) → verify WinsPanel displays correctly
   - CLIENT joins new room after quit → verify ready button works correctly

### Log Verification

- Enable `GameLogger` for both HOST and CLIENT
- Check CLIENT log for ready state messages after quit
- Check HOST log for `PushMatchResult` and WinsPanel display
- Verify no stale "Đã sẵn sàng!" messages in CLIENT log after quit

---

## Success Criteria

**Bug 1 Fixed:** CLIENT returns to LobbyPanel with correct "Sẵn sàng" button state immediately, no flicker or stale "Đã sẵn sàng!" display

**Bug 2 Fixed:** HOST's WinsPanel displays complete winner/loser information including names, scores, health, and "(Đã Rời Trận)" indicator

**No Regressions:** Normal match end flow and ready system continue to work correctly

**User Satisfaction:** "không được làm qua loa mà hãy kiểm tra và fix lại cho kĩ cho đúng" - thorough fix verified through actual testing, not superficial patches
