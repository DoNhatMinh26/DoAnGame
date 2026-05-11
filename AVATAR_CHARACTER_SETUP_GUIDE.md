# Hướng Dẫn Setup Avatar Character System

## 📋 Tổng Quan

Hệ thống avatar character sử dụng **CharacterContainerController** để tự động quản lý visibility của các character dựa trên panel đang active.

**Nguyên tắc quan trọng:**
- ✅ **CharacterContainer** (parent) phải luôn **ACTIVE**
- ✅ Các child characters (Player1Character, Player2Character, WinnerCharacter, LoserCharacter) được **tự động bật/tắt** bởi CharacterContainerController
- ✅ Mỗi character có 3 PSB con (Character Meo, Character Meo_Sad, MeoGoc34 Fix)
- ✅ Chỉ 1 PSB được hiển thị tại 1 thời điểm (quản lý bởi AvatarCharacterDisplay)

---

## 🏗️ Cấu Trúc Hierarchy (Scene: Test_FireBase_multi)

```
Canvas
├── GameplayPanel (BasePanelController)
│   ├── ... (UI elements)
│   └── (không chứa characters - characters nằm ở CharacterContainer)
│
├── Wins (UIWinsController)
│   ├── ... (UI elements)
│   └── (không chứa characters - characters nằm ở CharacterContainer)
│
└── CharacterContainer (CharacterContainerController) ← LUÔN ACTIVE
    ├── Player1Character (AvatarCharacterDisplay) ← Auto bật/tắt theo GameplayPanel
    │   ├── Character Meo (Animator) ← PSB 1: Idle + Happy
    │   │   ├── mascost1 (skin 0)
    │   │   ├── mascost2 (skin 1)
    │   │   ├── mascost3 (skin 2)
    │   │   └── mascost4 (skin 3)
    │   ├── Character Meo_Sad (Animator) ← PSB 2: Sad
    │   │   ├── mascost1 (skin 0)
    │   │   ├── mascost2 (skin 1)
    │   │   ├── mascost3 (skin 2)
    │   │   └── mascost4 (skin 3)
    │   └── MeoGoc34 Fix (Animator) ← PSB 3: Attack
    │       ├── Meo1 (skin 0)
    │       ├── Meo2 (skin 1)
    │       ├── Meo3 (skin 2)
    │       └── Meo4 (skin 3)
    │
    ├── Player2Character (AvatarCharacterDisplay) ← Auto bật/tắt theo GameplayPanel
    │   ├── Character Meo (Animator)
    │   ├── Character Meo_Sad (Animator)
    │   └── MeoGoc34 Fix (Animator)
    │
    ├── WinnerCharacter (AvatarCharacterDisplay) ← Auto bật/tắt theo Wins panel
    │   ├── Character Meo (Animator)
    │   ├── Character Meo_Sad (Animator)
    │   └── MeoGoc34 Fix (Animator)
    │
    └── LoserCharacter (AvatarCharacterDisplay) ← Auto bật/tắt theo Wins panel
        ├── Character Meo (Animator)
        ├── Character Meo_Sad (Animator)
        └── MeoGoc34 Fix (Animator)
```

---

## ⚙️ Setup Inspector

### 1. CharacterContainer GameObject

**Component:** `CharacterContainerController`

**Inspector Settings:**
```
CharacterContainerController
├── Battle Characters
│   ├── Player1Character: [Drag Player1Character GameObject]
│   └── Player2Character: [Drag Player2Character GameObject]
├── Wins Characters
│   ├── Winner Character: [Drag WinnerCharacter GameObject]
│   └── Loser Character: [Drag LoserCharacter GameObject]
└── Panels để theo dõi
    ├── Gameplay Panel: [Drag GameplayPanel GameObject]
    └── Wins Panel: [Drag Wins GameObject]
```

**GameObject State:**
- ✅ **CharacterContainer: ACTIVE** (luôn luôn)
- ⚠️ **Player1Character: INACTIVE** (ban đầu - sẽ được bật khi GameplayPanel active)
- ⚠️ **Player2Character: INACTIVE** (ban đầu - sẽ được bật khi GameplayPanel active)
- ⚠️ **WinnerCharacter: INACTIVE** (ban đầu - sẽ được bật khi Wins panel active)
- ⚠️ **LoserCharacter: INACTIVE** (ban đầu - sẽ được bật khi Wins panel active)

---

### 2. Player1Character / Player2Character GameObject

**Component:** `AvatarCharacterDisplay`

**Inspector Settings:**
```
AvatarCharacterDisplay
├── 3 PSB GameObjects
│   ├── Character Meo: [Drag "Character Meo" child GameObject]
│   ├── Character Meo Sad: [Drag "Character Meo_Sad" child GameObject]
│   └── Meo Goc34 Fix: [Drag "MeoGoc34 Fix" child GameObject]
```

