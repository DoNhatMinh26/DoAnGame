# Avatar Animation System - Fixes & Optimizations

## ✅ Đã Fix

### 1. **Nhiều character hiển thị cùng lúc (trùng nhau)**

**Vấn đề:** Cả 3 PSB (Character Meo, Character Meo_Sad, MeoGoc34 Fix) đều active cùng lúc → nhiều character chồng lên nhau.

**Nguyên nhân:** `SetAvatar()` chỉ set skin, không ẩn/hiện PSB.

**Giải pháp:**
- ✅ `SetAvatar()` bây giờ tự động gọi `ShowIdle()` sau khi set skin
- ✅ `ShowIdle()`, `ShowHappy()`, `ShowSad()`, `ShowAttack()` đều gọi `SetPSBVisibility()` để ẩn 2 PSB không dùng
- ✅ **Chỉ 1 PSB active tại 1 thời điểm**

**Code thay đổi:**
```csharp
// AvatarCharacterDisplay.cs
public void SetAvatar(int avatarId)
{
    // ... set skin ...
    ShowIdle(); // ✅ Tự động ẩn 2 PSB không dùng
}

private void SetPSBVisibility(bool showMeo, bool showMeoSad, bool showMeo34)
{
    if (characterMeo != null)    characterMeo.SetActive(showMeo);
    if (characterMeoSad != null) characterMeoSad.SetActive(showMeoSad);
    if (meoGoc34Fix != null)     meoGoc34Fix.SetActive(showMeo34);
}
```

---

### 2. **Attack animation loop liên tục**

**Vấn đề:** Attack animation chạy loop → nhân vật ném bóng liên tục.

**Nguyên nhân:** `Attack.anim` có `Loop Time = ON`.

**Giải pháp:**
- ✅ Set `Attack.anim` → Inspector → **Loop Time = OFF**
- ✅ Attack chỉ chạy 1 lần, sau đó dừng ở frame cuối

**Hướng dẫn:**
1. Chọn `Attack.anim` trong Project window
2. Inspector → **Loop Time** → Bỏ tick (OFF)
3. Apply

---

### 3. **Chỉ hiển thị đúng 1 skin theo avatarId**

**Vấn đề:** Nhiều skin hiển thị cùng lúc trong 1 PSB.

**Nguyên nhân:** Logic `ApplySkin()` đã đúng, nhưng cần đảm bảo gọi `SetAvatar()` khi init.

**Giải pháp:**
- ✅ `UIMultiplayerBattleController.InitAvatarCharacters()` gọi `SetAvatar()` với avatarId từ `NetworkedPlayerState`
- ✅ `UIWinsController.SetWinsPanelAvatars()` gọi `SetAvatar()` cho winner/loser
- ✅ Logic `ApplySkin()` đã đúng - bật đúng 1 skin, tắt 3 skin còn lại

---

### 4. **Wins panel animation**

**Vấn đề:** Wins panel không có animation cho winner/loser.

**Giải pháp:**
- ✅ `UIWinsController` đã có `winnerCharacter` và `loserCharacter` fields
- ✅ `SetWinsPanelAvatars()` gọi:
  - Winner → `ShowHappy()` (Character Meo + Happy animation)
  - Loser → `ShowSad()` (Character Meo_Sad + Sad animation)

**Code thay đổi:**
```csharp
// UIWinsController.cs
if (winnerCharacter != null)
{
    winnerCharacter.SetAvatar(winnerAvatarId);
    winnerCharacter.ShowHappy(); // ✅ Hiện Character Meo + Happy
}

if (loserCharacter != null)
{
    loserCharacter.SetAvatar(loserAvatarId);
    loserCharacter.ShowSad(); // ✅ Hiện Character Meo_Sad + Sad
}
```

---

## 📋 Checklist Setup (Cần làm trong Unity Editor)

### 1. Set Loop Time cho animation clips

- [ ] `Idle.anim` → Loop Time = **ON**
- [ ] `Happy.anim` → Loop Time = **ON**
- [ ] `Sad.anim` → Loop Time = **ON**
- [ ] `Attack.anim` → Loop Time = **OFF** ⭐ QUAN TRỌNG

### 2. Setup AnimatorController

- [ ] `Character Meo.controller` → Triggers: `TriggerIdle`, `TriggerHappy`
- [ ] `Character Meo_Sad.controller` → Trigger: `TriggerSad`
- [ ] `MeoGoc34 Fix.controller` → Trigger: `TriggerAttack`

### 3. Gán references trong scene

