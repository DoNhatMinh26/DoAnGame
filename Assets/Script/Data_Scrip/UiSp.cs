using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DoAnGame.Auth;
using UnityEngine.Video;
public class UiSp : MonoBehaviour
{
    public static UiSp Instance;
    [Header("Quản lý Animation")]
    public Animator gameplayAnimator;
    public Animator spaceshipAnimator;
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
    public GameObject panelWin;
    public GameObject panelLose;
    private bool isGameOver = false;

    [Header("Quản lý Tiền")]
    public TextMeshProUGUI totalCoinTxt; // Kéo Text ở Menu vào đây
    public TextMeshProUGUI levelCoinTxt; // Kéo Text ở Gameplay vào đây
    public Transform coinTarget;
    private int totalCoins = 0;
    private int levelCoins = 0;

    [Header("UI Thông báo")]
    public CanvasGroup notificationCanvasGroup;
    public TextMeshProUGUI notificationTxt;

    [Header("Giao diện Shop Phi Thuyền")]
    public ShipSkin[] allShips;
    public Image[] shipButtonImages;
    public TextMeshProUGUI[] shipPriceTexts;
    public SpriteRenderer shopShipRenderer;
    public SpriteRenderer gameplayShipRenderer; // Renderer của phi thuyền trong trận
    [Header("Giao diện Shop & Profile")]
    public TextMeshProUGUI shopScoreTxt; // 1. Hiện điểm ở Shop
    public TextMeshProUGUI shopLevelTxt; // 2. Hiện Level ở Shop

    [Header("Giao diện Gameplay")]
    public TextMeshProUGUI gameplayScoreRewardTxt; // 3. Điểm thưởng trong trận
    private int levelScore = 0; // Biến lưu điểm tạm thời của màn chơi

    [Header("Quản lý Panel Kết Thúc (6 Text)")]
    public TextMeshProUGUI winScoreTxt;    // 4. Điểm bảng Thắng
    public TextMeshProUGUI winRewardTxt;   // 5. Tiền bảng Thắng
    public TextMeshProUGUI winLevelInfoTxt; // 6. Thông tin màn (VD: Màn 1)

    public TextMeshProUGUI loseScoreTxt;   // 7. Điểm bảng Thua
    public TextMeshProUGUI loseRewardTxt;  // 8. Tiền bảng Thua
    public TextMeshProUGUI loseProgressTxt;// 9. Tiến trình (VD: 50%)
    [Header("Cấu hình Video kết thúc")]
    public VideoPlayer winVideo;   // Kéo Video Player thắng vào đây
    public VideoPlayer loseVideo;  // Kéo Video Player thua vào đây
    public GameObject videoBackground; // Một Panel đen để che màn hình khi phát video
    private int pendingShipIndex = -1;
    private void Awake() => Instance = this;

    private void Start()
    {
        UpdateShopProfileUI();
        LoadCoins();
        InitShipShop();
        GenerateLevelButtons();
        ShowHome();
    }
    #region QUẢN LÝ SHOP
    public void UpdateShopProfileUI()
    {
        // ✅ Dùng LocalStorageKeyResolver để đọc đúng key (có prefix)
        bool isGuest = DoAnGame.UI.UIQuickPlayNameController.IsGuestMode();
        string scoreKey = isGuest
            ? DoAnGame.Auth.LocalStorageKeyResolver.LocalGuestScore
            : DoAnGame.Auth.LocalStorageKeyResolver.UserScore;
        string levelKey = isGuest
            ? DoAnGame.Auth.LocalStorageKeyResolver.LocalGuestLevel
            : DoAnGame.Auth.LocalStorageKeyResolver.UserLevel;

        int score = PlayerPrefs.GetInt(scoreKey, 0);
        int level = PlayerPrefs.GetInt(levelKey, 1);

        if (shopScoreTxt != null) shopScoreTxt.text = "Điểm: +" + score.ToString();
        if (shopLevelTxt != null) shopLevelTxt.text = "Level" + level.ToString();
    }

