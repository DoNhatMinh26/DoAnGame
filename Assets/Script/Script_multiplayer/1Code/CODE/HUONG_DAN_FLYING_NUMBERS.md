# 🎯 Hướng Dẫn FlyingNumbersVFX - A to Z

## 📌 Tổng Quan
- **Script:** `FlyingNumbersVFX.cs`
- **Shader:** `FlyingNumbers.shader` (random texture atlas)
- **Texture:** Atlas chứa số 0-9 + dấu phép toán
- **Hiệu ứng:** Các con số bay lên ngẫu nhiên
- **Ứng dụng:** Background effects, logic/algorithm visualization

---

## ✅ BƯỚC 1: Dùng nhiều Sprite riêng lẻ (một file = một ký tự)

Nếu bạn có nhiều file hình (ví dụ 15 ảnh, mỗi ảnh một chữ số hoặc ký hiệu), ta sẽ dùng `Sprite[] symbolSprites` thay vì 1 atlas lớn. Script `FlyingNumbersVFX` hỗ trợ trực tiếp workflow này.

### 1.1 Chuẩn bị
- Import từng ảnh làm `Sprite` (Texture Type = Sprite (2D and UI)).
- Đặt tên và sắp xếp ảnh trong `Assets` (ví dụ `num_0.png`, `num_1.png`, ...).

### 1.2 Gán Sprite vào Component
1. Chọn GameObject có component `FlyingNumbersVFX`.
2. Trong Inspector → `Symbol Sprites` → Click `+` để tăng phần tử và kéo từng `Sprite` vào từng `Element`.
3. (Tùy ý) `Atlas Columns` bỏ qua — script hiện dùng danh sách sprite.

### 1.3 Lưu ý import
- Đảm bảo mỗi ảnh đã import là `Sprite` (Inspector của ảnh: `Texture Type = Sprite (2D and UI)`).

---

## ✅ BƯỚC 2: Tạo Material

### 2.1 Tạo Material mới
1. Right-click → **Create → Material** → `Mat_FlyingNumbers`

### 2.2 Gán Shader
1. Click Mat_FlyingNumbers
2. **Shader dropdown** → `Custom/FlyingNumbers`

### 2.3 Setup Properties
1. **Main Texture:** Kéo `NumbersAtlas.png` vào
2. **Tint Color:** White (hoặc màu muốn)
3. **Speed:** 1.0
4. **Intensity:** 1.0
5. **Grid Columns (X):** 5 (hoặc số cột atlas bạn dùng)
6. **Grid Rows (Y):** 4 (hoặc số hàng atlas bạn dùng)
7. **Random Seed:** 0.7

---

## ✅ BƯỚC 3: Tạo Prefab FlyingNumbers

### 3.1 Tạo Empty GameObject
1. Hierarchy: **Right-click → Create Empty**
2. Tên: `FlyingNumbers_VFX`
3. Position: (0, 0, -5)

### 3.2 Thêm Particle System
1. Click object → **Add Component → Particle System**

### 3.3 Setup Particle System

**Main Module:**
- Loop: ✅
- Duration: 5.0
- Start Lifetime: 2.0
- Start Size: 0.5

**Emission:**
- Rate over Time: 50

**Velocity over Lifetime:**
- Enabled: ✅
- X: (-1, 1) - random horizontal
- Y: (1.5, 2.5) - upward
- Z: 0

**Size over Lifetime:**
- Enabled: ✅
- Size: Curve (1 → 1 → 0.3)

**Renderer:**
- Material: `Mat_FlyingNumbers`
- Render Mode: Billboard
- Sorting Order: 10

---

## ✅ BƯỚC 4: Thêm Script

### 4.1 Gán Script
1. Click object
2. **Add Component → FlyingNumbersVFX**

### 4.2 Gán References
- **VFX Particle System:** Kéo particle system vào
- **Custom Material:** Kéo `Mat_FlyingNumbers` vào
- **Grid Columns X:** 5 (khớp với atlas)
- **Grid Rows Y:** 4 (khớp với atlas)

### 4.3 Tuỳ chỉnh Effect
- **Animation Speed:** 1.0
- **Intensity:** 1.0
- **Tint Color:** White
- **Particle Scale:** 0.5
- **Emission Rate:** 50
- **Particle Lifetime:** 2.0
- **Upward Force:** 2.0
- **Sideway Variation:** 1.0
- **Random Seed:** 0.7

---

## ✅ BƯỚC 5: Test & Tuning

### 5.1 Play trong editor
- Bấn Play
- Các con số sẽ bay lên ngẫu nhiên

