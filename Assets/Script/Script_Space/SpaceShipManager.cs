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

    [Header("Hiệu ứng chữ")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Cài đặt di chuyển thế giới")]
    public GameObject canvasPrefab;
    [HideInInspector] public float worldSpeed = 8f;
    [HideInInspector] public float distanceBetween = 45f;
    public int activeCount = 3;

    [Header("Logic Chiến Thắng")]
    public int gatesPassed = 0;
    public int totalGatesToWin = 10;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private float nextSpawnX = 15f;
    private Coroutine typingCoroutine;

    [HideInInspector] public string currentCorrectAnswer;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void OnEnable()
    {
        Time.timeScale = 1f;
        if (manHienTaiText != null)
            manHienTaiText.text = "Màn " + LevelManager.CurrentLevel;

        // Lấy độ khó
        if (difficultyConfig != null)
        {
            var diff = difficultyConfig.GetDifficulty(UIManager.SelectedGrade, LevelManager.CurrentLevel);
            totalGatesToWin = diff.gateCount;
            worldSpeed = diff.speed;
            distanceBetween = diff.distance;
        }

        // DỌN DẸP MẠNH TAY
        StopAllCoroutines();
        if (cauHoiDungYenText != null) cauHoiDungYenText.text = "";

        ClearExistingZones();
        nextSpawnX = 15f;
        gatesPassed = 0;

        // Tạo cổng
        for (int i = 0; i < activeCount; i++) SpawnNewZone();

        // Hiển thị câu hỏi đầu tiên
        if (pool.Count > 0)
        {
            QuestionZone firstZone = pool.Peek().GetComponent<QuestionZone>();
            currentCorrectAnswer = firstZone.dapAnDung;
            UpdateStaticQuestionUI(firstZone.cauHoiLuuTru);
        }
    }

   

    void Update()
    {
        foreach (GameObject zone in pool)
        {
            zone.transform.Translate(Vector3.left * worldSpeed * Time.deltaTime);
        }

        if (pool.Count > 0 && pool.Peek().transform.position.x < -20f)
        {
            GameObject oldZone = pool.Dequeue();

            // Dọn dẹp UI của câu hỏi vừa trôi qua để chuẩn bị cho câu tiếp theo
            UpdateStaticQuestionUI("");

            float maxPosX = -20f;
            foreach (GameObject g in pool)
            {
                if (g.transform.position.x > maxPosX) maxPosX = g.transform.position.x;
            }

            oldZone.transform.position = new Vector3(maxPosX + distanceBetween, 0, 0);
            SetupZone(oldZone.GetComponent<QuestionZone>());
            pool.Enqueue(oldZone);

            // Cập nhật câu hỏi cho cổng gần nhất
            QuestionZone nextQ = pool.Peek().GetComponent<QuestionZone>();
            currentCorrectAnswer = nextQ.dapAnDung;
            
        }
    }

    public void CountGatePassed()
    {
        gatesPassed++;
        Debug.Log("Đã vượt qua cổng: " + gatesPassed + "/" + totalGatesToWin);

        if (gatesPassed >= totalGatesToWin)
        {
            Debug.Log("ĐIỀU KIỆN THẮNG THỎA MÃN!");
            if (UiSp.Instance != null)
            {
                UiSp.Instance.ShowWin(); // Gọi bảng chiến thắng
            }
        }
    }

    private void SpawnNewZone()
    {
        GameObject zoneObj = Instantiate(canvasPrefab, new Vector3(nextSpawnX, 0, 0), Quaternion.identity);
        SetupZone(zoneObj.GetComponent<QuestionZone>());
        pool.Enqueue(zoneObj);
        nextSpawnX += distanceBetween;
    }

    private void SetupZone(QuestionZone zone)
    {
        if (zone == null) return;
        zone.gameObject.SetActive(true);

        string cauHoi, dapAn;
        GenerateFullMathQuestion(out cauHoi, out dapAn);

        List<string> choices = GenerateDynamicChoices(dapAn);
        zone.Setup(cauHoi, choices, dapAn);

        // XÓA DÒNG UpdateStaticQuestionUI(cauHoi) Ở ĐÂY
        // Vì việc cập nhật UI sẽ do hàm OnEnable hoặc SpaceShipPhysics điều khiển theo thứ tự Queue.
    }

    // Thêm hàm này để SpaceShipPhysics có thể lấy dữ liệu cổng tiếp theo
    public QuestionZone GetNextQuestionZone()
    {
        if (pool.Count > 0)
        {
            return pool.Peek().GetComponent<QuestionZone>();
        }
        return null;
    }

    private void ClearExistingZones()
    {
        foreach (GameObject zone in pool) Destroy(zone);
        pool.Clear();
    }

    public void UpdateStaticQuestionUI(string cauHoi = "")
    {
        if (cauHoiDungYenText == null) return;

        // 1. Dừng Coroutine cụ thể đang chạy thay vì dùng StopAllCoroutines (tránh dừng nhầm logic khác)
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null; // Đảm bảo biến được giải phóng
        }

        // 2. Xóa sạch text ngay lập tức để không bị chữ cũ đè chữ mới
        cauHoiDungYenText.text = "";

        // 3. Chỉ bắt đầu Coroutine mới nếu có nội dung câu hỏi
        if (!string.IsNullOrEmpty(cauHoi))
        {
            typingCoroutine = StartCoroutine(TypeEffect(cauHoi));
        }
    }

    private IEnumerator TypeEffect(string text)
    {
        // Xóa text một lần nữa khi bắt đầu chạy loop
        cauHoiDungYenText.text = "";

        foreach (char c in text.ToCharArray())
        {
            // Kiểm tra null đề phòng trường hợp object UI bị hủy đột ngột
            if (cauHoiDungYenText == null) yield break;

            cauHoiDungYenText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        typingCoroutine = null;
    }

    public void SetCauHoiDungYenResult(string title, string result)
    {
        if (cauHoiDungYenText != null)
            cauHoiDungYenText.text = title + " " + result;
    }

    // --- FULL LOGIC TẠO CÂU HỎI TOÁN HỌC (8 DẠNG) ---
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
}