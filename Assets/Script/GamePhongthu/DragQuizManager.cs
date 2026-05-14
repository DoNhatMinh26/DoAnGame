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

    private int minVal, maxVal;
    private float dapAnDung;
    private bool isDecimalMode = false;
    private List<string> activeOps = new List<string>();

    public void UpdateDifficulty()
    {
        if (levelData == null) return;

        var config = levelData.GetConfigForLevel(UIManager.SelectedGrade, LevelManager.CurrentLevel);

        if (config != null)
        {
            if (manHienTaiText != null) manHienTaiText.text = "Màn " + config.LevelIndex;

            minVal = config.MinNumber;
            maxVal = config.MaxNumber;

            activeOps.Clear();
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
                // Lưu ý: decimal_+- (op 8) có thể thêm nếu cần đồng bộ hoàn toàn với MathManager case 6 cũ
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
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} = ?"; break;

            case "-":
                n1 = Random.Range(minVal, maxVal + 1);
                n2 = Random.Range(minVal, maxVal + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                dapAnDung = n1 - n2; cauHoiText.text = $"{n1} - {n2} = ?"; break;

            case "x":
                if (UIManager.SelectedGrade >= 4)
                {
                    n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1);
                }
                else
                {
                    n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(2, 10);
                }
                dapAnDung = n1 * n2; cauHoiText.text = $"{n1} x {n2} = ?"; break;

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
                int sbc = sc * kq;
                dapAnDung = kq; cauHoiText.text = $"{sbc} : {sc} = ?"; break;

            case "find_+-": // Dạng 4: Tìm ẩn số dạng Cộng/Trừ
                int xVal = Random.Range(minVal, maxVal + 1);
                int bVal = Random.Range(minVal, maxVal + 1);

                if (Random.value > 0.5f)
                {
                    // Phép cộng: ? + B = TONG hoặc B + ? = TONG
                    int tong = xVal + bVal;
                    dapAnDung = xVal;
                    cauHoiText.text = (Random.value > 0.5f) ? $"? + {bVal} = {tong}" : $"{bVal} + ? = {tong}";
                }
                else
                {
                    // Phép trừ
                    if (Random.value > 0.5f)
                    {
                        // Dạng: ? - B = HIEU (X là số bị trừ, luôn lớn hơn B)
                        if (xVal < bVal) { int t = xVal; xVal = bVal; bVal = t; } // Đảo số để xVal >= bVal
                        int hieu = xVal - bVal;

                        dapAnDung = xVal;
                        cauHoiText.text = $"? - {bVal} = {hieu}";
                    }
                    else
                    {
                        // Dạng: A - ? = B (X là số trừ)
                        if (xVal < bVal) { int t = xVal; xVal = bVal; bVal = t; } // Đảo số để xVal >= bVal
                        int a = xVal; // Số bị trừ ban đầu
                        int b = bVal; // Hiệu số

                        dapAnDung = a - b; // Ẩn số là số trừ (luôn không âm)
                        cauHoiText.text = $"{a} - ? = {b}";
                    }
                }
                break;

            case "find x": // Dạng 5: Tìm x trong phép nhân
                int vX = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int vB = Random.Range(minVal, maxVal + 1);

                int tich = vX * vB;
                dapAnDung = vX;
                cauHoiText.text = (Random.value > 0.5f) ? $"? x {vB} = {tich}" : $"{vB} x ? = {tich}";
                break;

            case "find :": // Dạng 6: Tìm x trong phép chia
                int vX_c = (UIManager.SelectedGrade >= 4) ? Random.Range(minVal, maxVal + 1) : Random.Range(2, 10);
                int vB_c = Random.Range(minVal, maxVal + 1);
                if (vB_c == 0) vB_c = 1; // Bảo vệ: Không chia cho 0

                int sobc = vX_c * vB_c;
                if (Random.value > 0.5f)
                {
                    // Dạng: ? : B = X (Tìm số bị trừ)
                    dapAnDung = sobc;
                    cauHoiText.text = $"? : {vB_c} = {vX_c}";
                }
                else
                {
                    // Dạng: SBC : ? = X (Tìm số chia)
                    if (vX_c == 0) vX_c = 1; // Đảm bảo không tạo câu hỏi có thương bằng 0 dẫn đến x bằng ẩn số chia cho 0
                    int sobc_moi = vX_c * vB_c;
                    dapAnDung = vB_c;
                    cauHoiText.text = $"{sobc_moi} : ? = {vX_c}";
                }
                break;

            case "hai phép tính +-": // Dạng 7: Chuỗi hai phép tính liên tiếp
                int a1 = Random.Range(minVal, maxVal + 1);
                int a2 = Random.Range(minVal, maxVal + 1);

                if (Random.value > 0.5f)
                {
                    // Dạng toán: a1 + a2 - a3
                    // Tính toán khoảng giá trị an toàn cho a3 để (a1 + a2 - a3) không âm
                    int maxA3 = a1 + a2;
                    int thựcTếMax = Mathf.Min(maxVal, maxA3);
                    int thựcTếMin = Mathf.Min(minVal, thựcTếMax);

                    // Tạo trực tiếp a3 hợp lệ, loại bỏ hoàn toàn vòng lặp while gây crash game
                    int a3 = Random.Range(thựcTếMin, thựcTếMax + 1);

                    dapAnDung = a1 + a2 - a3;
                    cauHoiText.text = $"{a1} + {a2} - {a3} = ?";
                }
                else
                {
                    // Dạng toán: a1 - a2 + a3
                    // Đảm bảo bước tính đầu tiên (a1 - a2) không bị âm
                    if (a1 < a2)
                    {
                        a1 = a2 + Random.Range(0, 10);
                    }
                    int a3 = Random.Range(minVal, maxVal + 1);

                    dapAnDung = a1 - a2 + a3;
                    cauHoiText.text = $"{a1} - {a2} + {a3} = ?";
                }
                break;

            default:
                n1 = Random.Range(minVal, maxVal + 1); n2 = Random.Range(minVal, maxVal + 1);
                dapAnDung = n1 + n2; cauHoiText.text = $"{n1} + {n2} = ?"; break;
        }

        GenerateChoices();
    }

    private void GenerateChoices()
    {
        List<float> choices = new List<float> { (float)dapAnDung };

        // Đồng bộ sai số biến động theo khối lớp
        int gradeFactor = UIManager.SelectedGrade;
        float maxOffset = gradeFactor * 5f;

        int safety = 0;
        while (choices.Count < answerTexts.Length && safety < 100)
        {
            safety++;
            float offset = Random.Range(-maxOffset, maxOffset + 1);
            if (Mathf.Abs(offset) < 0.5f) offset = Random.value > 0.5f ? 1 : -1;

            float wrong = Mathf.Abs(dapAnDung + (int)offset);
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
            if (i < choices.Count) answerTexts[i].text = choices[i].ToString("0");
        }
    }

    public float GetCurrentCorrectAnswer() => dapAnDung;
}