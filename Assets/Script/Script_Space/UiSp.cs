using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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

    private void Awake() => Instance = this;

    private void Start()
    {
        LoadCoins();
        GenerateLevelButtons();
        ShowHome();
    }

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
        levelCoins += amount;
        totalCoins += amount;

        // Lưu lại ngay lập tức
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
    #region QUẢN LÝ PANEL
    public void ShowHome()
    {
        HideAllPanels();
        panelGameplay.SetActive(false);
        panelHome.SetActive(true);
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
        yield return new WaitForSeconds(delay); // Đợi 2 giây[cite: 8]

        HideAllPanels();
        if (panelWin != null)
        {
            panelWin.SetActive(true);
            Time.timeScale = 0f;
        }

        // Lưu tiến trình
        int currentHighest = PlayerPrefs.GetInt("Space_HighestLevel", 1);
        int wonLevel = LevelManager.CurrentLevel;
        if (wonLevel == currentHighest && wonLevel < 100)
        {
            PlayerPrefs.SetInt("Space_HighestLevel", wonLevel + 1);
            PlayerPrefs.Save();
        }
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
        // 1. Xóa toàn bộ dữ liệu đã lưu trong PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        LoadCoins();
        // 2. Cập nhật lại LevelManager về màn 1
        LevelManager.CurrentLevel = 1;

        // 3. Làm mới lại danh sách nút chọn màn (để cập nhật trạng thái khóa/mở)
        GenerateLevelButtons();

        // 4. Hiển thị thông báo cho người chơi
        ShowShopNotification("Đã reset toàn bộ tiến trình game!");

        Debug.Log("Dữ liệu game đã được xóa sạch.");
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