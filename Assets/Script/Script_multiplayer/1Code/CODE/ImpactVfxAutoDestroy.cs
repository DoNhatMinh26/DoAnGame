using UnityEngine;

/// <summary>
/// Script tự động hủy VFX khi ParticleSystem chạy xong.
/// Gắn vào prefab Impact VFX để tự hủy mà không cần code phứcế tạp.
/// </summary>
public class ImpactVfxAutoDestroy : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 2f; // Thời gian chờ trước khi hủy (giây)

    private ParticleSystem particleSystem;

    private void Start()
    {
        // Tìm ParticleSystem trên GameObject này hoặc child
        particleSystem = GetComponentInChildren<ParticleSystem>();
        
        if (particleSystem == null)
        {
            Debug.LogWarning($"[ImpactVfxAutoDestroy] Không tìm thấy ParticleSystem trên {gameObject.name}!");
            // Nếu không có ParticleSystem, hủy sau 1 giây
            Destroy(gameObject, 1f);
            return;
        }

        // Hủy GameObject sau khi ParticleSystem chạy xong + delay thêm
        float duration = particleSystem.main.duration + destroyDelay;
        Debug.Log($"[ImpactVfxAutoDestroy] VFX sẽ hủy sau {duration:F2}s (duration={particleSystem.main.duration:F2}s + delay={destroyDelay}s)");
        Destroy(gameObject, duration);
    }
}
