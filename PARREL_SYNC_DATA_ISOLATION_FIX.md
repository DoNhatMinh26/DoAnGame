# ParrelSync Data Isolation Fix — Complete Summary

## Overview
Fixed data isolation issue where ParrelSync main and clone accounts were sharing PlayerPrefs keys, causing data to mix between accounts.

**Status**: ✅ **COMPLETE** — All 4 legacy files updated, build verified (0 errors)

---

## Problem
When using ParrelSync for multi-client testing:
- Main account (acc A) and Clone account (acc B) were sharing the same PlayerPrefs keys
- Example: Both accounts read/write to `"TotalCoins"`, `"Class_HighestLevel"`, etc.
- Result: Data from one account overwrote the other's data

**Root Cause**: Legacy files used hardcoded string keys instead of `LocalStorageKeyResolver`, which automatically adds a prefix based on the dataPath hash in Editor mode.

---

## Solution
Replaced all hardcoded PlayerPrefs keys with `LocalStorageKeyResolver` properties in 4 legacy files:

### Files Updated

#### 1. **UiClass.cs** (Assets/Script/Data_Scrip/)
- **Lines changed**: 4 locations
- **Keys replaced**:
  - `"TotalCoins"` → `LocalStorageKeyResolver.TotalCoins`
  - `"Class_HighestLevel"` → `LocalStorageKeyResolver.ClassHighest`
  - `"SelectedClassSkinID"` → `LocalStorageKeyResolver.SelectedClassSkinID`
  - `"ClassSkinUnlockedKey(index)"` → `LocalStorageKeyResolver.ClassSkinUnlockedKey(index)`

#### 2. **UiTp.cs** (Assets/Script/Data_Scrip/)
- **Lines changed**: 4 locations
- **Keys replaced**:
  - `"TotalCoins"` → `LocalStorageKeyResolver.TotalCoins`
  - `"HighestLevelReached"` → `LocalStorageKeyResolver.KeoThaHighest`
  - `"SelectedPhaoID"` → `LocalStorageKeyResolver.SelectedPhaoID`
  - `"PhaoUnlockedKey(index)"` → `LocalStorageKeyResolver.PhaoUnlockedKey(index)`

#### 3. **UiSp.cs** (Assets/Script/Data_Scrip/)
- **Lines changed**: 1 location (UpdateShopProfileUI method)
- **Keys replaced**:
  - `"LocalGuestScore"` / `"UserScore"` → `LocalStorageKeyResolver.LocalGuestScore` / `LocalStorageKeyResolver.UserScore`
  - `"LocalGuestLevel"` / `"UserLevel"` → `LocalStorageKeyResolver.LocalGuestLevel` / `LocalStorageKeyResolver.UserLevel`

#### 4. **ProfileUI.cs** (Assets/Script/Data_Scrip/)
- **Lines changed**: 1 location (UpdateProfileDisplay method)
- **Keys replaced**:
  - `"LocalGuestScore"` / `"UserScore"` → `LocalStorageKeyResolver.LocalGuestScore` / `LocalStorageKeyResolver.UserScore`
  - `"LocalGuestLevel"` / `"UserLevel"` → `LocalStorageKeyResolver.LocalGuestLevel` / `LocalStorageKeyResolver.UserLevel`

---

## How LocalStorageKeyResolver Works

**In Editor (ParrelSync)**:
- Automatically adds prefix based on dataPath hash
- Main account: `local_abc:TotalCoins` (hash of main dataPath)
- Clone account: `local_def:TotalCoins` (hash of clone dataPath)
- Result: Completely separate keys ✅

**On Android (Production)**:
- Only 1 instance → no prefix added
- Keys remain unchanged: `TotalCoins`, `Class_HighestLevel`, etc.
- No impact on production behavior ✅

---

## Verification

### Build Status
```
✅ Build succeeded (0 errors)
⚠️ 15 pre-existing warnings (unrelated to this fix)
```

### Testing Checklist
- [ ] Main account (acc A): Play game, earn coins, unlock skins
- [ ] Clone account (acc B): Verify separate data (different coins, skins, levels)
- [ ] Verify data persists when switching between accounts
- [ ] Android build: Verify normal behavior (no prefix changes)

---

## Data Isolation After Fix

| Scenario | Before | After |
|---|---|---|
| ParrelSync Main (acc A) coins | ⚠️ Shared | ✅ Isolated |
| ParrelSync Clone (acc B) coins | ⚠️ Shared | ✅ Isolated |
| ParrelSync Main level progress | ⚠️ Shared | ✅ Isolated |
| ParrelSync Clone level progress | ⚠️ Shared | ✅ Isolated |
| ParrelSync Main skins | ⚠️ Shared | ✅ Isolated |
| ParrelSync Clone skins | ⚠️ Shared | ✅ Isolated |
| Android production data | ✅ Normal | ✅ Normal |

---

## Related Fixes (Previous Tasks)

This fix completes the ParrelSync data isolation work. Previous related fixes:

1. **Task 1**: Fixed ParrelSync auth/session isolation (already done via `LocalStorageKeyResolver`)
2. **Task 2**: Added Firestore data recovery mechanism (`FirebaseManager.cs`)
3. **Task 3**: Fixed Profile UI score display bug (`ProfileUI.cs`)
4. **Task 4**: Fixed level threshold formula (`DataManager.cs`)
5. **Task 5**: Made level threshold configurable (`DataManager.cs`)
6. **Task 6**: Investigating UIWinsController loser health display bug (in progress)

---

## Files Modified

```
Assets/Script/Data_Scrip/UiClass.cs
Assets/Script/Data_Scrip/UiTp.cs
Assets/Script/Data_Scrip/UiSp.cs
Assets/Script/Data_Scrip/ProfileUI.cs
```

## Key Takeaway

All legacy files now use `LocalStorageKeyResolver` for PlayerPrefs access, ensuring:
- ✅ ParrelSync accounts have completely isolated data
- ✅ Android production behavior unchanged
- ✅ Future-proof: any new keys should also use `LocalStorageKeyResolver`

---

**Last Updated**: May 11, 2026
**Build Status**: ✅ Verified (0 errors)