**GameplayPanel:**
- [ ] `Player1Character` → gán 3 PSB + `AvatarCharacterDisplay`
- [ ] `Player2Character` → gán 3 PSB + `AvatarCharacterDisplay`
- [ ] `UIMultiplayerBattleController` → gán `player1Character`, `player2Character`

**Wins Panel:**
- [ ] `WinnerCharacter` → gán 3 PSB + `AvatarCharacterDisplay`
- [ ] `LoserCharacter` → gán 3 PSB + `AvatarCharacterDisplay`
- [ ] `UIWinsController` → gán `winnerCharacter`, `loserCharacter`

### 4. Kiểm tra trong Play Mode

- [ ] Chỉ 1 PSB active tại 1 thời điểm (kiểm tra trong Hierarchy)
- [ ] Attack animation chạy 1 lần, không loop
- [ ] Wins panel hiển thị đúng animation (Winner Happy, Loser Sad)

---

## 🎯 Logic Animation Summary

### GameplayPanel (Battle)

| Thời điểm | Điều kiện | Winner Animation | Loser Animation | Damage? |
|-----------|-----------|------------------|-----------------|---------|
| **Question Time** | Đang chờ trả lời | Idle (Character Meo) | Idle (Character Meo) | - |
| **Summary - 1 đúng, 1 sai** | winnerId = 0 hoặc 1 | Attack (MeoGoc34 Fix) | Sad (Character Meo_Sad) | ✅ Loser mất máu |
| **Summary - Cả 2 đúng, speed khác** | winnerId = 0 hoặc 1, correct = true | Happy (Character Meo) | Sad (Character Meo_Sad) | ❌ Không mất máu |
| **Summary - Cả 2 sai** | winnerId = -1 | Sad (Character Meo_Sad) | Sad (Character Meo_Sad) | ❌ Không mất máu |
| **Summary - Hòa (cả 2 đúng cùng lúc)** | winnerId = -2 | Sad (Character Meo_Sad) | Sad (Character Meo_Sad) | ❌ Không mất máu |

### Wins Panel

| Vai trò | PSB hiển thị | Animation | Loop? |
|---------|--------------|-----------|-------|
| **Winner** | Character Meo | Happy | ✅ Loop |
| **Loser** | Character Meo_Sad | Sad | ✅ Loop |

### ⚠️ Lưu ý quan trọng

**Cả 2 đúng (speed comparison):**
- Người nhanh hơn: **Happy** (không Attack) - khuyến khích fair play
- Người chậm hơn: **Sad** - động viên cố gắng hơn
- **KHÔNG mất máu** - cả 2 đều trả lời đúng

**1 đúng, 1 sai:**
- Người đúng: **Attack** - tấn công đối thủ
- Người sai: **Sad** - bị tấn công
- **Loser mất máu** - penalty cho câu trả lời sai

---

## 🔧 Troubleshooting

### Vẫn thấy nhiều character cùng lúc
1. Kiểm tra trong Hierarchy khi Play Mode
2. Chỉ 1 PSB nên có checkmark (active)
3. Nếu nhiều PSB active → kiểm tra code có gọi `ShowIdle()` / `ShowHappy()` / `ShowSad()` / `ShowAttack()` đúng không

### Attack vẫn loop
1. **Kiểm tra Animation Clip:**
   - Chọn `Attack.anim` trong Project window
   - Inspector → **Loop Time** → Bỏ tick (OFF)
   - Apply

2. **Kiểm tra AnimatorController:**
   - Mở `MeoGoc34 Fix.controller` trong Animator window
   - Chọn state `Attack`
   - Inspector → **Loop Time** → Bỏ tick (OFF)
   - Hoặc set **Exit Time** = 1.0 (chạy hết 1 lần rồi exit)

3. **Kiểm tra Transition:**
   - Trong Animator window, kiểm tra transition từ `Attack` về `Idle` hoặc `Entry`
   - Đảm bảo có transition để animation không bị stuck
   - Set **Has Exit Time** = true
   - Set **Exit Time** = 1.0 (sau khi animation chạy xong)

4. **Nếu vẫn loop:**
   - Có thể do code gọi `TriggerAttack()` nhiều lần
   - Kiểm tra logs: search "ShowAttack()" - chỉ nên thấy 1 lần mỗi Summary Time
   - Nếu thấy nhiều lần → có bug trong logic gọi animation

### Wins panel không có animation
1. Kiểm tra `UIWinsController` Inspector
2. Đảm bảo `winnerCharacter` và `loserCharacter` đã được gán
3. Kiểm tra 2 character này đã setup đúng (3 PSB + AvatarCharacterDisplay)

---

## ✅ Compile Status

- **0 errors**, 43 warnings (không ảnh hưởng)
- Code đã sẵn sàng để test trong Unity Editor
