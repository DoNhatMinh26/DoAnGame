using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

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

    [Header("Quản lý Panel Kết Thúc")]
    public GameObject panelWin;   // Kéo Panel chiến thắng vào đây
    public GameObject panelLose;  // Kéo Panel thất bại vào đây
    private bool isGameOver = false;

    [Header("Quản lý Tiền")]
    public TextMeshProUGUI totalCoinTxt; // Text hiển thị ở Shop/Menu
    public TextMeshProUGUI levelCoinTxt; // Text hiển thị trong trận đấu (Gameplay)
    public Transform coinTarget;

    [Header("Quản lý Skin")]
    public SpriteRenderer catRenderer;
    public SpriteRenderer gameplayCatRenderer;// Kéo SpriteRenderer của con mèo vào đây
    public CatSkin[] allSkins; // Danh sách các Skin bạn tạo ra
    [Header("Giao diện Shop")]
    public Image[] skinButtonImages;
    public TextMeshProUGUI[] skinPriceTexts;
    private int pendingSkinIndex = -1;

    private int totalCoins = 0;
    private int levelCoins = 0;
    private int lastSelectedType = 0;

    [Header("Quản lý Pháo")]
    public SpriteRenderer phaoRenderer;
    public SpriteRenderer shopPhaoRenderer;// Kéo Renderer khẩu pháo ở Gameplay vào đây
    public PhaoSkin[] allPhaoSkins;     // Danh sách các loại Pháo
    public Image[] phaoButtonImages;   // Các ảnh Pháo trong Shop để làm tối/sáng
    public TextMeshProUGUI[] phaoPriceTexts;
    private int pendingPhaoIndex = -1;
    [Header("Thông báo Shop")]
    public TextMeshProUGUI shopNotificationTxt;
    public CanvasGroup notificationCanvasGroup;
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

    private void Start()
    {
        // Tải tiền tổng từ máy
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        // Tải và mặc skin đã lưu
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
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();
        UpdateCoinUI();
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
    #region LOGIC skin Meo
    public void LoadCurrentSkin()
    {
        int currentSkinID = PlayerPrefs.GetInt("SelectedSkinID", 0);

        if (allSkins != null && currentSkinID < allSkins.Length && allSkins[currentSkinID] != null)
        {
            Sprite skinSprite = allSkins[currentSkinID].skinSprite;

            // Gắn cho mèo ở Menu
            if (catRenderer != null) catRenderer.sprite = skinSprite;

            // Gắn cho mèo trong trận đấu
            if (gameplayCatRenderer != null) gameplayCatRenderer.sprite = skinSprite;

            Debug.Log("Đã cập nhật skin cho cả Menu và Gameplay.");
        }
    }
    public bool IsSkinUnlocked(int index)
    {
        // Skin 0 luôn mở
        if (index == 0) return true;

        // Kiểm tra PlayerPrefs, trả về 0 nếu chưa từng lưu (mặc định là khóa)
        return PlayerPrefs.GetInt("SkinUnlocked_" + index, 0) == 1;
    }



    public void UpdateShopUI()
    {
        if (allSkins == null || skinButtonImages == null) return;

        for (int i = 0; i < allSkins.Length; i++)
        {
            if (i >= skinButtonImages.Length || skinButtonImages[i] == null) continue;

            bool isUnlocked = IsSkinUnlocked(i);

            // 1. Cập nhật màu sắc (sáng/tối)
            if (isUnlocked)
            {
                skinButtonImages[i].color = Color.white;

                // 2. Cập nhật chữ hiển thị thành "Đã sở hữu" nếu mảng Text tồn tại
                if (i < skinPriceTexts.Length && skinPriceTexts[i] != null)
                {
                    skinPriceTexts[i].text = "Đã sở hữu";
                }
            }
            else
            {
                skinButtonImages[i].color = new Color(0.3f, 0.3f, 0.3f, 1f);

                // Hiện lại giá gốc từ ScriptableObject nếu chưa mua
                if (i < skinPriceTexts.Length && skinPriceTexts[i] != null)
                {
                    skinPriceTexts[i].text = allSkins[i].price.ToString() + "$";
                }
            }
        }
    }
    public void SelectSkinToPreview(int index)
    {
        if (allSkins == null || index < 0 || index >= allSkins.Length) return;

        pendingSkinIndex = index;
        lastSelectedType = 1; // Ưu tiên chọn Mèo

        // Hiển thị hình ảnh lên các Renderer để xem thử
        if (catRenderer != null) catRenderer.sprite = allSkins[index].skinSprite;
        if (gameplayCatRenderer != null) gameplayCatRenderer.sprite = allSkins[index].skinSprite;

        // KIỂM TRA TRẠNG THÁI SỞ HỮU
        if (IsSkinUnlocked(index))
        {
            // Nếu đã có rồi thì tự động lưu và báo "Đã mặc"
            PlayerPrefs.SetInt("SelectedSkinID", index);
            PlayerPrefs.Save();
            ShowShopNotification("Đã mặc: " + allSkins[index].skinName);
        }
        else
        {
            // Nếu chưa có thì báo "Chưa sở hữu"
            ShowShopNotification("Chưa sở hữu nhân vật này!");
        }
    }

    public void Click_ConfirmPurchase()
    {
        // 1. Kiểm tra nếu chưa chọn skin nào để xem
        if (pendingSkinIndex == -1)
        {
            ShowShopNotification("Vui lòng chọn một nhân vật!");
            return;
        }

        // Lấy thông tin skin đang chọn
        CatSkin skin = allSkins[pendingSkinIndex];
        bool isUnlocked = IsSkinUnlocked(pendingSkinIndex);

        // 2. Logic xử lý Mua hoặc Mặc
        if (isUnlocked || totalCoins >= skin.price)
        {
            if (!isUnlocked)
            {
                // Thực hiện trừ tiền nếu chưa mua
                totalCoins -= skin.price;
                PlayerPrefs.SetInt("TotalCoins", totalCoins);
                PlayerPrefs.SetInt("SkinUnlocked_" + pendingSkinIndex, 1);

                ShowShopNotification("Mua thành công: " + skin.skinName + "!");
            }
            else
            {
                // Thông báo khi nhấn vào món đã sở hữu
                ShowShopNotification("Đã Sở Hữu: " + skin.skinName);
            }

            // 3. Lưu và mặc skin vừa chọn
            PlayerPrefs.SetInt("SelectedSkinID", pendingSkinIndex);
            PlayerPrefs.Save();

            // 4. Cập nhật giao diện đồng nhất với Shop
            UpdateCoinUI(); // Hiển thị số tiền mới (kèm dấu $ nếu bạn đã sửa)
            UpdateShopUI(); // Làm sáng nút skin và đổi chữ thành "Đã sở hữu"
            LoadCurrentSkin(); // Cập nhật hình ảnh mèo cho cả Menu và Gameplay
        }
        else
        {
            // Thông báo chi tiết số tiền còn thiếu tương tự như Pháo
            int thieu = skin.price - totalCoins;
            ShowShopNotification("Bạn còn thiếu " + thieu + "$ để mua skin này!");
            Debug.Log("Không đủ tiền mua skin này!");
        }
    }
    public void BuyAndApplySkin(int index)
    {
        if (allSkins == null || index < 0 || index >= allSkins.Length) return;

        pendingSkinIndex = index;
        CatSkin skin = allSkins[index];

        // Cho xem thử trên cả hai renderer
        if (catRenderer != null) catRenderer.sprite = skin.skinSprite;
        if (gameplayCatRenderer != null) gameplayCatRenderer.sprite = skin.skinSprite;

        if (IsSkinUnlocked(index))
        {
            PlayerPrefs.SetInt("SelectedSkinID", index);
            PlayerPrefs.Save();
        }
    }
    #endregion
    #region LOGIC skin Pháo
    public void LoadCurrentPhao()
    {
        int id = PlayerPrefs.GetInt("SelectedPhaoID", 0);
        if (allPhaoSkins != null && id < allPhaoSkins.Length)
        {
            Sprite currentPhaoSprite = allPhaoSkins[id].phaoSprite;

            // Cập nhật pháo trong trận đấu
            if (phaoRenderer != null)
            {
                phaoRenderer.sprite = currentPhaoSprite;
            }

            // Cập nhật luôn pháo ở Shop/Menu nếu cần đồng bộ
            if (shopPhaoRenderer != null)
            {
                shopPhaoRenderer.sprite = currentPhaoSprite;
            }
        }
    }

    public void SelectPhaoToPreview(int index)
    {
        if (allPhaoSkins == null || index < 0 || index >= allPhaoSkins.Length) return;

        pendingPhaoIndex = index;
        lastSelectedType = 2; // Ưu tiên chọn Pháo

        Sprite previewSprite = allPhaoSkins[index].phaoSprite;
        if (shopPhaoRenderer != null) shopPhaoRenderer.sprite = previewSprite;
        if (phaoRenderer != null) phaoRenderer.sprite = previewSprite;

        // KIỂM TRA TRẠNG THÁI SỞ HỮU
        bool isUnlocked = (index == 0 || PlayerPrefs.GetInt("PhaoUnlocked_" + index, 0) == 1);

        if (isUnlocked)
        {
            // Nếu đã có rồi thì tự động lưu và báo "Đã trang bị"
            PlayerPrefs.SetInt("SelectedPhaoID", index);
            PlayerPrefs.Save();
            ShowShopNotification("Đã trang bị pháo!");
        }
        else
        {
            // Nếu chưa có thì báo "Chưa sở hữu"
            ShowShopNotification("Chưa sở hữu khẩu pháo này!");
        }
    }

    public void Click_ConfirmPurchasePhao()
    {
        // 1. Kiểm tra nếu chưa chọn pháo nào để xem
        if (pendingPhaoIndex == -1)
        {
            ShowShopNotification("Vui lòng chọn một khẩu pháo!");
            return;
        }

        PhaoSkin skin = allPhaoSkins[pendingPhaoIndex];
        // Kiểm tra trạng thái mở khóa
        bool isUnlocked = pendingPhaoIndex == 0 || PlayerPrefs.GetInt("PhaoUnlocked_" + pendingPhaoIndex, 0) == 1;

        // 2. Logic xử lý Mua hoặc Mặc
        if (isUnlocked || totalCoins >= skin.price)
        {
            if (!isUnlocked)
            {
                // Thực hiện trừ tiền nếu chưa mua
                totalCoins -= skin.price;
                PlayerPrefs.SetInt("TotalCoins", totalCoins);
                PlayerPrefs.SetInt("PhaoUnlocked_" + pendingPhaoIndex, 1);

                ShowShopNotification("Đã mua thành công: " + skin.phaoName + "!");
            }
            else
            {
                ShowShopNotification("Đã sở hữu: " + skin.phaoName);
            }

            // Lưu lựa chọn vào máy
            PlayerPrefs.SetInt("SelectedPhaoID", pendingPhaoIndex);
            PlayerPrefs.Save();

            // 3. Cập nhật giao diện và hình ảnh
            UpdateCoinUI();      // Sẽ hiện tiền kèm dấu $ nếu bạn đã sửa hàm này
            UpdatePhaoShopUI();  // Sẽ hiện chữ "Đã sở hữu" hoặc giá kèm dấu $
            LoadCurrentPhao();   // Đồng bộ hình ảnh cho cả Shop và Gameplay
        }
        else
        {
            // Thông báo khi không đủ tiền
            int thieu = skin.price - totalCoins;
            ShowShopNotification("Bạn còn thiếu " + thieu + "$ để mua pháo này!");
        }
    }
    public void UpdatePhaoShopUI()
    {
        for (int i = 0; i < allPhaoSkins.Length; i++)
        {
            bool unlocked = i == 0 || PlayerPrefs.GetInt("PhaoUnlocked_" + i, 0) == 1;
            if (i < phaoButtonImages.Length)
            {
                phaoButtonImages[i].color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
            }
            if (i < phaoPriceTexts.Length)
            {
                phaoPriceTexts[i].text = unlocked ? "Đã sở hữu" : allPhaoSkins[i].price.ToString() + "$";
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
            panelWin.SetActive(true);
            Time.timeScale = 0f;
            DragAndDrop.SetGlobalLock(true);

            // LƯU DỮ LIỆU MỞ KHÓA MÀN TIẾP THEO
            int currentHighest = PlayerPrefs.GetInt("HighestLevelReached", 1);
            int wonLevel = LevelManager.CurrentLevel;

            // Nếu thắng màn hiện tại là màn cao nhất, mở màn tiếp theo
            if (wonLevel == currentHighest && wonLevel < 100)
            {
                PlayerPrefs.SetInt("HighestLevelReached", wonLevel + 1);
                PlayerPrefs.Save(); // Đảm bảo dữ liệu được ghi xuống bộ nhớ

                // Cập nhật lại danh sách nút để hiển thị màn mới vừa mở
                GenerateLevelButtons();
            }
        }
    }
    // Hàm đợi 3 giây trước khi hiện bảng thắng để tiền kịp nạp vào
    private System.Collections.IEnumerator WaitAndShowWin()
    {
        // Đợi 3 giây thực tế (không bị ảnh hưởng bởi Time.timeScale nếu bạn muốn)
        yield return new WaitForSecondsRealtime(2f);

        // Gọi hàm hiện bảng thắng gốc của bạn
        ShowWin();
    }
    public void ShowLose()
    {
        if (panelLose != null)
        {
            panelLose.SetActive(true);
            Time.timeScale = 0f; // Tạm dừng game khi thua
            DragAndDrop.SetGlobalLock(true); // KHÓA kéo thả khi thắng
        }

    }

    public void Click_Retry()
    {
        Time.timeScale = 1f;
        DragAndDrop.ReleaseAllLocks();
        BatDauChoiMan(LevelManager.CurrentLevel);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
    }

    public void Click_Next()
    {
        Time.timeScale = 1f;
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
    }
    public void ResetAllGameData()
    {
        // Xóa tiền
        PlayerPrefs.DeleteKey("TotalCoins");
        totalCoins = 0;
        UpdateCoinUI();

        // Xóa skin (gọi hàm vừa tạo ở trên)
        ResetSkins();

        // Xóa tiến trình màn chơi
        PlayerPrefs.DeleteKey("HighestLevelReached");

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
        PlayerPrefs.SetInt("SelectedSkinID", 0);

        // 2. Khóa lại tất cả skin mèo (trừ cái đầu tiên)
        if (allSkins != null)
        {
            for (int i = 1; i < allSkins.Length; i++)
            {
                PlayerPrefs.DeleteKey("SkinUnlocked_" + i);
            }
        }

        // --- RESET SKIN PHÁO ---
        // 3. Reset ID pháo về mặc định (0)
        PlayerPrefs.SetInt("SelectedPhaoID", 0);

        // 4. Khóa lại tất cả skin pháo (trừ cái đầu tiên)
        if (allPhaoSkins != null)
        {
            for (int i = 1; i < allPhaoSkins.Length; i++)
            {
                PlayerPrefs.DeleteKey("PhaoUnlocked_" + i);
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

    #region LOGIC SINH NÚT MÀN CHƠI
    private void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null || contentParent == null) return;
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Lấy màn cao nhất đã mở khóa (Mặc định là màn 1)
        int highestLevelReached = PlayerPrefs.GetInt("HighestLevelReached", 1);

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            Button btn = btnObj.GetComponent<Button>();

            // Tính toán vị trí nút (giữ nguyên logic của bạn)
            float posX = (i - 1) * buttonSpacing;
            float posY = Mathf.Sin(i * waveFrequency) * waveAmplitude;
            btnRect.anchoredPosition = new Vector2(posX + (buttonSpacing / 2f), posY);

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = i.ToString();

            int levelIndex = i;

            // KIỂM TRA MỞ KHÓA
            if (i <= highestLevelReached)
            {
                // Màn đã mở: Cho phép bấm và để màu bình thường
                btn.interactable = true;
                btn.image.color = Color.white;
                btn.onClick.AddListener(() => BatDauChoiMan(levelIndex));
            }
            else
            {
                // Màn bị khóa: Không cho bấm và làm mờ nút
                btn.interactable = false;
                btn.image.color = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Màu xám mờ
            }
        }
        contentParent.sizeDelta = new Vector2(100 * buttonSpacing, contentParent.sizeDelta.y);
    }

    public void BatDauChoiMan(int levelIndex)
    {
        LoadCurrentPhao();
        LoadCurrentSkin();
        levelCoins = 0;
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
        }

        if (WallHealth.Instance != null)
        {
            WallHealth.Instance.ResetHealth();
        }

        DragQuizManager qm = FindObjectOfType<DragQuizManager>();
        if (qm != null) qm.UpdateDifficulty();
    }
    
    #endregion
}