**Child PSB State (ban đầu):**
- ✅ **Character Meo: ACTIVE** (default - Idle animation)
- ⚠️ **Character Meo_Sad: INACTIVE**
- ⚠️ **MeoGoc34 Fix: INACTIVE**

**Skin State (trong mỗi PSB):**
- ✅ **mascost1 / Meo1: ACTIVE** (default skin - avatarId 0)
- ⚠️ **mascost2 / Meo2: INACTIVE**
- ⚠️ **mascost3 / Meo3: INACTIVE**
- ⚠️ **mascost4 / Meo4: INACTIVE**

---

### 3. WinnerCharacter / LoserCharacter GameObject

**Component:** `AvatarCharacterDisplay`

**Inspector Settings:** (giống Player1/2Character)
```
AvatarCharacterDisplay
├── 3 PSB GameObjects
│   ├── Character Meo: [Drag "Character Meo" child GameObject]
│   ├── Character Meo Sad: [Drag "Character Meo_Sad" child GameObject]
│   └── Meo Goc34 Fix: [Drag "MeoGoc34 Fix" child GameObject]
```

**Child PSB State (ban đầu):**
- ✅ **Character Meo: ACTIVE** (default)
- ⚠️ **Character Meo_Sad: INACTIVE**
- ⚠️ **MeoGoc34 Fix: INACTIVE**

**Skin State:** (giống Player1/2Character)

---

### 4. UIMultiplayerBattleController (GameplayPanel)

**Inspector Settings:**
```
UIMultiplayerBattleController
├── Avatar Characters
│   ├── Player1 Character: [Drag CharacterContainer/Player1Character]
│   └── Player2 Character: [Drag CharacterContainer/Player2Character]
```

⚠️ **LƯU Ý:** Gán đúng GameObject từ **CharacterContainer**, KHÔNG phải từ GameplayPanel!

---

### 5. UIWinsController (Wins Panel)

**Inspector Settings:**
```
UIWinsController
├── Avatar Characters
│   ├── Winner Character: [Drag CharacterContainer/WinnerCharacter]
│   └── Loser Character: [Drag CharacterContainer/LoserCharacter]
```

⚠️ **LƯU Ý:** Gán đúng GameObject từ **CharacterContainer**, KHÔNG phải từ Wins panel!

---

## 🔄 Logic Hoạt Động

### CharacterContainerController (Auto Management)

**Update() Loop:**
```csharp
// Theo dõi GameplayPanel
if (gameplayPanel.activeInHierarchy)
{
    // Bật Player1Character và Player2Character
    player1Character.SetActive(true);
    player2Character.SetActive(true);
}
else
{
    // Tắt Player1Character và Player2Character
    player1Character.SetActive(false);
    player2Character.SetActive(false);
}

// Theo dõi Wins panel
if (winsPanel.activeInHierarchy)
{
    // Bật WinnerCharacter và LoserCharacter
    winnerCharacter.SetActive(true);
    loserCharacter.SetActive(true);
}
else
{
    // Tắt WinnerCharacter và LoserCharacter
    winnerCharacter.SetActive(false);
    loserCharacter.SetActive(false);
}
```

**Kết quả:**
- ✅ Khi vào **GameplayPanel** → Player1/2Character tự động **BẬT**, Winner/LoserCharacter tự động **TẮT**
- ✅ Khi vào **Wins panel** → Winner/LoserCharacter tự động **BẬT**, Player1/2Character tự động **TẮT**
- ✅ Không cần code thủ công SetActive() trong UIMultiplayerBattleController hay UIWinsController

---

### AvatarCharacterDisplay (PSB & Skin Management)

**SetAvatar(avatarId):**
1. Apply skin đúng avatarId trên cả 3 PSB (bật 1 skin, tắt 3 skin còn lại)
2. Gọi `ShowIdle()` → Hiển thị Character Meo, ẩn 2 PSB còn lại, trigger Idle animation

**ShowIdle():**
- Character Meo: **ACTIVE** → Trigger Idle
- Character Meo_Sad: **INACTIVE**
- MeoGoc34 Fix: **INACTIVE**

**ShowHappy():**
- Character Meo: **ACTIVE** → Trigger Happy
- Character Meo_Sad: **INACTIVE**
- MeoGoc34 Fix: **INACTIVE**

**ShowSad():**
- Character Meo: **INACTIVE**
- Character Meo_Sad: **ACTIVE** → Trigger Sad
- MeoGoc34 Fix: **INACTIVE**

**ShowAttackThenHappy(duration):**
1. MeoGoc34 Fix: **ACTIVE** → Trigger Attack
2. Character Meo: **INACTIVE**
3. Character Meo_Sad: **INACTIVE**
4. Sau `duration` giây (default 1.5s) → Gọi `ShowHappy()`

