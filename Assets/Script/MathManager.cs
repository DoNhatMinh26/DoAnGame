using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private LevelGenerate levelData;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI cauHoiText;
    [SerializeField] private TextMeshProUGUI manHienTaiText;
    [SerializeField] private Button[] dapAnBnt;

    private int minVal, maxVal, dapAnDung;
    private float dapAnDungDecimal; // Thêm biến để lưu đáp án thập phân
    private bool isDecimalMode = false; // Kiểm tra xem có đang chơi số thập phân không
    private List<string> activeOps = new List<string>();

    public void UpdateDifficulty()
    {
        if (levelData == null) return;

        // LẤY DỮ LIỆU TỪ UIManager.SelectedGrade
        // gradeIndex: 1-5, levelIndex: 1-100
        var config = levelData.GetConfigForLevel(UIManager.SelectedGrade, LevelManager.CurrentLevel);

        if (config != null)
        {
            if (manHienTaiText != null)
                manHienTaiText.text = "Màn " + config.LevelIndex;

            // Tự động lấy Min/Max từ AnimationCurve của đúng Lớp đã chọn
            minVal = config.MinNumber;
            maxVal = config.MaxNumber;

            activeOps.Clear();
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

            TaoCauHoi();
        }
    }

    public void TaoCauHoi()
    {
        StopAllCoroutines();
        SetButtonsInteractable(true);
        ResetColorButtons();
        isDecimalMode = false;

        if (activeOps.Count == 0) activeOps.Add("+");
        string phepToanHienTai = activeOps[Random.Range(0, activeOps.Count)];

        int n1, n2;

        switch (phepToanHienTai)
        {
            case "+":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} = ?"; break;

            case "-":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                dapAnDung = n1 - n2; cauHoiText.text = $"{n1} - {n2} = ?"; break;

            case "x":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(2, 10);
                dapAnDung = n1 * n2; cauHoiText.text = $"{n1} x {n2} = ?"; break;

            case ":":
                int kq = Random.Range(minVal, maxVal + 1);
                int sc = Random.Range(2, 10);
                dapAnDung = kq; cauHoiText.text = $"{sc * kq} : {sc} = ?"; break;

            case "find_+-":
                int x = Random.Range(minVal, maxVal + 1);
                int b = Random.Range(minVal, maxVal + 1);
                int findXType = Random.Range(0, 2);
                if (findXType == 0)
                {
                    int tong = x + b; dapAnDung = x;
                    cauHoiText.text = (Random.value > 0.5f) ? $"? + {b} = {tong}" : $"{b} + ? = {tong}";
                }
                else
                {
                    dapAnDung = x;
                    if (Random.value > 0.5f)
                    {
                        int hieu = x - b;
                        if (x < b) { int t = x; x = b; b = t; hieu = x - b; dapAnDung = x; }
                        cauHoiText.text = $"? - {b} = {hieu}";
                    }
                    else
                    {
                        int a = x + b; dapAnDung = x; cauHoiText.text = $"{a} - ? = {b}";
                    }
                }
                break;

            case "find_x:":
                int valX = Random.Range(minVal, maxVal + 1);
                int valB = Random.Range(minVal, maxVal + 1);
                int findXMode = Random.Range(0, 2);
                if (findXMode == 0)
                {
                    int tich = valX * valB; dapAnDung = valX;
                    cauHoiText.text = (Random.value > 0.5f) ? $"? x {valB} = {tich}" : $"{valB} x ? = {tich}";
                }
                else
                {
                    if (valB == 0) valB = 1;
                    int soBiChia = valX * valB;
                    if (Random.value > 0.5f) { dapAnDung = soBiChia; cauHoiText.text = $"? : {valB} = {valX}"; }
                    else { dapAnDung = valB; cauHoiText.text = $"{soBiChia} : ? = {valX}"; }
                }
                break;

            case "decimal_+-":
                isDecimalMode = true;
                // Lấy số nguyên từ đồ thị và chia 10 để ra số thập phân (Ví dụ 15 -> 1.5)
                float d1 = Random.Range(minVal, maxVal + 1) / 10f;
                float d2 = Random.Range(minVal, maxVal + 1) / 10f;
                int decType = Random.Range(0, 2);

                if (decType == 0)
                {
                    dapAnDungDecimal = (float)System.Math.Round(d1 + d2, 1);
                    cauHoiText.text = $"{d1:F1} + {d2:F1} = ?";
                }
                else
                {
                    if (d1 < d2) { float t = d1; d1 = d2; d2 = t; }
                    dapAnDungDecimal = (float)System.Math.Round(d1 - d2, 1);
                    cauHoiText.text = $"{d1:F1} - {d2:F1} = ?";
                }
                break;
        }

        if (isDecimalMode) GenerateDecimalChoices();
        else GenerateChoices();
    }

    private void GenerateChoices()
    {
        List<int> choices = new List<int> { dapAnDung };
        while (choices.Count < dapAnBnt.Length)
        {
            int offset = Random.Range(-5, 6);
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

    private void GenerateDecimalChoices()
    {
        List<float> choices = new List<float> { dapAnDungDecimal };
        while (choices.Count < dapAnBnt.Length)
        {
            float offset = Random.Range(-5, 6) / 10f;
            if (Mathf.Abs(offset) < 0.1f) offset = 0.1f;
            float wrong = (float)System.Math.Round(Mathf.Abs(dapAnDungDecimal + offset), 1);
            if (!choices.Contains(wrong)) choices.Add(wrong);
        }
        // Trộn danh sách float
        for (int i = 0; i < choices.Count; i++)
        {
            float t = choices[i]; int r = Random.Range(i, choices.Count);
            choices[i] = choices[r]; choices[r] = t;
        }
        for (int i = 0; i < dapAnBnt.Length; i++)
        {
            dapAnBnt[i].GetComponentInChildren<TextMeshProUGUI>().text = choices[i].ToString("F1");
            dapAnBnt[i].onClick.RemoveAllListeners();
            float val = choices[i];
            dapAnBnt[i].onClick.AddListener(() => CheckDapAnDecimal(val));
        }
    }

    private void CheckDapAn(int val)
    {
        SetButtonsInteractable(false);
        if (val == dapAnDung) { HighlightButton(val.ToString(), Color.green); StartCoroutine(ActionAfterDelay(1f, true)); }
        else { HighlightButton(val.ToString(), Color.red); StartCoroutine(ActionAfterDelay(0.5f, false)); }
    }

    private void CheckDapAnDecimal(float val)
    {
        SetButtonsInteractable(false);
        if (Mathf.Abs(val - dapAnDungDecimal) < 0.01f) { HighlightButton(val.ToString("F1"), Color.green); StartCoroutine(ActionAfterDelay(1f, true)); }
        else { HighlightButton(val.ToString("F1"), Color.red); StartCoroutine(ActionAfterDelay(0.5f, false)); }
    }

    IEnumerator ActionAfterDelay(float d, bool win)
    {
        yield return new WaitForSeconds(d);
        if (win) TaoCauHoi();
        else { ResetColorButtons(); SetButtonsInteractable(true); }
    }

    private void HighlightButton(string label, Color c)
    {
        foreach (var b in dapAnBnt)
            if (b.GetComponentInChildren<TextMeshProUGUI>().text == label) b.image.color = c;
    }

    private void ResetColorButtons() { foreach (var b in dapAnBnt) b.image.color = Color.white; }
    private void SetButtonsInteractable(bool s) { foreach (var b in dapAnBnt) b.interactable = s; }
    private void ShuffleList(List<int> l) { for (int i = 0; i < l.Count; i++) { int t = l[i]; int r = Random.Range(i, l.Count); l[i] = l[r]; l[r] = t; } }
}