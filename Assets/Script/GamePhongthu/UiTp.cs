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
        Time.timeScale = 1f;
        GenerateLevelButtons();

        // Đảm bảo các panel kết thúc ẩn lúc đầu
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
        if (panelSetting != null) panelSetting.SetActive(false);

        ShowHome();
    }

    #region CÁC HÀM ĐIỀU HƯỚNG
    public void Click_OpenChonMan()
    {
        DeactivateAll();
        if (panelChonMan != null) panelChonMan.SetActive(true);
        Time.timeScale = 1f;
    }

    public void Click_BackToHome()
    {
        Time.timeScale = 1f;
        ClearEnemies(); // Dọn dẹp quái khi thoát

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
            Time.timeScale = 0f; // Tạm dừng game khi thắng
        }
    }

    public void ShowLose()
    {
        if (panelLose != null)
        {
            panelLose.SetActive(true);
            Time.timeScale = 0f; // Tạm dừng game khi thua
        }
    }

    public void Click_Retry()
    {
        // 1. Chạy lại thời gian
        Time.timeScale = 1f;

        // 2. Reset lại lượng máu của tường thành về đầy cây
        if (WallHealth.Instance != null)
        {
            WallHealth.Instance.ResetHealth();
        }

        // 3. Gọi lại hàm bắt đầu chơi màn hiện tại
        BatDauChoiMan(LevelManager.CurrentLevel);

        // 4. Ẩn các Panel thông báo (nếu có)
        if (panelWin != null) panelWin.SetActive(false);
        if (panelLose != null) panelLose.SetActive(false);
    }

    public void Click_Next()
    {
        Time.timeScale = 1f;
        int nextLevel = LevelManager.CurrentLevel + 1;
        if (nextLevel > 100) nextLevel = 100; // Giới hạn 100 màn
        BatDauChoiMan(nextLevel);
    }

    public void Click_OpenSetting()
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
            // Chỉ chạy lại thời gian nếu không ở bảng kết thúc
            if (!panelWin.activeSelf && !panelLose.activeSelf)
                Time.timeScale = 1f;
        }
    }

    private void ClearEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject e in enemies) Destroy(e);
    }
    #endregion

    #region LOGIC SINH NÚT MÀN CHƠI
    private void GenerateLevelButtons()
    {
        if (levelButtonPrefab == null || contentParent == null) return;
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();

            float posX = (i - 1) * buttonSpacing;
            float posY = Mathf.Sin(i * waveFrequency) * waveAmplitude;
            btnRect.anchoredPosition = new Vector2(posX + (buttonSpacing / 2f), posY);

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = i.ToString();

            int levelIndex = i;
            btnObj.GetComponent<Button>().onClick.AddListener(() => BatDauChoiMan(levelIndex));
        }
        contentParent.sizeDelta = new Vector2(100 * buttonSpacing, contentParent.sizeDelta.y);
    }

    public void BatDauChoiMan(int levelIndex)
    {
        ClearEnemies(); // Xóa địch cũ khi sang màn mới
        LevelManager.CurrentLevel = levelIndex;
        ShowGameplay();

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.enabled = true;
            spawner.ResetSpawner();
        }

        DragQuizManager qm = FindObjectOfType<DragQuizManager>();
        if (qm != null) qm.UpdateDifficulty();
    }
    #endregion
}