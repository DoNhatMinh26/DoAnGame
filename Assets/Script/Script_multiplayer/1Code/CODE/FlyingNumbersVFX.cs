using UnityEngine;

/// <summary>
/// FlyingNumbersVFX - Bắn các con số/chữ ngẫu nhiên từ danh sách sprite riêng
/// Tính năng:
/// - Random chữ số 0-9 + dấu phép toán từ texture/sprite riêng lẻ
/// - Tự ghép các texture thành atlas runtime
/// - Mỗi particle giữ nguyên frame sau khi spawn
/// - Hiệu năng tối ưu
/// - Tuỳ chỉnh dễ dàng
/// </summary>
public class FlyingNumbersVFX : MonoBehaviour
{
    [Header("=== PARTICLE SYSTEM ===")]
    public ParticleSystem vfxParticleSystem;

    [Header("=== SYMBOL SPRITES ===")]
    [Tooltip("Kéo từng Sprite riêng lẻ vào đây. Mỗi particle sẽ chọn 1 hình cố định khi spawn.")]
    public Sprite[] symbolSprites;

    [Tooltip("Số cột của atlas runtime. Nếu để 5 thì 15 hình sẽ thành 5x3.")]
    public int atlasColumns = 5;
    
    [Header("=== MATERIAL & SHADER ===")]
    public Material customMaterial;
    public bool useCustomShader = true;

    [Header("=== TINT ===")]
    public Color tintColor = Color.white;

    [Header("=== LAYER & SORTING ===")]
    public int sortingOrder = 10;

    [Header("=== EFFECT PROPERTIES ===")]
    [Range(0f, 3f)]
    public float animationSpeed = 1f;

    private Texture2D runtimeAtlasTexture;

    [Range(0.1f, 3f)]
    public float intensity = 1f;
    public float particleScale = 0.5f;

    [Header("=== EMISSION SETTINGS ===")]
    [Range(10f, 200f)]
    public float emissionRate = 50f;

    [Range(0.5f, 5f)]
    public float particleLifetime = 2f;

    [Header("=== VELOCITY SETTINGS ===")]
    [Range(0f, 5f)]
    public float upwardForce = 2f;

    [Range(-2f, 2f)]
    public float sidewayVariation = 1f;

    [Header("=== RANDOMIZATION ===")]
    [Range(0f, 1f)]
    public float randomSeed = 0.7f;