    // Cộng điểm trong khi chơi
    public void AddScore(int amount)
    {
        if (isGameOver) return;

        // 1. Cộng vào biến tạm trong màn để hiện bảng Win/Lose
        levelScore += amount;

        // 2. Cập nhật Text hiển thị trong lúc chơi
        if (gameplayScoreRewardTxt != null)
            gameplayScoreRewardTxt.text = "Điểm: +" + levelScore.ToString();

        // 3. Gọi DataManager để lưu tổng điểm và đồng bộ Firebase
        if (DataManager.Instance != null)
        {
            DataManager.Instance.AddScore(amount);
            
        }
    }
    public void InitShipShop()
    {
        LoadCurrentShip();
        UpdateShipShopUI();
    }

    public void SelectShipToPreview(int index)
    {
        if (allShips == null || index < 0 || index >= allShips.Length) return;

        pendingShipIndex = index;

        // 1. Hiển thị hình ảnh lên Renderer ở Shop để xem trước
        if (shopShipRenderer != null)
            shopShipRenderer.sprite = allShips[index].shipSprite;

        // 2. Cho chạy Animation xem thử (Kể cả chưa mua vẫn xem được animation bay)
        if (spaceshipAnimator != null)
        {
            spaceshipAnimator.Play("Ship_" + index); // Nhảy thẳng đến animation của phi thuyền đang chọn
        }
        if (gameplayAnimator != null) gameplayAnimator.Play("Ship_" + index);
        // 3. Kiểm tra trạng thái sở hữu để Trang bị
        if (IsShipUnlocked(index))
        {
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedShipID, index);
            PlayerPrefs.Save();

            // Cập nhật lại toàn bộ (bao gồm cả renderer trong gameplay)
            LoadCurrentShip();

            ShowShopNotification("Đã trang bị: " + allShips[index].shipName);
        }
        else
        {
            ShowShopNotification("Chưa sở hữu");
        }

