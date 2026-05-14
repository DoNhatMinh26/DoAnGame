using DoAnGame.Auth;
using DoAnGame.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UiClass : MonoBehaviour
{
    public static UiClass Instance;
    [Header("Cấu hình vị trí thủ công")]
    public RectTransform[] waypoints; // Kéo 20 cái P1...P20 vào đây
    public float backgroundWidth = 1920f; // Chiều rộng 1 tấm ảnh của bạn

    [Header("Cấu hình Danh sách Màn học")]
    public GameObject levelButtonPrefab;
    public RectTransform contentParent;
    [SerializeField] private float buttonSpacing = 160f;
    [SerializeField] private float waveAmplitude = 100f;
    [SerializeField] private float waveFrequency = 0.4f;

    [Header("Quản lý các Panel Chính")]
    public GameObject panelHome;
    public GameObject panelChonBai;
    public GameObject panelGameplay;
    public GameObject panelSetting;
    public Button settingButton;

    [Header("Quản lý Kết Thúc")]
    public GameObject panelWin;
    public GameObject panelLose;
    public bool isGameOver = false;

    [Header("UI Bảng Win")]
    public TextMeshProUGUI winRewardTxt;   // Hiện số tiền thưởng (Ví dụ: +10$)
    public TextMeshProUGUI winScoreTxt;    // Hiện số điểm trong màn (Ví dụ: +100 Điểm)
    public TextMeshProUGUI winLevelInfoTxt; // Hiện "Hoàn thành Màn X"
    [Header("UI Bảng Lose")]
    public TextMeshProUGUI loseRewardTxt; // Hiện tiền nhặt được khi thua
    public TextMeshProUGUI loseScoreTxt;  // Hiện điểm nhặt được khi thua
    public TextMeshProUGUI loseProgressTxt; // Hiện "Số câu đúng: X/Y"

    [Header("Quản lý Tiền & Phần thưởng")]
    public TextMeshProUGUI totalCoinTxt; // Text hiển thị ở Menu
    public TextMeshProUGUI levelCoinTxt; // Text hiển thị trong Gameplay
    public Transform coinTarget;        // Vị trí icon tiền để tiền bay về
    private int totalCoins = 0;
    private int levelCoins = 0;
    public TextMeshProUGUI levelScoreGameplayTxt; // Text hiển thị điểm TRONG trận đấu
    private int levelScore = 0;
    [Header("Cấu hình hiệu ứng tiền")]
    public GameObject coinPrefab; // Kéo Prefab hình đồng xu vào đây
    public Transform coinSpawnPoint; // Kéo Empty Object (vị trí sinh tiền) vào đây
    public float flySpeed = 15f;
    [Header("UI Thông báo")]
    public CanvasGroup notificationCanvasGroup;
    public TextMeshProUGUI notificationTxt;
    [Header("Cấu hình Độ khó")]
    [SerializeField] private ClassDifficultyConfig classDifficulty;

    [Header("UI Hiển Thị Tiến Độ")]
    [SerializeField] private TextMeshProUGUI progressTxt;
    public int targetCorrectAnswers; // Mục tiêu từ file ClassDifficultyConfig[cite: 17]
    public int currentCorrectCount;
    [Header("Quản lý Máu (HP)")]
    public GameObject[] hearts; // Kéo 3 trái tim tim1, tim1(1), tim1(2) vào đây
    private int currentHealth;
    [Header("Cấu hình thời gian chờ")]
    [SerializeField] private float delayTime = 1.0f;

    [Header("Dữ liệu & UI Shop")]
    public MathSkin[] allSkins;
    public Image[] skinButtonImages;
    public TextMeshProUGUI[] skinPriceTexts;
    public Image[] skinPriceBackgrounds;

    [Header("Cấu hình 4 Mèo Chung Xương")]
    public GameObject[] catSkins; // Mảng chứa 4 con mèo
    public Animator sharedAnimator; // Animator tổng điều khiển xương
    public TextMeshProUGUI shopLevelTxt; // Kéo Text hiển thị Level vào đây
    public TextMeshProUGUI shopScoreTxt; // Kéo Text hiển thị Điểm vào đây// Hình ảnh linh vật mèo trong trận đấu
    [Header("Cấu hình Hiệu ứng Nhấn")]
    [SerializeField] private float scaleAmount = 1.2f; // Độ phóng to (1.2 là 120%)
    [SerializeField] private float scaleDuration = 0.1f; // Thời gian phóng to/thu nhỏ
    private int pendingSkinIndex = -1;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        InitSkinShop();
        UpdateShopProfileUI();
        LoadCoins(); // Tự nạp tiền từ PlayerPrefs[cite: 9]
        GenerateLevelButtons(); // Tự sinh danh sách nút[cite: 9]
        ShowHome();

    }
    // Hàm cập nhật chữ hiển thị lên màn hình[cite: 15]
    private void UpdateProgressUI()
    {
        if (progressTxt != null)
        {
            // Hiển thị theo định dạng: Số câu đúng : Hiện tại / Mục tiêu[cite: 15, 17]
            progressTxt.text = $"Số câu đúng : {currentCorrectCount}/{targetCorrectAnswers}";
        }
    }
    public void UpdateShopProfileUI()
    {
        // 1. Xác định đúng Key (Guest hoặc User) giống như trong DataManager
        bool isGuest = DoAnGame.UI.UIQuickPlayNameController.IsGuestMode();
        string scoreKey = isGuest ? LocalStorageKeyResolver.LocalGuestScore : LocalStorageKeyResolver.UserScore;
        string levelKey = isGuest ? LocalStorageKeyResolver.LocalGuestLevel : LocalStorageKeyResolver.UserLevel;
        

        // 2. Lấy dữ liệu mới nhất
        int currentScore = PlayerPrefs.GetInt(scoreKey, 0);
        int currentLevel = PlayerPrefs.GetInt(levelKey, 1);
        

        // 3. Cập nhật lên UI
        if (shopScoreTxt != null) shopScoreTxt.text = "Điểm : "+currentScore.ToString();
        if (shopLevelTxt != null) shopLevelTxt.text = "Level: " + currentLevel.ToString(); // Dòng quan trọng cập nhật Level
        
    }
    private void SetSettingButtonInteractable(bool state)
    {
        if (settingButton != null)
        {
            settingButton.interactable = state;
        }
    }
    public void AddLevelScore(int amount)
    {
        levelScore += amount;
        if (levelScoreGameplayTxt != null)
        {
            levelScoreGameplayTxt.text = "Điểm: +" + levelScore.ToString();
        }
    }
    #region QUẢN LÝ SHOP
    public void SpawnAndFlyCoin(int amount)
    {
        if (coinPrefab == null || coinSpawnPoint == null || coinTarget == null)
        {
            // Nếu chưa kịp setup hiệu ứng thì cộng thẳng luôn để không lỗi game
            AddCoins(amount);
            return;
        }

        // Sinh ra đồng xu tại vị trí Empty Object
        GameObject newCoin = Instantiate(coinPrefab, coinSpawnPoint.position, Quaternion.identity, transform);

        // Bắt đầu hiệu ứng bay
        StartCoroutine(FlyCoinRoutine(newCoin, amount));
    }

    private IEnumerator FlyCoinRoutine(GameObject coin, int amount)
    {
        // Bay từ điểm sinh đến mục tiêu (coinTarget)
        while (Vector3.Distance(coin.transform.position, coinTarget.position) > 0.1f)
        {
            coin.transform.position = Vector3.MoveTowards(coin.transform.position, coinTarget.position, flySpeed * Time.deltaTime);
            yield return null;
        }

        // Khi đã chạm vào biểu tượng tiền trên UI
        Destroy(coin); // Xóa đồng xu hiệu ứng
        AddCoins(amount); // Lúc này mới thực sự cộng tiền vào hệ thống
    }
    #endregion
    #region QUẢN LÝ SHOP (Dựa trên UiSp)

    public void InitSkinShop()
    {
        LoadCurrentSkin();
        UpdateSkinShopUI();
    }
    private Coroutine activeScaleCoroutine; // Khai báo biến này ở đầu Class
    public void SelectSkinToPreview(int index)
    {
        if (allSkins == null || index < 0 || index >= allSkins.Length) return;
        if (index < skinButtonImages.Length && skinButtonImages[index] != null)
        {
            RectTransform rt = skinButtonImages[index].GetComponent<RectTransform>();
            if (rt != null)
            {
                // Dừng coroutine đang chạy (nếu có) để tránh xung đột tỷ lệ scale
                if (activeScaleCoroutine != null) StopCoroutine(activeScaleCoroutine);
                activeScaleCoroutine = StartCoroutine(ScaleButtonRoutine(rt));
            }
        }
        pendingSkinIndex = index; // Ghi nhận đang xem con nào

        // 1. Chỉ bật hình ảnh để xem thử (Preview)
        for (int i = 0; i < catSkins.Length; i++)
        {
            if (catSkins[i] != null) catSkins[i].SetActive(i == index);
        }

        // 2. Kiểm tra trạng thái để hiện thông báo
        if (IsSkinUnlocked(index))
        {
            // Nếu đã sở hữu, có thể tự động trang bị luôn
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedClassSkinID, index);
            PlayerPrefs.Save();
            
        }
        else
        {
            // Nếu chưa mua, CHỈ hiện giá, KHÔNG lưu vào PlayerPrefs
            ShowClassNotification("Giá: " + allSkins[index].price + "$");

        }

        UpdateSkinShopUI();
    }
    public void ResetToCurrentSkin()
    {
        // Gọi lại hàm Load để bật đúng con mèo đã mua/lưu
        LoadCurrentSkin();
    }
    public void PlayMascotAnimation(string triggerName)
    {
        // Đổi tên biến thành sharedAnimator cho khớp với khai báo của bạn
        if (sharedAnimator != null)
        {
            sharedAnimator.SetTrigger(triggerName);
        }
    }
    public void Click_BuySkin()
    {
        if (pendingSkinIndex == -1)
        {
            ShowClassNotification("Vui lòng chọn một trang phục!"); 
        return;
        }

        MathSkin skin = allSkins[pendingSkinIndex];
        bool isUnlocked = IsSkinUnlocked(pendingSkinIndex);

        // Kiểm tra tiền và trạng thái khóa
        if (!isUnlocked && totalCoins >= skin.price)
        {
            AddCoins(-skin.price); // Trừ tiền bằng hàm có sẵn
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.ClassSkinUnlockedKey(pendingSkinIndex), 1);
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedClassSkinID, pendingSkinIndex);
            PlayerPrefs.Save();

            ShowClassNotification("Mua thành công: " + skin.shipName + "!"); 
            UpdateSkinShopUI();
            LoadCurrentSkin();

            // Sync shop lên Firebase
            SyncChondaShop();
        }
        else if (isUnlocked)
        {
            ShowClassNotification("Đã sở hữu trang phục này"); 
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedClassSkinID, pendingSkinIndex);
            PlayerPrefs.Save();
            LoadCurrentSkin();

            // Sync selected skin lên Firebase
            SyncChondaShop();
        }
        else
        {
            int thieu = skin.price - totalCoins;
            ShowClassNotification("Bạn còn thiếu " + thieu + "$!"); 
        }
    }

    private void SyncChondaShop()
    {
        int selected = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedClassSkinID, 0);
        var unlocked = new System.Collections.Generic.List<int> { 0 };
        for (int i = 1; i < allSkins.Length; i++)
        {
            if (IsSkinUnlocked(i)) unlocked.Add(i);
        }
        DoAnGame.Auth.CloudSyncService.Instance?.OnShopPurchased("chonda_skin", selected, unlocked.ToArray());
    }

    public void LoadCurrentSkin()
    {
        int selectedID = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedClassSkinID, 0);

        // Duyệt mảng để bật/tắt từng con mèo
        if (catSkins != null)
        {
            for (int i = 0; i < catSkins.Length; i++)
            {
                if (catSkins[i] != null)
                {
                    catSkins[i].SetActive(i == selectedID);
                }
            }
        }

        // Làm mới trạng thái Animator sau khi thay đổi
        if (sharedAnimator != null)
        {
            sharedAnimator.Rebind();
            sharedAnimator.Update(0f);
        }
    }

    private bool IsSkinUnlocked(int index) => index == 0 || PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.ClassSkinUnlockedKey(index), 0) == 1;

    public void UpdateSkinShopUI()
    {
        // Lấy ID con mèo đang được mặc thực tế từ máy
        int currentEquippedID = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedClassSkinID, 0);

        for (int i = 0; i < allSkins.Length; i++)
        {
            bool unlocked = IsSkinUnlocked(i);

            // 1. Làm mờ ảnh nếu chưa mua
            if (i < skinButtonImages.Length)
                skinButtonImages[i].color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);

            // 2. Cập nhật văn bản hiển thị
            if (i < skinPriceTexts.Length)
            {
                if (unlocked)
                {
                    // Nếu ID này trùng với ID đang mặc thì hiện "Đã trang bị"
                    if (i == currentEquippedID)
                    {
                        skinPriceTexts[i].text = "Đang dùng";
                        skinPriceTexts[i].color = Color.white;
                        Color customColor;
                        if (ColorUtility.TryParseHtmlString("#007BFF", out customColor))
                        {
                            skinPriceBackgrounds[i].color = customColor;
                        }
                    }
                    else
                    {
                        skinPriceTexts[i].text = "Sở hữu"; // Hoặc "Đã sở hữu"
                        skinPriceTexts[i].color = Color.white;
                        Color customColor;
                        if (ColorUtility.TryParseHtmlString("#00FF5D", out customColor))
                        {
                            skinPriceBackgrounds[i].color = customColor;
                        }
                            
                    }
                }
                else
                {
                    // Nếu chưa mua thì hiện giá tiền
                    skinPriceTexts[i].text = allSkins[i].price + "$";
                    Color customColor;
                    if (ColorUtility.TryParseHtmlString("#736921", out customColor))
                    {
                        skinPriceTexts[i].color = customColor;
                    }
                    skinPriceBackgrounds[i].color = Color.white;
                }
            }
        }
    }
    private IEnumerator ScaleButtonRoutine(RectTransform target)
    {
        // 1. LƯU LẠI SCALE BAN ĐẦU thực tế của Object đó
        Vector3 originalScale = target.localScale;

        // 2. Tính toán mục tiêu phóng to (ví dụ phóng lên thêm 20% so với gốc)
        Vector3 targetScale = originalScale * scaleAmount;

        float elapsed = 0;
        

        // Giai đoạn phóng to
        while (elapsed < scaleDuration)
        {
            target.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / scaleDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        target.localScale = targetScale;

        // Giai đoạn thu nhỏ về
        elapsed = 0;
        while (elapsed < scaleDuration)
        {
            target.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / scaleDuration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // 3. TRẢ VỀ ĐÚNG GIÁ TRỊ GỐC đã lưu ở bước 1
        target.localScale = originalScale;
    }
    #endregion
    #region QUẢN LÝ TIỀN (Độc lập hoàn toàn)
    private void LoadCoins()
    {
        // Sử dụng LocalStorageKeyResolver để main/clone không đụng chung
        totalCoins = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.TotalCoins, 0);
        levelCoins = 0;
        UpdateCoinUI();
    }

    public void AddCoins(int amount)
    {
        if (amount > 0) levelCoins += amount;
        totalCoins += amount;

        PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.TotalCoins, totalCoins);
        PlayerPrefs.Save();
        UpdateCoinUI();

        // Sync coins lên Firebase nếu đã đăng nhập
        if (amount != 0)
            DoAnGame.Auth.CloudSyncService.Instance?.OnCoinsChanged(totalCoins);
    }

    private void UpdateCoinUI()
    {
        if (totalCoinTxt != null) totalCoinTxt.text = totalCoins.ToString();
        if (levelCoinTxt != null) levelCoinTxt.text = levelCoins.ToString();
    }
    #endregion
    #region ĐIỀU HƯỚNG VÀ ĐIỀU KHIỂN PANEL

    // Hàm gọi khi nhấn nút "Play" ở màn hình chính để vào chọn màn
    public void Click_OpenChonMan()
    {
        DeactivateAll();
        if (panelChonBai != null)
        {
            panelChonBai.SetActive(true);
            GenerateLevelButtons(); // Cập nhật lại trạng thái các nút (khóa/mở)
        }
        Time.timeScale = 1f;
    }

    // Hàm gọi khi nhấn nút "Back" hoặc biểu tượng Home
    public void Click_BackToHome()
    {
        Time.timeScale = 1f;

        // Bạn có thể thêm lệnh dọn dẹp quái/vật phẩm tại đây nếu cần

        DeactivateAll();
        if (panelHome != null) panelHome.SetActive(true);
    }

    // Hàm mở bảng Setting (Cài đặt)
    public void Click_OpenSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(true);
            Time.timeScale = 0f; // Tạm dừng game để chỉnh chỉ số
        }
    }

    // Hàm đóng bảng Setting
    public void Click_CloseSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);

            // Chỉ chạy lại thời gian nếu người chơi không đang ở bảng Win/Lose
            if (!panelWin.activeSelf && !panelLose.activeSelf)
            {
                Time.timeScale = 1f;
            }
        }
    }

    // Hàm chơi lại màn hiện tại (Retry)
    public void Click_Retry()
    {
        Time.timeScale = 1f;
        BatDauBaiHoc(LevelManager.CurrentLevel);

        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
    }

    // Hàm sang màn kế tiếp (Next Level)
    public void Click_NextLevel()
    {
        Time.timeScale = 1f;
        int nextLevel = LevelManager.CurrentLevel + 1;

        // Giới hạn tối đa 100 màn
        if (nextLevel > 100)
        {
            ShowClassNotification("Bạn đã hoàn thành tất cả các màn!");
            Click_OpenChonMan();
        }
        else
        {
            BatDauBaiHoc(nextLevel);
        }

        if (panelWin != null) panelWin.SetActive(false);
    }
    public void Click_ThoatNgayLapTuc()
    {
        if (isGameOver) return;
        isGameOver = true;
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);
        }
        // 1. Dừng thời gian game ngay lập tức
        Time.timeScale = 0f;

        // 2. Khóa các tương tác khác
        SetSettingButtonInteractable(false);

        // 3. Gán dữ liệu ngay lập tức cho các Text ở bảng Lose
        if (loseRewardTxt != null)
            loseRewardTxt.text = "+" + levelCoins.ToString();

        if (loseScoreTxt != null)
            loseScoreTxt.text = "Điểm: +" + levelScore.ToString();

        if (loseProgressTxt != null)
        {
            // currentCorrectCount: số câu đã làm đúng hiện tại
            // targetCorrectAnswers: tổng số câu cần để thắng
            loseProgressTxt.text = $"Số câu đúng: {currentCorrectCount}/{targetCorrectAnswers}";
        }

        // 4. Hiện bảng thua ngay lập tức (không delay)
        if (panelLose != null)
        {
            panelLose.SetActive(true);
        }

        Debug.Log("Đã thoát nhanh: Hiện bảng thua.");
    }
    #endregion
    #region LOGIC SINH NÚT MÀN CHƠI
    public void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null || contentParent == null || waypoints.Length == 0) return;

        // 1. Xóa các nút cũ nhưng giữ lại Background và Waypoints_Group
        foreach (Transform child in contentParent)
        {
            // Giữ lại các đối tượng chứa "Image" và đối tượng "ViTriMan"
            if (!child.name.Contains("Image") && child.name != "ViTriMan")
            {
                Destroy(child.gameObject);
            }
        }

        int highestLevel = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.ClassHighest, 1);

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();

            // 2. Tính toán vị trí dựa trên Waypoint
            int indexInImage = (i - 1) % 10; // Chia cho 10 vì mỗi ảnh có 10 nút
            int imageIndex = (i - 1) / 10;   // Xác định nút thuộc tấm ảnh thứ mấy (0-9)

            // Lấy tọa độ X, Y của điểm mốc tương ứng
            Vector2 pointPos = waypoints[indexInImage].anchoredPosition;

            // Cộng thêm độ lệch theo chiều ngang của tấm ảnh thứ n
            float finalPosX = pointPos.x + (imageIndex * backgroundWidth);

            btnRect.anchoredPosition = new Vector2(finalPosX, pointPos.y);

            // 3. Gán số và xử lý logic khóa/mở nút
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = i.ToString();

            Button btn = btnObj.GetComponent<Button>();
            int levelIndex = i;
            if (i <= highestLevel || i == 30 || i == 50 || i == 70 || i == 100)
            {
                btn.interactable = true;
                btn.image.color = Color.white;
                btn.onClick.AddListener(() => BatDauBaiHoc(levelIndex));
            }
            else
            {
                btn.interactable = false;
                btn.image.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            }
        }

        // 4. Thiết lập độ dài vùng kéo cho 5 tấm ảnh
        contentParent.sizeDelta = new Vector2((backgroundWidth * 10)+180, contentParent.sizeDelta.y);
    }

    public void BatDauBaiHoc(int levelIndex)
    {
        LevelManager.CurrentLevel = levelIndex;
        levelCoins = 0;
        levelScore = 0;
        if (levelScoreGameplayTxt != null) levelScoreGameplayTxt.text = "Điểm: 0";
        UpdateCoinUI();
        isGameOver = false;
        SetSettingButtonInteractable(true);
        ResetHealth();
        ShowGameplay();
        MathManager math = FindObjectOfType<MathManager>();
        if (math != null)
        {
            math.UpdateDifficulty(); // Gọi hàm này để nạp cấu hình màn chơi
        }
    }
    #endregion

    #region QUẢN LÝ PANEL (Tự động hóa hoàn toàn)
    public void ShowHome()
    {
        UpdateShopProfileUI();
        UpdateSkinShopUI();
        LoadCurrentSkin();
        DeactivateAll();
        if (panelHome != null) panelHome.SetActive(true);
        Time.timeScale = 1f;
    }

    public void ShowGameplay()
    {
        DeactivateAll();
        
        if (panelGameplay != null) panelGameplay.SetActive(true);
        Time.timeScale = 1f;
    }
    public void SetupLevelDifficulty(int grade, int level)
    {
        if (classDifficulty != null)
        {
            targetCorrectAnswers = classDifficulty.GetTargetQuestions(grade, level); // Đọc từ AnimationCurve[cite: 17]
            currentCorrectCount = 0;
            isGameOver = false;
            UpdateProgressUI();
        }
    }

    // Hàm được MathManager gọi mỗi khi người chơi chọn đúng[cite: 15, 16]
    public void OnCorrectAnswer()
    {
        if (isGameOver) return;

        currentCorrectCount++;
        UpdateProgressUI();

        // Gọi trực tiếp thông qua Property Instance thông minh đã sửa ở trên
        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddScore(10);
        }
        else
        {
            Debug.LogError("Vẫn không tìm thấy DataManager! Hãy kiểm tra xem đối tượng DataManager đã được tạo trong Scene Menu chưa.");
        }

        if (currentCorrectCount >= targetCorrectAnswers)
        {
            WinGame();
        }
    }
    public void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        SetSettingButtonInteractable(false);
        // Lưu tiến trình local
        int highestLevel = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.ClassHighest, 1);
        if (LevelManager.CurrentLevel >= highestLevel)
        {
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.ClassHighest, LevelManager.CurrentLevel + 1);
            PlayerPrefs.Save();
        }

        // Sync lên Firebase (nếu đã đăng nhập)
        DoAnGame.Auth.CloudSyncService.Instance?.OnLevelCompleted(
            gameMode:     "chonda",
            grade:        UIManager.SelectedGrade,
            levelNumber:  LevelManager.CurrentLevel,
            score:        currentCorrectCount * 10,  // điểm màn này = số câu đúng × 10
            coinsEarned:  levelCoins
        );

        // Chạy Coroutine chờ rồi mới hiện bảng Win
        StartCoroutine(ShowWinPanelWithDelay());
    }

    IEnumerator ShowWinPanelWithDelay()
    {
        yield return new WaitForSeconds(delayTime); // Đợi 1 khoảng thời gian[cite: 17]
        if (winRewardTxt != null)
            winRewardTxt.text = "+" + levelCoins.ToString();

        // 2. Hiển thị Điểm nhặt được
        if (winScoreTxt != null)
            winScoreTxt.text = "Điểm: +" + levelScore.ToString() ;


        // 3. Chỉ giữ lại thông tin Hoàn thành màn chơi
        if (winLevelInfoTxt != null)
        {
            // LevelManager.CurrentLevel là biến lưu số màn bạn đang chơi
            winLevelInfoTxt.text = "Hoàn thành Màn " + LevelManager.CurrentLevel;
        }
        if (panelWin != null) panelWin.SetActive(true);
    }
    public void ResetHealth()
    {
        currentHealth = hearts.Length; 
    for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i].SetActive(true); // Hiển thị lại tất cả trái tim
        }
        isGameOver = false; 
}

    public void OnWrongAnswer()
    {
        if (isGameOver) return; 

        currentHealth--; 

    // Ẩn trái tim tương ứng
        if (currentHealth >= 0 && currentHealth < hearts.Length)
            {
            hearts[currentHealth].SetActive(false); 
        }

        // Kiểm tra nếu hết máu thì thua
        if (currentHealth <= 0)
        {
            LoseGame(); 
        }
    }

    public void LoseGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        SetSettingButtonInteractable(false);
        // Chạy Coroutine chờ rồi mới hiện bảng Lose[cite: 17]
        StartCoroutine(ShowLosePanelWithDelay());
    }

    IEnumerator ShowLosePanelWithDelay()
    {
        
        yield return new WaitForSeconds(delayTime); // Đợi 1 khoảng thời gian[cite: 17]
        Time.timeScale = 0f; // Tạm dừng game sau khi bảng hiện lên[cite: 17]
        if (loseRewardTxt != null)
            loseRewardTxt.text = "+" + levelCoins.ToString();

        // 2. Hiển thị Điểm nhặt được (levelScore)
        if (loseScoreTxt != null)
            loseScoreTxt.text = "Điểm: +" + levelScore.ToString();

        // 3. Hiển thị số câu đúng/mục tiêu (currentCorrectCount/targetCorrectAnswers)
        if (loseProgressTxt != null)
        {
            loseProgressTxt.text = $"Số câu đúng: {currentCorrectCount}/{targetCorrectAnswers}";
        }

        if (panelLose != null) panelLose.SetActive(true);
    }
    private void DeactivateAll()
    {
        if (panelHome != null) panelHome.SetActive(false);
        if (panelChonBai != null) panelChonBai.SetActive(false);
        if (panelGameplay != null) panelGameplay.SetActive(false);
        if (panelSetting != null) panelSetting.SetActive(false);
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
    }
    #endregion

    #region HIỆU ỨNG THÔNG BÁO (Fade Effect)
    public void ShowClassNotification(string message)
    {
        if (notificationTxt != null && notificationCanvasGroup != null)
        {
            StopAllCoroutines();
            notificationTxt.text = message;
            StartCoroutine(FadeNotificationRoutine());
        }
    }

    private IEnumerator FadeNotificationRoutine()
    {
        notificationCanvasGroup.alpha = 1f;
        notificationCanvasGroup.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(1.5f);

        float duration = 1f;
        float currentTime = 0f;
        while (currentTime < duration)
        {
            currentTime += Time.unscaledDeltaTime;
            notificationCanvasGroup.alpha = Mathf.Lerp(1f, 0f, currentTime / duration); 
            yield return null;
        }
        notificationCanvasGroup.gameObject.SetActive(false);
    }
    #endregion
}