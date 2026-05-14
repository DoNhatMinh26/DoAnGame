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

    [Header("Projectile Points")]
    [SerializeField] private Transform attackMuzzle;
    [SerializeField] private Transform hitPoint;

    // Tên skin con theo thứ tự avatarId
    private static readonly string[] SkinNamesMeo    = { "mascost1", "mascost2", "mascost3", "mascost4" };
    private static readonly string[] SkinNamesMeoSad = { "mascost1", "mascost2", "mascost3", "mascost4" };
    private static readonly string[] SkinNamesMeo34  = { "Meo1",     "Meo2",     "Meo3",     "Meo4"     };

    // Hash trigger parameters
    private static readonly int HashIdle   = Animator.StringToHash("TriggerIdle");
    private static readonly int HashHappy  = Animator.StringToHash("TriggerHappy");
    private static readonly int HashSad    = Animator.StringToHash("TriggerSad");
    private static readonly int HashAttack = Animator.StringToHash("TriggerAttack");

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
        
        // ✅ Validate references ngay khi Awake để phát hiện lỗi Inspector sớm
        ValidateReferences();
    }

    /// <summary>
    /// Kiểm tra các PSB references có bị null hoặc duplicate không.
    /// Log warning rõ ràng để dễ fix trong Inspector.
    /// </summary>
    private void ValidateReferences()
    {
        if (characterMeo == null)
            Debug.LogError($"[AvatarCharacterDisplay] ❌ {gameObject.name}: 'characterMeo' (Character Meo) chưa được assign trong Inspector!");
        
        if (characterMeoSad == null)
            Debug.LogError($"[AvatarCharacterDisplay] ❌ {gameObject.name}: 'characterMeoSad' (Character Meo_Sad) chưa được assign trong Inspector!");
        
        if (meoGoc34Fix == null)
            Debug.LogError($"[AvatarCharacterDisplay] ❌ {gameObject.name}: 'meoGoc34Fix' (MeoGoc34 Fix) chưa được assign trong Inspector!");

        // Kiểm tra duplicate references
        if (characterMeo != null && characterMeoSad != null && characterMeo == characterMeoSad)
            Debug.LogError($"[AvatarCharacterDisplay] ❌ {gameObject.name}: 'characterMeo' và 'characterMeoSad' đang trỏ vào cùng 1 GameObject! Hãy fix trong Inspector.");
        
        if (characterMeo != null && meoGoc34Fix != null && characterMeo == meoGoc34Fix)
            Debug.LogError($"[AvatarCharacterDisplay] ❌ {gameObject.name}: 'characterMeo' và 'meoGoc34Fix' đang trỏ vào cùng 1 GameObject! Hãy fix trong Inspector.");
        
        if (characterMeoSad != null && meoGoc34Fix != null && characterMeoSad == meoGoc34Fix)
            Debug.LogError($"[AvatarCharacterDisplay] ❌ {gameObject.name}: 'characterMeoSad' và 'meoGoc34Fix' đang trỏ vào cùng 1 GameObject! Hãy fix trong Inspector.");
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API — Skin selection
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Bật đúng skin tương ứng avatarId trên cả 3 PSB, tắt các skin còn lại.
    /// Tự động gọi ShowIdle() ở cuối để bắt đầu ở trạng thái Idle.
    /// </summary>
    public void SetAvatar(int avatarId)
    {
        if (avatarId == currentAvatarId)
        {
            Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → SetAvatar called with same avatarId={avatarId} — forcing ShowIdle() to reset visuals.");
            ShowIdle();
            return;
        }

        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → SetAvatar(avatarId={avatarId}) START");

        // ✅ FIX: Validate không có duplicate references
        ValidateReferences();

        ApplySkin(characterMeo,    SkinNamesMeo,    avatarId);
        if (characterMeoSad != null && characterMeoSad != characterMeo)
            ApplySkin(characterMeoSad, SkinNamesMeoSad, avatarId);
        if (meoGoc34Fix != null && meoGoc34Fix != characterMeo && meoGoc34Fix != characterMeoSad)
            ApplySkin(meoGoc34Fix, SkinNamesMeo34, avatarId);

        currentAvatarId = avatarId;
        
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → SetAvatar(avatarId={avatarId}) DONE, calling ShowIdle()...");
        
        // ✅ Bắt đầu ở trạng thái Idle - chỉ hiển thị Character Meo, ẩn 2 PSB còn lại
        ShowIdle();
        
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] ✅ {gameObject.name} → SetAvatar(avatarId={avatarId}) COMPLETE");
        
        // Log trạng thái sau khi SetAvatar
        LogCurrentState();
    }

    /// <summary>
    /// Bật đúng skin tương ứng avatarId trên cả 3 PSB, tắt các skin còn lại.
    /// KHÔNG tự động gọi ShowIdle() — dùng cho WinsPanel để tránh animation double-trigger.
    /// ✅ CRITICAL FIX: Reset PSB visibility về trạng thái sạch (tất cả INACTIVE) trước khi apply skin.
    /// Sau khi gọi method này, phải gọi ShowHappy()/ShowSad()/ShowAttack() để hiển thị animation.
    /// </summary>
    public void SetAvatarWithoutAnimation(int avatarId)
    {
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → SetAvatarWithoutAnimation(avatarId={avatarId}) START");

        // ✅ CRITICAL FIX: Reset tất cả PSB về INACTIVE trước khi apply skin
        // Tránh overlapping characters từ trạng thái cũ (GameplayPanel → WinsPanel)
        SetPSBVisibility(showMeo: false, showMeoSad: false, showMeo34: false);
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Reset all PSBs to INACTIVE");

        // ✅ FIX: Validate không có duplicate references trước khi apply
        ValidateReferences();

        // ✅ FIX: Chỉ apply skin cho PSB nào tồn tại, tránh duplicate call
        if (characterMeo != null)
        {
            ApplySkin(characterMeo, SkinNamesMeo, avatarId);
        }
        
        if (characterMeoSad != null && characterMeoSad != characterMeo)
        {
            ApplySkin(characterMeoSad, SkinNamesMeoSad, avatarId);
        }
        
        if (meoGoc34Fix != null && meoGoc34Fix != characterMeo && meoGoc34Fix != characterMeoSad)
        {
            ApplySkin(meoGoc34Fix, SkinNamesMeo34, avatarId);
        }

        currentAvatarId = avatarId;
        
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] ✅ {gameObject.name} → SetAvatarWithoutAnimation(avatarId={avatarId}) COMPLETE (NO animation triggered)");
        
        // Log trạng thái sau khi SetAvatar
        LogCurrentState();
    }

    public int GetCurrentAvatarId() => currentAvatarId;

    public Transform AttackMuzzle => ResolveCachedTransform(ref attackMuzzle, "AttackMuzzle");
    public Transform HitPoint => ResolveCachedTransform(ref hitPoint, "HitPoint");

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API — Animation triggers
    // Gọi trigger trên tất cả Animator của 3 PSB.
    // PSB nào active sẽ phản hồi animation — logic show/hide PSB theo sự kiện tính sau.
    // ─────────────────────────────────────────────────────────────

    public void TriggerIdle()   => SetTriggerAll(HashIdle);
    public void TriggerHappy()  => SetTriggerAll(HashHappy);
    public void TriggerSad()    => SetTriggerAll(HashSad);
    public void TriggerAttack() => SetTriggerAll(HashAttack);

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API — Show/Hide PSB theo sự kiện battle
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Hiển thị Character Meo (Idle/Happy), ẩn Meo_Sad và Meo34, trigger Idle animation.
    /// Dùng trong Question Time (đang chờ trả lời).
    /// </summary>
    public void ShowIdle()
    {
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → ShowIdle() START");
        SetPSBVisibility(showMeo: true, showMeoSad: false, showMeo34: false);
        TriggerIdle();
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] ✅ {gameObject.name} → ShowIdle() DONE (Meo=ON, MeoSad=OFF, Meo34=OFF, Trigger=Idle)");
        LogCurrentState();
    }

    /// <summary>
    /// Hiển thị Character Meo (Idle/Happy), ẩn Meo_Sad và Meo34, trigger Happy animation.
    /// Dùng trong Summary Time khi player được cộng điểm (đúng hoặc nhanh hơn).
    /// </summary>
    public void ShowHappy()
    {
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → ShowHappy() START");
        SetPSBVisibility(showMeo: true, showMeoSad: false, showMeo34: false);
        TriggerHappy();
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] ✅ {gameObject.name} → ShowHappy() DONE (Meo=ON, MeoSad=OFF, Meo34=OFF, Trigger=Happy)");
        LogCurrentState();
    }

    /// <summary>
    /// Hiển thị Character Meo_Sad, ẩn Meo và Meo34, trigger Sad animation.
    /// Dùng trong Summary Time khi player trả lời sai hoặc không được cộng điểm.
    /// </summary>
    public void ShowSad()
    {
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → ShowSad() START");
        SetPSBVisibility(showMeo: false, showMeoSad: true, showMeo34: false);
        TriggerSad();
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] ✅ {gameObject.name} → ShowSad() DONE (Meo=OFF, MeoSad=ON, Meo34=OFF, Trigger=Sad)");
        LogCurrentState();
    }

    /// <summary>
    /// Hiển thị MeoGoc34 Fix (góc 3/4), ẩn Meo và Meo_Sad, trigger Attack animation.
    /// Dùng trong Summary Time khi player thắng (đúng hoặc nhanh hơn) → tấn công đối thủ.
    /// Animation này sẽ có hành động ném (sau này có thể spawn projectile).
    /// </summary>
    public void ShowAttack()
    {
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → ShowAttack() START");
        SetPSBVisibility(showMeo: false, showMeoSad: false, showMeo34: true);
        TriggerAttack();
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] ✅ {gameObject.name} → ShowAttack() DONE (Meo=OFF, MeoSad=OFF, Meo34=ON, Trigger=Attack)");
        LogCurrentState();
    }

    /// <summary>
    /// Hiển thị Attack animation, sau đó tự động chuyển sang Happy animation.
    /// Dùng trong Summary Time khi player thắng (1 đúng 1 sai) → Attack → Happy.
    /// Attack animation chạy 1 lần (không loop), sau ~1.5 giây chuyển sang Happy.
    /// </summary>
    public void ShowAttackThenHappy(float attackDuration = 1.5f)
    {
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → ShowAttackThenHappy() START (attackDuration={attackDuration}s)");
        
        // Hiển thị Attack trước
        SetPSBVisibility(showMeo: false, showMeoSad: false, showMeo34: true);
        TriggerAttack();
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Attack animation started");
        
        // Sau attackDuration giây, chuyển sang Happy
        Invoke(nameof(TransitionToHappyAfterAttack), attackDuration);
        
        LogCurrentState();
    }

    /// <summary>
    /// Chuyển từ Attack sang Happy (được gọi bởi ShowAttackThenHappy sau delay)
    /// </summary>
    private void TransitionToHappyAfterAttack()
    {
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → TransitionToHappyAfterAttack() - Switching to Happy");
        ShowHappy();
    }

    // ─────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────

    private void ApplySkin(GameObject parent, string[] skinNames, int avatarId)
    {
        if (parent == null) return;

        int index = Mathf.Clamp(avatarId, 0, skinNames.Length - 1);
        
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → ApplySkin on '{parent.name}': avatarId={avatarId}, skinIndex={index}, skinName='{skinNames[index]}'");

        for (int i = 0; i < skinNames.Length; i++)
        {
            Transform skin = parent.transform.Find(skinNames[i]);
            if (skin != null)
            {
                bool shouldActivate = (i == index);
                skin.gameObject.SetActive(shouldActivate);
                Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Skin '{skinNames[i]}' in '{parent.name}' = {(shouldActivate ? "ACTIVE" : "INACTIVE")}");
            }
            else
            {
                Debug.LogWarning($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → ⚠️ Không tìm thấy skin '{skinNames[i]}' trong '{parent.name}'");
            }
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
        if (anim == null || anim.runtimeAnimatorController == null)
            return;

        // ✅ FIX: Check if parameter exists before resetting/setting
        if (!HasParameter(anim, hash))
        {
            Debug.LogWarning($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Animator parameter hash {hash} does not exist in {anim.gameObject.name}");
            return;
        }

        // Reset all triggers first
        if (HasParameter(anim, HashIdle))   anim.ResetTrigger(HashIdle);
        if (HasParameter(anim, HashHappy))  anim.ResetTrigger(HashHappy);
        if (HasParameter(anim, HashSad))    anim.ResetTrigger(HashSad);
        if (HasParameter(anim, HashAttack)) anim.ResetTrigger(HashAttack);
        
        // Set the target trigger
        anim.SetTrigger(hash);
    }

    /// <summary>
    /// Check if animator has a parameter with the given hash
    /// </summary>
    private bool HasParameter(Animator anim, int hash)
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            return false;

        foreach (var param in anim.parameters)
        {
            if (param.nameHash == hash)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Set visibility của 3 PSB (Meo, MeoSad, Meo34).
    /// Chỉ 1 PSB được hiển thị tại 1 thời điểm.
    /// ✅ FIX: Nếu field bị null (lỗi Inspector), tìm theo tên trong children để vẫn ẩn được.
    /// ✅ FIX: Nếu có duplicate (2 field trỏ vào cùng 1 GameObject), ưu tiên state cuối cùng.
    /// </summary>
    private void SetPSBVisibility(bool showMeo, bool showMeoSad, bool showMeo34)
    {
        Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → SetPSBVisibility(Meo={showMeo}, MeoSad={showMeoSad}, Meo34={showMeo34})");
        
        // ✅ FIX: Nếu field null, tìm theo tên để vẫn có thể ẩn/hiện
        GameObject resolvedMeo    = characterMeo    ?? FindChildByName("Character Meo");
        GameObject resolvedMeoSad = characterMeoSad ?? FindChildByName("Character Meo_Sad");
        GameObject resolvedMeo34  = meoGoc34Fix     ?? FindChildByName("MeoGoc34 Fix");

        // ✅ SIMPLE FIX: Không dùng Dictionary - set trực tiếp và check duplicate
        // Set Meo
        if (resolvedMeo != null)
        {
            resolvedMeo.SetActive(showMeo);
            Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Character Meo = {(showMeo ? "ACTIVE" : "INACTIVE")}");
        }
        else
        {
            Debug.LogWarning($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Character Meo is NULL (not found in children either)!");
        }
        
        // Set MeoSad (nếu không trùng với Meo)
        if (resolvedMeoSad != null)
        {
            if (resolvedMeoSad == resolvedMeo)
            {
                // Duplicate - ưu tiên showMeoSad
                resolvedMeoSad.SetActive(showMeoSad);
                Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Character Meo_Sad = {(showMeoSad ? "ACTIVE" : "INACTIVE")} (DUPLICATE with Meo, overridden)");
            }
            else
            {
                resolvedMeoSad.SetActive(showMeoSad);
                Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Character Meo_Sad = {(showMeoSad ? "ACTIVE" : "INACTIVE")}");
            }
        }
        else
        {
            Debug.LogWarning($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → Character Meo_Sad is NULL (not found in children either)!");
        }
        
        // Set Meo34 (nếu không trùng với Meo hoặc MeoSad)
        if (resolvedMeo34 != null)
        {
            if (resolvedMeo34 == resolvedMeo || resolvedMeo34 == resolvedMeoSad)
            {
                // Duplicate - ưu tiên showMeo34
                resolvedMeo34.SetActive(showMeo34);
                Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → MeoGoc34 Fix = {(showMeo34 ? "ACTIVE" : "INACTIVE")} (DUPLICATE, overridden)");
            }
            else
            {
                resolvedMeo34.SetActive(showMeo34);
                Debug.Log($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → MeoGoc34 Fix = {(showMeo34 ? "ACTIVE" : "INACTIVE")}");
            }
        }
        else
        {
            Debug.LogWarning($"[AvatarCharacterDisplay] [{GetRole()}] {gameObject.name} → MeoGoc34 Fix is NULL (not found in children either)!");
        }
    }

    /// <summary>
    /// Tìm child GameObject theo tên (dùng khi field bị null do lỗi Inspector)
    /// ✅ FIX: Với Battle Point (AttackMuzzle/HitPoint), ưu tiên tên theo side trước rồi mới fallback.
    /// </summary>
    private GameObject FindChildByName(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
            return null;

        // 1️⃣ TRY: Direct child (1 level deep)
        Transform found = transform.Find(childName);
        if (found != null) return found.gameObject;
        
        // 2️⃣ PRIORITY: For battle points, search by side-specific aliases first
        if (IsBattlePointName(childName))
        {
            GameObject activePSB = null;
            if (characterMeo != null && characterMeo.activeInHierarchy)
                activePSB = characterMeo;
            else if (characterMeoSad != null && characterMeoSad.activeInHierarchy)
                activePSB = characterMeoSad;
            else if (meoGoc34Fix != null && meoGoc34Fix.activeInHierarchy)
                activePSB = meoGoc34Fix;
            
            if (activePSB != null)
            {
                Debug.Log($"[AvatarCharacterDisplay] {gameObject.name}.FindChildByName('{childName}') → active PSB: {activePSB.name}");

                            string[] candidateNames = GetBattlePointCandidateNames(childName);
                            foreach (string candidateName in candidateNames)
                            {
                                GameObject foundInActivePsb = FindChildInSubtree(activePSB.transform, candidateName);
                                if (foundInActivePsb != null)
                                {
                                    Debug.Log($"[AvatarCharacterDisplay] {gameObject.name}.FindChildByName('{childName}') → Found '{candidateName}' in ACTIVE PSB at {GetHierarchyPath(foundInActivePsb.transform)}");
                                    return foundInActivePsb;
                                }
                            }

                            Debug.LogWarning($"[AvatarCharacterDisplay] {gameObject.name}.FindChildByName('{childName}') → Active PSB '{activePSB.name}' does not contain any alias: {string.Join(", ", candidateNames)}");
            }
            else
            {
                Debug.LogWarning($"[AvatarCharacterDisplay] {gameObject.name}.FindChildByName('{childName}') → ⚠️ NO ACTIVE PSB found!");
            }
        }
        
        // 3️⃣ FALLBACK: Search all children (including inactive) - for other transforms
        Debug.Log($"[AvatarCharacterDisplay] {gameObject.name}.FindChildByName('{childName}') → Fallback: searching all children...");
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                Debug.Log($"[AvatarCharacterDisplay] {gameObject.name}.FindChildByName('{childName}') → Found in fallback at {GetHierarchyPath(child)}");
                return child.gameObject;
            }
        }
        return null;
    }

    private static bool IsBattlePointName(string childName)
    {
        return childName == "HitPoint"
            || childName == "AttackMuzzle"
            || childName.StartsWith("HitPoint_")
            || childName.StartsWith("AttackMuzzle_");
    }

    private string[] GetBattlePointCandidateNames(string baseName)
    {
        string sideTag = GetBattlePointSideTag();
        if (string.IsNullOrEmpty(sideTag))
        {
            return new[] { baseName };
        }

        if (baseName == "HitPoint")
        {
            return new[]
            {
                $"HitPoint_{sideTag}",
                $"HitPoint_{sideTag}_1",
                $"HitPoint_{sideTag}_2",
                "HitPoint"
            };
        }

        if (baseName == "AttackMuzzle")
        {
            return new[]
            {
                $"AttackMuzzle_{sideTag}",
                $"AttackMuzzle_{sideTag}_1",
                $"AttackMuzzle_{sideTag}_2",
                "AttackMuzzle"
            };
        }

        return new[] { baseName };
    }

    private string GetBattlePointSideTag()
    {
        string characterName = gameObject.name;
        if (characterName.Contains("Player1") || characterName.Contains("Left"))
            return "Player1";

        if (characterName.Contains("Player2") || characterName.Contains("Right"))
            return "Player2";

        return string.Empty;
    }

    private GameObject FindChildInSubtree(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private Transform ResolveCachedTransform(ref Transform cached, string name)
    {
        // For HitPoint: always look for it on active character (don't cache if character inactive)
        if (name == "HitPoint" && cached != null && !cached.gameObject.activeInHierarchy)
        {
            Debug.Log($"[AvatarCharacterDisplay] {gameObject.name}.{name} was on inactive object, re-resolving...");
            cached = null;  // Force re-resolve
        }

        if (cached != null)
        {
            Debug.Log($"[AvatarCharacterDisplay] {gameObject.name}.{name} cached hit: {GetHierarchyPath(cached)}");
            return cached;
        }

        var found = FindChildByName(name);
        if (found != null)
        {
            cached = found.transform;
            Debug.Log($"[AvatarCharacterDisplay] {gameObject.name}.{name} RESOLVED to: {GetHierarchyPath(cached)}");
        }
        else
        {
            Debug.LogWarning($"[AvatarCharacterDisplay] {gameObject.name}.{name} NOT FOUND!");
        }

        return cached;
    }
    
    private string GetHierarchyPath(Transform t)
    {
        if (t == null) return "NULL";
        string path = t.name;
        Transform parent = t.parent;
        while (parent != null && parent != gameObject.transform)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return gameObject.name + "/" + path;
    }

    /// <summary>
    /// Lấy role hiện tại (HOST/CLIENT) để log
    /// </summary>
    private string GetRole()
    {
        var net = Unity.Netcode.NetworkManager.Singleton;
        if (net == null) return "OFFLINE";
        if (net.IsServer) return "HOST";
        return "CLIENT";
    }

    /// <summary>
    /// Log trạng thái hiện tại của tất cả PSB và skin (dùng để debug)
    /// </summary>
    public void LogCurrentState()
    {
        string role = GetRole();
        Debug.Log($"[AvatarCharacterDisplay] [{role}] ===== CURRENT STATE: {gameObject.name} =====");
        Debug.Log($"[AvatarCharacterDisplay] [{role}] CurrentAvatarId: {currentAvatarId}");
        
        // Log Character Meo
        if (characterMeo != null)
        {
            Debug.Log($"[AvatarCharacterDisplay] [{role}] Character Meo: {(characterMeo.activeSelf ? "ACTIVE" : "INACTIVE")}");
            for (int i = 0; i < SkinNamesMeo.Length; i++)
            {
                Transform skin = characterMeo.transform.Find(SkinNamesMeo[i]);
                if (skin != null)
                {
                    Debug.Log($"[AvatarCharacterDisplay] [{role}]   - Skin '{SkinNamesMeo[i]}': {(skin.gameObject.activeSelf ? "ACTIVE" : "INACTIVE")}");
                }
            }
        }
        else
        {
            Debug.Log($"[AvatarCharacterDisplay] [{role}] Character Meo: NULL");
        }
        
        // Log Character Meo_Sad
        if (characterMeoSad != null)
        {
            Debug.Log($"[AvatarCharacterDisplay] [{role}] Character Meo_Sad: {(characterMeoSad.activeSelf ? "ACTIVE" : "INACTIVE")}");
            for (int i = 0; i < SkinNamesMeoSad.Length; i++)
            {
                Transform skin = characterMeoSad.transform.Find(SkinNamesMeoSad[i]);
                if (skin != null)
                {
                    Debug.Log($"[AvatarCharacterDisplay] [{role}]   - Skin '{SkinNamesMeoSad[i]}': {(skin.gameObject.activeSelf ? "ACTIVE" : "INACTIVE")}");
                }
            }
        }
        else
        {
            Debug.Log($"[AvatarCharacterDisplay] [{role}] Character Meo_Sad: NULL");
        }
        
        // Log MeoGoc34 Fix
        if (meoGoc34Fix != null)
        {
            Debug.Log($"[AvatarCharacterDisplay] [{role}] MeoGoc34 Fix: {(meoGoc34Fix.activeSelf ? "ACTIVE" : "INACTIVE")}");
            for (int i = 0; i < SkinNamesMeo34.Length; i++)
            {
                Transform skin = meoGoc34Fix.transform.Find(SkinNamesMeo34[i]);
                if (skin != null)
                {
                    Debug.Log($"[AvatarCharacterDisplay] [{role}]   - Skin '{SkinNamesMeo34[i]}': {(skin.gameObject.activeSelf ? "ACTIVE" : "INACTIVE")}");
                }
            }
        }
        else
        {
            Debug.Log($"[AvatarCharacterDisplay] [{role}] MeoGoc34 Fix: NULL");
        }
        
        Debug.Log($"[AvatarCharacterDisplay] [{role}] ===== END STATE =====");
    }
}
