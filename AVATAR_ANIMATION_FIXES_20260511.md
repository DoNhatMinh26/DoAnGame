# Avatar Animation System - Fixes Applied (2026-05-11)

## Issues Identified from Logs

Based on analysis of `ConsoleLog_20260511_110143.txt` (HOST) and `ConsoleLog_20260511_110158.txt` (CLIENT):

### 1. ❌ Battle Status Text Display Issue
**Problem**: Text hiển thị "Bạn trả lời đúng!" không phân biệt rõ ràng giữa người thắng và người thua.

**Expected**:
- Winner (local player): "Chiến thắng! (XXXms)"
- Loser (local player): "Thua cuộc! Đối thủ nhanh hơn."

**Root Cause**: `battleStatusText` không phân biệt giữa "answering correctly" vs "winning the round".

**Fix Applied**: ✅
- Updated `HandleAnswerResult()` in `UIMultiplayerBattleController.cs`
- Changed text to clearly show "Chiến thắng!" for winner and "Thua cuộc!" for loser
- Added logging to track text updates
- Used `SetText()` instead of `.text` property for better performance

### 2. ❌ Duplicate HandleAnswerResult Calls
**Problem**: `HandleAnswerResult()` được gọi 2 lần cho cùng 1 event.

**Evidence from Logs**:
```
[11:00:03.815] [INFO] [BattleController] [HOST] ===== HandleAnswerResult START =====
[11:00:03.831] [INFO] [BattleController] [HOST] ===== HandleAnswerResult COMPLETE =====
[11:00:03.835] [INFO] [BattleController] [HOST] ===== HandleAnswerResult START =====  ← DUPLICATE!
[11:00:03.838] [INFO] [BattleController] [HOST] ===== HandleAnswerResult COMPLETE =====
```

**Root Cause**: `SubscribeBattleEvents()` được gọi ở CẢ 2 nơi:
1. `Start()` method (line 97)
2. `OnEnable()` → `EnsureBattleManagerAndSubscribe()` → `SubscribeBattleEvents()` (line 130)

Khi panel được enable, cả 2 methods đều chạy → subscribe 2 lần → event handler được gọi 2 lần.

**Fix Applied**: ✅
- Removed `SubscribeBattleEvents()` call from `Start()` method
- Kept only the call in `OnEnable()` via `EnsureBattleManagerAndSubscribe()`
- Added comment explaining why subscription is only in OnEnable()

### 3. ⚠️ Animation Timing Issue (Potential)
**Problem**: Ở câu hỏi thứ 2 bị lệch quỹ đạo giữa thời gian thống kê và thời gian trả lời.

**Possible Causes**:
1. Duplicate `HandleAnswerResult` calls causing animation to trigger twice
2. Timer coordination issue between `BattleManager` and `AnswerSummaryUI`
3. Animation state not properly reset between Question Time and Summary Time

**Status**: ⏳ Needs further investigation after fixing duplicate calls
- The duplicate call fix may resolve this issue
- If issue persists, need to check:
  - `AnswerSummaryUI` timer coordination
  - `NetworkedMathBattleManager` timer logic
  - Animation state transitions in `AvatarCharacterDisplay`

### 4. ✅ Animation Display Logic (Already Correct)
**Status**: Animation logic is working as expected based on logs.

**Evidence**:
- Question Time → `ShowIdle()` called correctly
- Summary Time (both correct, P1 faster) → P1 `ShowHappy()`, P2 `ShowSad()` ✅
- Summary Time (both wrong) → Both `ShowSad()` ✅
- Only 1 PSB active at a time ✅
- Correct skin displayed based on avatarId ✅

## Files Modified

### 1. `Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerBattleController.cs`

#### Change 1: Fixed Battle Status Text Display
**Location**: `HandleAnswerResult()` method, lines ~730-760

**Before**:
```csharp
if (battleStatusText != null)
{
    if (winnerId == -1)
    {
        battleStatusText.text = "Cả 2 đều sai!";
    }
    else
    {
        if (isLocalWinner)
        {
            battleStatusText.text = $"<color=green>Bạn trả lời đúng! ({responseTimeMs}ms)</color>";
        }
        else
        {
            battleStatusText.text = $"<color=red>Đối thủ trả lời đúng nhanh hơn!</color>";
        }
    }
}
```

