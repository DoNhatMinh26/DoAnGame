# Avatar Animation System - Complete Logic

## Tổng quan

Hệ thống avatar animation cho multiplayer battle với 3 PSB files:
- **Character Meo**: Idle + Happy animations
- **Character Meo_Sad**: Sad animation
- **MeoGoc34 Fix**: Attack animation (góc 3/4)

**Quy tắc quan trọng**: Chỉ 1 PSB được hiển thị tại 1 thời điểm.

---

## Logic Animation Chi Tiết

### 1. GameplayPanel - Question Time (10s)

**Khi câu hỏi mới bắt đầu** (`HandleQuestionGenerated`):

```
A: ShowIdle() → Character Meo ACTIVE → Idle animation
B: ShowIdle() → Character Meo ACTIVE → Idle animation

→ Chỉ hiển thị Character Meo (Idle)
→ Character Meo_Sad = INACTIVE
→ MeoGoc34 Fix = INACTIVE
```

**Timing**: Từ khi câu hỏi hiển thị đến hết `questionTimeLimit` (default 10s từ `GameRulesConfig`)

---

### 2. GameplayPanel - Summary Time (delayBetweenQuestions)

**Sau khi cả 2 player trả lời hoặc hết thời gian** (`HandleAnswerResultDetailed`):

#### Case 1: Cả 2 sai (winnerId = -1)
```
P1: ShowSad() → Character Meo_Sad ACTIVE → Sad animation
    → Character Meo = INACTIVE
    → MeoGoc34 Fix = INACTIVE

P2: ShowSad() → Character Meo_Sad ACTIVE → Sad animation
    → Character Meo = INACTIVE
    → MeoGoc34 Fix = INACTIVE

→ KHÔNG mất máu
→ battleStatusText: "Cả 2 đều sai!"
```

#### Case 2: Hòa - Cả 2 đúng cùng lúc (winnerId = -2)
```
P1: ShowSad() → Character Meo_Sad ACTIVE → Sad animation
    → Character Meo = INACTIVE
    → MeoGoc34 Fix = INACTIVE

P2: ShowSad() → Character Meo_Sad ACTIVE → Sad animation
    → Character Meo = INACTIVE
    → MeoGoc34 Fix = INACTIVE

→ KHÔNG mất máu
→ battleStatusText: "Hòa! Cả 2 đều đúng cùng lúc."
```

#### Case 3: Cả 2 đúng, P1 nhanh hơn (winnerId = 0, bothCorrect = true)
```
P1 (Winner - nhanh hơn):
    ShowHappy() → Character Meo ACTIVE → Happy animation
    → Character Meo_Sad = INACTIVE
    → MeoGoc34 Fix = INACTIVE

P2 (Loser - chậm hơn):
    ShowSad() → Character Meo_Sad ACTIVE → Sad animation
    → Character Meo = INACTIVE
    → MeoGoc34 Fix = INACTIVE

→ KHÔNG mất máu (cả 2 đều đúng)
→ KHÔNG có Attack animation (chỉ Happy/Sad)
→ P1 +10 điểm, P2 +5 điểm (khuyến khích)
→ battleStatusText (P1): "Chiến thắng! (XXXms)"
→ battleStatusText (P2): "Thua cuộc! Đối thủ nhanh hơn."
```

#### Case 4: Cả 2 đúng, P2 nhanh hơn (winnerId = 1, bothCorrect = true)
```
P2 (Winner - nhanh hơn):
    ShowHappy() → Character Meo ACTIVE → Happy animation
    → Character Meo_Sad = INACTIVE
    → MeoGoc34 Fix = INACTIVE

P1 (Loser - chậm hơn):
    ShowSad() → Character Meo_Sad ACTIVE → Sad animation
    → Character Meo = INACTIVE
    → MeoGoc34 Fix = INACTIVE

→ KHÔNG mất máu (cả 2 đều đúng)
→ KHÔNG có Attack animation (chỉ Happy/Sad)
→ P2 +10 điểm, P1 +5 điểm (khuyến khích)
→ battleStatusText (P2): "Chiến thắng! (XXXms)"
→ battleStatusText (P1): "Thua cuộc! Đối thủ nhanh hơn."
```