        UpdateShipShopUI();
    }

    public void Click_BuyShip()
    {
        if (pendingShipIndex == -1)
        {
            ShowShopNotification("Vui lòng chọn một phi thuyền!");
            return;
        }

        ShipSkin ship = allShips[pendingShipIndex];
        bool isUnlocked = IsShipUnlocked(pendingShipIndex);

        if (!isUnlocked && totalCoins >= ship.price)
        {
            AddCoins(-ship.price);
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.ShipUnlockedKey(pendingShipIndex), 1);
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedShipID, pendingShipIndex);
            PlayerPrefs.Save();

            ShowShopNotification("Mua thành công: " + ship.shipName + "!");
            UpdateShipShopUI();
            LoadCurrentShip();

            // Sync shop lên Firebase
            SyncPhiThuyenShop();
        }
        else if (isUnlocked)
        {
            ShowShopNotification("Đã sở hữu");
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedShipID, pendingShipIndex);
            PlayerPrefs.Save();
            LoadCurrentShip();

            // Sync selected ship lên Firebase
            SyncPhiThuyenShop();
        }
        else
        {
            int thieu = ship.price - totalCoins;
            ShowShopNotification("Bạn còn thiếu " + thieu + "$ để mua!");
        }
    }

    private void SyncPhiThuyenShop()
    {
        int selected = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedShipID, 0);
        var unlocked = new System.Collections.Generic.List<int> { 0 };
        if (allShips != null)
        {
            for (int i = 1; i < allShips.Length; i++)
            {
                if (IsShipUnlocked(i)) unlocked.Add(i);
            }
        }
        DoAnGame.Auth.CloudSyncService.Instance?.OnShopPurchased("phithuyen_ship", selected, unlocked.ToArray());
    }

    public void LoadCurrentShip()
    {
        int id = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SelectedShipID, 0);

        if (id < allShips.Length)
        {
            Sprite currentSprite = allShips[id].shipSprite;

            // Cập nhật cho cả phi thuyền ở Shop và trong trận
            if (shopShipRenderer != null) shopShipRenderer.sprite = currentSprite;
            if (gameplayShipRenderer != null) gameplayShipRenderer.sprite = currentSprite;
        }
        if (spaceshipAnimator != null) spaceshipAnimator.Play("Ship_" + id);
       
        if (gameplayAnimator != null) gameplayAnimator.Play("Ship_" + id);



    }

    private bool IsShipUnlocked(int index) => index == 0 || PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.ShipUnlockedKey(index), 0) == 1;

    public void UpdateShipShopUI()
    {
        for (int i = 0; i < allShips.Length; i++)
        {
            bool unlocked = IsShipUnlocked(i);
            if (i < shipButtonImages.Length)
                shipButtonImages[i].color = unlocked ? Color.white : new Color(0.3f, 0.3f, 0.3f, 1f);
            if (i < shipPriceTexts.Length)
                shipPriceTexts[i].text = unlocked ? "Đã sở hữu" : allShips[i].price + "$";
        }
    }
    #endregion
    #region QUẢN LÝ TIỀN
    private void LoadCoins()
    {
        // Load tiền từ máy khi vừa mở game
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
    #region QUẢN LÝ PANEL
    public void ShowHome()
    {
        HideAllPanels();
        panelGameplay.SetActive(false);
        panelHome.SetActive(true);

        // Cập nhật lại UI Shop mỗi khi quay về màn hình chính
        UpdateShipShopUI();
        LoadCurrentShip();

        Time.timeScale = 1f;
    }

    public void ShowChonMan()
    {
        HideAllPanels();
        panelGameplay.SetActive(false);
        panelChonMan.SetActive(true);
        GenerateLevelButtons();
    }

    public void ShowGameplay()
    {
        HideAllPanels();
        panelGameplay.SetActive(true);
        levelCoins = 0;
        UpdateCoinUI();
        Time.timeScale = 1f;
    }

    public void Click_Setting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void Click_CloseSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);
            if (panelGameplay.activeSelf) Time.timeScale = 1f;
        }
    }

    public void Click_BackToHome()
    {
        Time.timeScale = 1f;

        if (SpaceShipManager.Instance != null)
        {
            // Gọi trực tiếp hàm dọn dẹp
            SpaceShipManager.Instance.ClearExistingZones();

            // Sau đó mới tắt Object
            SpaceShipManager.Instance.gameObject.SetActive(false);
        }

        ShowHome();

        if (Enemy.Instance != null)
        {
            Enemy.Instance.ResetPosition();
        }
    }

    private void HideAllPanels()
    {
        panelHome.SetActive(false);
        panelChonMan.SetActive(false);
        
        if (panelWin) panelWin.SetActive(false);
        if (panelLose) panelLose.SetActive(false);
        if (panelSetting) panelSetting.SetActive(false);
    }


    public void Click_ThoatNgayLapTuc()
    {
        if (isGameOver) return;
        isGameOver = true; // Đánh dấu kết thúc game ngay lập tức

        if (panelSetting != null) panelSetting.SetActive(false);
        if (settingButton != null) settingButton.interactable = false;

        // Gọi hàm hiện panel luôn, không đợi video
        ShowLosePanelDirectly();
    }
    public void ShowLose()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Vô hiệu hóa nút cài đặt
        if (settingButton != null) settingButton.interactable = false;

        // Bắt đầu chuỗi hiệu ứng thua
        StartCoroutine(ShowLoseRoutine());
    }

    private IEnumerator ShowLoseRoutine()
    {
        Time.timeScale = 0f;
        // 1. Hiện nền đen và phát video Thua
        // Lưu ý: Không tắt panelGameplay để nó vẫn hiện phía sau
        if (videoBackground != null) videoBackground.SetActive(true);

        if (loseVideo != null)
        {
            loseVideo.gameObject.SetActive(true);
            loseVideo.Play();
            // Đợi video chạy xong
            yield return new WaitForSecondsRealtime(8.0f);
            loseVideo.gameObject.SetActive(false);
        }

        // 2. Tắt nền video đen ĐỂ LỘ GAMEPLAY PHÍA SAU
        if (videoBackground != null) videoBackground.SetActive(false);

        // 3. Cập nhật dữ liệu UI trước khi hiện Panel
        if (loseScoreTxt != null) loseScoreTxt.text = "Điểm: +" + levelScore;
        if (loseRewardTxt != null) loseRewardTxt.text = "+" + levelCoins;
        if (loseProgressTxt != null && SpaceShipManager.Instance != null)
        {
            int passed = SpaceShipManager.Instance.CorrectAnswersCount;
            int total = SpaceShipManager.Instance.TotalGatesToWin;
            loseProgressTxt.text = "Hoàn thành: " + passed + "/" + total + " Cổng";
        }

        // 4. HIỆN PANEL THUA đè lên Gameplay
        // Quan trọng: Chỉ ẩn các panel không liên quan, không ẩn panelGameplay
        panelHome.SetActive(false);
        panelChonMan.SetActive(false);
        if (panelWin) panelWin.SetActive(false);

        if (panelLose != null)
        {
            panelLose.SetActive(true);
            Time.timeScale = 0f; // Dừng chuyển động của gameplay phía sau
        }
    }
    private void ShowLosePanelDirectly()
    {
        Time.timeScale = 0f;

        // 1. Cập nhật dữ liệu UI trước khi hiện Panel
        if (loseScoreTxt != null) loseScoreTxt.text = "Điểm: +" + levelScore;
        if (loseRewardTxt != null) loseRewardTxt.text = "+" + levelCoins;
        if (loseProgressTxt != null && SpaceShipManager.Instance != null)
        {
            int passed = SpaceShipManager.Instance.CorrectAnswersCount;
            int total = SpaceShipManager.Instance.TotalGatesToWin;
            loseProgressTxt.text = "Hoàn thành: " + passed + "/" + total + " Cổng";
        }

        // 2. Ẩn các panel không liên quan
        panelHome.SetActive(false);
        panelChonMan.SetActive(false);
        if (panelWin) panelWin.SetActive(false);

        // 3. HIỆN PANEL THUA đè lên Gameplay
        if (panelLose != null)
        {
            panelLose.SetActive(true);
        }

        // Đảm bảo video và nền đen bị tắt
        if (videoBackground != null) videoBackground.SetActive(false);
        if (loseVideo != null) loseVideo.gameObject.SetActive(false);
    }

    // --- PHẦN SỬA LẠI CHO SHOW WIN ---
    public void ShowWin()
    {
        // Chuyển isGameOver lên trước để chặn va chạm liên tục
        if (isGameOver) return;
        isGameOver = true;

        StartCoroutine(ShowWinRoutine(1f));
    }

    private IEnumerator ShowWinRoutine(float delayBeforeVideo)
    {
        
        SpaceShipPhysics ship = FindObjectOfType<SpaceShipPhysics>();
        if (ship != null)
        {
            ship.BoostForwardOnWin();
        }
        if (settingButton != null) settingButton.interactable = false;

        yield return new WaitForSeconds(delayBeforeVideo);
        Time.timeScale = 0f;
        // 1. Hiện nền đen và phát video Thắng
        if (videoBackground != null) videoBackground.SetActive(true);
        if (winVideo != null)
        {
            winVideo.gameObject.SetActive(true);
            winVideo.Play();
            yield return new WaitForSecondsRealtime(6.3f);
            winVideo.gameObject.SetActive(false);
        }

        // 2. Cập nhật dữ liệu UI
        if (winScoreTxt != null) winScoreTxt.text = "Điểm: +" + levelScore;
        if (winRewardTxt != null) winRewardTxt.text = "+" + levelCoins;
        if (winLevelInfoTxt != null) winLevelInfoTxt.text = "Hoàn thành Màn " + LevelManager.CurrentLevel;

        // 3. Hiện Panel Thắng và dừng game
        HideAllPanels();
        if (panelWin != null)
        {
            panelWin.SetActive(true);
            Time.timeScale = 0f;
        }
        if (videoBackground != null) videoBackground.SetActive(false);

        // 4. Lưu tiến trình (Giữ nguyên logic của bạn)
        int currentHighest = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SpaceHighest, 1);
        int wonLevel = LevelManager.CurrentLevel;
        if (wonLevel == currentHighest && wonLevel < 100)
        {
            PlayerPrefs.SetInt(DoAnGame.Auth.LocalStorageKeyResolver.SpaceHighest, wonLevel + 1);
            PlayerPrefs.Save();
        }

        // 5. Sync Firebase
        DoAnGame.Auth.CloudSyncService.Instance?.OnLevelCompleted(
            gameMode: "phithuyen",
            grade: UIManager.SelectedGrade,
            levelNumber: wonLevel,
            score: 100,
            coinsEarned: levelCoins
        );
    }

    public void Click_Retry()
    {
        BatDauChoiSpace(LevelManager.CurrentLevel);
    }

    public void Click_NextLevel()
    {
        if (LevelManager.CurrentLevel < 100)
        {
            BatDauChoiSpace(LevelManager.CurrentLevel + 1);
        }
        else
        {
            ShowChonMan();
            ShowShopNotification("Bạn đã hoàn thành tất cả các màn!");
        }
    }

    public void Click_BackToChonMan()
    {
        ShowChonMan();
    }
    public void Click_ResetGameProgress()
    {
        // 1. Xóa toàn bộ dữ liệu đã lưu trong PlayerPrefs (Tiền, Level, Skins)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. Cập nhật lại các biến logic về mặc định
        LevelManager.CurrentLevel = 1;
        totalCoins = 0;
        levelCoins = 0;
        pendingShipIndex = -1;

        // 3. Làm mới giao diện ngay lập tức[cite: 12]
        LoadCoins();           // Cập nhật Text tiền về 0
        LoadCurrentShip();     // Trả phi thuyền về Skin mặc định (ID 0)
        UpdateShipShopUI();    // Khóa lại các nút Skin trong Shop
        GenerateLevelButtons(); // Khóa lại các màn chơi (trừ màn 1)[cite: 12]

        // 4. Hiển thị thông báo[cite: 12]
        ShowShopNotification("Đã xóa sạch tiến trình và Skin!");

        Debug.Log("Dữ liệu game và Skin đã được reset hoàn toàn.");
    }
    
    #endregion

    #region LOGIC MÀN CHƠI
    private void GenerateLevelButtons()
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        int highestLevel = PlayerPrefs.GetInt(DoAnGame.Auth.LocalStorageKeyResolver.SpaceHighest, 1);
        float startOffset = 200f;

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            float x = startOffset + (i - 1) * buttonSpacing;
            float y = Mathf.Sin((i - 1) * waveFrequency) * waveAmplitude;
            btnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);

            Button btn = btnObj.GetComponent<Button>();
            btnObj.GetComponentInChildren<TextMeshProUGUI>().text = i.ToString();

            int levelIndex = i;
            if (i <= highestLevel || i == 30 || i == 50 || i == 70 || i == 100)
            {
                btn.interactable = true;
                btn.onClick.AddListener(() => BatDauChoiSpace(levelIndex));
            }
            else
            {
                btn.interactable = false;
                btn.image.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            }
        }
        contentParent.sizeDelta = new Vector2((100 * buttonSpacing) + startOffset * 2, contentParent.sizeDelta.y);
    }

    public void BatDauChoiSpace(int levelIndex)
    {

        if (settingButton != null) settingButton.interactable = true;
        levelScore = 0; // Reset điểm về 0 khi bắt đầu
        if (gameplayScoreRewardTxt != null) gameplayScoreRewardTxt.text = "Điểm: +0";

        UpdateShopProfileUI(); // Cập nhật lại UI shop
        // Đảm bảo Reset kẻ địch khi chơi lại[cite: 8]
        if (Enemy.Instance != null) Enemy.Instance.ResetPosition();

        LevelManager.CurrentLevel = levelIndex;
        isGameOver = false;

        ShowGameplay();
        LoadCurrentShip();
        if (SpaceShipManager.Instance != null)
        {
            SpaceShipManager.Instance.gameObject.SetActive(false);
            SpaceShipManager.Instance.gameObject.SetActive(true);
        }

        SpaceShipPhysics ship = FindObjectOfType<SpaceShipPhysics>();
        if (ship != null)
        {
            ship.ResetPosition();
            ship.ResetMovement();
        }

        ShowShopNotification("Bắt đầu Màn " + levelIndex);
    }
    #endregion

    #region THÔNG BÁO
    public void ShowShopNotification(string message)
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