### 5.2 Tuỳ chỉnh runtime
- **Upward Force** ↑: Particles bay nhanh hơn
- **Sideway Variation** ↑: Phân tán ngang hơn
- **Emission Rate** ↑: Nhiều số hơn
- **Tint Color:** Đổi màu
- **Random Seed:** Tăng để randomization nhiều hơn

---

## ✅ BƯỚC 6: Save Prefab

1. Tạo folder: `Assets/Prefabs/VFX`
2. Drag `FlyingNumbers_VFX` vào folder
3. Tên: `FlyingNumbers_VFX.prefab`

---

## 🎨 Sử Dụng trong Code

### 7.1 Basic Control
```csharp
FlyingNumbersVFX numbersVFX = GetComponent<FlyingNumbersVFX>();

// Thay đổi màu
numbersVFX.SetEffectColor(Color.green, 1.5f);

// Thay đổi tốc độ bay
numbersVFX.SetUpwardForce(3f);

// Thay đổi emission
numbersVFX.SetEmissionRate(100f);

// Pause/Resume
numbersVFX.PauseVFX(true);
```

### 7.2 Ví dụ: Theo chế độ game
```csharp
void Start()
{
    FlyingNumbersVFX numbers = GetComponent<FlyingNumbersVFX>();
    
    // Math mode - green numbers
    numbers.SetEffectColor(Color.green, 1.2f);
    numbers.SetEmissionRate(80f);
    numbers.SetUpwardForce(2.5f);
    
    // Hoặc Algorithm mode - cyan numbers
    // numbers.SetEffectColor(Color.cyan, 1.0f);
}
```

---

## 🔧 Troubleshooting

| Problem | Solution |
|---------|----------|
| Không hiển thị gì | 1. Check Material assign đúng<br>2. Check gridX, gridY đúng atlas<br>3. Check Camera sees layer |
| Số hiển thị sai/lỗi | 1. Check texture atlas import settings<br>2. Verify grid columns/rows<br>3. Re-import texture |
| Quá tối | Tăng Intensity hoặc change Tint Color sáng hơn |
| Particles không bay | Check Upward Force > 0 |
| Hiệu năng tệ | Giảm Emission Rate hoặc Lifetime |

---

## 💡 Advanced Tips

### 1. Multiple Layers
```csharp
// Layer 1 - Math operators (slow)
FlyingNumbers op = Instantiate(operatorPrefab);
op.SetUpwardForce(1f);
op.SetEffectColor(Color.yellow, 0.8f);

// Layer 2 - Numbers (fast)
FlyingNumbers num = Instantiate(numbersPrefab);
num.SetUpwardForce(2.5f);
num.SetEffectColor(Color.cyan, 1.2f);
```

### 2. Dynamic Grid Change
```csharp
// Nếu thay đổi texture atlas
numbers.SetGridSize(5, 2); // Nếu chỉ dùng 5x2 = 10 ô
```

### 3. Integrate với ImpactVFX
```csharp
// Trigger numbers khi có impact
void OnImpact()
{
    FlyingNumbersVFX numbers = GetComponent<FlyingNumbersVFX>();
    numbers.SetEmissionRate(150f);
    numbers.SetEffectColor(Color.red, 2f);
}
```

---

## 📝 Texture Atlas Generation Helper

Nếu cần generate texture atlas programmatically:

```csharp
// Tạo simple atlas từ text
// File: Assets/Script/TextureAtlasGenerator.cs (optional)

public static void GenerateNumbersAtlas()
{
    // Create 512x512 texture
    Texture2D atlas = new Texture2D(512, 512, TextureFormat.RGBA32, false);
    
    // Draw numbers 0-9 + operators
    string chars = "0123456789+-*=/().,";
    int cellSize = 128; // 4x4 grid
    
    // ... render each character to grid ...
    
    // Save
    byte[] pngData = atlas.EncodeToPNG();
    System.IO.File.WriteAllBytes("Assets/Resources/Textures/NumbersAtlas.png", pngData);
}
```

---

## ✨ Recommendations

1. **Texture Quality:** Use clean, readable fonts
2. **Grid Size:** 5x4 là khá tối ưu (20 characters)
3. **Particle Lifetime:** 2-3s là đủ cho flying effect
4. **Emission Rate:** 50-100 tùy game intensity
5. **Performance:** Test FPS trên target device

---

## 📝 Setup Checklist

- ✅ Texture atlas tạo & import
- ✅ Material `Mat_FlyingNumbers` tạo
- ✅ Shader `Custom/FlyingNumbers` gán
- ✅ Particle System setup
- ✅ Script `FlyingNumbersVFX` gán
- ✅ Grid columns/rows khớp atlas
- ✅ Prefab save
- ✅ Test hiệu ứng

---

## 🎉 Hoàn tất!
Bây giờ bạn có hệ thống VFX bắn số hoàn chỉnh, support random chữ số từ atlas!
