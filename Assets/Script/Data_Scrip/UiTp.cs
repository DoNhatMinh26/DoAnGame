using DoAnGame.Auth;
using DoAnGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;
    [Header("Cấu hình vị trí thủ công")]
    public RectTransform[] waypoints; // Kéo 20 điểm p(1) đến p(20) vào đây
    public float backgroundWidth = 1920f; // Chiều rộng 1 tấm ảnh nền
    [Header("Cấu hình Danh sách Màn chơi")]
    public GameObject levelButtonPrefab;
    public RectTransform contentParent;
    [SerializeField] private float buttonSpacing = 160f;
    [SerializeField] private float waveAmplitude = 100f;
    [SerializeField] private float waveFrequency = 0.4f;

    [Header("Quản lý các Panel Chính")]
    public GameObject panelHome;
    public GameObject panelChonMan;
    public GameObject panelGameplay;
    public GameObject panelSetting;
    public Button settingButton;
    [Header("Quản lý Panel Kết Thúc")]
    public GameObject panelWin;   // Kéo Panel chiến thắng vào đây
    public GameObject panelLose;  // Kéo Panel thất bại vào đây
    private bool isGameOver = false;
    [Header("Giao diện Shop & Profile")]
    public TextMeshProUGUI shopScoreTxt; // Text hiện điểm ở Shop
    public TextMeshProUGUI shopLevelTxt; // Text hiện Level ở Shop

    [Header("Giao diện Gameplay")]
    public TextMeshProUGUI gameplayScoreRewardTxt; // Text hiện điểm thưởng tạm thời khi đang chơi

    [Header("Quản lý Panel Kết Thúc (Bổ sung)")]
    public TextMeshProUGUI winLevelInfoTxt;
    public TextMeshProUGUI winScoreTxt;    // Điểm hiện trên bảng Win
    public TextMeshProUGUI winRewardTxt;   // Tiền hiện trên bảng Win (đã có levelCoinTxt nhưng nên tách riêng nếu cần)
    public TextMeshProUGUI loseScoreTxt;   // Điểm hiện trên bảng Lose
    public TextMeshProUGUI loseRewardTxt;  // Tiền hiện trên bảng Lose
    public TextMeshProUGUI loseProgressTxt; // Hiện số câu đúng/số quái đã giết

    private int levelScore = 0; // Điểm tích lũy riêng trong màn này
    [Header("Quản lý Tiền")]
    public TextMeshProUGUI totalCoinTxt; // Text hiển thị ở Shop/Menu
    public TextMeshProUGUI levelCoinTxt; // Text hiển thị trong trận đấu (Gameplay)
    public Transform coinTarget;

    [Header("Quản lý Skin & Animation")]
    public GameObject[] catSkins;
    public GameObject[] catSkins2;// Mảng chứa các Object Mèo trong game (tương tự UiClass)
    public Animator sharedAnimator; // Animator tổng điều khiển xương
    public CatSkin[] allSkins; // Danh sách ScriptableObject

    [Header("Giao diện Shop Mèo")]
    public Image[] skinButtonImages;
    public TextMeshProUGUI[] skinPriceTexts;
    public Image[] skinPriceBackgrounds;
    [SerializeField] private float scaleAmount = 1.2f;
    [SerializeField] private float scaleDuration = 0.1f;
    private int pendingSkinIndex = -1;
    private Coroutine activeScaleCoroutine;

    private Coroutine activePhaoScaleCoroutine;

    private int totalCoins = 0;
    private int levelCoins = 0;
    private int lastSelectedType = 0;
    [Header("Quản lý Pháo giống cấu trúc Mèo")]
    public GameObject[] phaoSkins;        // ĐÃ THÊM: Mảng chứa các Object Pháo trong trận đấu (tương tự catSkins)
    public GameObject[] shopPhaoSkins;    // ĐÃ THÊM: Mảng chứa các Object Pháo hiển thị ở UI Shop/Preview (tương tự catSkins2)
    public Animator sharedPhaoAnimator;   // ĐÃ THÊM: Animator tổng điều khiển chung cho Pháo (nếu dùng chung xương, hoặc để Rebind)

    public PhaoSkin[] allPhaoSkins;       // Danh sách ScriptableObject cấu hình giá/tên pháo
    public Image[] phaoButtonImages;      // Các ảnh nút Pháo trong Shop
    public TextMeshProUGUI[] phaoPriceTexts;
    private int pendingPhaoIndex = -1;
    [Header("Thông báo Shop")]
    public TextMeshProUGUI shopNotificationTxt;
    public CanvasGroup notificationCanvasGroup;
    [Header("Quản lý Kẻ địch")]
    public TextMeshProUGUI enemyCounterTxt; // Kéo Text (TMP) hiển thị "0/0" vào đây
    private int killedEnemies = 0;
    private int totalEnemiesInLevel = 0;
    private int maTranDau = 0;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    void UpdateGameplayScoreUI()
    {
        if (gameplayScoreRewardTxt != null)
            gameplayScoreRewardTxt.text = "Điểm: " + levelScore.ToString();
    }
    private void Start()
    {
        // Tải tiền tổng từ máy
        totalCoins = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.TotalCoins, 0);

        // Tải và mặc skin đã lưu
        UpdateShopProfileUI();
        LoadCurrentSkin();
        UpdateShopUI();
        UpdateCoinUI();
        LoadCurrentPhao();
        UpdatePhaoShopUI();

        GenerateLevelButtons();

        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
        if (panelSetting != null) panelSetting.SetActive(false);

        ShowHome();
    }
    private void Update()
    {
        if (panelGameplay.activeSelf && !isGameOver)
        {
            CheckWinCondition();
        }
    }
    public void SetSettingButtonInteractable(bool state)
    {
        if (settingButton != null)
        {
            settingButton.interactable = state;
        }
    }
    public void UpdateShopProfileUI()
    {

        bool isGuest = UIQuickPlayNameController.IsGuestMode();
        string scoreKey = isGuest
            ? DoAnGame.Auth.LocalStorageKeyResolver.LocalGuestScore
            : DoAnGame.Auth.LocalStorageKeyResolver.UserScore;
        string levelKey = isGuest
            ? DoAnGame.Auth.LocalStorageKeyResolver.LocalGuestLevel
            : DoAnGame.Auth.LocalStorageKeyResolver.UserLevel;

        int score = PlayerPrefs.GetInt(scoreKey, 0);
        int level = PlayerPrefs.GetInt(levelKey, 1);

        if (shopScoreTxt != null) shopScoreTxt.text = "Điểm: " + score.ToString();
        if (shopLevelTxt != null) shopLevelTxt.text = "Level: " + level.ToString();
    }
    public void AddScore(int amount)
    {
        levelScore += amount; // Lưu vào biến tạm để hiện bảng Win/Lose

        // Hiển thị điểm nhảy số trong lúc chơi
        if (gameplayScoreRewardTxt != null)
            gameplayScoreRewardTxt.text = "Điểm: " + levelScore.ToString();

        // Gửi điểm sang DataManager để lưu vào máy (PlayerPrefs) và đồng bộ Firebase
        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddScore(amount);

            // Cập nhật luôn Text ở Shop để đồng bộ số liệu ngay lập tức
            UpdateShopProfileUI();
        }
    }
    void UpdateCoinUI()
    {
        if (totalCoinTxt != null) totalCoinTxt.text = totalCoins.ToString();
        if (levelCoinTxt != null) levelCoinTxt.text = levelCoins.ToString();
    }
    public void UpdateCoinsFromShop(int newTotal)
    {
        totalCoins = newTotal;
        UpdateCoinUI();
    }
    public void AddCoins(int amount)
    {
        levelCoins += amount;
        totalCoins += amount;
        PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.TotalCoins, totalCoins);
        PlayerPrefs.Save();
        UpdateCoinUI();

        // Sync coins lên Firebase nếu đã đăng nhập
        if (amount != 0)
            DoAnGame.Auth.CloudSyncService.Instance?.OnCoinsChanged(totalCoins);
    }
    public void ShowShopNotification(string message)
    {
        if (shopNotificationTxt != null && notificationCanvasGroup != null)
        {
            // Dừng các hiệu ứng đang chạy dở để tránh xung đột
            StopAllCoroutines();

            shopNotificationTxt.text = message;
            StartCoroutine(FadeNotificationRoutine());
        }
    }

    private System.Collections.IEnumerator FadeNotificationRoutine()
    {
        // 1. Hiện thông báo ngay lập tức
        notificationCanvasGroup.alpha = 1f;
        notificationCanvasGroup.gameObject.SetActive(true);

        // 2. Giữ nguyên trong 1.5 giây để người dùng kịp đọc
        yield return new WaitForSecondsRealtime(1.5f);

        // 3. Hiệu ứng mờ dần trong 1 giây
        float duration = 1f;
        float currentTime = 0f;
        while (currentTime < duration)
        {
            currentTime += Time.unscaledDeltaTime;
            notificationCanvasGroup.alpha = Mathf.Lerp(1f, 0f, currentTime / duration);
            yield return null;
        }

        // 4. Ẩn hẳn đối tượng khi đã mờ hết
        notificationCanvasGroup.alpha = 0f;
        notificationCanvasGroup.gameObject.SetActive(false);
    }
    // Hàm cập nhật chữ hiển thị lên UI
    public void UpdateEnemyCounterUI()
    {
        if (enemyCounterTxt != null)
        {
            enemyCounterTxt.text = killedEnemies + "/" + totalEnemiesInLevel;
        }
    }

    // Hàm để gọi từ bên ngoài khi kẻ địch bị tiêu diệt
    public void OnEnemyKilled()
    {
        killedEnemies++;
        UpdateEnemyCounterUI();
    }
    public void PlayMascotAnimation(string triggerName)
    {
        // Chạy cho Animator chính được kéo vào Inspector
        if (sharedAnimator != null && sharedAnimator.gameObject.activeInHierarchy)
        {
            sharedAnimator.SetTrigger(triggerName);
        }
    }
    #region LOGIC SKIN MÈO 

    public void LoadCurrentSkin()
    {
        int selectedID = PlayerPrefs.GetInt(LocalStorageKeyResolver.SelectedSkinID, 0);

        if (catSkins != null)
        {
            for (int i = 0; i < catSkins.Length; i++)
            {
                if (catSkins[i] != null) catSkins[i].SetActive(i == selectedID);
            }
        }
        if (catSkins2 != null)
        {
            for (int i = 0; i < catSkins2.Length; i++)
            {
                if (catSkins2[i] != null) catSkins2[i].SetActive(i == selectedID);
            }
        }
        if (sharedAnimator != null)
        {
            sharedAnimator.Rebind();
            sharedAnimator.Update(0f);
        }
    }

    public bool IsSkinUnlocked(int index)
    {
        if (index == 0) return true;
        return PlayerPrefs.GetInt(LocalStorageKeyResolver.SkinUnlockedKey(index), 0) == 1;
    }

    public void SelectSkinToPreview(int index)
    {
        if (allSkins == null || index < 0 || index >= allSkins.Length) return;

        // Hiệu ứng phóng to nút
        if (index < phaoButtonImages.Length && phaoButtonImages[index] != null)
        {
            RectTransform rt = phaoButtonImages[index].GetComponent<RectTransform>();
            if (rt != null)
            {
                // Dừng hiệu ứng cũ nếu đang chạy để tránh nút bị to/nhỏ bất thường
                if (activePhaoScaleCoroutine != null) StopCoroutine(activePhaoScaleCoroutine);
                activePhaoScaleCoroutine = StartCoroutine(ScaleButtonRoutine(rt));
            }
        }

        pendingSkinIndex = index;
        lastSelectedType = 1;

        // Bật Preview skin
        for (int i = 0; i < catSkins.Length; i++)
        {
            if (catSkins[i] != null) catSkins[i].SetActive(i == index);
        }

        if (IsSkinUnlocked(index))
        {
            PlayerPrefs.SetInt(LocalStorageKeyResolver.SelectedSkinID, index);
            PlayerPrefs.Save();
            ShowShopNotification("Đã mặc trang phục!");
        }
        else
        {
            ShowShopNotification("Giá: " + allSkins[index].price + "$");
        }

        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        int currentEquippedID = PlayerPrefs.GetInt(LocalStorageKeyResolver.SelectedSkinID, 0);

        for (int i = 0; i < allSkins.Length; i++)
        {
            bool unlocked = IsSkinUnlocked(i);

            if (i < skinButtonImages.Length)
                skinButtonImages[i].color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);

            if (i < skinPriceTexts.Length)
            {
                if (unlocked)
                {
                    if (i == currentEquippedID)
                    {
                        skinPriceTexts[i].text = "Đang dùng";
                        skinPriceTexts[i].color = Color.white;
                        if (skinPriceBackgrounds.Length > i) skinPriceBackgrounds[i].color = new Color(0, 0.48f, 1f); // Blue
                    }
                    else
                    {
                        skinPriceTexts[i].text = "Sở hữu";
                        skinPriceTexts[i].color = Color.white;
                        if (skinPriceBackgrounds.Length > i) skinPriceBackgrounds[i].color = new Color(0, 1f, 0.36f); // Green
                    }
                }
                else
                {
                    skinPriceTexts[i].text = allSkins[i].price + "$";
                    skinPriceTexts[i].color = new Color(0.45f, 0.41f, 0.13f); // Dark Gold
                    if (skinPriceBackgrounds.Length > i) skinPriceBackgrounds[i].color = Color.white;
                }
            }
        }
    }

    public void Click_ConfirmPurchase()
    {
        if (pendingSkinIndex == -1) { ShowShopNotification("Chọn nhân vật!"); return; }

        CatSkin skin = allSkins[pendingSkinIndex];
        bool unlocked = IsSkinUnlocked(pendingSkinIndex);

        if (!unlocked && totalCoins >= skin.price)
        {
            totalCoins -= skin.price;
            PlayerPrefs.SetInt(LocalStorageKeyResolver.TotalCoins, totalCoins);
            PlayerPrefs.SetInt(LocalStorageKeyResolver.SkinUnlockedKey(pendingSkinIndex), 1);
            PlayerPrefs.SetInt(LocalStorageKeyResolver.SelectedSkinID, pendingSkinIndex);
            PlayerPrefs.Save();

            ShowShopNotification("Mua thành công!");
            UpdateCoinUI();
            UpdateShopUI();
            LoadCurrentSkin();
            SyncKeoThadaSkinShop();
        }
        else if (unlocked)
        {
            PlayerPrefs.SetInt(LocalStorageKeyResolver.SelectedSkinID, pendingSkinIndex);
            PlayerPrefs.Save();
            LoadCurrentSkin();
            UpdateShopUI();
        }
        else
        {
            ShowShopNotification("Thiếu " + (skin.price - totalCoins) + "$");
        }
    }

    private IEnumerator ScaleButtonRoutine(RectTransform target)
    {
        Vector3 originalScale = Vector3.one; // Hoặc giá trị mặc định của bạn
        Vector3 targetScale = originalScale * scaleAmount;
        float elapsed = 0;

        while (elapsed < scaleDuration)
        {
            target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / scaleDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        elapsed = 0;
        while (elapsed < scaleDuration)
        {
            target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / scaleDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        target.localScale = originalScale;
    }
    private void SyncKeoThadaSkinShop()
    {
        // Lấy ID skin đang chọn hiện tại
        int selected = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedSkinID, 0);

        // Tạo danh sách các skin đã mở khóa
        var unlocked = new System.Collections.Generic.List<int> { 0 }; // Skin 0 mặc định mở
        if (allSkins != null)
        {
            for (int i = 1; i < allSkins.Length; i++)
            {
                if (IsSkinUnlocked(i)) unlocked.Add(i);
            }
        }

        // Gửi dữ liệu đồng bộ lên Cloud
        DoAnGame.Auth.CloudSyncService.Instance?.OnShopPurchased("keothada_skin", selected, unlocked.ToArray());
    }
    #endregion
    #region LOGIC skin Pháo
    public void LoadCurrentPhao()
    {
        int selectedPhaoID = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedPhaoID, 0);

        // 1. Cập nhật các Object Pháo trong trận đấu theo ID đang mặc
        if (phaoSkins != null)
        {
            for (int i = 0; i < phaoSkins.Length; i++)
            {
                if (phaoSkins[i] != null) phaoSkins[i].SetActive(i == selectedPhaoID);
            }
        }

        // 2. Cập nhật các Object Pháo ở UI Shop/Menu để đồng bộ
        if (shopPhaoSkins != null)
        {
            for (int i = 0; i < shopPhaoSkins.Length; i++)
            {
                if (shopPhaoSkins[i] != null) shopPhaoSkins[i].SetActive(i == selectedPhaoID);
            }
        }

        // Rebind lại trạng thái hoạt họa nếu bạn có sử dụng Animator tổng
        if (sharedPhaoAnimator != null)
        {
            sharedPhaoAnimator.Rebind();
            sharedPhaoAnimator.Update(0f);
        }
    }

    public bool IsPhaoUnlocked(int index)
    {
        if (index == 0) return true; // Pháo đầu tiên (Pháo gỗ) mặc định mở khóa
        return PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.PhaoUnlockedKey(index), 0) == 1;
    }

    public void SelectPhaoToPreview(int index)
    {
        if (allPhaoSkins == null || index < 0 || index >= allPhaoSkins.Length) return;

        // Hiệu ứng co giãn nút bấm khi click (SỬA LỖI: Dùng đúng mảng phaoButtonImages của pháo thay vì skinButtonImages)
        if (index < phaoButtonImages.Length && phaoButtonImages[index] != null)
        {
            RectTransform rt = phaoButtonImages[index].GetComponent<RectTransform>();
            if (rt != null)
            {
                if (activePhaoScaleCoroutine != null) StopCoroutine(activePhaoScaleCoroutine);
                activePhaoScaleCoroutine = StartCoroutine(ScaleButtonRoutine(rt));
            }
        }

        pendingPhaoIndex = index;
        lastSelectedType = 2; // Ưu tiên chọn Pháo

        // SỬA LỖI CHÍNH: Bật/Tắt đồng thời cả phaoSkins để hình ảnh pháo ở giữa màn hình thay đổi ngay lập tức
        if (phaoSkins != null)
        {
            for (int i = 0; i < phaoSkins.Length; i++)
            {
                if (phaoSkins[i] != null) phaoSkins[i].SetActive(i == index);
            }
        }

        // Bật Object Preview tương ứng trong Shop để xem trước
        if (shopPhaoSkins != null)
        {
            for (int i = 0; i < shopPhaoSkins.Length; i++)
            {
                if (shopPhaoSkins[i] != null) shopPhaoSkins[i].SetActive(i == index);
            }
        }

        if (IsPhaoUnlocked(index))
        {
            // Nếu đã sở hữu (như Pháo gỗ) -> Tự động lưu trang bị luôn
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedPhaoID, index);
            PlayerPrefs.Save();
            ShowShopNotification("Đã trang bị pháo!");

            // Cập nhật lại trạng thái thực tế của Gameplay
            LoadCurrentPhao();
        }
        else
        {
            ShowShopNotification("Giá: " + allPhaoSkins[index].price + "$");
        }

        // Luôn làm mới lại UI chữ của các ô trong shop
        UpdatePhaoShopUI();
    }

    public void UpdatePhaoShopUI()
    {
        // LẤY ĐÚNG ID CỦA PHÁO ĐANG MẶC
        int currentEquippedPhaoID = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedPhaoID, 0);

        for (int i = 0; i < allPhaoSkins.Length; i++)
        {
            bool unlocked = IsPhaoUnlocked(i);

            // Cập nhật màu sắc icon pháo (Mờ đi nếu chưa mua)
            if (i < phaoButtonImages.Length && phaoButtonImages[i] != null)
            {
                phaoButtonImages[i].color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            // Cập nhật text trạng thái / giá tiền của Pháo
            if (i < phaoPriceTexts.Length && phaoPriceTexts[i] != null)
            {
                if (unlocked)
                {
                    if (i == currentEquippedPhaoID)
                    {
                        phaoPriceTexts[i].text = "Đang dùng";
                        phaoPriceTexts[i].color = Color.white;
                    }
                    else
                    {
                        phaoPriceTexts[i].text = "Sở hữu";
                        phaoPriceTexts[i].color = Color.white;
                    }
                }
                else
                {
                    phaoPriceTexts[i].text = allPhaoSkins[i].price + "$";
                    phaoPriceTexts[i].color = new Color(0.45f, 0.41f, 0.13f); // Màu vàng tối đồng bộ
                }
            }
        }
    }

    public void Click_ConfirmPurchasePhao()
    {
        if (pendingPhaoIndex == -1) { ShowShopNotification("Chọn khẩu pháo!"); return; }

        PhaoSkin skin = allPhaoSkins[pendingPhaoIndex];
        bool unlocked = IsPhaoUnlocked(pendingPhaoIndex);

        if (!unlocked && totalCoins >= skin.price)
        {
            totalCoins -= skin.price;
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.TotalCoins, totalCoins);
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.PhaoUnlockedKey(pendingPhaoIndex), 1);
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedPhaoID, pendingPhaoIndex);
            PlayerPrefs.Save();

            ShowShopNotification("Mua thành công pháo!");
            UpdateCoinUI();
            UpdatePhaoShopUI();
            LoadCurrentPhao();
            SyncKeoThadaPhaoShop();
        }
        else if (unlocked)
        {
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedPhaoID, pendingPhaoIndex);
            PlayerPrefs.Save();
            LoadCurrentPhao();
            UpdatePhaoShopUI();
        }
        else
        {
            ShowShopNotification("Thiếu " + (skin.price - totalCoins) + "$");
        }
    }

    private void SyncKeoThadaPhaoShop()
    {
        int selected = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedPhaoID, 0);
        var unlocked = new System.Collections.Generic.List<int> { 0 };
        if (allPhaoSkins != null)
        {
            for (int i = 1; i < allPhaoSkins.Length; i++)
            {
                if (IsPhaoUnlocked(i)) unlocked.Add(i);
            }
        }
        DoAnGame.Auth.CloudSyncService.Instance?.OnShopPurchased("keothada_phao", selected, unlocked.ToArray());
    }

    public void PlayPhaoAnimation(string triggerName)
    {
        // Cách 1: Nếu bạn đã kéo Object PhaoGame vào ô "Shared Phao Animator" trong Inspector
        if (sharedPhaoAnimator != null && sharedPhaoAnimator.gameObject.activeInHierarchy)
        {
            sharedPhaoAnimator.SetTrigger(triggerName);
            return;
        }

        // Cách 2 (Dự phòng): Nếu không gán sharedPhaoAnimator, tự động tìm Animator ở Object cha của pháo đang kích hoạt
        if (phaoSkins != null)
        {
            for (int i = 0; i < phaoSkins.Length; i++)
            {
                if (phaoSkins[i] != null && phaoSkins[i].activeInHierarchy)
                {
                    // Tìm component Animator ở chính nó hoặc component Animator ở các Object cha (như PhaoGame)
                    Animator anim = phaoSkins[i].GetComponentInParent<Animator>();
                    if (anim != null)
                    {
                        anim.SetTrigger(triggerName);
                    }
                    break;
                }
            }
        }
    }
    #endregion
    #region CÁC HÀM ĐIỀU HƯỚNG
    public void Click_GlobalConfirmPurchase()
    {
        // Ưu tiên mua Mèo nếu chọn Mèo cuối cùng
        if (lastSelectedType == 1 && pendingSkinIndex != -1)
        {
            Click_ConfirmPurchase(); // Gọi hàm mua mèo hiện tại của bạn
        }
        // Ưu tiên mua Pháo nếu chọn Pháo cuối cùng
        else if (lastSelectedType == 2 && pendingPhaoIndex != -1)
        {
            Click_ConfirmPurchasePhao(); // Gọi hàm mua pháo hiện tại của bạn
        }
        else
        {
            Debug.Log("Vui lòng chọn một sản phẩm trước khi mua!");
        }
    }
    public void Click_OpenChonMan()
    {
        DeactivateAll();
        if (panelChonMan != null) panelChonMan.SetActive(true);
        Time.timeScale = 1f;
    }

    public void Click_BackToHome()
    {
        UpdateShopUI();
        DragAndDrop[] allAnswers = FindObjectsOfType<DragAndDrop>();
        foreach (DragAndDrop btn in allAnswers)
        {
            btn.ForceResetPosition();
        }
        Time.timeScale = 1f;

        // 1. Dọn dẹp quái và đạn
        ClearAllGameplayObjects();

        // 2. GIẢI QUYẾT LỖI: Giải phóng hoàn toàn các biến khóa static
        DragAndDrop.ReleaseAllLocks();
        DragAndDrop.SetGlobalLock(false);

        // 3. Ngừng Spawner
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null) spawner.enabled = false;

        DeactivateAll();
        if (panelHome != null) panelHome.SetActive(true);
    }

    public void ShowGameplay()
    {
        DeactivateAll();
        if (panelGameplay != null) panelGameplay.SetActive(true);
        Time.timeScale = 1f;
    }

    private void DeactivateAll()
    {
        if (panelHome != null) panelHome.SetActive(false);
        if (panelChonMan != null) panelChonMan.SetActive(false);
        if (panelGameplay != null) panelGameplay.SetActive(false);
        if (panelSetting != null) panelSetting.SetActive(false);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
    }

    public void ShowHome()
    {
        DeactivateAll();
        if (panelHome != null) panelHome.SetActive(true);
    }
    #endregion

    #region HÀM XỬ LÝ THẮNG / THUA / SETTING
    public void ShowWin()
    {

        if (panelWin != null)
        {
            SetSettingButtonInteractable(false);
            if (winLevelInfoTxt != null)
            {
                // LevelManager.CurrentLevel thường bắt đầu từ 0, nên +1 để hiển thị cho người dùng
                winLevelInfoTxt.text = "Hoàn thành Màn " + (LevelManager.CurrentLevel).ToString();
            }
            if (winScoreTxt != null)
                winScoreTxt.text = "Điểm: +" + levelScore.ToString();

            if (winRewardTxt != null)
                winRewardTxt.text = "+" + levelCoins.ToString();
            var am = AudioManager.Instance;
            if (am != null) am.PlaySFX(am.soundWin);

            panelWin.SetActive(true);
            Time.timeScale = 0f;
            DragAndDrop.SetGlobalLock(true);

            // Lưu tiến trình local
            int currentHighest = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.KeoThaHighest, 1);
            int wonLevel = LevelManager.CurrentLevel;

            if (wonLevel == currentHighest && wonLevel < 100)
            {
                PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.KeoThaHighest, wonLevel + 1);
                PlayerPrefs.Save();
                GenerateLevelButtons();
            }

            // Sync lên Firebase (nếu đã đăng nhập)
            DoAnGame.Auth.CloudSyncService.Instance?.OnLevelCompleted(
                gameMode: "keothada",
                grade: UIManager.SelectedGrade,
                levelNumber: wonLevel,
                score: levelScore,
                coinsEarned: levelCoins
            );
        }
    }

    // Hàm đợi 3 giây trước khi hiện bảng thắng để tiền kịp nạp vào
    private System.Collections.IEnumerator WaitAndShowWin()
    {
        DragAndDrop.SetGlobalLock(true);
        // Đợi 3 giây thực tế (không bị ảnh hưởng bởi Time.timeScale nếu bạn muốn)
        yield return new WaitForSecondsRealtime(1.5f);

        // Gọi hàm hiện bảng thắng gốc của bạn
        ShowWin();
    }
    public void ShowLose()
    {
        if (isGameOver) return;
        isGameOver = true; // Khóa ngay lập tức để chặn hàm Update() quét song song

        // Kích hoạt luồng và truyền mã trận đấu hiện tại vào trong
        StartCoroutine(WaitAndShowLose(maTranDau));
    }

    private System.Collections.IEnumerator WaitAndShowLose(int idTrậnĐấuLúcKíchHoạt)
    {
        DragAndDrop.SetGlobalLock(true);
        SetSettingButtonInteractable(false);
        if (sharedAnimator != null) sharedAnimator.SetTrigger("TpSad");

        // Đợi 2 giây thời gian thực
        yield return new WaitForSecondsRealtime(2f);

        // KIỂM TRA CHÍ MẠNG: Nếu mã trận đấu hiện tại đã thay đổi (do người chơi bấm chơi lại liền)
        // thì lập tức thoát hàm, hủy lệnh hiện bảng thua cũ!
        if (idTrậnĐấuLúcKíchHoạt != maTranDau)
        {
            yield break; // Thoát hẳn Coroutine ngầm
        }

        // Nếu vẫn trùng khớp mã trận đấu -> Hiện bảng thua chuẩn xác
        ExecuteShowLosePanel();
    }


    public void Click_Retry()
    {
        Time.timeScale = 1f;
        levelScore = 0;
        DragAndDrop.ReleaseAllLocks();
        BatDauChoiMan(LevelManager.CurrentLevel);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
    }

    public void Click_Next()
    {
        Time.timeScale = 1f;
        DragAndDrop.ReleaseAllLocks();
        int nextLevel = LevelManager.CurrentLevel + 1;
        if (nextLevel > 100) nextLevel = 100;

        BatDauChoiMan(nextLevel); // Tự động reset nút bên trong hàm này
        if (panelWin != null) panelWin.SetActive(false);
    }
    public void Click_OpenSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(true);
            Time.timeScale = 0f; // Tự động làm bộ đếm bên DragAndDrop dừng lại
            DragAndDrop.SetGlobalLock(true);
        }
    }

    public void Click_CloseSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);

            if (!panelWin.activeSelf && !panelLose.activeSelf)
            {
                Time.timeScale = 1f; // Tự động làm bộ đếm chạy tiếp từ giây cũ

                // Hàm này cực kỳ quan trọng để kiểm tra xem có ai đang bị phạt không
                CheckPunishmentStatus();
            }
        }
    }
    private void ExecuteShowLosePanel()
    {
        isGameOver = true;

        if (panelLose != null)
        {
            if (loseScoreTxt != null) loseScoreTxt.text = "Điểm: +" + levelScore;
            if (loseRewardTxt != null) loseRewardTxt.text = "Tiền: +" + levelCoins;

            if (loseProgressTxt != null)
            {
                loseProgressTxt.text = $"Tiến trình: {killedEnemies}/{totalEnemiesInLevel}";
            }

            var am = AudioManager.Instance;
            if (am != null) am.PlaySFX(am.soundLose);

            panelLose.SetActive(true);
            Time.timeScale = 0f; // Dừng game
            DragAndDrop.SetGlobalLock(true);
        }
    }

    // Hàm xử lý nút bấm thoát nhanh từ panel Cài đặt hiện luôn bảng Thua
    public void Click_ThoatNgayLapTuc()
    {
        Time.timeScale = 1f;
        if (panelSetting != null) panelSetting.SetActive(false);

        // 1. Dừng mọi hành động nạp câu hỏi mới đang chờ
        DragAndDrop[] allAnswers = FindObjectsOfType<DragAndDrop>();
        foreach (DragAndDrop answer in allAnswers)
        {
            answer.StopAllCoroutines();
        }

        // 2. Khóa cứng trạng thái và tắt tương tác nút
        DragAndDrop.SetGlobalLock(true);
        SetSettingButtonInteractable(false);

        // 3. Kích hoạt trực tiếp panel thua không cần qua Coroutine đợi 3 giây nữa
        ExecuteShowLosePanel();
    }

    private void CheckPunishmentStatus()
    {
        DragAndDrop[] allChoices = FindObjectsOfType<DragAndDrop>();
        bool isAnyoneRed = false;

        foreach (var choice in allChoices)
        {
            // Nếu vẫn còn nút màu đỏ, nghĩa là nó vẫn đang trong thời gian phạt (Coroutine chưa xong)
            if (choice.GetComponent<Image>().color == choice.colorWrong)
            {
                isAnyoneRed = true;
                break;
            }
        }

        // Nếu không ai bị đỏ thì mới mở khóa kéo thả
        if (!isAnyoneRed)
        {
            DragAndDrop.SetGlobalLock(false);
        }
    }

    private void ClearAllGameplayObjects()
    {
        // 1. Tìm và xóa sạch kẻ địch (Tag: Enemy)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies) Destroy(e);

        // 2. Tìm và xóa sạch các viên đạn đang bay (Tag: Dan)
        // Đảm bảo bạn đã đặt Tag cho Prefab viên đạn là "Dan" trong Inspector
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Dan");
        foreach (GameObject b in bullets) Destroy(b);

        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");
        foreach (GameObject c in coins) Destroy(c);
    }
    public void ResetAllGameData()
    {
        // Xóa tiền
        PlayerPrefs.DeleteKey(DoAnGame.Auth.LocalStorageKeyResolver.TotalCoins);
        totalCoins = 0;
        UpdateCoinUI();

        // Xóa skin (gọi hàm vừa tạo ở trên)
        ResetSkins();

        // Xóa tiến trình màn chơi
        PlayerPrefs.DeleteKey(DoAnGame.Auth.LocalStorageKeyResolver.KeoThaHighest);

        PlayerPrefs.Save();

        // Tải lại danh sách nút chọn màn để khóa các màn đã mở
        GenerateLevelButtons();

        Debug.Log("Dữ liệu game đã được xóa sạch hoàn toàn!");
    }
    // Thêm hàm này vào class GameUIManager trong file UiTp.cs
    public void ResetSkins()
    {
        // --- RESET SKIN MÈO ---
        // 1. Reset ID skin mèo về mặc định (0)
        PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedSkinID, 0);

        // 2. Khóa lại tất cả skin mèo (trừ cái đầu tiên)
        if (allSkins != null)
        {
            for (int i = 1; i < allSkins.Length; i++)
            {
                PlayerPrefs.DeleteKey(DoAnGame.Auth.LocalStorageKeyResolver.SkinUnlockedKey(i));
            }
        }

        // --- RESET SKIN PHÁO ---
        // 3. Reset ID pháo về mặc định (0)
        PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedPhaoID, 0);

        // 4. Khóa lại tất cả skin pháo (trừ cái đầu tiên)
        if (allPhaoSkins != null)
        {
            for (int i = 1; i < allPhaoSkins.Length; i++)
            {
                PlayerPrefs.DeleteKey(DoAnGame.Auth.LocalStorageKeyResolver.PhaoUnlockedKey(i));
            }
        }

        // 5. Lưu thay đổi
        PlayerPrefs.Save();

        // 6. Cập nhật lại hình ảnh ngay lập tức trên tất cả Renderer
        LoadCurrentSkin();  // Cập nhật mèo (Menu & Gameplay)
        LoadCurrentPhao();  // Cập nhật pháo (Shop & Gameplay)

        // 7. Cập nhật lại giao diện Shop để làm tối các ô vừa bị khóa
        UpdateShopUI();     // Shop Mèo
        UpdatePhaoShopUI(); // Shop Pháo

        Debug.Log("Đã reset toàn bộ Skin Mèo và Pháo về trạng thái mặc định!");
    }
    void CheckWinCondition()
    {
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();

        // 1. Kiểm tra Spawner đã sinh hết quái theo cấu hình chưa
        if (spawner != null && spawner.enabled && spawner.IsAllEnemiesSpawned())
        {
            // 2. Đếm số lượng Enemy còn sống trên màn hình
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            if (enemies.Length == 0)
            {
                isGameOver = true;
                StartCoroutine(WaitAndShowWin()); // Gọi hàm hiện bảng thắng đã viết ở turn trước
            }
        }
    }
    #endregion
    //KeoThaHighest
    #region LOGIC SINH NÚT MÀN CHƠI
    public void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null || contentParent == null || waypoints.Length == 0) return;

        // 1. Xóa nút cũ, giữ lại các thành phần Map
        foreach (Transform child in contentParent)
        {
            // Kiểm tra tên đối tượng trong Hierarchy của Map Phòng Thủ để tránh xóa nhầm
            if (!child.name.Contains("Image") && child.name != "ViTriMan")
            {
                Destroy(child.gameObject);
            }
        }

        // 2. Lấy đúng level cao nhất của chế độ Phòng Thủ
        int highestLevel = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.KeoThaHighest, 1);

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();

            // 3. Logic tính vị trí theo Waypoint (20 điểm lặp lại)
            int indexInImage = (i - 1) % 10;
            int imageIndex = (i - 1) / 10;

            Vector2 pointPos = waypoints[indexInImage].anchoredPosition;
            float finalPosX = pointPos.x + (imageIndex * backgroundWidth);

            btnRect.anchoredPosition = new Vector2(finalPosX, pointPos.y);

            // 4. Thiết lập hiển thị số thứ tự màn
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = i.ToString();

            // 5. Logic khóa/mở màn dựa trên KeoThaHighest
            Button btn = btnObj.GetComponent<Button>();
            int levelIndex = i;

            if (i <= highestLevel || i == 30 || i == 50 || i == 70 || i == 100)
            {
                btn.interactable = true;
                btn.image.color = Color.white;
                // Gọi hàm bắt đầu chơi của chế độ Phòng Thủ
                btn.onClick.AddListener(() => BatDauChoiMan(levelIndex));
            }
            else
            {
                btn.interactable = false;
                btn.image.color = new Color(0.6f, 0.6f, 0.6f, 1f);
            }
        }

        // 6. Cập nhật vùng kéo khớp với chiều dài 5 tấm ảnh nền
        contentParent.sizeDelta = new Vector2((backgroundWidth * 10) + 180, contentParent.sizeDelta.y);
    }

    public void BatDauChoiMan(int levelIndex)
    {
        maTranDau++;
        levelScore = 0;
        UpdateGameplayScoreUI();// Reset điểm về 0
        SetSettingButtonInteractable(true);
        UpdateShopProfileUI(); // Cập nhật lại chỉ số ở Shop
        LoadCurrentPhao();
        LoadCurrentSkin();
        levelCoins = 0;
        killedEnemies = 0;
        UpdateCoinUI();

        // Luôn ưu tiên mở khóa và chạy lại thời gian đầu tiên
        Time.timeScale = 1f;
        DragAndDrop.ReleaseAllLocks();
        DragAndDrop.SetGlobalLock(false);

        // Reset vị trí các nút (đã thêm ở bước trước)
        DragAndDrop[] allAnswers = FindObjectsOfType<DragAndDrop>();
        foreach (DragAndDrop btn in allAnswers)
        {
            btn.ForceResetPosition();
        }

        isGameOver = false;
        ClearAllGameplayObjects();
        LevelManager.CurrentLevel = levelIndex;
        ShowGameplay();

        // Reset Spawner và Máu tường
        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.enabled = true;
            spawner.ResetSpawner();
            totalEnemiesInLevel = spawner.GetMaxEnemies();
        }

        if (WallHealth.Instance != null)
        {
            WallHealth.Instance.ResetHealth();
        }

        DragQuizManager qm = FindObjectOfType<DragQuizManager>();
        if (qm != null) qm.UpdateDifficulty();
        UpdateEnemyCounterUI();
    }

    #endregion
}