---

## 🎬 Animation Flow

### GameplayPanel (Battle)

**Question Time (10s):**
```
HandleQuestionGenerated() được gọi
→ Player1Character.ShowIdle()
→ Player2Character.ShowIdle()
→ Cả 2 hiển thị Character Meo (Idle animation)
```

**Summary Time (delayBetweenQuestions từ GameRulesConfig):**

**Case 1: Cả 2 đúng, P1 nhanh hơn**
```
HandleAnswerResultDetailed(winnerId=0, bothCorrect=true)
→ Player1Character.ShowHappy() (người nhanh hơn)
→ Player2Character.ShowSad() (người chậm hơn)
→ KHÔNG mất máu
```

**Case 2: P1 đúng, P2 sai**
```
HandleAnswerResultDetailed(winnerId=0, bothCorrect=false)
→ Player1Character.ShowAttackThenHappy() (Attack 1.5s → Happy)
→ Player2Character.ShowSad()
→ CÓ mất máu
```

**Case 3: Cả 2 sai**
```
HandleAnswerResultDetailed(winnerId=-1)
→ Player1Character.ShowSad()
→ Player2Character.ShowSad()
→ KHÔNG mất máu
```

**Case 4: Hòa (cả 2 đúng cùng lúc)**
```
HandleAnswerResultDetailed(winnerId=-2)
→ Player1Character.ShowSad()
→ Player2Character.ShowSad()
→ KHÔNG mất máu
```

**Sau delayBetweenQuestions → Câu hỏi mới:**
```
HandleQuestionGenerated() được gọi lại
→ Reset về ShowIdle() cho cả 2
```

---

### Wins Panel (Match End)

**Khi trận kết thúc:**
```
HandleMatchEnded(winnerId)
→ PushMatchResultToWinsController() (cache data)
→ NavigateToWinsPanel()
→ UIWinsController.OnShow()
→ DisplayMatchResult()
→ SetWinsPanelAvatars()
```

**SetWinsPanelAvatars():**
```
1. WinnerCharacter.SetAvatarWithoutAnimation(winnerAvatarId)
   → Apply skin, KHÔNG trigger animation
   
2. LoserCharacter.SetAvatarWithoutAnimation(loserAvatarId)
   → Apply skin, KHÔNG trigger animation
   
3. Delay 0.1s (đảm bảo SetAvatar hoàn tất)

4. WinnerCharacter.ShowHappy()
   → Character Meo ACTIVE → Happy animation
   
5. LoserCharacter.ShowSad()
   → Character Meo_Sad ACTIVE → Sad animation
```

**Tại sao dùng SetAvatarWithoutAnimation():**
- ✅ Tránh double-trigger: SetAvatar() → Idle, sau đó ShowHappy() → Happy (2 animation chồng nhau)
- ✅ SetAvatarWithoutAnimation() chỉ apply skin, KHÔNG trigger animation
- ✅ Sau đó gọi ShowHappy()/ShowSad() 1 lần duy nhất

---

## ✅ Checklist Setup

### Bước 1: Tạo CharacterContainer
- [ ] Tạo GameObject "CharacterContainer" trong Canvas
- [ ] Add component `CharacterContainerController`
- [ ] Set **CharacterContainer: ACTIVE**

### Bước 2: Tạo 4 Character GameObjects
- [ ] Tạo "Player1Character" trong CharacterContainer
- [ ] Tạo "Player2Character" trong CharacterContainer
- [ ] Tạo "WinnerCharacter" trong CharacterContainer
- [ ] Tạo "LoserCharacter" trong CharacterContainer
- [ ] Set tất cả 4 characters: **INACTIVE** (ban đầu)

### Bước 3: Setup mỗi Character
- [ ] Add component `AvatarCharacterDisplay`
- [ ] Tạo 3 child PSB: "Character Meo", "Character Meo_Sad", "MeoGoc34 Fix"
- [ ] Import PSB files vào Unity (với skeleton + 4 skins)
- [ ] Drag 3 PSB vào hierarchy làm child của character
- [ ] Set **Character Meo: ACTIVE**, 2 PSB còn lại: **INACTIVE**
- [ ] Trong mỗi PSB, set **skin 0 (mascost1/Meo1): ACTIVE**, 3 skin còn lại: **INACTIVE**
- [ ] Gán 3 PSB vào Inspector của AvatarCharacterDisplay

### Bước 4: Setup CharacterContainerController
- [ ] Gán Player1Character vào "Player1 Character" field
- [ ] Gán Player2Character vào "Player2 Character" field
- [ ] Gán WinnerCharacter vào "Winner Character" field
- [ ] Gán LoserCharacter vào "Loser Character" field
- [ ] Gán GameplayPanel vào "Gameplay Panel" field
- [ ] Gán Wins vào "Wins Panel" field

