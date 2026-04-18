using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpaceShipManager : MonoBehaviour
{
    public static SpaceShipManager Instance;

    [Header("Data Source")]
    public LevelGenerate levelData;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI manHienTaiText;

    [Header("Prefabs & Player")]
    public GameObject canvasPrefab;
    public Transform player;

    [Header("Settings - World Movement")]
    public float worldSpeed = 8f;
    public float distanceBetween = 40f;
    public int activeCount = 3;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private float nextSpawnX = 15f;

    void Awake() => Instance = this;

    // SỬA TẠI ĐÂY: Dùng OnEnable thay cho Start để cập nhật mỗi khi vào màn
    void OnEnable()
    {
        // 1. Cập nhật chữ hiển thị màn chơi
        if (manHienTaiText != null)
            manHienTaiText.text = "Màn " + LevelManager.CurrentLevel;

        // 2. Dọn dẹp câu hỏi cũ (nếu có) để tránh chồng chéo màn cũ-mới
        ClearExistingZones();

        // 3. Khởi tạo các trạm câu hỏi mới cho màn này
        nextSpawnX = 15f;
        for (int i = 0; i < activeCount; i++) SpawnNewZone();
    }

    void ClearExistingZones()
    {
        while (pool.Count > 0)
        {
            Destroy(pool.Dequeue());
        }
    }

    void Update()
    {
        foreach (var zone in pool)
        {
            if (zone != null)
                zone.transform.position += Vector3.left * worldSpeed * Time.deltaTime;
        }

        if (pool.Count > 0 && pool.Peek().transform.position.x < -35f)
            RecycleZone();
    }

    void SpawnNewZone()
    {
        if (canvasPrefab == null) return;
        GameObject zone = Instantiate(canvasPrefab, new Vector3(nextSpawnX, 0, 0), Quaternion.identity);
        SetupDataForZone(zone);
        pool.Enqueue(zone);
        nextSpawnX += distanceBetween;
    }

    void RecycleZone()
    {
        if (pool.Count == 0) return;

        // 1. Lấy Canvas cũ nhất ra khỏi hàng đợi
        GameObject oldZone = pool.Dequeue();

        // 2. Tìm vị trí X xa nhất hiện tại trong các Canvas còn lại
        float maxCurrentX = -1000f;
        foreach (GameObject zone in pool)
        {
            if (zone.transform.position.x > maxCurrentX)
            {
                maxCurrentX = zone.transform.position.x;
            }
        }

        // 3. Đặt Canvas cũ vào vị trí mới nối đuôi chính xác
        // Nếu đây là lần đầu hoặc không tìm thấy, dùng giá trị mặc định
        float newX = (maxCurrentX == -1000f) ? nextSpawnX : maxCurrentX + distanceBetween;

        oldZone.transform.position = new Vector3(newX, 0, 0);

        // Kích hoạt lại các cổng đã bị ẩn khi va chạm
        foreach (Transform child in oldZone.transform) child.gameObject.SetActive(true);

        SetupDataForZone(oldZone);
        pool.Enqueue(oldZone);
    }

    public void UpdateWorldSpeed(float amount) => worldSpeed = Mathf.Clamp(worldSpeed + amount, 4f, 25f);

    void SetupDataForZone(GameObject zoneObj)
    {
        // Sử dụng LevelManager.CurrentLevel để lấy đúng độ khó
        var config = levelData.GetConfigForLevel(UIManager.SelectedGrade, LevelManager.CurrentLevel);
        if (config == null) return;

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
            if (op == 5) activeOps.Add("find_x:");
            if (op == 6) activeOps.Add("decimal_+-");
        }

        if (activeOps.Count == 0) activeOps.Add("+");
        string phepToan = activeOps[Random.Range(0, activeOps.Count)];
        string cauHoiText = "";
        string dapAnDung = "";
        bool isDecimal = (phepToan == "decimal_+-");

        // Logic sinh câu hỏi (Giữ nguyên từ MathManager của bạn)
        int n1, n2;
        switch (phepToan)
        {
            case "+":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                cauHoiText = $"{n1} + {n2} = ?";
                dapAnDung = (n1 + n2).ToString(); break;
            case "-":
                n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                cauHoiText = $"{n1} - {n2} = ?";
                dapAnDung = (n1 - n2).ToString(); break;
            case "x":
                n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(2, 10);
                cauHoiText = $"{n1} x {n2} = ?";
                dapAnDung = (n1 * n2).ToString(); break;
            case ":":
                int kq = Random.Range(minVal, maxVal + 1); int sc = Random.Range(2, 10);
                cauHoiText = $"{sc * kq} : {sc} = ?";
                dapAnDung = kq.ToString(); break;
            case "find_+-":
                int x = Random.Range(minVal, maxVal + 1); int b = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                {
                    int tong = x + b; cauHoiText = (Random.value > 0.5f) ? $"? + {b} = {tong}" : $"{b} + ? = {tong}";
                }
                else
                {
                    int a = x + b; cauHoiText = $"{a} - ? = {b}";
                }
                dapAnDung = x.ToString(); break;
            case "find_x:":
                int vx = Random.Range(minVal, maxVal + 1); int vb = Random.Range(2, 10);
                if (Random.value > 0.5f)
                {
                    int tich = vx * vb; cauHoiText = $"? x {vb} = {tich}"; dapAnDung = vx.ToString();
                }
                else
                {
                    int sbc = vx * vb; cauHoiText = $"{sbc} : ? = {vb}"; dapAnDung = vx.ToString();
                }
                break;
            case "decimal_+-":
                float d1 = Random.Range(minVal, maxVal + 1) / 10f; float d2 = Random.Range(minVal, maxVal + 1) / 10f;
                if (Random.value > 0.5f)
                {
                    cauHoiText = $"{d1:F1} + {d2:F1} = ?";
                    dapAnDung = System.Math.Round(d1 + d2, 1).ToString("F1");
                }
                else
                {
                    if (d1 < d2) { float t = d1; d1 = d2; d2 = t; }
                    cauHoiText = $"{d1:F1} - {d2:F1} = ?";
                    dapAnDung = System.Math.Round(d1 - d2, 1).ToString("F1");
                }
                break;
        }

        List<string> choices = new List<string> { dapAnDung };
        while (choices.Count < 3)
        {
            string wrong;
            if (isDecimal) wrong = (float.Parse(dapAnDung) + (Random.Range(-5, 6) / 10f)).ToString("F1");
            else wrong = (int.Parse(dapAnDung) + Random.Range(-5, 6)).ToString();
            if (!choices.Contains(wrong) && wrong != "0") choices.Add(wrong);
        }

        // Đảm bảo không bị MissingComponentException
        QuestionZone qZone = zoneObj.GetComponent<QuestionZone>();
        if (qZone != null) qZone.Setup(cauHoiText, choices, dapAnDung);
    }
}