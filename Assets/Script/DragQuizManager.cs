using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DragQuizManager : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private LevelGenerate levelData;

    [Header("UI References")]
    public TextMeshProUGUI cauHoiText;
    public TextMeshProUGUI manHienTaiText;
    public TextMeshProUGUI[] answerTexts;

    private int min, max;
    private float dapAnDung; // Đổi sang float để dùng chung cho cả số nguyên và thập phân
    private bool isDecimalMode = false;
    private List<string> activeOps = new List<string>();

    public void UpdateDifficulty()
    {
        if (levelData == null) return;

        var config = levelData.GetConfigForLevel(UIManager.SelectedGrade, LevelManager.CurrentLevel);

        if (config != null)
        {
            if (manHienTaiText != null) manHienTaiText.text = "Màn " + config.LevelIndex;

            min = config.MinNumber;
            max = config.MaxNumber;

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

            SetRandomQuestion();
        }
    }

    public void SetRandomQuestion()
    {
        isDecimalMode = false;
        if (activeOps.Count == 0) activeOps.Add("+");
        string op = activeOps[Random.Range(0, activeOps.Count)];

        int n1, n2;

        switch (op)
        {
            case "+":
                n1 = Random.Range(min, max + 1); n2 = Random.Range(min, max + 1);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} = ?"; break;
            case "-":
                n1 = Random.Range(min, max + 1); n2 = Random.Range(min, max + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                dapAnDung = n1 - n2; cauHoiText.text = $"{n1} - {n2} = ?"; break;
            case "x":
                n1 = Random.Range(min, max + 1); n2 = Random.Range(2, 10);
                dapAnDung = n1 * n2; cauHoiText.text = $"{n1} x {n2} = ?"; break;
            case ":":
                int kq = Random.Range(min, max + 1); int sc = Random.Range(2, 10);
                dapAnDung = kq; cauHoiText.text = $"{sc * kq} : {sc} = ?"; break;

            case "find_+-":
                int x = Random.Range(min, max + 1); int b = Random.Range(min, max + 1);
                if (Random.Range(0, 2) == 0)
                { // Cộng
                    int tong = x + b; dapAnDung = x;
                    cauHoiText.text = (Random.value > 0.5f) ? $"? + {b} = {tong}" : $"{b} + ? = {tong}";
                }
                else
                { // Trừ
                    dapAnDung = x;
                    if (Random.value > 0.5f)
                    {
                        int hieu = x - b; if (x < b) { x = b; b = x - hieu; }
                        cauHoiText.text = $"? - {b} = {hieu}";
                    }
                    else
                    {
                        int a = x + b; cauHoiText.text = $"{a} - ? = {b}";
                    }
                }
                break;

            case "find_x:":
                int vX = Random.Range(min, max + 1); int vB = Random.Range(min, max + 1);
                if (Random.Range(0, 2) == 0)
                { // Nhân
                    int tich = vX * vB; dapAnDung = vX;
                    cauHoiText.text = (Random.value > 0.5f) ? $"? x {vB} = {tich}" : $"{vB} x ? = {tich}";
                }
                else
                { // Chia
                    if (vB == 0) vB = 1; int sbc = vX * vB;
                    if (Random.value > 0.5f) { dapAnDung = sbc; cauHoiText.text = $"? : {vB} = {vX}"; }
                    else { dapAnDung = vB; cauHoiText.text = $"{sbc} : ? = {vX}"; }
                }
                break;

            case "decimal_+-":
                isDecimalMode = true;
                float d1 = Random.Range(min, max + 1) / 10f;
                float d2 = Random.Range(min, max + 1) / 10f;
                if (Random.Range(0, 2) == 0)
                {
                    dapAnDung = (float)System.Math.Round(d1 + d2, 1);
                    cauHoiText.text = $"{d1:F1} + {d2:F1} = ?";
                }
                else
                {
                    if (d1 < d2) { float t = d1; d1 = d2; d2 = t; }
                    dapAnDung = (float)System.Math.Round(d1 - d2, 1);
                    cauHoiText.text = $"{d1:F1} - {d2:F1} = ?";
                }
                break;

            default:
                n1 = Random.Range(min, max + 1); n2 = Random.Range(min, max + 1);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} = ?"; break;
        }

        GenerateChoices();
    }

    private void GenerateChoices()
    {
        List<float> choices = new List<float> { dapAnDung };
        int safety = 0;
        float step = isDecimalMode ? 0.1f : 1f;

        while (choices.Count < answerTexts.Length && safety < 100)
        {
            safety++;
            float offset = Random.Range(-5, 6) * step;
            if (Mathf.Abs(offset) < 0.01f) offset = step;

            float wrong = (float)System.Math.Round(Mathf.Abs(dapAnDung + offset), 1);
            if (!choices.Contains(wrong)) choices.Add(wrong);
        }

        // Shuffle
        for (int i = 0; i < choices.Count; i++)
        {
            float temp = choices[i];
            int r = Random.Range(i, choices.Count);
            choices[i] = choices[r];
            choices[r] = temp;
        }

        for (int i = 0; i < answerTexts.Length; i++)
        {
            if (i < choices.Count)
            {
                // Nếu là số thập phân thì hiện 1 chữ số sau dấu phẩy, ngược lại hiện số nguyên
                answerTexts[i].text = isDecimalMode ? choices[i].ToString("F1") : choices[i].ToString("0");
            }
        }
    }

    // Trả về float để Script xử lý kéo thả so sánh chính xác hơn
    public float GetCurrentCorrectAnswer() => dapAnDung;
}