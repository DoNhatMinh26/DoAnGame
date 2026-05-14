# Hướng dẫn setup Attack bắn bóng (Arc Projectile)

## Mục tiêu
- Khi nhân vật chuyển sang animation **Attack**, bắn ra 1 quả bóng (sprite PNG).
- Bóng bay theo **vòng cung** đến **đối thủ**, **tự xoay (spin)** trong lúc bay.
- Đến đích: **phát VFX/SFX**, bóng biến mất, **đối thủ bị trừ máu** (damage đã có sẵn trong logic server).

## Lựa chọn Renderer (Unity 2022.3.6)
- **Đề xuất: SpriteRenderer (world/2D)**
  - Phù hợp với PSB character (Animator + SpriteRenderer).
  - Dễ căn sort layer với nhân vật và FX.
- UI Image (Canvas) chỉ dùng khi bạn muốn vẽ bóng trên UI (không phù hợp với PSB world).

## Bối cảnh scene hiện tại (Test_FireBase_multi)
- Character nằm trong: `Canvas/CharacterContainer/Player1Character` và `Canvas/CharacterContainer/Player2Character`.
- `CharacterContainer` luôn ACTIVE, các character được bật/tắt tự động theo panel.
- `UIMultiplayerBattleController` nằm ở `Canvas/GameplayPanel`.

## 1) Chuẩn bị asset PNG
- Đường dẫn: `Assets/TaiNguyen/Giang_`
- Import Settings gợi ý:
  - Texture Type: **Sprite (2D and UI)**
  - Sprite Mode: **Single**
  - Pixels Per Unit: giống nhân vật (vd 100)
  - Mipmap: **Off**
  - Filter: **Bilinear** (hoặc Point nếu muốn pixel style)
  - Compression: **Normal**

## 2) Tạo Prefab bóng
1. Tạo GameObject `BallProjectile`.
2. Add **SpriteRenderer** và gán sprite bóng.
3. Add script `ArcProjectile` (đã có trong project).
4. (Tùy chọn) thêm TrailRenderer hoặc ParticleSystem nhỏ.
5. Lưu thành prefab vào `Assets/Prefabs/` (hoặc thư mục bạn muốn).

Gợi ý sorting:
- Sorting Layer: `CharacterFx` (nếu có), hoặc `Default`.
- Order in Layer: lớn hơn nhân vật (vd 5-10) để bóng nằm trên nhân vật.

## 3) Đặt điểm bắn (muzzle) ở tay nhân vật
Trong mỗi character (Player1, Player2):
1. Mở `MeoGoc34 Fix` (PSB Attack) dưới `Player1Character` / `Player2Character`.
2. Tìm bone tay trong `MeoGoc34 Fix/Root/Than/TayTrai` hoặc `TayPhai`.
3. Tạo child empty `AttackMuzzle` dưới bone tay đó.
4. Kéo `AttackMuzzle` đúng **vị trí tay** (điểm bắt đầu bắn).

Lưu ý quan trọng:
- **Không cần tạo AttackMuzzle cho từng skin (Meo1–Meo4).**
- Mỗi PSB có **1 bộ xương (Root)** dùng chung cho 4 skin, nên **chỉ cần 1 AttackMuzzle** gắn vào bone tay.

## 4) Đặt điểm trúng (hit) ở tay đối thủ
Trong mỗi character (Player1, Player2):
1. Khi bị trừ máu, đối thủ đang ở trạng thái **Sad** → dùng PSB `Character Meo_Sad`.
2. Vào `Character Meo_Sad/root/Than/TayTrai` hoặc `TayPhai`.
3. Tạo child empty `HitPoint` dưới bone tay đó và canh vị trí va chạm.

> Lưu ý quan trọng:
> - **Không cần tạo HitPoint cho từng skin (mascost1–mascost4).**
> - Mỗi PSB chỉ cần **1 HitPoint** gắn vào bone tay trong `root`.
> - Nếu muốn chính xác cho mọi trạng thái (Idle/Sad/Attack), tạo 3 HitPoint cho 3 PSB và chọn theo PSB đang active.

## 5) Kết nối với code (gợi ý)
### A) Thêm reference vào `AvatarCharacterDisplay`
- Thêm field:
  - `Transform attackMuzzle`
  - `Transform hitPoint`
- Tạo getter để bên ngoài lấy điểm bắn và điểm trúng.

Gợi ý kéo trong Inspector:
- Chọn `Player1Character` → component `AvatarCharacterDisplay`.
- Kéo **AttackMuzzle** (đã tạo trong `MeoGoc34 Fix/Root/.../Tay...`) vào field `attackMuzzle`.
- Kéo **HitPoint** (đã tạo trong `Character Meo_Sad/root/.../Tay...`) vào field `hitPoint`.
- Lặp lại tương tự cho `Player2Character`.

### B) Spawn bóng khi Attack
Nơi gọi hợp lý là trong [Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerBattleController.cs](Assets/Script/Script_multiplayer/1Code/CODE/UIMultiplayerBattleController.cs):
- Hiện code chỉ gọi `ShowAttackThenHappy()` khi **1 đúng 1 sai** (có trừ máu).
- Chèn thêm lệnh bắn bóng ngay sau `ShowAttackThenHappy()`.

### C) Gán prefab trong Inspector
Trong `UIMultiplayerBattleController` (GameplayPanel):
- `Projectile Prefab`: kéo prefab `BallProjectile` vào.
- `Projectile Impact Vfx`: kéo prefab VFX (nếu có).
- `Projectile Parent`: kéo `CharacterContainer` (để projectile cùng layer/scale với nhân vật).
- `Projectile Arc Height`, `Flight Time`, `Spin Speed`: chỉnh theo cảm giác.

Pseudocode:
```csharp
// winner bắn
leftCharacter.ShowAttackThenHappy();
ProjectileSpawner.Spawn(
    leftCharacter.AttackMuzzle,
    rightCharacter.HitPoint,
    projectilePrefab,
    arcHeight: 1.5f,
    flightTime: 0.5f,
    spinSpeed: 720f
);
```

## 6) Damage (không xử lý trong projectile)
- Hiện tại damage đã xử lý ở server trong [Assets/Script/Script_multiplayer/1Code/Multiplay/NetworkedMathBattleManager.cs](Assets/Script/Script_multiplayer/1Code/Multiplay/NetworkedMathBattleManager.cs).
- Projectile chỉ là **visual effect**.
- Nếu muốn đồng bộ thời gian, canh `flightTime` trùng với thời điểm trừ máu.

## 7) Kiểm tra nhanh
- Attack -> bóng xuất hiện, bay theo vòng cung, xoay, chạm đích -> VFX.
- HP đối thủ giảm (đã có sẵn trong logic server).

## 8) Test nhanh trong Unity
1. Mở scene `Assets/Scenes/Test_FireBase_multi.unity`.
2. Chọn `GameplayPanel` → component `UIMultiplayerBattleController`:
  - Đảm bảo đã gán `Projectile Prefab`.
3. Chạy Play Mode, tạo phòng và trả lời:
  - Khi **1 đúng 1 sai**, người thắng bắn bóng theo vòng cung.
  - Khi **cả 2 đúng** hoặc **cả 2 sai** thì không bắn.

---
Nếu bạn muốn, mình có thể:
- Tạo script `ArcProjectile` + `ProjectileSpawner`.
- Hook thẳng vào `UIMultiplayerBattleController` và `AvatarCharacterDisplay`.
- Tạo VFX chạm đích cơ bản.
