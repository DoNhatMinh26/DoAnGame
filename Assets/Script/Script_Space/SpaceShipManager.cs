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
            GameObject obj = pool.Dequeue();
            if (obj != null) Destroy(obj);
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

        foreach (Collider2D c in oldZone.GetComponentsInChildren<Collider2D>()) c.enabled = true;

        SetupDataForZone(oldZone);
        pool.Enqueue(oldZone);

        UpdateStaticQuestionUI();
    }

    public void UpdateStaticQuestionUI()
    {
        if (pool.Count > 0 && cauHoiDungYenText != null)
        {
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

    public void SetCauHoiDungYenResult(string phepTinh, string dapAn)
    {
        if (cauHoiDungYenText != null)
        {
            cauHoiDungYenText.text = $"{phepTinh} <color=green>{dapAn}</color>";
        }
    }

    public void UpdateWorldSpeed(float amount) => worldSpeed = Mathf.Clamp(worldSpeed + amount, 4f, 25f);

    void SetupDataForZone(GameObject zoneObj)
    {
        // Lấy cấu hình dựa trên Grade và Level
        var config = levelData.GetConfigForLevel(UIManager.SelectedGrade, LevelManager.CurrentLevel);
        if (config == null) return;

        int minVal = config.MinNumber;
        int maxVal = config.MaxNumber;

        List<string> activeOps = new List<string>();
        // Đồng bộ 8 dạng toán theo ảnh image_1dd911.png
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

        string cauHoiStr = "";
        string dapAnStr = "";
        int n1, n2, n3;

        switch (phepToan)
        {
            case "+":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                cauHoiStr = $"{n1} + {n2} = ?";
                dapAnStr = (n1 + n2).ToString();
                break;

            case "-":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                cauHoiStr = $"{n1} - {n2} = ?";
                dapAnStr = (n1 - n2).ToString();
                break;

            case "x":
                if (UIManager.SelectedGrade >= 4)
                {
                    n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1);
                }
                else
                {
                    n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(2, 10);
                }
                cauHoiStr = $"{n1} x {n2} = ?";
                dapAnStr = (n1 * n2).ToString();
                break;

            case ":":
                int sc, kq;
                if (UIManager.SelectedGrade >= 4)
                {
                    sc = Random.Range(minVal, maxVal + 1); kq = Random.Range(minVal, maxVal + 1);
                    if (sc == 0) sc = 1;
                }
                else
                {
                    kq = Random.Range(2, 10); sc = Random.Range(minVal, maxVal + 1);
                }
                cauHoiStr = $"{sc * kq} : {sc} = ?";
                dapAnStr = kq.ToString();
                break;

            case "find_+-":
                int x = Random.Range(minVal, maxVal + 1);
                int b = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                {
                    int tong = x + b; cauHoiStr = (Random.value > 0.5f) ? $"? + {b} = {tong}" : $"{b} + ? = {tong}";
                    dapAnStr = x.ToString();
                }
                else
                {
                    if (x < b) x = b + Random.Range(1, 10);
                    int hieu = x - b;
                    if (Random.value > 0.5f) { cauHoiStr = $"? - {b} = {hieu}"; dapAnStr = x.ToString(); }
                    else { cauHoiStr = $"{x} - ? = {hieu}"; dapAnStr = b.ToString(); }
                }
                break;

            case "find x":
                int vX = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int vB = Random.Range(minVal, maxVal + 1);
                int tich = vX * vB;
                cauHoiStr = (Random.value > 0.5f) ? $"? x {vB} = {tich}" : $"{vB} x ? = {tich}";
                dapAnStr = vX.ToString();
                break;

            case "find :":
                int vXc = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int vBc = Random.Range(minVal, maxVal + 1);
                if (vBc == 0) vBc = 1;
                int sbcVal = vXc * vBc;
                if (Random.value > 0.5f) { cauHoiStr = $"? : {vBc} = {vXc}"; dapAnStr = sbcVal.ToString(); }
                else { cauHoiStr = $"{sbcVal} : ? = {vXc}"; dapAnStr = vBc.ToString(); }
                break;

            case "hai phép tính +-":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                n3 = Random.Range(minVal, maxVal + 1);
                if (Random.value > 0.5f)
                {
                    int res = n1 + n2 - n3;
                    if (res < 0) res = n1 + n2 + n3;
                    cauHoiStr = res == (n1 + n2 - n3) ? $"{n1} + {n2} - {n3} = ?" : $"{n1} + {n2} + {n3} = ?";
                    dapAnStr = res.ToString();
                }
                else
                {
                    if (n1 < n2) n1 = n2 + Random.Range(1, 10);
                    cauHoiStr = $"{n1} - {n2} + {n3} = ?";
                    dapAnStr = (n1 - n2 + n3).ToString();
                }
                break;
        }

        // Logic sai số biến động theo lớp giống MathManager
        List<string> choices = new List<string> { dapAnStr };
        int gradeFactor = UIManager.SelectedGrade;
        int maxOffset = gradeFactor * 5;
        int trueVal = int.Parse(dapAnStr);

        int safety = 0;
        while (choices.Count < 3 && safety < 50)
        {
            safety++;
            int offset = Random.Range(-maxOffset, maxOffset + 1);
            if (offset == 0) offset = 1;
            string wrong = Mathf.Abs(trueVal + offset).ToString();

            if (!choices.Contains(wrong) && wrong != "0") choices.Add(wrong);
        }

        QuestionZone qZone = zoneObj.GetComponent<QuestionZone>();
        if (qZone != null) qZone.Setup(cauHoiStr, choices, dapAnStr);
    }
}