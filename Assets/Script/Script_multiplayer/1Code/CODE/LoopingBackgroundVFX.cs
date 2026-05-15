using UnityEngine;

/// <summary>
/// LoopingBackgroundVFX - Hệ thống VFX looping cho background
/// Tính năng:
/// - Loop không giới hạn
/// - Tuỳ chỉnh material và shader tại runtime
/// - Flying particles/numbers
/// - Overlay trên background
/// </summary>
public class LoopingBackgroundVFX : MonoBehaviour
{
    [Header("=== PARTICLE SYSTEM ===")]
    public ParticleSystem vfxParticleSystem;
    
    [Header("=== MATERIAL & SHADER ===")]
    public Material customMaterial;
    public bool useCustomShader = true;

    [Header("=== LAYER & SORTING ===")]
    public Canvas targetCanvas;
    public int sortingOrder = 10;

    [Header("=== EFFECT PROPERTIES ===")]
    [Range(0f, 2f)]
    public float animationSpeed = 1f;

    [Range(0.1f, 5f)]
    public float intensity = 1f;

    public Color tintColor = Color.white;

    [Range(0.1f, 3f)]
    public float scale = 1f;

    [Header("=== SCROLL ANIMATION ===")]
    [Range(-2f, 2f)]
    public float scrollSpeedX = 0.5f;

    [Range(-2f, 2f)]
    public float scrollSpeedY = 0.5f;

    [Header("=== EMISSION SETTINGS ===")]
    [Range(10f, 200f)]
    public float emissionRate = 50f;

    [Range(0.5f, 5f)]
    public float particleLifetime = 2f;

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
            Debug.LogError("[LoopingBackgroundVFX] ParticleSystem chưa được gán!");
            return;
        }

        // Setup particle system
        emissionModule = vfxParticleSystem.emission;
        SetupParticleSystem();

        // Setup material
        if (customMaterial != null && useCustomShader)
        {
            // Tạo instance material riêng để không ảnh hưởng original
            materialInstance = new Material(customMaterial);
            GetComponent<ParticleSystemRenderer>().material = materialInstance;
            Debug.Log("[LoopingBackgroundVFX] Material instance được tạo");
        }

        // Setup canvas (nếu có)
        if (targetCanvas != null)
        {
            GetComponent<RectTransform>().SetParent(targetCanvas.transform, false);
            GetComponent<Canvas>().sortingOrder = sortingOrder;
        }

        Debug.Log("[LoopingBackgroundVFX] Khởi tạo thành công!");
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

        // Emission
        emissionModule.rateOverTime = emissionRate * intensity;

        // Velocity
        var velocityModule = vfxParticleSystem.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocityModule.y = new ParticleSystem.MinMaxCurve(0.2f, 1f);

        // Size over lifetime
        var sizeModule = vfxParticleSystem.sizeOverLifetime;
        sizeModule.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.5f, 1),
            new Keyframe(1, 0.5f)
        );
        sizeModule.size = new ParticleSystem.MinMaxCurve(scale, sizeCurve);

        Debug.Log("[LoopingBackgroundVFX] Particle system setup xong");
    }

    /// <summary>
    /// Cập nhật properties effect trong real-time
    /// </summary>
    void UpdateEffectProperties()
    {
        if (materialInstance != null && useCustomShader)
        {
            // Update shader properties
            materialInstance.SetFloat("_Speed", animationSpeed);
            materialInstance.SetFloat("_Intensity", intensity);
            materialInstance.SetFloat("_ScrollX", scrollSpeedX);
            materialInstance.SetFloat("_ScrollY", scrollSpeedY);
            materialInstance.SetColor("_Color", tintColor);
            materialInstance.SetFloat("_Scale", scale);
        }

        // Update emission
        if (emissionModule.enabled)
        {
            emissionModule.rateOverTime = emissionRate * intensity;
        }

        // Update particle lifetime
        var mainModule = vfxParticleSystem.main;
        mainModule.startLifetime = particleLifetime;
    }

    /// <summary>
    /// Set color và intensity cùng lúc
    /// </summary>
    public void SetEffectColor(Color color, float newIntensity = 1f)
    {
        tintColor = color;
        intensity = newIntensity;

        if (materialInstance != null)
        {
            materialInstance.SetColor("_Color", color);
            materialInstance.SetFloat("_Intensity", newIntensity);
        }
    }

    /// <summary>
    /// Set animation speed
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = Mathf.Clamp(speed, 0.1f, 2f);
    }

    /// <summary>
    /// Set emission rate
    /// </summary>
    public void SetEmissionRate(float rate)
    {
        emissionRate = Mathf.Clamp(rate, 10f, 200f);
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
        scale = 1f;
        scrollSpeedX = 0.5f;
        scrollSpeedY = 0.5f;
        emissionRate = 50f;
        particleLifetime = 2f;
        
        Initialize();
    }
}
