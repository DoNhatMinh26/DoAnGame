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
        // Chỉ kiểm tra khi đang trong màn chơi và chưa kết thúc game
        
        GenerateLevelButtons();

        // Đảm bảo các panel kết thúc ẩn lúc đầu
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
        ClearAllGameplayObjects();
        DragAndDrop.ReleaseAllLocks();// Dọn dẹp quái khi thoát

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
            DragAndDrop.SetGlobalLock(true); // KHÓA kéo thả khi thắng
        }
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
        // 1. Phải chạy lại thời gian (TimeScale = 1) để các Coroutine hoạt động lại
        Time.timeScale = 1f;

        // 2. Giải phóng biến 'isLocked' dùng chung cho tất cả các nút
        DragAndDrop.ReleaseAllLocks();

        
        

        // 4. Tìm tất cả các nút đáp án trên màn hình và đưa chúng về vị trí cũ
        DragAndDrop[] allAnswers = FindObjectsOfType<DragAndDrop>();
        foreach (DragAndDrop btn in allAnswers)
        {
            btn.ForceResetPosition(); // Hàm này sẽ reset màu sắc và vị trí
        }

        // 5. Bắt đầu lại màn chơi hiện tại (sinh quái mới, câu hỏi mới)
        BatDauChoiMan(LevelManager.CurrentLevel);

        // 6. Ẩn bảng Thua/Thắng
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
            DragAndDrop.SetGlobalLock(true); // KHÓA kéo thả khi thắng
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
            DragAndDrop.SetGlobalLock(false); // KHÓA kéo thả khi thắng
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
        DragAndDrop.ReleaseAllLocks();
        isGameOver = false;
        ClearAllGameplayObjects(); // Xóa địch cũ khi sang màn mới
        LevelManager.CurrentLevel = levelIndex;
        ShowGameplay();

        EnemySpawner spawner = FindObjectOfType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.enabled = true;
            spawner.ResetSpawner();
        }
        // 3. Reset máu cho tường thành
        if (WallHealth.Instance != null)
        {
            WallHealth.Instance.ResetHealth();
        }

        DragQuizManager qm = FindObjectOfType<DragQuizManager>();
        if (qm != null) qm.UpdateDifficulty();
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
                ShowWin(); // Gọi hàm hiện bảng thắng đã viết ở turn trước
            }
        }
    }
    
    #endregion
}