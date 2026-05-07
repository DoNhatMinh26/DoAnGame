using UnityEngine;

/// <summary>
/// Gán lên Player1Character / Player2Character trong GameplayPanel
/// và Win_image_animation / Lost_image_animation trong Wins panel.
///
/// Nhận avatarId → load AvatarData từ AvatarManager → set AnimatorController + Sprite.
/// Expose TriggerIdle / TriggerHappy / TriggerSad để BattleController và WinsController gọi.
/// </summary>
public class AvatarCharacterDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private SpriteRenderer characterSprite; // optional — nếu dùng SpriteRenderer

    // Hash các parameter để tránh string lookup mỗi frame
    private static readonly int HashIdle  = Animator.StringToHash("TriggerIdle");
    private static readonly int HashHappy = Animator.StringToHash("TriggerHappy");
    private static readonly int HashSad   = Animator.StringToHash("TriggerSad");

    private int currentAvatarId = -1;

    private void Awake()
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Load AvatarData theo id và apply AnimatorController + Sprite.
    /// Tự động reset về Idle sau khi set.
    /// </summary>
    public void SetAvatar(int avatarId)
    {
        if (avatarId == currentAvatarId) return; // Không set lại nếu đã đúng

        if (AvatarManager.Instance == null)
        {
            Debug.LogWarning("[AvatarCharacterDisplay] AvatarManager chưa sẵn sàng.");
            return;
        }

        AvatarData data = AvatarManager.Instance.GetById(avatarId);
        if (data == null)
        {
            // Fallback về default nếu không tìm thấy
            data = AvatarManager.Instance.GetCurrentAvatar();
            if (data == null)
            {
                Debug.LogWarning($"[AvatarCharacterDisplay] Không tìm thấy AvatarData id={avatarId}");
                return;
            }
        }

        // Gán AnimatorController
        if (characterAnimator != null && data.animatorController != null)
        {
            characterAnimator.runtimeAnimatorController = data.animatorController;
        }

        // Gán sprite (nếu dùng SpriteRenderer)
        if (characterSprite != null && data.fullAvatar != null)
        {
            characterSprite.sprite = data.fullAvatar;
        }

        currentAvatarId = avatarId;
        TriggerIdle();

        Debug.Log($"[AvatarCharacterDisplay] ✅ Set avatar: {data.avatarName} (id={avatarId}) trên {gameObject.name}");
    }

    public void TriggerIdle()
    {
        if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
            characterAnimator.SetTrigger(HashIdle);
    }

    public void TriggerHappy()
    {
        if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
            characterAnimator.SetTrigger(HashHappy);
    }

    public void TriggerSad()
    {
        if (characterAnimator != null && characterAnimator.runtimeAnimatorController != null)
            characterAnimator.SetTrigger(HashSad);
    }
}