    private ParticleSystem.EmissionModule emissionModule;
    private Material materialInstance;

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        UpdateEffectProperties();
    }

    void OnDisable()
    {
        if (vfxParticleSystem != null)
            vfxParticleSystem.Stop();
    }

    void OnEnable()
    {
        if (vfxParticleSystem != null)
            vfxParticleSystem.Play();
    }

    /// <summary>
    /// Khởi tạo hệ thống VFX
    /// </summary>
    public void Initialize()
    {
        if (vfxParticleSystem == null)
        {
            Debug.LogError("[FlyingNumbersVFX] ParticleSystem chưa được gán!");
            return;
        }

        // Setup particle system
        emissionModule = vfxParticleSystem.emission;

        // Setup material
        if (customMaterial != null && useCustomShader)
        {
            materialInstance = new Material(customMaterial);
            GetComponent<ParticleSystemRenderer>().material = materialInstance;
            UpdateMaterialProperties();
            Debug.Log("[FlyingNumbersVFX] Material instance được tạo");
        }

        SetupParticleSystem();

        Debug.Log("[FlyingNumbersVFX] Khởi tạo thành công!");
    }

    /// <summary>
    /// Thiết lập particle system
    /// </summary>
    void SetupParticleSystem()
    {
        var mainModule = vfxParticleSystem.main;
        mainModule.loop = true;
        mainModule.duration = 5f;
        mainModule.startLifetime = particleLifetime;
        mainModule.startSize = particleScale;

        // Emission
        emissionModule.rateOverTime = emissionRate * intensity;

        // Velocity - particles bay lên trên với variation
        var velocityModule = vfxParticleSystem.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.x = new ParticleSystem.MinMaxCurve(-sidewayVariation, sidewayVariation);
        velocityModule.y = new ParticleSystem.MinMaxCurve(upwardForce - 0.5f, upwardForce + 0.5f);
        velocityModule.z = 0;

        // Size over lifetime - fade out effect
        var sizeModule = vfxParticleSystem.sizeOverLifetime;
        sizeModule.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0, 1),
            new Keyframe(0.7f, 1),
            new Keyframe(1, 0.3f)
        );
        sizeModule.size = new ParticleSystem.MinMaxCurve(1, sizeCurve);

        // Mỗi particle chọn 1 sprite cố định ngay lúc spawn và giữ nguyên đến khi chết
        var sheetModule = vfxParticleSystem.textureSheetAnimation;
        sheetModule.enabled = true;
        sheetModule.mode = ParticleSystemAnimationMode.Sprites;
        sheetModule.frameOverTime = 0f;
        sheetModule.cycleCount = 1;
        sheetModule.rowMode = ParticleSystemAnimationRowMode.Random;

        int symbolCount = GetSymbolCount();
        for (int i = 0; i < symbolCount; i++)
        {
            if (symbolSprites[i] != null)
            {
                sheetModule.AddSprite(symbolSprites[i]);
            }
        }

        sheetModule.animation = ParticleSystemAnimationType.WholeSheet;
        sheetModule.startFrame = new ParticleSystem.MinMaxCurve(0f, Mathf.Max(0, symbolCount - 1));

        if (symbolCount <= 0)
        {
            Debug.LogWarning("[FlyingNumbersVFX] Chưa gán symbolSprites. Particle sẽ không hiển thị đúng.");
        }

        // Rotate randomly
        var rotationModule = vfxParticleSystem.rotationOverLifetime;
        rotationModule.enabled = false;

        Debug.Log("[FlyingNumbersVFX] Particle system setup xong");
    }

    /// <summary>
    /// Update material properties từ script
    /// </summary>
    void UpdateMaterialProperties()
    {
        if (materialInstance != null)
        {
            materialInstance.SetColor("_Color", tintColor);
            materialInstance.SetFloat("_Speed", animationSpeed);
            materialInstance.SetFloat("_Intensity", intensity);
            if (runtimeAtlasTexture != null)
            {
                materialInstance.mainTexture = runtimeAtlasTexture;
            }
        }
    }

    /// <summary>
    /// Cập nhật properties effect trong real-time
    /// </summary>
    void UpdateEffectProperties()
    {
        UpdateMaterialProperties();

        // Update emission
        if (emissionModule.enabled)
        {
            emissionModule.rateOverTime = emissionRate * intensity;
        }

        // Update particle lifetime
        var mainModule = vfxParticleSystem.main;
        mainModule.startLifetime = particleLifetime;
        mainModule.startSize = particleScale;

        // Update velocity
        var velocityModule = vfxParticleSystem.velocityOverLifetime;
        velocityModule.x = new ParticleSystem.MinMaxCurve(-sidewayVariation, sidewayVariation);
        velocityModule.y = new ParticleSystem.MinMaxCurve(upwardForce - 0.5f, upwardForce + 0.5f);
    }

    /// <summary>
    /// Set color và intensity
    /// </summary>
    public void SetEffectColor(Color color, float newIntensity = 1f)
    {
        tintColor = color;
        intensity = Mathf.Clamp(newIntensity, 0.1f, 3f);
    }

    /// <summary>
    /// Set animation speed
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = Mathf.Clamp(speed, 0.1f, 3f);
    }

    /// <summary>
    /// Set emission rate
    /// </summary>
    public void SetEmissionRate(float rate)
    {
        emissionRate = Mathf.Clamp(rate, 10f, 200f);
    }

    /// <summary>
    /// Set upward flying force
    /// </summary>
    public void SetUpwardForce(float force)
    {
        upwardForce = Mathf.Clamp(force, 0f, 5f);
    }

    /// <summary>
    /// Set sprite list at runtime
    /// </summary>
    public void SetSymbolSprites(Sprite[] sprites)
    {
        symbolSprites = sprites;
        Initialize();
    }

    [ContextMenu("Refresh FlyingNumbersVFX")]
    void RefreshFromInspector()
    {
        // Helpful when editing in inspector: re-run initialize to update textureSheetAnimation
        if (vfxParticleSystem == null)
        {
            Debug.LogWarning("[FlyingNumbersVFX] Vfx Particle System reference is null.");
            return;
        }

        // Clear existing sprites in the module then re-add
        var sheetModule = vfxParticleSystem.textureSheetAnimation;
        if (!sheetModule.enabled)
            sheetModule.enabled = true;

        // There is no direct Clear API; recreate module by disabling/enabling
        sheetModule.enabled = false;
        sheetModule.enabled = true;

        int count = GetSymbolCount();
        for (int i = 0; i < count; i++)
        {
            if (symbolSprites[i] != null)
                sheetModule.AddSprite(symbolSprites[i]);
        }

        Debug.Log("[FlyingNumbersVFX] Refreshed sprite list from inspector.");
    }

    /// <summary>
    /// Pause/Resume particle system
    /// </summary>
    public void PauseVFX(bool pause)
    {
        if (vfxParticleSystem != null)
        {
            if (pause)
                vfxParticleSystem.Pause();
            else
                vfxParticleSystem.Play();
        }
    }

    /// <summary>
    /// Reset to default
    /// </summary>
    public void ResetToDefault()
    {
        animationSpeed = 1f;
        intensity = 1f;
        tintColor = Color.white;
        particleScale = 0.5f;
        emissionRate = 50f;
        particleLifetime = 2f;
        upwardForce = 2f;
        sidewayVariation = 1f;
        
        Initialize();
    }

    int GetSymbolCount()
    {
        return symbolSprites == null ? 0 : symbolSprites.Length;
    }
}
