# Ready System Implementation - TASK 9

## Overview
Implemented a Ready system for multiplayer lobby that allows:
- **Host**: Sees "Bắt đầu" button (disabled) → Lights up when client marks ready
- **Client**: Sees "Sẵn sàng" button → Marks ready → Button becomes "Đã sẵn sàng" (disabled)

## Changes Made

### 1. UIMultiplayerRoomController.cs

#### Added Constants
```csharp
private const string Player1ReadyKey = "Player1Ready";
private const string Player2ReadyKey = "Player2Ready";
```

#### Modified HandleStartMatch()
- **Host**: Checks if client is ready before allowing start
- **Client**: Calls `HandleReadyButton()` instead of starting match
- Added `IsClientReady(lobby)` check before proceeding

#### New Methods Added

**HandleReadyButton()**
- Client marks themselves as ready
- Calls `MarkClientReadyAsync()`

**MarkClientReadyAsync()**
- Updates lobby metadata with `Player2ReadyKey = "1"`
- Sets status to "Đã sẵn sàng!"
- Updates button state

**UpdateReadyButtonState()**
- **Host**: 
  - Button interactable = client is ready
  - Text: "Bắt đầu" (if ready) or "Chờ sẵn sàng..." (if not)
- **Client**:
  - Button interactable = NOT ready (so they can click to mark ready)
  - Text: "Sẵn sàng" (if not ready) or "Đã sẵn sàng" (if ready)

**IsClientReady(Lobby lobby)**
- Checks if `Player2ReadyKey` exists in lobby metadata with value "1"
- Returns true if client is ready, false otherwise

#### Updated Methods

**OnShow()**
- Added `UpdateReadyButtonState()` call to initialize button state when panel shows

**PollLobbyOnce()**
- Added `UpdateReadyButtonState()` call at end to refresh button state on each poll

## Flow

### Client Ready Flow
1. Client joins lobby
2. Client sees "Sẵn sàng" button (interactable)
3. Client clicks button → `HandleStartMatch()` → `HandleReadyButton()` → `MarkClientReadyAsync()`
4. Lobby metadata updated: `Player2ReadyKey = "1"`
5. Button becomes "Đã sẵn sàng" (disabled)
6. Status shows "Đã sẵn sàng!"

### Host Start Flow
1. Host sees "Chờ sẵn sàng..." button (disabled)
2. Client marks ready
3. Poll detects `Player2ReadyKey = "1"`
4. Button becomes "Bắt đầu" (interactable)
5. Host clicks "Bắt đầu" → `HandleStartMatch()` → checks `IsClientReady()` → starts match

## Lobby Metadata
- `Player2ReadyKey` = "1" when client is ready
- Stored in lobby data (public visibility)
- Persists until lobby is deleted or client leaves

## Status Messages
- Host waiting: "Chờ sẵn sàng..."
- Client ready: "Đã sẵn sàng!"
- Host can start: "Bắt đầu" (button enabled)

## Files Modified
- `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerRoomController.cs`

## Testing Checklist
- [ ] Host creates room, sees "Chờ sẵn sàng..." (disabled)
- [ ] Client joins, sees "Sẵn sàng" (enabled)
- [ ] Client clicks "Sẵn sàng" → button becomes "Đã sẵn sàng" (disabled)
- [ ] Host sees "Bắt đầu" (enabled)
- [ ] Host clicks "Bắt đầu" → match starts
- [ ] If client leaves, host sees "Chờ sẵn sàng..." again
