using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelManager : MonoBehaviour
{
    [Header("Cấu hình sinh nút tự động")]
    public GameObject levelButtonPrefab;
    public RectTransform contentParent;

    [Header("Cài đặt đường lượn sóng (Sin Wave)")]
    [SerializeField] private float buttonSpacing = 160f;    // Khoảng cách ngang giữa các nút
    [SerializeField] private float waveAmplitude = 150f;    // Độ cao của đỉnh sóng
    [SerializeField] private float waveFrequency = 0.5f;    // Độ dày của sóng (Số càng lớn sóng càng dày)
    [SerializeField] private float heightOffset = 0f;       // Độ cao trung tâm của dải sóng

    [Header("UI Màn Chơi")]
    public GameObject panelChonMan;
    public GameObject panelGameplay;

    public static int CurrentLevel = 1;

    void Start()
    {
        if (panelChonMan != null) panelChonMan.SetActive(true);
        if (panelGameplay != null) panelGameplay.SetActive(false);

        GenerateLevelButtonsWave();
    }

    private void GenerateLevelButtonsWave()
    {
        if (levelButtonPrefab == null || contentParent == null) return;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 1; i <= 100; i++)
        {
            GameObject btnObj = Instantiate(levelButtonPrefab, contentParent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();

            // --- LOGIC TOÁN HỌC TẠO HÌNH LƯỢN SÓNG (SIN) ---

            // 1. Vị trí X tịnh tiến dần từ trái sang phải
            float posX = (i - 1) * buttonSpacing;

            // 2. Vị trí Y tính theo hàm Sin
            // i * waveFrequency giúp tạo sự thay đổi góc dựa theo số thứ tự nút
            float posY = Mathf.Sin(i * waveFrequency) * waveAmplitude;

            // 3. Thiết lập vị trí
            btnRect.anchoredPosition = new Vector2(posX + (buttonSpacing / 2f), posY + heightOffset);

            // ----------------------------------------------

            TextMeshProUGUI txt = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.text = i.ToString();

            int levelIndex = i;
            btnObj.GetComponent<Button>().onClick.AddListener(() => BatDauChoiMan(levelIndex));
        }

        float totalWidth = 100 * buttonSpacing;
        contentParent.sizeDelta = new Vector2(totalWidth, contentParent.sizeDelta.y);
    }

    public void BatDauChoiMan(int levelIndex)
    {
        CurrentLevel = levelIndex;
        panelChonMan.SetActive(false);
        panelGameplay.SetActive(true);

        // Tìm MathManager và yêu cầu cập nhật độ khó dựa trên dữ liệu mới
        MathManager math = FindObjectOfType<MathManager>();
        if (math != null)
        {
            // MathManager sẽ tự động dùng UIManager.SelectedGrade để tra cứu LevelGenerate
            math.UpdateDifficulty();
        }

        DragQuizManager drag = FindObjectOfType<DragQuizManager>();
        if (drag != null) drag.UpdateDifficulty();
    }

    public void QuayLaiChonMan()
    {
        panelChonMan.SetActive(true);
        panelGameplay.SetActive(false);
    }

    public void QuayVeMenuChonCachChoi()
    {
        
    }
}