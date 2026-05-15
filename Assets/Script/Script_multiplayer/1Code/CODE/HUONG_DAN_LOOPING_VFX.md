# 🎬 Hướng Dẫn Setup LoopingBackgroundVFX - A to Z

## 📌 Tổng Quan
- **Script:** `LoopingBackgroundVFX.cs` 
- **Shader:** `LoopingVFX.shader`
- **Vị trí:** Đặt prefab trực tiếp trong Hierarchy (độc lập)
- **Chức năng:** Hiệu ứng VFX looping cho background với flying particles/numbers

---

## ✅ BƯỚC 1: Setup Texture

### 1.1 Chuẩn bị texture
- Tạo hoặc import texture cho VFX (ví dụ: particle sprite, rain, numbers, code)
- Khuyến nghị: 512x512 px hoặc 1024x1024 px
- Format: PNG với alpha channel (transparent)
- Lưu vào: `Assets/Resources/Textures/` hoặc `Assets/TaiNguyen/`

### 1.2 Setup texture import settings
1. Click vào texture trong Assets
2. Inspector → mở hộp thoại texture settings:
   - **Texture Type:** Sprite (2D and UI)
   - **Sprite Mode:** Single
   - **Filter Mode:** Bilinear (smooth) hoặc Point (pixelated)
   - **Compression:** Compressed
   - **Apply** → check

---

## ✅ BƯỚC 2: Tạo Material

### 2.1 Tạo material mới
1. Trong `Assets/Resources/` (hoặc thư mục của bạn), **Right-click**
2. **Create → Material** → đặt tên: `Mat_LoopingVFX`

### 2.2 Gán shader
1. Click vào `Mat_LoopingVFX`
2. Inspector → **Shader dropdown** → tìm `Custom/LoopingVFX`
3. Chọn shader này

### 2.3 Thiết lập properties
1. **Main Texture:** Kéo texture vào khung _MainTex
2. **Tint Color:** Màu mong muốn (white = không đổi màu)
3. **Animation Speed:** 1.0 (có thể tuỳ chỉnh sau)
4. **Intensity:** 1.0
5. **Scroll X/Y:** 0.5 (tốc độ cuộn)
6. **Scale:** 1.0

**Ví dụ mặc định bạn không cần thay đổi gì, sử dụng giá trị mặc định trước**

---

## ✅ BƯỚC 3: Tạo Prefab VFX

### 3.1 Tạo Empty GameObject

1. Trong Hierarchy: **Right-click → Create Empty**
2. Đặt tên: `BackgroundVFX_Loop`
3. Position: (0, 0, 0)
4. Scale: (1, 1, 1)

### 3.2 Thêm Particle System

1. Click `BackgroundVFX_Loop` → **Add Component → Particle System**
2. (Particle System sẽ tự tạo kèm Particle System Renderer)

### 3.3 Thiết lập Particle System

Click Particle System trong Inspector, setup:

**Main:**
- Loop: ✅ (checked)
- Duration: 5.0
- Emission: 50 particles/sec (default)

**Emission Module:**
- Rate over Time: 50
- (Script sẽ điều chỉnh thêm)

**Velocity over Lifetime:**
- Enabled: ✅
- X: (-0.5, 0.5)
- Y: (0.2, 1.0) - flow upward
- Z: (0, 0)

**Size over Lifetime:**
- Enabled: ✅
- Size: curve từ 0 → 1 → 0.5

**Renderer:**
- Material: `Mat_LoopingVFX` (kéo vào)
- Render Mode: Billboard/Mesh (tuỳ ý)
- Sorting Order: 10 (hoặc số lớn hơn để lên trên)

---

## ✅ BƯỚC 4: Thêm Script

### 4.1 Gán script
1. Vẫn chọn `BackgroundVFX_Loop`
2. **Add Component → LoopingBackgroundVFX** (script vừa tạo)

### 4.2 Gán references trong Inspector

Trong script component:
- **VFX Particle System:** Kéo Particle System component vào
- **Custom Material:** Kéo `Mat_LoopingVFX` vào
- **Use Custom Shader:** ✅ (checked)
- **Target Canvas:** (optional, để trống nếu không dùng Canvas)
- **Sorting Order:** 10

### 4.3 Tuỳ chỉnh effect properties

- **Animation Speed:** 1.0
- **Intensity:** 1.0
- **Tint Color:** White (C, W, W, 1)
- **Scale:** 1.0
- **Scroll Speed X:** 0.5
- **Scroll Speed Y:** 0.5
- **Emission Rate:** 50
- **Particle Lifetime:** 2.0

---

## ✅ BƯỚC 5: Save Prefab

### 5.1 Tạo thư mục Prefabs
1. `Assets/Prefabs/` → Create folder: `VFX`