#### Case 5: P1 đúng, P2 sai (winnerId = 0, bothCorrect = false)
```
P1 (Winner - đúng):
    ShowAttackThenHappy() → 
    BƯỚC 1 (1.5s): MeoGoc34 Fix ACTIVE → Attack animation (chạy 1 lần, không loop)
                   → Character Meo = INACTIVE
                   → Character Meo_Sad = INACTIVE
    
    BƯỚC 2 (sau 1.5s): Character Meo ACTIVE → Happy animation
                       → MeoGoc34 Fix = INACTIVE
                       → Character Meo_Sad = INACTIVE

P2 (Loser - sai):
    ShowSad() → Character Meo_Sad ACTIVE → Sad animation
    → Character Meo = INACTIVE
    → MeoGoc34 Fix = INACTIVE

→ CÓ mất máu: P2 -1 HP
→ P1 +10 điểm, P2 +0 điểm
→ battleStatusText (P1): "Chiến thắng! (XXXms)"
→ battleStatusText (P2): "Thua cuộc! Đối thủ nhanh hơn."
```

#### Case 6: P1 sai, P2 đúng (winnerId = 1, bothCorrect = false)
```
P2 (Winner - đúng):
    ShowAttackThenHappy() → 
    BƯỚC 1 (1.5s): MeoGoc34 Fix ACTIVE → Attack animation (chạy 1 lần, không loop)
                   → Character Meo = INACTIVE
                   → Character Meo_Sad = INACTIVE
    
    BƯỚC 2 (sau 1.5s): Character Meo ACTIVE → Happy animation
                       → MeoGoc34 Fix = INACTIVE
                       → Character Meo_Sad = INACTIVE

P1 (Loser - sai):
    ShowSad() → Character Meo_Sad ACTIVE → Sad animation
    → Character Meo = INACTIVE
    → MeoGoc34 Fix = INACTIVE

→ CÓ mất máu: P1 -1 HP
→ P2 +10 điểm, P1 +0 điểm
→ battleStatusText (P2): "Chiến thắng! (XXXms)"
→ battleStatusText (P1): "Thua cuộc! Đối thủ nhanh hơn."
```

**Timing**: Hiển thị trong `delayBetweenQuestions` giây (default 5s từ `GameRulesConfig`)

---

### 3. Sau Summary Time → Câu hỏi mới

**Sau khi hết `delayBetweenQuestions`** (`HandleQuestionGenerated` được gọi lại):

```
A: ShowIdle() → Character Meo ACTIVE → Idle animation
    → Character Meo_Sad = INACTIVE
    → MeoGoc34 Fix = INACTIVE

B: ShowIdle() → Character Meo ACTIVE → Idle animation
    → Character Meo_Sad = INACTIVE
    → MeoGoc34 Fix = INACTIVE

→ Reset về Question Time cho câu hỏi mới
```

---

### 4. WinsPanel - Kết quả trận đấu

**Khi trận đấu kết thúc** (1 người hết máu hoặc bỏ cuộc):

```
Winner:
    SetAvatarWithoutAnimation(avatarId) → Set skin KHÔNG trigger animation
    → Delay 0.1s
    → ShowHappy() → Character Meo ACTIVE → Happy animation
                  → Character Meo_Sad = INACTIVE
                  → MeoGoc34 Fix = INACTIVE

Loser:
    SetAvatarWithoutAnimation(avatarId) → Set skin KHÔNG trigger animation
    → Delay 0.1s
    → ShowSad() → Character Meo_Sad ACTIVE → Sad animation
                → Character Meo = INACTIVE
                → MeoGoc34 Fix = INACTIVE

→ trangThaiText (Winner): "CHIẾN THẮNG!" (màu xanh)
→ trangThaiText (Loser): "THUA CUỘC!" (màu đỏ)
```

