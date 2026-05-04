using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UiClass : MonoBehaviour
{
    public static UiClass Instance;

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

    [Header("Quản lý Kết Thúc")]
    public GameObject panelWin;
    public GameObject panelLose;
    public bool isGameOver = false;

    [Header("Quản lý Tiền & Phần thưởng")]
    public TextMeshProUGUI totalCoinTxt; // Text hiển thị ở Menu
    public TextMeshProUGUI levelCoinTxt; // Text hiển thị trong Gameplay
    public Transform coinTarget;        // Vị trí icon tiền để tiền bay về
    private int totalCoins = 0;
    private int levelCoins = 0;
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
    [Header("Giao diện Shop Trang Phục")]
    public MathSkin[] allSkins;          // Danh sách ScriptableObject trang phục[cite: 18, 20]
    public Image[] skinButtonImages;     // Hình ảnh hiển thị trên các nút chọn
    public TextMeshProUGUI[] skinPriceTexts; // Text hiển thị giá tiền
    public SpriteRenderer shopMascotPreview;
    public SpriteRenderer gameplayMascotRenderer; // Hình ảnh linh vật mèo trong trận đấu

    private int pendingSkinIndex = -1;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        InitSkinShop();
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

    public void SelectSkinToPreview(int index)
    {
        if (allSkins == null || index < 0 || index >= allSkins.Length) return;

        pendingSkinIndex = index;

        // 1. Hiển thị hình ảnh xem trước trong Shop
        if (shopMascotPreview != null)
            shopMascotPreview.sprite = allSkins[index].shipSprite; // Dùng shipSprite từ MathSkin[cite: 18, 20]

        // 2. Kiểm tra trạng thái sở hữu
        if (IsSkinUnlocked(index))
        {
            // Nếu đã có, trang bị ngay lập tức
            PlayerPrefs.SetInt("SelectedClassSkinID", index);
            PlayerPrefs.Save();

            // Cập nhật hình ảnh linh vật trong trận[cite: 20]
            if (gameplayMascotRenderer != null)
                gameplayMascotRenderer.sprite = allSkins[index].shipSprite;

            ShowClassNotification("Đã trang bị: " + allSkins[index].shipName); 
    }
        else
        {
            ShowClassNotification("Giá: " + allSkins[index].price + "$"); 
    }

        UpdateSkinShopUI();
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
            PlayerPrefs.SetInt("ClassSkinUnlocked" + pendingSkinIndex, 1);
            PlayerPrefs.SetInt("SelectedClassSkinID", pendingSkinIndex);
            PlayerPrefs.Save();

            ShowClassNotification("Mua thành công: " + skin.shipName + "!"); 
        UpdateSkinShopUI();
            LoadCurrentSkin();
        }
        else if (isUnlocked)
        {
            ShowClassNotification("Đã sở hữu trang phục này"); 
        PlayerPrefs.SetInt("SelectedClassSkinID", pendingSkinIndex);
            PlayerPrefs.Save();
            LoadCurrentSkin();
        }
        else
        {
            int thieu = skin.price - totalCoins;
            ShowClassNotification("Bạn còn thiếu " + thieu + "$!"); 
    }
    }

    public void LoadCurrentSkin()
    {
        int id = PlayerPrefs.GetInt("SelectedClassSkinID", 0);

        if (id < allSkins.Length)
        {
            Sprite currentSprite = allSkins[id].shipSprite;

            // Cập nhật linh vật mèo cho cả Shop và Gameplay[cite: 20]
            if (shopMascotPreview != null) shopMascotPreview.sprite = currentSprite;
            if (gameplayMascotRenderer != null) gameplayMascotRenderer.sprite = currentSprite;
        }
    }

    private bool IsSkinUnlocked(int index) => index == 0 || PlayerPrefs.GetInt("ClassSkinUnlocked" + index, 0) == 1;

    public void UpdateSkinShopUI()
    {
        for (int i = 0; i < allSkins.Length; i++)
        {
            bool unlocked = IsSkinUnlocked(i);

            // Làm mờ nút nếu chưa mở khóa[cite: 20]
            if (i < skinButtonImages.Length)
                skinButtonImages[i].color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);

            // Hiển thị trạng thái Giá hoặc "Đã có"[cite: 20]
            if (i < skinPriceTexts.Length)
                skinPriceTexts[i].text = unlocked ? "Đã sở hữu" : allSkins[i].price + "$";
        }
    }
    #endregion
    #region QUẢN LÝ TIỀN (Độc lập hoàn toàn)
    private void LoadCoins()
    {
        // Sử dụng chung Key TotalCoins để đồng bộ tài sản toàn game[cite: 9]
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        levelCoins = 0;
        UpdateCoinUI();
    }

    public void AddCoins(int amount)
    {
        if (amount > 0) levelCoins += amount;
        totalCoins += amount;

        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();
        UpdateCoinUI();
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
    
    #endregion
    #region LOGIC SINH NÚT MÀN CHƠI
    public void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null || contentParent == null) return;

        // Xóa các nút cũ trước khi sinh mới[cite: 9]
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Lưu tiến trình riêng cho lớp học[cite: 9]
        int highestLevel = PlayerPrefs.GetInt("Class_HighestLevel", 1);

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();

            // Công thức sóng Sin tạo hiệu ứng uốn lượn[cite: 9]
            float posX = (i - 1) * buttonSpacing;
            float posY = Mathf.Sin(i * waveFrequency) * waveAmplitude;
            btnRect.anchoredPosition = new Vector2(posX + (buttonSpacing / 2f), posY);

            // Gán số màn chơi vào Text
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = i.ToString();

            Button btn = btnObj.GetComponent<Button>();
            int levelIndex = i;

            // Kiểm tra điều kiện mở khóa hoặc các mốc Test[cite: 1]
            if (i <= highestLevel || i == 30 || i == 50 || i == 70 || i == 100)
            {
                btn.interactable = true;
                btn.image.color = Color.white;
                btn.onClick.AddListener(() => BatDauBaiHoc(levelIndex));
            }
            else
            {
                btn.interactable = false;
                btn.image.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }
        }
        // Cập nhật độ dài của Content để ScrollView hoạt động[cite: 9]
        contentParent.sizeDelta = new Vector2(100 * buttonSpacing, contentParent.sizeDelta.y);
    }

    public void BatDauBaiHoc(int levelIndex)
    {
        LevelManager.CurrentLevel = levelIndex;
        levelCoins = 0;
        UpdateCoinUI();
        isGameOver = false;
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

        // Lưu tiến trình[cite: 17]
        int highestLevel = PlayerPrefs.GetInt("Class_HighestLevel", 1);
        if (LevelManager.CurrentLevel >= highestLevel)
        {
            PlayerPrefs.SetInt("Class_HighestLevel", LevelManager.CurrentLevel + 1);
            PlayerPrefs.Save();
        }

        // Chạy Coroutine chờ rồi mới hiện bảng Win[cite: 17]
        StartCoroutine(ShowWinPanelWithDelay());
    }

    IEnumerator ShowWinPanelWithDelay()
    {
        yield return new WaitForSeconds(delayTime); // Đợi 1 khoảng thời gian[cite: 17]
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
        
        // Chạy Coroutine chờ rồi mới hiện bảng Lose[cite: 17]
        StartCoroutine(ShowLosePanelWithDelay());
    }

    IEnumerator ShowLosePanelWithDelay()
    {
        
        yield return new WaitForSeconds(delayTime); // Đợi 1 khoảng thời gian[cite: 17]
        Time.timeScale = 0f; // Tạm dừng game sau khi bảng hiện lên[cite: 17]
       
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