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
    private bool isGameOver = false;

    [Header("Quản lý Tiền & Phần thưởng")]
    public TextMeshProUGUI totalCoinTxt; // Text hiển thị ở Menu
    public TextMeshProUGUI levelCoinTxt; // Text hiển thị trong Gameplay
    public Transform coinTarget;        // Vị trí icon tiền để tiền bay về
    private int totalCoins = 0;
    private int levelCoins = 0;

    [Header("UI Thông báo")]
    public CanvasGroup notificationCanvasGroup;
    public TextMeshProUGUI notificationTxt;
    [Header("Cấu hình Độ khó")]
    [SerializeField] private ClassDifficultyConfig classDifficulty;

    [Header("UI Hiển Thị Tiến Độ")]
    [SerializeField] private TextMeshProUGUI progressTxt;
    private int targetCorrectAnswers; // Mục tiêu từ file ClassDifficultyConfig[cite: 17]
    private int currentCorrectCount;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
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
    public void Click_ResetToanBoTienTrinh()
    {
        // 1. Xóa Key lưu màn chơi cao nhất của Lớp học
        PlayerPrefs.DeleteKey("Class_HighestLevel");

        // 2. Xóa Key tiền (nếu bạn muốn reset cả tiền)
        PlayerPrefs.DeleteKey("TotalCoins");

        // 3. Lưu lại thay đổi[cite: 9]
        PlayerPrefs.Save();

        // 4. Cập nhật lại các biến trong code để đồng bộ ngay lập tức[cite: 9, 14]
        LoadCoins(); // Nạp lại tiền (sẽ về 0)[cite: 9]
        GenerateLevelButtons(); // Sinh lại danh sách nút (tất cả sẽ bị khóa trừ màn 1)

        // 5. Thông báo cho người chơi[cite: 14]
        ShowClassNotification("Đã xóa toàn bộ tiến trình chơi!");

        // Nếu đang ở trong Shop, có thể đóng panel hoặc quay về Home
        ShowHome(); 
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
        // Kiểm tra điều kiện thắng[cite: 15]
        if (currentCorrectCount >= targetCorrectAnswers)
        {
            WinGame();
        }
    }

    public void WinGame()
    {
        isGameOver = true;

        // Lưu tiến trình mở khóa màn mới[cite: 15]
        int highestLevel = PlayerPrefs.GetInt("Class_HighestLevel", 1);
        if (LevelManager.CurrentLevel >= highestLevel)
        {
            PlayerPrefs.SetInt("Class_HighestLevel", LevelManager.CurrentLevel + 1);
            PlayerPrefs.Save();
        }

        // Hiển thị Panel thắng[cite: 15]
        if (panelWin != null) panelWin.SetActive(true);
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