**Lưu ý**: 
- Dùng `SetAvatarWithoutAnimation()` thay vì `SetAvatar()` để tránh Idle animation chạy trước Happy/Sad
- Delay 0.1s giữa `SetAvatarWithoutAnimation()` và `ShowHappy()/ShowSad()` để đảm bảo skin setup hoàn tất

---

## Timing Summary

| Phase | Duration | Animation |
|---|---|---|
| Question Time | `questionTimeLimit` (10s) | Idle |
| Summary Time | `delayBetweenQuestions` (5s) | Happy/Sad/Attack→Happy |
| Attack animation | 1.5s | Attack (không loop) |
| Happy after Attack | Còn lại của Summary Time | Happy (loop) |
| WinsPanel | Vô thời hạn | Happy (winner) / Sad (loser) |

---

## Animation Clip Settings (Unity Editor)

### Character Meo (Idle + Happy)
- `Idle.anim`: **Loop Time = ON**
- `Happy.anim`: **Loop Time = ON**

### Character Meo_Sad (Sad)
- `Sad.anim`: **Loop Time = ON**

### MeoGoc34 Fix (Attack)
- `Attack.anim`: **Loop Time = OFF** ⚠️ CRITICAL - Chỉ chạy 1 lần

---

## Code Implementation

### AvatarCharacterDisplay.cs

```csharp
// Question Time
public void ShowIdle()
{
    SetPSBVisibility(showMeo: true, showMeoSad: false, showMeo34: false);
    TriggerIdle();
}

// Summary Time - Winner (cả 2 đúng, nhanh hơn)
public void ShowHappy()
{
    SetPSBVisibility(showMeo: true, showMeoSad: false, showMeo34: false);
    TriggerHappy();
}

// Summary Time - Loser (sai hoặc chậm hơn)
public void ShowSad()
{
    SetPSBVisibility(showMeo: false, showMeoSad: true, showMeo34: false);
    TriggerSad();
}

// Summary Time - Winner (1 đúng 1 sai) - Attack THEN Happy
public void ShowAttackThenHappy(float attackDuration = 1.5f)
{
    // BƯỚC 1: Hiển thị Attack
    SetPSBVisibility(showMeo: false, showMeoSad: false, showMeo34: true);
    TriggerAttack();
    
    // BƯỚC 2: Sau 1.5s, chuyển sang Happy
    Invoke(nameof(TransitionToHappyAfterAttack), attackDuration);
}

private void TransitionToHappyAfterAttack()
{
    ShowHappy(); // Chuyển sang Character Meo + Happy animation
}

// WinsPanel - Set avatar KHÔNG trigger animation
public void SetAvatarWithoutAnimation(int avatarId)
{
    ApplySkin(characterMeo, SkinNamesMeo, avatarId);
    ApplySkin(characterMeoSad, SkinNamesMeoSad, avatarId);
    ApplySkin(meoGoc34Fix, SkinNamesMeo34, avatarId);
    currentAvatarId = avatarId;
    // KHÔNG gọi ShowIdle() - để WinsController tự gọi ShowHappy()/ShowSad()
}
```

### UIMultiplayerBattleController.cs

```csharp
// Question Time
private void HandleQuestionGenerated(string question, int[] choices)
{
    // ... display question ...
    
    // Reset về Idle
    player1Character?.ShowIdle();
    player2Character?.ShowIdle();
}

// Summary Time
private void HandleAnswerResultDetailed(int winnerId, bool correct, ...)
{
    bool bothCorrect = player1Correct && player2Correct;
    
    if (winnerId == -1) // Cả 2 sai
    {
        player1Character?.ShowSad();
        player2Character?.ShowSad();
    }
    else if (winnerId == -2) // Hòa
    {
        player1Character?.ShowSad();
        player2Character?.ShowSad();
    }
    else if (winnerId == 0) // P1 thắng
    {
        if (bothCorrect)
        {
            // Cả 2 đúng, P1 nhanh hơn
            player1Character?.ShowHappy();
            player2Character?.ShowSad();
        }
        else
        {
            // P1 đúng, P2 sai
            player1Character?.ShowAttackThenHappy();
            player2Character?.ShowSad();
        }
    }
    else if (winnerId == 1) // P2 thắng
    {
        if (bothCorrect)
        {
            // Cả 2 đúng, P2 nhanh hơn
            player2Character?.ShowHappy();
            player1Character?.ShowSad();
        }
        else
        {
            // P2 đúng, P1 sai
            player2Character?.ShowAttackThenHappy();
            player1Character?.ShowSad();
        }
    }
}
```

