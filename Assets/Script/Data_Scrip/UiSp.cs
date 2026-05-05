using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DoAnGame.Auth;

public class UiSp : MonoBehaviour
{
    public static UiSp Instance;

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

    private int pendingShipIndex = -1;
    private void Awake() => Instance = this;

    private void Start()
    {
        LoadCoins();
        InitShipShop();
        GenerateLevelButtons();
        ShowHome();
    }
    #region QUẢN LÝ SHOP
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

        // 2. Kiểm tra trạng thái sở hữu
        if (IsShipUnlocked(index))
        {
            PlayerPrefs.SetInt("SelectedShipID", index);
            PlayerPrefs.Save();

            // Nếu đã sở hữu, cập nhật luôn hình ảnh cho cả phi thuyền trong trận
            if (gameplayShipRenderer != null)
                gameplayShipRenderer.sprite = allShips[index].shipSprite;

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
            PlayerPrefs.SetInt("ShipUnlocked_" + pendingShipIndex, 1);
            PlayerPrefs.SetInt("SelectedShipID", pendingShipIndex);
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
            PlayerPrefs.SetInt("SelectedShipID", pendingShipIndex);
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
        int selected = PlayerPrefs.GetInt("SelectedShipID", 0);
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
        int id = PlayerPrefs.GetInt("SelectedShipID", 0);

        if (id < allShips.Length)
        {
            Sprite currentSprite = allShips[id].shipSprite;

            // Cập nhật cho cả phi thuyền ở Shop và trong trận
            if (shopShipRenderer != null) shopShipRenderer.sprite = currentSprite;
            if (gameplayShipRenderer != null) gameplayShipRenderer.sprite = currentSprite;
        }
    }

    private bool IsShipUnlocked(int index) => index == 0 || PlayerPrefs.GetInt("ShipUnlocked_" + index, 0) == 1;

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

    public void ShowLose()
    {
        HideAllPanels();
        if (panelLose != null)
        {
            panelLose.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    // Tối ưu: Chuyển ShowWin thành Coroutine để đợi vài giây trước khi hiện bảng[cite: 8]
    public void ShowWin()
    {
        StartCoroutine(ShowWinRoutine(2.0f));
    }

    private IEnumerator ShowWinRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        HideAllPanels();
        if (panelWin != null)
        {
            panelWin.SetActive(true);
            Time.timeScale = 0f;
        }

        // Lưu tiến trình local
        int currentHighest = PlayerPrefs.GetInt("Space_HighestLevel", 1);
        int wonLevel = LevelManager.CurrentLevel;
        if (wonLevel == currentHighest && wonLevel < 100)
        {
            PlayerPrefs.SetInt("Space_HighestLevel", wonLevel + 1);
            PlayerPrefs.Save();
        }

        // Sync lên Firebase (nếu đã đăng nhập)
        DoAnGame.Auth.CloudSyncService.Instance?.OnLevelCompleted(
            gameMode:    "phithuyen",
            grade:       UIManager.SelectedGrade,
            levelNumber: wonLevel,
            score:       100,  // điểm cố định mỗi màn thắng PhiThuyen
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

        int highestLevel = PlayerPrefs.GetInt("Space_HighestLevel", 1);
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
        if (ship != null) ship.ResetMovement();

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