### Bước 5: Setup UIMultiplayerBattleController
- [ ] Mở GameplayPanel trong Inspector
- [ ] Tìm component `UIMultiplayerBattleController`
- [ ] Gán CharacterContainer/Player1Character vào "Player1 Character" field
- [ ] Gán CharacterContainer/Player2Character vào "Player2 Character" field

### Bước 6: Setup UIWinsController
- [ ] Mở Wins panel trong Inspector
- [ ] Tìm component `UIWinsController`
- [ ] Gán CharacterContainer/WinnerCharacter vào "Winner Character" field
- [ ] Gán CharacterContainer/LoserCharacter vào "Loser Character" field

### Bước 7: Test
- [ ] Play scene trong Unity Editor
- [ ] Vào GameplayPanel → Kiểm tra Player1/2Character hiển thị
- [ ] Trả lời câu hỏi → Kiểm tra animation Happy/Sad/Attack
- [ ] Kết thúc trận → Kiểm tra Wins panel hiển thị Winner/Loser với animation đúng
- [ ] Kiểm tra KHÔNG có character chồng nhau (overlapping)

---

## 🐛 Troubleshooting

### Lỗi: Character chồng nhau (overlapping) trong Wins panel

**Nguyên nhân:**
- SetAvatarWithoutAnimation() chỉ apply skin, KHÔNG reset PSB visibility
- Tất cả 3 PSB vẫn ở trạng thái cũ từ GameplayPanel (có thể cả 3 đều ACTIVE)

**Giải pháp:**
- ✅ Đã fix trong code: SetAvatarWithoutAnimation() bây giờ gọi SetPSBVisibility(false, false, false) trước khi apply skin
- ✅ ShowHappy()/ShowSad() sẽ bật đúng 1 PSB, tắt 2 PSB còn lại

### Lỗi: Chỉ hiển thị Idle animation trong Wins panel

**Nguyên nhân:**
- SetAvatar() tự động gọi ShowIdle() ở cuối
- Sau đó ShowHappy()/ShowSad() được gọi → 2 animation chồng nhau

**Giải pháp:**
- ✅ Dùng SetAvatarWithoutAnimation() thay vì SetAvatar() trong WinsPanel
- ✅ Delay 0.1s trước khi gọi ShowHappy()/ShowSad()

### Lỗi: Character không hiển thị trong GameplayPanel

**Nguyên nhân:**
- CharacterContainer hoặc Player1/2Character bị INACTIVE
- CharacterContainerController chưa được gán đúng panels

**Giải pháp:**
- ✅ Kiểm tra CharacterContainer: phải **ACTIVE**
- ✅ Kiểm tra CharacterContainerController Inspector: đã gán đúng GameplayPanel và Wins panel chưa
- ✅ Kiểm tra Console log: "[CharContainer] GameplayPanel ACTIVE - Battle characters bat"

### Lỗi: Animation không chạy

**Nguyên nhân:**
- Animator Controller chưa được gán vào PSB
- Trigger parameters (TriggerIdle, TriggerHappy, TriggerSad, TriggerAttack) chưa được tạo trong Animator

**Giải pháp:**
- ✅ Mở mỗi PSB trong Inspector → Kiểm tra Animator component có Controller chưa
- ✅ Mở Animator window → Kiểm tra có 4 trigger parameters chưa
- ✅ Kiểm tra transitions giữa các states (Idle → Happy, Idle → Sad, Idle → Attack)

---

## 📝 Tóm Tắt

**CharacterContainer:**
- ✅ Luôn ACTIVE
- ✅ Chứa 4 characters: Player1, Player2, Winner, Loser
- ✅ CharacterContainerController tự động bật/tắt characters theo panel

**Mỗi Character:**
- ✅ Ban đầu INACTIVE (được bật bởi CharacterContainerController)
- ✅ Có 3 PSB con: Character Meo (Idle/Happy), Character Meo_Sad (Sad), MeoGoc34 Fix (Attack)
- ✅ Chỉ 1 PSB hiển thị tại 1 thời điểm
- ✅ Mỗi PSB có 4 skins (avatarId 0-3), chỉ 1 skin active

**Animation Flow:**
- ✅ Question Time → ShowIdle() (Character Meo)
- ✅ Summary Time → ShowHappy()/ShowSad()/ShowAttackThenHappy() (tùy kết quả)
- ✅ Wins Panel → SetAvatarWithoutAnimation() + delay + ShowHappy()/ShowSad()

**Không cần:**
- ❌ Không cần SetActive() thủ công trong UIMultiplayerBattleController
- ❌ Không cần SetActive() thủ công trong UIWinsController
- ❌ Không cần duplicate characters trong GameplayPanel hay Wins panel
- ❌ Tất cả characters nằm trong CharacterContainer duy nhất
