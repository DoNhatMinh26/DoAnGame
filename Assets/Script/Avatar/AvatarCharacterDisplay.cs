using UnityEngine;

/// <summary>
/// Gán lên Player1Character / Player2Character trong GameplayPanel
/// và WinnerCharacter / LoserCharacter trong Wins panel.
///
/// Nhận avatarId → bật đúng skin con trên cả 3 PSB, tắt 3 skin còn lại.
/// TriggerIdle/Happy/Sad → gọi trigger trên Animator của từng PSB đang active.
///
/// 3 PSB có 3 bộ xương riêng biệt — không swap AnimatorController.
/// Controller đã gán sẵn trong từng PSB khi import.
///
/// Skin con:
///   Character Meo / Character Meo_Sad : mascost1–mascost4
///   MeoGoc34 Fix                      : Meo1–Meo4
/// avatarId 0 → skin index 0 (mascost1/Meo1), avatarId 1 → index 1, ...
/// </summary>
public class AvatarCharacterDisplay : MonoBehaviour
{
    [Header("3 PSB GameObjects")]
    [SerializeField] private GameObject characterMeo;       // Character Meo (Idle + Happy)
    [SerializeField] private GameObject characterMeoSad;    // Character Meo_Sad (Sad)
    [SerializeField] private GameObject meoGoc34Fix;        // MeoGoc34 Fix (Attack 3/4)

    // Tên skin con theo thứ tự avatarId
    private static readonly string[] SkinNamesMeo    = { "mascost1", "mascost2", "mascost3", "mascost4" };
    private static readonly string[] SkinNamesMeoSad = { "mascost1", "mascost2", "mascost3", "mascost4" };
    private static readonly string[] SkinNamesMeo34  = { "Meo1",     "Meo2",     "Meo3",     "Meo4"     };

    // Hash trigger parameters
    private static readonly int HashIdle  = Animator.StringToHash("TriggerIdle");
    private static readonly int HashHappy = Animator.StringToHash("TriggerHappy");
    private static readonly int HashSad   = Animator.StringToHash("TriggerSad");

    private int currentAvatarId = -1;

    // Cache Animator của từng PSB
    private Animator animMeo;
    private Animator animMeoSad;
    private Animator animMeo34;

    private void Awake()
    {
        if (characterMeo    != null) animMeo    = characterMeo.GetComponent<Animator>();
        if (characterMeoSad != null) animMeoSad = characterMeoSad.GetComponent<Animator>();
        if (meoGoc34Fix     != null) animMeo34  = meoGoc34Fix.GetComponent<Animator>();
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API — Skin selection
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Bật đúng skin tương ứng avatarId trên cả 3 PSB, tắt các skin còn lại.
    /// </summary>
    public void SetAvatar(int avatarId)
    {
        if (avatarId == currentAvatarId) return;

        ApplySkin(characterMeo,    SkinNamesMeo,    avatarId);
        ApplySkin(characterMeoSad, SkinNamesMeoSad, avatarId);
        ApplySkin(meoGoc34Fix,     SkinNamesMeo34,  avatarId);

        currentAvatarId = avatarId;
        Debug.Log($"[AvatarCharacterDisplay] ✅ Set avatar id={avatarId} trên {gameObject.name}");
    }

    public int GetCurrentAvatarId() => currentAvatarId;

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API — Animation triggers
    // Gọi trigger trên tất cả Animator của 3 PSB.
    // PSB nào active sẽ phản hồi animation — logic show/hide PSB theo sự kiện tính sau.
    // ─────────────────────────────────────────────────────────────

    public void TriggerIdle()  => SetTriggerAll(HashIdle);
    public void TriggerHappy() => SetTriggerAll(HashHappy);
    public void TriggerSad()   => SetTriggerAll(HashSad);

    // ─────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────

    private void ApplySkin(GameObject parent, string[] skinNames, int avatarId)
    {
        if (parent == null) return;

        int index = Mathf.Clamp(avatarId, 0, skinNames.Length - 1);

        for (int i = 0; i < skinNames.Length; i++)
        {
            Transform skin = parent.transform.Find(skinNames[i]);
            if (skin != null)
                skin.gameObject.SetActive(i == index);
            else
                Debug.LogWarning($"[AvatarCharacterDisplay] Không tìm thấy '{skinNames[i]}' trong '{parent.name}'");
        }
    }

    private void SetTriggerAll(int hash)
    {
        TrySetTrigger(animMeo,    hash);
        TrySetTrigger(animMeoSad, hash);
        TrySetTrigger(animMeo34,  hash);
    }

    private void TrySetTrigger(Animator anim, int hash)
    {
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.SetTrigger(hash);
    }
}
