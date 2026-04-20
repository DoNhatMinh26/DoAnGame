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
    [SerializeField] private TextMeshProUGUI cauHoiDungYenText;

    [Header("Hiệu ứng chữ")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Settings - World Movement")]
    public GameObject canvasPrefab;
    public float worldSpeed = 8f;
    public float distanceBetween = 40f;
    public int activeCount = 3;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private float nextSpawnX = 15f;
    private Coroutine typingCoroutine;

    [HideInInspector] public string currentCorrectAnswer;

    void Awake() => Instance = this;

    void OnEnable()
    {
        if (manHienTaiText != null)
            manHienTaiText.text = "Màn " + LevelManager.CurrentLevel;

        ClearExistingZones();
        nextSpawnX = 15f;
        for (int i = 0; i < activeCount; i++) SpawnNewZone();

        UpdateStaticQuestionUI();
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
        GameObject oldZone = pool.Dequeue();

        float maxCurrentX = -1000f;
        foreach (GameObject zone in pool)
        {
            if (zone.transform.position.x > maxCurrentX)
                maxCurrentX = zone.transform.position.x;
        }

        float newX = (maxCurrentX == -1000f) ? nextSpawnX : maxCurrentX + distanceBetween;
        oldZone.transform.position = new Vector3(newX, 0, 0);

        // RESET COLLIDER & MOVEMENT: Bật lại va chạm cho các cổng khi trạm quay trở lại
        foreach (Collider2D c in oldZone.GetComponentsInChildren<Collider2D>()) c.enabled = true;

        SetupDataForZone(oldZone);
        pool.Enqueue(oldZone);

        UpdateStaticQuestionUI();
    }

    public void UpdateStaticQuestionUI()
    {
        if (pool.Count > 0 && cauHoiDungYenText != null)
        {
            // MỞ KHÓA DI CHUYỂN cho phi thuyền khi chuẩn bị trạm mới
            SpaceShipPhysics playerScript = FindObjectOfType<SpaceShipPhysics>();
            if (playerScript != null) playerScript.ResetMovement();

            QuestionZone nextZone = pool.Peek().GetComponent<QuestionZone>();
            if (nextZone != null)
            {
                currentCorrectAnswer = nextZone.dapAnDung;
                cauHoiDungYenText.color = Color.white;

                if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                typingCoroutine = StartCoroutine(TypeText(nextZone.cauHoiLuuTru));
            }
        }
    }

    IEnumerator TypeText(string textToType)
    {
        cauHoiDungYenText.text = "";
        foreach (char letter in textToType.ToCharArray())
        {
            cauHoiDungYenText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    public string GetCauHoiDungYenText() => cauHoiDungYenText != null ? cauHoiDungYenText.text : "";

    public void SetCauHoiDungYenResult(string phepTinh, string dapAn)
    {
        if (cauHoiDungYenText != null)
        {
            // Hiển thị kết quả đúng với số màu xanh trên UI đứng yên
            cauHoiDungYenText.text = $"{phepTinh} <color=green>{dapAn}</color>";
        }
    }

    public void UpdateWorldSpeed(float amount) => worldSpeed = Mathf.Clamp(worldSpeed + amount, 4f, 25f);

    void SetupDataForZone(GameObject zoneObj)
    {
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
        string phepToanHienTai = activeOps[Random.Range(0, activeOps.Count)];

        string cauHoiText = "";
        string dapAnDungStr = "";
        bool isDecimalMode = (phepToanHienTai == "decimal_+-");

        int n1, n2;

        switch (phepToanHienTai)
        {
            case "+":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                cauHoiText = $"{n1} + {n2} = ?";
                dapAnDungStr = (n1 + n2).ToString();
                break;

            case "-":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                cauHoiText = $"{n1} - {n2} = ?";
                dapAnDungStr = (n1 - n2).ToString();
                break;

            case "x":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(2, 10);
                cauHoiText = $"{n1} x {n2} = ?";
                dapAnDungStr = (n1 * n2).ToString();
                break;

            case ":":
                int kq = Random.Range(minVal, maxVal + 1);
                int sc = Random.Range(2, 10);
                cauHoiText = $"{sc * kq} : {sc} = ?";
                dapAnDungStr = kq.ToString();
                break;

            case "find_+-":
                int x = Random.Range(minVal, maxVal + 1);
                int b = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                {
                    int tong = x + b;
                    cauHoiText = (Random.value > 0.5f) ? $"? + {b} = {tong}" : $"{b} + ? = {tong}";
                }
                else
                {
                    int a = x + b;
                    cauHoiText = $"{a} - ? = {b}";
                }
                dapAnDungStr = x.ToString();
                break;

            case "find_x:":
                int valX = Random.Range(minVal, maxVal + 1);
                int valB = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                {
                    int tich = valX * valB;
                    cauHoiText = (Random.value > 0.5f) ? $"? x {valB} = {tich}" : $"{valB} x ? = {tich}";
                    dapAnDungStr = valX.ToString();
                }
                else
                {
                    if (valB == 0) valB = 1;
                    int soBiChia = valX * valB;
                    if (Random.value > 0.5f) { cauHoiText = $"? : {valB} = {valX}"; dapAnDungStr = soBiChia.ToString(); }
                    else { cauHoiText = $"{soBiChia} : ? = {valX}"; dapAnDungStr = valB.ToString(); }
                }
                break;

            case "decimal_+-":
                float d1 = Random.Range(minVal, maxVal + 1) / 10f;
                float d2 = Random.Range(minVal, maxVal + 1) / 10f;
                if (Random.value > 0.5f)
                {
                    float res = (float)System.Math.Round(d1 + d2, 1);
                    cauHoiText = $"{d1:F1} + {d2:F1} = ?";
                    dapAnDungStr = res.ToString("F1");
                }
                else
                {
                    if (d1 < d2) { float t = d1; d1 = d2; d2 = t; }
                    float res = (float)System.Math.Round(d1 - d2, 1);
                    cauHoiText = $"{d1:F1} - {d2:F1} = ?";
                    dapAnDungStr = res.ToString("F1");
                }
                break;
        }

        List<string> choices = new List<string> { dapAnDungStr };
        while (choices.Count < 3)
        {
            string wrong;
            if (isDecimalMode)
            {
                float trueVal = float.Parse(dapAnDungStr);
                float offset = Random.Range(-5, 6) / 10f;
                if (Mathf.Abs(offset) < 0.1f) offset = 0.1f;
                wrong = Mathf.Abs((float)System.Math.Round(trueVal + offset, 1)).ToString("F1");
            }
            else
            {
                int trueVal = int.Parse(dapAnDungStr);
                int offset = Random.Range(-5, 6);
                if (offset == 0) offset = 1;
                wrong = Mathf.Abs(trueVal + offset).ToString();
            }

            if (!choices.Contains(wrong) && wrong != "0") choices.Add(wrong);
        }

        QuestionZone qZone = zoneObj.GetComponent<QuestionZone>();
        if (qZone != null) qZone.Setup(cauHoiText, choices, dapAnDungStr);
    }
}