### UIWinsController.cs

```csharp
private void SetWinsPanelAvatars(MatchResultData r)
{
    // Set avatar WITHOUT animation
    winnerCharacter?.SetAvatarWithoutAnimation(winnerAvatarId);
    loserCharacter?.SetAvatarWithoutAnimation(loserAvatarId);
    
    // Delay 0.1s để skin setup hoàn tất
    StartCoroutine(DelayedShowAnimation(winnerCharacter, isHappy: true, 0.1f));
    StartCoroutine(DelayedShowAnimation(loserCharacter, isHappy: false, 0.1f));
}

private IEnumerator DelayedShowAnimation(AvatarCharacterDisplay character, bool isHappy, float delay)
{
    yield return new WaitForSeconds(delay);
    
    if (isHappy)
        character.ShowHappy();
    else
        character.ShowSad();
}
```

---

## Troubleshooting

### Vấn đề: Attack animation loop liên tục
**Nguyên nhân**: `Attack.anim` có Loop Time = ON
**Giải pháp**: Set Loop Time = OFF trong Unity Inspector

### Vấn đề: Nhiều PSB hiển thị cùng lúc (overlapping)
**Nguyên nhân**: `SetPSBVisibility()` không được gọi đúng
**Giải pháp**: Luôn gọi `SetPSBVisibility()` trước `TriggerXXX()` trong mọi Show method

### Vấn đề: WinsPanel hiển thị Idle trước Happy/Sad
**Nguyên nhân**: Dùng `SetAvatar()` thay vì `SetAvatarWithoutAnimation()`
**Giải pháp**: Dùng `SetAvatarWithoutAnimation()` + delay 0.1s + `ShowHappy()/ShowSad()`

### Vấn đề: Attack không chuyển sang Happy
**Nguyên nhân**: `ShowAttack()` được gọi thay vì `ShowAttackThenHappy()`
**Giải pháp**: Gọi `ShowAttackThenHappy()` trong case "1 đúng 1 sai"

---

## Inspector Setup Checklist

- [ ] 3 PSB files imported với skeleton
- [ ] Mỗi PSB có 4 skin con (mascost1-4 hoặc Meo1-4)
- [ ] AnimatorController gán cho mỗi PSB
- [ ] Animation clips: Idle, Happy, Sad, Attack
- [ ] Loop Time settings: Idle/Happy/Sad = ON, Attack = OFF
- [ ] Triggers trong AnimatorController: TriggerIdle, TriggerHappy, TriggerSad, TriggerAttack
- [ ] AvatarCharacterDisplay components gán 3 PSB references
- [ ] UIMultiplayerBattleController gán player1Character, player2Character
- [ ] UIWinsController gán winnerCharacter, loserCharacter

---

## Testing

1. **Question Time**: Cả 2 player thấy Idle animation
2. **Cả 2 đúng, P1 nhanh hơn**: P1 Happy, P2 Sad, KHÔNG mất máu
3. **P1 đúng, P2 sai**: P1 Attack (1.5s) → Happy, P2 Sad, P2 -1 HP
4. **Cả 2 sai**: Cả 2 Sad, KHÔNG mất máu
5. **WinsPanel**: Winner Happy, Loser Sad, KHÔNG có Idle trước đó

---

**Last Updated**: 2026-05-11
**Version**: 2.0 - Complete logic with Attack→Happy transition
