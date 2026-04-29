using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    [Header("UI Thông báo")]
    public CanvasGroup notificationCanvasGroup;
    public TextMeshProUGUI notificationTxt;

    private void Awake() => Instance = this;

    private void Start()
    {
        GenerateLevelButtons();
        ShowHome();
    }

    #region QUẢN LÝ PANEL
    public void ShowHome()
    {
        HideAllPanels();
        panelHome.SetActive(true);
        Time.timeScale = 1f;
    }

    public void ShowChonMan()
    {
        HideAllPanels();
        panelChonMan.SetActive(true);
        GenerateLevelButtons(); // Cập nhật trạng thái khóa/mở
    }

    public void ShowGameplay()
    {
        HideAllPanels();
        panelGameplay.SetActive(true);
        Time.timeScale = 1f;
    }

    public void Click_Setting()
    {
        // Hiển thị Panel Setting mà không ẩn các panel khác (để hiện đè lên Gameplay hoặc Home)
        if (panelSetting != null)
        {
            panelSetting.SetActive(true);

            // Tạm dừng trò chơi khi mở bảng cài đặt
            Time.timeScale = 0f;
        }
    }

    public void Click_CloseSetting()
    {
        if (panelSetting != null)
        {
            panelSetting.SetActive(false);

            // Nếu đang ở trong Gameplay thì tiếp tục chạy game, nếu ở Home thì giữ nguyên
            if (panelGameplay.activeSelf)
            {
                Time.timeScale = 1f;
            }
        }
    }
    public void Click_BackToHome()
    {
        // 1. Dừng mọi hoạt động của game nếu đang chơi
        Time.timeScale = 1f; // Đảm bảo thời gian trở lại bình thường khi về Home

        // 2. Vô hiệu hóa SpaceShipManager để dừng logic sinh cổng
        if (SpaceShipManager.Instance != null)
        {
            SpaceShipManager.Instance.gameObject.SetActive(false);
        }

        // 3. Hiển thị lại màn hình Home
        ShowHome();

        // 4. (Tùy chọn) Reset vị trí kẻ địch nếu có
        if (Enemy.Instance != null)
        {
            Enemy.Instance.ResetPosition();
        }
    }
    private void HideAllPanels()
    {
        panelHome.SetActive(false);
        panelChonMan.SetActive(false);
        panelGameplay.SetActive(false);
        if (panelWin) panelWin.SetActive(false);
        if (panelLose) panelLose.SetActive(false);
        if (panelSetting) panelSetting.SetActive(false); // THÊM DÒNG NÀY
    }
    public void ShowLose()
    {
        // 1. Ẩn tất cả các panel khác
        HideAllPanels();

        // 2. Hiện bảng thua cuộc
        if (panelLose != null)
        {
            panelLose.SetActive(true);

            // 3. Dừng thời gian để game không chạy tiếp khi đã thua
            Time.timeScale = 0f;
        }

        Debug.Log("Game Over! Bạn đã bị kẻ địch bắt kịp.");
    }
    public void ShowWin()
    {
        HideAllPanels();
        if (panelWin != null)
        {
            panelWin.SetActive(true);
            Time.timeScale = 0f; // Dừng game để người chơi nhận thưởng
        }

        // LƯU TIẾN TRÌNH: Mở khóa màn tiếp theo cho chế độ Space
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
        // Chơi lại màn hiện tại
        BatDauChoiSpace(LevelManager.CurrentLevel);
    }
    public void Click_NextLevel()
    {
        // 1. Kiểm tra xem có màn tiếp theo không (giới hạn 100 màn)
        if (LevelManager.CurrentLevel < 100)
        {
            // 2. Tăng màn hiện tại lên 1
            int nextLevel = LevelManager.CurrentLevel + 1;

            // 3. Gọi hàm bắt đầu chơi với màn mới
            // Hàm này đã bao gồm Reset kẻ địch, cập nhật UI và SpaceShipManager
            BatDauChoiSpace(nextLevel);

            Debug.Log("Chuyển sang màn tiếp theo: " + nextLevel);
        }
        else
        {
            // Nếu đã ở màn cuối cùng thì quay về màn hình chọn màn
            ShowChonMan();
            ShowShopNotification("Bạn đã hoàn thành tất cả các màn!");
        }
    }

    public void Click_BackToChonMan()
    {
        ShowChonMan();
    }
    #endregion

    #region LOGIC MÀN CHƠI
    private void GenerateLevelButtons()
    {
        // Xóa các nút cũ
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        // Lấy màn cao nhất của Space Mode (Tạm thời dùng PlayerPrefs)
        int highestLevel = PlayerPrefs.GetInt("Space_HighestLevel", 10);

        float startOffset = 200f;

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);

            // Đặt vị trí hình lượn sóng giống UiTp
            float x = startOffset + (i - 1) * buttonSpacing;
            float y = Mathf.Sin((i - 1) * waveFrequency) * waveAmplitude;
            btnObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);

            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = i.ToString();

            int levelIndex = i;
            if (i <= highestLevel)
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
        if (Enemy.Instance != null)
        {
            Enemy.Instance.ResetPosition();
        }
        LevelManager.CurrentLevel = levelIndex;
        isGameOver = false;
        ShowGameplay();

        // Kích hoạt SpaceShipManager để bắt đầu game
        if (SpaceShipManager.Instance != null)
        {
            SpaceShipManager.Instance.gameObject.SetActive(false);
            SpaceShipManager.Instance.gameObject.SetActive(true);
        }

        ShowShopNotification("Bắt đầu Màn " + levelIndex);
    }
    #endregion

    #region THÔNG BÁO (FADE EFFECT)
    public void ShowShopNotification(string message)
    {
        if (notificationTxt != null && notificationCanvasGroup != null)
        {
            StopAllCoroutines();
            notificationTxt.text = message;
            StartCoroutine(FadeNotificationRoutine());
        }
    }

    private System.Collections.IEnumerator FadeNotificationRoutine()
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