### 5.2 Drag to Prefab
1. Chọn `BackgroundVFX_Loop` từ Hierarchy
2. **Drag vào** `Assets/Prefabs/VFX/`
3. Đặt tên: `BackgroundVFX_Loop.prefab`

**Xong! Bây giờ bạn có 1 reusable prefab**

---

## ✅ BƯỚC 6: Sử dụng trong Scene

### 6.1 Drag prefab vào Hierarchy
1. Từ `Assets/Prefabs/VFX/BackgroundVFX_Loop.prefab`
2. Kéo trực tiếp vào Hierarchy (hoặc Scene viewport)

### 6.2 Điều chỉnh vị trí
- Position: (0, 0, -5) hoặc (-5) để nó nhìn từ phía sau
- Scale: (1, 1, 1)

### 6.3 Tuỳ chỉnh effect runtime
Trong Inspector, bạn có thể thay đổi:
- **Animation Speed** ↑/↓ (0.5 = chậm, 2.0 = nhanh)
- **Intensity** ↑/↓ (số lượng particles)
- **Tint Color** = đổi màu effect
- **Scroll Speed** = tốc độ cuộn texture
- **Scale** = kích thước particles

---

## 🎯 BƯỚC 7: Sử dụng Script dari Code (Optional)

### 7.1 Control từ code
```csharp
// Lấy component
LoopingBackgroundVFX bgVFX = GetComponent<LoopingBackgroundVFX>();

// Thay đổi màu + intensity
bgVFX.SetEffectColor(Color.cyan, 1.5f);

// Thay đổi tốc độ animation
bgVFX.SetAnimationSpeed(0.8f);

// Thay đổi emission rate
bgVFX.SetEmissionRate(100f);

// Pause/Resume
bgVFX.PauseVFX(true);  // pause
bgVFX.PauseVFX(false); // resume

// Reset to default
bgVFX.ResetToDefault();
```

### 7.2 Ví dụ: Thay đổi khi start scene
```csharp
void Start()
{
    LoopingBackgroundVFX bgVFX = GetComponent<LoopingBackgroundVFX>();
    
    // Flying code effect
    bgVFX.SetEffectColor(Color.green, 1.2f);
    bgVFX.SetAnimationSpeed(1.5f);
}
```

---

## 🎨 ADVANCED: Tạo Multiple VFX Layers

Bạn có thể tạo nhiều instance của prefab với settings khác nhau:

```
Scene
├── BackgroundVFX_Loop (Layer 1 - Rain)
│   └── Speed: 2.0, Color: Cyan, Scale: 0.5
│
├── BackgroundVFX_Loop (2) (Layer 2 - Dust)
│   └── Speed: 0.5, Color: Yellow, Scale: 0.8
│
└── BackgroundVFX_Loop (3) (Layer 3 - Flying Numbers)
    └── Speed: 1.5, Color: Green, Scale: 1.2
```

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| Particles không hiển thị | 1. Check Material đã gán đúng<br>2. Check Texture đã import<br>3. Check Camera culling layers |
| Effect tối quá | Tăng Intensity hoặc thay Material thành có alpha cao hơn |
| Effect chạy quá nhanh/chậm | Tuỳ chỉnh Animation Speed |
| Shader error | Ngủ gọi Shader path: `Custom/LoopingVFX` hoặc reimport shader |
| Memory leak | Đảm bảo destroy prefab khi không dùng hoặc dùng multiple instances cẩn thận |

---

## ✨ Tips & Tricks

1. **Cho flying numbers/code:**
   - Dùng texture có chứa ký hiệu số/code
   - Tăng Scale để kích thước lớn hơn
   - Tuỳ chỉnh Velocity để particles bay theo mẫu muốn

2. **Cho rain/snow:**
   - Scale nhỏ (0.3-0.5)
   - Scroll Speed Y cao (1.5-2.0)
   - Emission Rate cao (100+)

3. **Để loop seamless:**
   - Texture phải wrap-able (edges khớp)
   - Hoặc dùng texture tileable patterns

4. **Performance:**
   - Hạn chế emission rate quá cao
   - Dùng simpler textures
   - Limit lifecycle của particles

---

## 📝 Checklist Final

- ✅ Shader được tạo tại `Assets/Shaders/LoopingVFX.shader`
- ✅ Script được tạo tại `Assets/Script/Script_multiplayer/1Code/CODE/LoopingBackgroundVFX.cs`
- ✅ Material `Mat_LoopingVFX` được tạo và gán shader
- ✅ Prefab `BackgroundVFX_Loop` được tạo
- ✅ Particle System được setup đúng
- ✅ Prefab được thêm vào Hierarchy
- ✅ Test và tuỳ chỉnh effect

---

## 🎉 Hoàn tất!
Bây giờ bạn có 1 hệ thống VFX looping đầy đủ, có thể tái sử dụng cho bất kỳ scene nào!