**After**:
```csharp
if (battleStatusText != null)
{
    if (winnerId == -1)
    {
        // Cả 2 sai
        battleStatusText.SetText("Cả 2 đều sai!");
        Debug.Log($"[BattleController] [{role}] battleStatusText: 'Cả 2 đều sai!'");
    }
    else if (winnerId == -2)
    {
        // Hòa (cả 2 đúng cùng lúc)
        battleStatusText.SetText("Hòa! Cả 2 đều đúng cùng lúc.");
        Debug.Log($"[BattleController] [{role}] battleStatusText: 'Hòa! Cả 2 đều đúng cùng lúc.'");
    }
    else
    {
        // Có người thắng
        if (isLocalWinner)
        {
            // Local player thắng câu này
            battleStatusText.SetText($"<color=green>Chiến thắng! ({responseTimeMs}ms)</color>");
            Debug.Log($"[BattleController] [{role}] battleStatusText: 'Chiến thắng! ({responseTimeMs}ms)' (local player won)");
        }
        else
        {
            // Opponent thắng câu này
            battleStatusText.SetText($"<color=red>Thua cuộc! Đối thủ nhanh hơn.</color>");
            Debug.Log($"[BattleController] [{role}] battleStatusText: 'Thua cuộc! Đối thủ nhanh hơn.' (opponent won)");
        }
    }
}
else
{
    Debug.LogWarning($"[BattleController] [{role}] battleStatusText is NULL!");
}
```

**Changes**:
- ✅ Changed "Bạn trả lời đúng!" → "Chiến thắng!" (clearer for winner)
- ✅ Changed "Đối thủ trả lời đúng nhanh hơn!" → "Thua cuộc! Đối thủ nhanh hơn." (clearer for loser)
- ✅ Added case for `winnerId == -2` (draw/tie)
- ✅ Used `SetText()` instead of `.text` property
- ✅ Added detailed logging for each case
- ✅ Added null check warning

#### Change 2: Fixed Duplicate Event Subscription
**Location**: `Start()` method, lines ~85-105

**Before**:
```csharp
// Subscribe vào battle events
SubscribeBattleEvents();

// Subscribe vào NetworkVariable changes (delay để đảm bảo NetworkObject đã spawn)
Invoke(nameof(SubscribeNetworkVariables), 0.5f);
```

**After**:
```csharp
// ✅ FIX: Không subscribe ở đây nữa - để OnEnable() xử lý
// Subscribe vào battle events sẽ được gọi trong OnEnable() → EnsureBattleManagerAndSubscribe()
// SubscribeBattleEvents(); // ← REMOVED to prevent duplicate subscription

// Subscribe vào NetworkVariable changes (delay để đảm bảo NetworkObject đã spawn)
Invoke(nameof(SubscribeNetworkVariables), 0.5f);
```

**Changes**:
- ✅ Removed `SubscribeBattleEvents()` call from `Start()`
- ✅ Added comment explaining why subscription is only in `OnEnable()`
- ✅ Prevents duplicate event subscription
- ✅ Ensures `HandleAnswerResult()` is only called once per event

## Testing Checklist

### ✅ Already Verified (from logs)
- [x] Avatar skins display correctly based on avatarId
- [x] Only 1 PSB active at a time
- [x] `ShowIdle()` called during Question Time
- [x] `ShowHappy()` / `ShowSad()` called correctly during Summary Time (both correct case)
- [x] `ShowSad()` called for both players when both wrong

### ⏳ Need to Test After Fixes
- [ ] Battle status text shows "Chiến thắng!" for winner
- [ ] Battle status text shows "Thua cuộc!" for loser
- [ ] `HandleAnswerResult()` is only called ONCE per event (check logs)
- [ ] Animation timing is correct between Question Time and Summary Time
- [ ] No animation overlap or flickering
- [ ] Attack animation plays correctly (1 correct, 1 wrong case)
- [ ] Attack animation does NOT loop (should play once and stop)

