using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private TextAsset gameDataJson;
    private GameDataContainer gameData;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI cauHoiText;
    [SerializeField] private TextMeshProUGUI manHienTaiText; // THÊM BIẾN NÀY
    [SerializeField] private Button[] dapAnBnt;

    // Biến nạp từ JSON
    private int minVal, maxVal;
    private bool phepCong, phepTru, phepNhan, phepChia, haiPhepTinh, phepTimX, phepNgoac;

    private int SaiTu = -5;
    private int SaiDen = 5;
    private int dapAnDung;

    void Awake()
    {
        if (gameDataJson != null)
            gameData = JsonUtility.FromJson<GameDataContainer>(gameDataJson.text);
    }

    public void UpdateDifficultyFromJSON()
    {
        if (gameData == null) return;

        // Lấy thông tin từ MenuManager và LevelManager (static)
        var grade = gameData.grades.Find(g => g.gradeID == MenuManager.SelectedGrade);
        var mode = grade?.gameModes.Find(m => m.modeName == MenuManager.SelectedMode);
        var config = mode?.levels.Find(l => l.levelID == LevelManager.CurrentLevel);

        if (config != null)
        {
            // CẬP NHẬT HIỂN THỊ SỐ MÀN CHƠI LÊN UI
            if (manHienTaiText != null)
            {
                manHienTaiText.text = "Màn " + config.levelID;
            }

            // Nạp thông số min/max
            minVal = config.minVal;
            maxVal = config.maxVal;

            // Reset tất cả các chế độ trước khi nạp mới
            phepCong = phepTru = phepNhan = phepChia = haiPhepTinh = phepTimX = phepNgoac = false;

            foreach (string op in config.allowOps)
            {
                if (op == "+") phepCong = true;
                if (op == "-") phepTru = true;
                if (op == "x") phepNhan = true;
                if (op == ":") phepChia = true;
                if (op == "2") haiPhepTinh = true;
                if (op == "find_x") phepTimX = true;
                if (op == "bracket") phepNgoac = true;
            }
            TaoCauHoi();
        }
    }

    // ... (Giữ nguyên hàm TaoCauHoi và các hàm phụ bên dưới) ...
    public void TaoCauHoi()
    {
        StopAllCoroutines();
        SetButtonsInteractable(true);
        ResetColorButtons();

        List<string> options = new List<string>();
        if (phepCong) options.Add("+");
        if (phepTru) options.Add("-");
        if (phepNhan) options.Add("x");
        if (phepChia) options.Add(":");
        if (haiPhepTinh) options.Add("2");
        if (phepTimX) options.Add("find_x");
        if (phepNgoac) options.Add("bracket");

        if (options.Count == 0) options.Add("+");
        string phepToanHienTai = options[Random.Range(0, options.Count)];

        int n1, n2, n3;

        switch (phepToanHienTai)
        {
            case "+":
                n1 = Random.Range(minVal, maxVal); n2 = Random.Range(minVal, maxVal);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} = ?"; break;
            case "-":
                n1 = Random.Range(minVal, maxVal); n2 = Random.Range(minVal, maxVal);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                dapAnDung = n1 - n2; cauHoiText.text = $"{n1} - {n2} = ?"; break;
            case "x":
                n1 = Random.Range(minVal, Mathf.Max(minVal + 1, maxVal / 2));
                n2 = Random.Range(2, 10);
                dapAnDung = n1 * n2; cauHoiText.text = $"{n1} x {n2} = ?"; break;
            case ":":
                int kq = Random.Range(minVal, maxVal); int sc = Random.Range(2, 10);
                dapAnDung = kq; cauHoiText.text = $"{sc * kq} : {sc} = ?"; break;
            case "2":
                n1 = Random.Range(minVal, maxVal); n2 = Random.Range(minVal, maxVal); n3 = Random.Range(minVal, maxVal);
                dapAnDung = n1 + n2 - n3; cauHoiText.text = $"{n1} + {n2} - {n3} = ?"; break;
            case "find_x":
                int x = Random.Range(minVal, maxVal);
                int v2 = Random.Range(minVal, maxVal);
                int tong = x + v2;
                dapAnDung = x; cauHoiText.text = $"? + {v2} = {tong}"; break;
            case "bracket":
                n1 = Random.Range(minVal, maxVal); n2 = Random.Range(minVal, maxVal); n3 = Random.Range(2, 6);
                dapAnDung = (n1 + n2) * n3; cauHoiText.text = $"({n1} + {n2}) x {n3} = ?"; break;
        }

        List<int> choices = new List<int> { dapAnDung };
        while (choices.Count < dapAnBnt.Length)
        {
            int offset = Random.Range(SaiTu, SaiDen + 1);
            if (offset == 0) offset = 1;
            int wrong = Mathf.Abs(dapAnDung + offset);
            if (!choices.Contains(wrong)) choices.Add(wrong);
        }

        ShuffleList(choices);
        for (int i = 0; i < dapAnBnt.Length; i++)
        {
            dapAnBnt[i].GetComponentInChildren<TextMeshProUGUI>().text = choices[i].ToString();
            dapAnBnt[i].onClick.RemoveAllListeners();
            int val = choices[i];
            dapAnBnt[i].onClick.AddListener(() => CheckDapAn(val));
        }
    }

    private void CheckDapAn(int val)
    {
        SetButtonsInteractable(false);
        if (val == dapAnDung)
        {
            HighlightButton(val, Color.green);
            StartCoroutine(ActionAfterDelay(1f, true));
        }
        else
        {
            HighlightButton(val, Color.red);
            StartCoroutine(ActionAfterDelay(0.5f, false));
        }
    }

    IEnumerator ActionAfterDelay(float d, bool win)
    {
        yield return new WaitForSeconds(d);
        if (win) TaoCauHoi();
        else { ResetColorButtons(); SetButtonsInteractable(true); }
    }

    private void HighlightButton(int v, Color c)
    {
        foreach (var b in dapAnBnt)
            if (b.GetComponentInChildren<TextMeshProUGUI>().text == v.ToString()) b.image.color = c;
    }

    private void ResetColorButtons() { foreach (var b in dapAnBnt) b.image.color = Color.white; }
    private void SetButtonsInteractable(bool s) { foreach (var b in dapAnBnt) b.interactable = s; }
    private void ShuffleList(List<int> l) { for (int i = 0; i < l.Count; i++) { int t = l[i]; int r = Random.Range(i, l.Count); l[i] = l[r]; l[r] = t; } }
}