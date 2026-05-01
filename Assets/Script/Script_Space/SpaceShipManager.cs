using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpaceShipManager : MonoBehaviour
{
    public static SpaceShipManager Instance;

    [Header("Data Source")]
    public LevelGenerate levelData;

    [Header("Cấu hình Độ khó (ScriptableObject)")]
    public SpaceDifficultyConfig difficultyConfig;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI manHienTaiText;
    [SerializeField] private TextMeshProUGUI cauHoiDungYenText;
    [SerializeField] private TextMeshProUGUI dkWinText;

    [Header("Hiệu ứng chữ")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Cài đặt di chuyển thế giới")]
    public GameObject canvasPrefab;
    [HideInInspector] public float worldSpeed = 8f;
    [HideInInspector] public float distanceBetween = 45f;
    public string gateTag = "Gate";
    public int activeCount = 3;

    [Header("Cấu hình Lưới Tiền (3x3)")]
    public int coinRows = 3;
    public int coinCols = 3;
    public float spacingX = 1.8f;
    public float spacingY = 1.8f;

    [Tooltip("Khoảng cách lùi lại phía trước cổng")]
    public float fixedDistanceBeforeGate = 12f;

    [Tooltip("Vị trí chiều cao trung tâm của lưới tiền")]
    public float centerYOffset = 0f;
    public GameObject coinPrefab; // Kéo Prefab đồng xu vào đây
    

    private int correctAnswersCount = 0;
    private int totalGatesToWin = 10;

    public int CorrectAnswersCount => correctAnswersCount;
    public int TotalGatesToWin => totalGatesToWin;

    private float nextSpawnX = 30f;
    private Coroutine typingCoroutine;

    [HideInInspector] public string currentCorrectAnswer;
    private List<GameObject> activeZones = new List<GameObject>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnEnable()
    {
        Time.timeScale = 1f;

        if (manHienTaiText != null)
        {
            manHienTaiText.text = "Màn " + LevelManager.CurrentLevel;
        }

        if (difficultyConfig != null)
        {
            var diff = difficultyConfig.GetDifficulty(UIManager.SelectedGrade, LevelManager.CurrentLevel);
            totalGatesToWin = diff.gateCount;
            worldSpeed = diff.speed;
            distanceBetween = diff.distance;
            correctAnswersCount = 0;
            UpdateWinConditionUI();
        }

        ClearExistingZones();
        nextSpawnX = 30f;

        for (int i = 0; i < activeCount; i++)
        {
            SpawnNewZone();
        }

        UpdateFirstQuestion();
    }

    void Update()
    {
        // Duyệt ngược danh sách để xóa an toàn khi di chuyển
        for (int i = activeZones.Count - 1; i >= 0; i--)
        {
            GameObject zone = activeZones[i];
            if (zone == null)
            {
                activeZones.RemoveAt(i);
                continue;
            }

            zone.transform.Translate(Vector3.left * worldSpeed * Time.deltaTime);

            if (zone.transform.position.x < -25f)
            {
                activeZones.RemoveAt(i);
                Destroy(zone);
            }
        }

        if (activeZones.Count < activeCount)
        {
            SpawnNewZone();
        }
    }

    private void SpawnNewZone()
    {
        float spawnPos = nextSpawnX;

        if (activeZones.Count > 0)
        {
            float furthestX = -100f;
            foreach (var zone in activeZones)
            {
                if (zone == null) continue;
                // Tìm Portal/Gate con để lấy tọa độ X chuẩn nhất
                Transform gate = zone.transform.Find("Gate") ?? zone.transform;
                if (gate.position.x > furthestX) furthestX = gate.position.x;
            }
            spawnPos = furthestX + distanceBetween;
        }

        // 1. Sinh cổng câu hỏi
        GameObject zoneObj = Instantiate(canvasPrefab, new Vector3(spawnPos, 0, 0), Quaternion.identity);
        activeZones.Add(zoneObj);
        SetupZone(zoneObj.GetComponent<QuestionZone>());

        // 2. Sinh tiền ngẫu nhiên phía trước cổng vừa tạo
        TrySpawnCoin(spawnPos, zoneObj.transform);
    }

    private void TrySpawnCoin(float zoneX, Transform parentZone)
    {
        if (coinPrefab == null) return;

        // 1. Tính toán điểm bắt đầu để lưới tiền nằm chính giữa theo trục Y dựa trên Inspector
        // Sử dụng coinRows, spacingY và centerYOffset từ bảng cấu hình
        float totalHeight = (coinRows - 1) * spacingY;
        float topY = centerYOffset + (totalHeight / 2f);

        // 2. Vị trí X bắt đầu lùi lại phía trước cổng dựa trên Fixed Distance Before Gate
        float startX = zoneX - fixedDistanceBeforeGate;

        // 3. Lặp qua từng cột (coinCols)
        for (int c = 0; c < coinCols; c++)
        {
            // Chọn ngẫu nhiên 1 hàng duy nhất trong số các hàng (coinRows)[cite: 8]
            int randomRow = Random.Range(0, coinRows);

            // Tính toán vị trí dựa trên Spacing X và Spacing Y từ Inspector[cite: 8]
            float posX = startX + (c * spacingX);
            float posY = topY - (randomRow * spacingY);

            Vector3 spawnPos = new Vector3(posX, posY, 0);

            // 4. Sinh đồng xu duy nhất cho cột này
            GameObject coinObj = Instantiate(coinPrefab, spawnPos, Quaternion.identity);

            // Gán vào parent để trôi cùng cổng câu hỏi với tốc độ worldSpeed[cite: 8]
            coinObj.transform.SetParent(parentZone);
        }
    }
    private void UpdateFirstQuestion()
    {
        QuestionZone firstZone = GetNextQuestionZone();
        if (firstZone != null)
        {
            currentCorrectAnswer = firstZone.dapAnDung;
            UpdateStaticQuestionUI(firstZone.cauHoiLuuTru);
        }
    }

    public QuestionZone GetNextQuestionZone()
    {
        GameObject closest = null;
        float minX = float.MaxValue;

        foreach (var zone in activeZones)
        {
            if (zone == null) continue;
            float posX = zone.transform.position.x;

            if (posX > -8f && posX < minX)
            {
                minX = posX;
                closest = zone;
            }
        }
        return closest != null ? closest.GetComponent<QuestionZone>() : null;
    }

    public void ClearExistingZones()
    {
        StopAllCoroutines();
        typingCoroutine = null;

        if (activeZones != null)
        {
            foreach (var zone in activeZones)
            {
                if (zone != null) DestroyImmediate(zone);
            }
            activeZones.Clear();
        }

        GameObject[] leftovers = GameObject.FindGameObjectsWithTag(gateTag);
        foreach (GameObject g in leftovers)
        {
            if (g == null) continue;
            DestroyImmediate(g.transform.parent != null ? g.transform.parent.gameObject : g);
        }

        nextSpawnX = 10f;
        if (cauHoiDungYenText != null) cauHoiDungYenText.text = "";
    }

    private void SetupZone(QuestionZone zone)
    {
        if (zone == null) return;
        zone.gameObject.SetActive(true);

        string cauHoi, dapAn;
        GenerateFullMathQuestion(out cauHoi, out dapAn);
        List<string> choices = GenerateDynamicChoices(dapAn);
        zone.Setup(cauHoi, choices, dapAn);
    }

    public void CountCorrectAnswer()
    {
        correctAnswersCount++;
        UpdateWinConditionUI();
        if (correctAnswersCount >= totalGatesToWin)
        {
            if (UiSp.Instance != null) UiSp.Instance.ShowWin();
        }
    }

    #region UI & EFFECTS
    private void UpdateWinConditionUI()
    {
        if (dkWinText != null)
        {
            dkWinText.text = $"{correctAnswersCount}/{totalGatesToWin}";
        }
    }

    public void UpdateStaticQuestionUI(string cauHoi = "")
    {
        if (cauHoiDungYenText == null) return;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        cauHoiDungYenText.text = "";

        if (!string.IsNullOrEmpty(cauHoi))
            typingCoroutine = StartCoroutine(TypeEffect(cauHoi));
    }

    private IEnumerator TypeEffect(string text)
    {
        cauHoiDungYenText.text = "";
        foreach (char c in text.ToCharArray())
        {
            if (cauHoiDungYenText == null) yield break;
            cauHoiDungYenText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
    }

    public void SetCauHoiDungYenResult(string title, string result)
    {
        if (cauHoiDungYenText != null)
            cauHoiDungYenText.text = $"{title} {result}";
    }
    #endregion

    #region MATH LOGIC (GIỮ NGUYÊN)
    private void GenerateFullMathQuestion(out string cauHoiStr, out string dapAnStr)
    {
        var config = levelData.GetConfigForLevel(UIManager.SelectedGrade, LevelManager.CurrentLevel);
        int minVal = config.MinNumber;
        int maxVal = config.MaxNumber;

        List<string> activeOps = new List<string>();
        foreach (byte op in config.AllowedOperators)
        {
            if (op == 0) activeOps.Add("+");
            if (op == 1) activeOps.Add("-");
            if (op == 2) activeOps.Add("x");
            if (op == 3) activeOps.Add(":");
            if (op == 4) activeOps.Add("find_+-");
            if (op == 5) activeOps.Add("find x");
            if (op == 6) activeOps.Add("find :");
            if (op == 7) activeOps.Add("hai phép tính +-");
        }

        if (activeOps.Count == 0) activeOps.Add("+");
        string phepToan = activeOps[Random.Range(0, activeOps.Count)];

        cauHoiStr = ""; dapAnStr = "";
        int n1, n2, n3;

        switch (phepToan)
        {
            case "+":
                n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1);
                cauHoiStr = $"{n1} + {n2} = ?"; dapAnStr = (n1 + n2).ToString();
                break;
            case "-":
                n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                cauHoiStr = $"{n1} - {n2} = ?"; dapAnStr = (n1 - n2).ToString();
                break;
            case "x":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                cauHoiStr = $"{n1} x {n2} = ?"; dapAnStr = (n1 * n2).ToString();
                break;
            case ":":
                int sc = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int kq = Random.Range(minVal, maxVal + 1);
                if (sc == 0) sc = 1;
                cauHoiStr = $"{sc * kq} : {sc} = ?"; dapAnStr = kq.ToString();
                break;
            case "find_+-":
                int x = Random.Range(minVal, maxVal + 1); int b = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                {
                    int tong = x + b; cauHoiStr = (Random.value > 0.5f) ? $"? + {b} = {tong}" : $"{b} + ? = {tong}";
                    dapAnStr = x.ToString();
                }
                else
                {
                    if (x < b) x = b + Random.Range(1, 10);
                    int hieu = x - b; cauHoiStr = (Random.value > 0.5f) ? $"? - {b} = {hieu}" : $"{x} - ? = {hieu}";
                    dapAnStr = (cauHoiStr.Contains("? -")) ? x.ToString() : b.ToString();
                }
                break;
            case "find x":
                int vX = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int vB = Random.Range(minVal, maxVal + 1);
                int tich = vX * vB; cauHoiStr = (Random.value > 0.5f) ? $"? x {vB} = {tich}" : $"{vB} x ? = {tich}";
                dapAnStr = vX.ToString();
                break;
            case "find :":
                int vXc = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int vBc = Random.Range(minVal, maxVal + 1); if (vBc == 0) vBc = 1;
                int sbcVal = vXc * vBc; cauHoiStr = (Random.value > 0.5f) ? $"? : {vBc} = {vXc}" : $"{sbcVal} : ? = {vXc}";
                dapAnStr = (cauHoiStr.Contains("? :")) ? sbcVal.ToString() : vBc.ToString();
                break;
            case "hai phép tính +-":
                n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1); n3 = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                {
                    int res = n1 + n2 - n3; if (res < 0) res = n1 + n2 + n3;
                    cauHoiStr = (res == n1 + n2 - n3) ? $"{n1} + {n2} - {n3} = ?" : $"{n1} + {n2} + {n3} = ?";
                    dapAnStr = res.ToString();
                }
                else
                {
                    if (n1 < n2) n1 = n2 + Random.Range(1, 10);
                    cauHoiStr = $"{n1} - {n2} + {n3} = ?"; dapAnStr = (n1 - n2 + n3).ToString();
                }
                break;
        }
    }

    private List<string> GenerateDynamicChoices(string dapAn)
    {
        List<string> choices = new List<string> { dapAn };
        int gradeFactor = UIManager.SelectedGrade;
        int maxOffset = gradeFactor * 5;
        int trueVal = int.Parse(dapAn);

        int safety = 0;
        while (choices.Count < 3 && safety < 50)
        {
            safety++;
            int offset = Random.Range(-maxOffset, maxOffset + 1);
            if (offset == 0) offset = 1;
            string wrong = Mathf.Abs(trueVal + offset).ToString();
            if (!choices.Contains(wrong) && wrong != "0") choices.Add(wrong);
        }
        return choices;
    }
    #endregion
}