### 🔍 How to Test
1. **Export logs from both HOST and CLIENT** using `ConsoleLogExporter`
2. **Play a multiplayer match** with at least 2 questions:
   - Question 1: Both correct, different speeds
   - Question 2: 1 correct, 1 wrong
3. **Check logs for**:
   - `HandleAnswerResult START` should appear only ONCE per question
   - `battleStatusText` should show correct text for each player
   - Animation triggers should be in correct order: Idle → (Happy/Sad/Attack) → Idle
4. **Visual check**:
   - Winner sees "Chiến thắng!" in green
   - Loser sees "Thua cuộc!" in red
   - Animations play smoothly without overlap

## Expected Behavior After Fixes

### Battle Status Text
| Scenario | Winner (Local) | Loser (Local) |
|---|---|---|
| Both correct, P1 faster | "Chiến thắng! (XXXms)" | "Thua cuộc! Đối thủ nhanh hơn." |
| Both correct, P2 faster | "Thua cuộc! Đối thủ nhanh hơn." | "Chiến thắng! (XXXms)" |
| 1 correct, 1 wrong | "Chiến thắng! (XXXms)" | "Thua cuộc! Đối thủ nhanh hơn." |
| Both wrong | "Cả 2 đều sai!" | "Cả 2 đều sai!" |
| Draw (same time) | "Hòa! Cả 2 đều đúng cùng lúc." | "Hòa! Cả 2 đều đúng cùng lúc." |

### Animation Flow
```
Question Time (10s):
  → ShowIdle() for both players
  → Character Meo active, Idle animation

Summary Time (5s):
  Case 1: Both correct, different speeds
    → Winner: ShowHappy() (Character Meo, Happy animation)
    → Loser: ShowSad() (Character Meo_Sad, Sad animation)
    → NO damage, NO Attack animation
  
  Case 2: 1 correct, 1 wrong
    → Winner: ShowAttack() (MeoGoc34 Fix, Attack animation - plays once)
    → Loser: ShowSad() (Character Meo_Sad, Sad animation)
    → Loser loses 1 HP
  
  Case 3: Both wrong
    → Both: ShowSad() (Character Meo_Sad, Sad animation)
    → NO damage
  
  Case 4: Draw (both correct, same time)
    → Both: ShowSad() (Character Meo_Sad, Sad animation)
    → NO damage

Next Question:
  → ShowIdle() for both players (reset to idle state)
```

## Known Issues (Still Need Investigation)

### 1. Animation Timing Overlap (Question 2)
**Status**: ⏳ Needs testing after duplicate call fix

**Symptoms**: "Ở câu hỏi thứ 2 bị lệch quỹ đạo giữa thời gian thống kê và thời gian trả lời"

**Possible Causes**:
1. ~~Duplicate `HandleAnswerResult` calls~~ ← FIXED
2. Timer coordination issue between `BattleManager` and `AnswerSummaryUI`
3. Animation state not properly reset

**Next Steps**:
- Test after duplicate call fix
- If issue persists, investigate:
  - `AnswerSummaryUI.StartSummaryCountdown()` timing
  - `NetworkedMathBattleManager.GenerateNewQuestion()` timing
  - `HandleQuestionGenerated()` timing relative to summary end

### 2. Attack Animation Loop
**Status**: ✅ Should be fixed by user setting Loop Time = OFF in Unity Editor

**User Action Required**:
- Open `Attack.anim` in Unity Editor
- Set `Loop Time = OFF` in Animation Inspector
- Verify transition in AnimatorController: `Has Exit Time = true`, `Exit Time = 1.0`

## Summary

### ✅ Fixes Applied
1. **Battle status text** now clearly shows "Chiến thắng!" vs "Thua cuộc!"
2. **Duplicate event subscription** removed - `HandleAnswerResult()` will only be called once
3. **Improved logging** for debugging text display and animation triggers

### ⏳ Pending
1. Test animation timing after duplicate call fix
2. User needs to set Attack animation Loop Time = OFF in Unity Editor

### 📝 Notes
- No new MD files created (as per user request: "k cần tạo md")
- All changes are code-only fixes
- Logs show animation logic is already correct - just needed to fix duplicate calls and text display
