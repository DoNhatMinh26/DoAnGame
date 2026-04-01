using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DragQuizManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI cauHoiText;
    public TextMeshProUGUI[] answerTexts; // Mảng các Text hiển thị trên các nút kéo thả

    [Header("Cài đặt phép tính")]
    [SerializeField] private bool phepCong = true;
    [SerializeField] private int CTu = 1, CDen = 10;
    [SerializeField] private bool phepTru = true;
    [SerializeField] private int TTu = 1, TDen = 10;
    [SerializeField] private bool phepNhan = false;
    [SerializeField] private int NTu = 1, NDen = 10;
    [SerializeField] private bool phepChia = false;
    [SerializeField] private int CCTu = 1, CCDen = 10;

    [Header("Cài đặt 2 phép tính")]
    [SerializeField] private bool haiPhepTinh = false;
    [SerializeField] private bool congHaiPhep = true;
    [SerializeField] private bool truHaiPhep = true;
    [SerializeField] private int Tu = 1, Den = 10;

    [Header("Cài đặt đáp án sai")]
    [SerializeField] private int SaiTu = -5;
    [SerializeField] private int SaiDen = 5;

    private int dapAnDung;

    void Start()
    {
        SetRandomQuestion();
    }

    public void SetRandomQuestion()
    {
        // 1. Reset trạng thái
        StopAllCoroutines();

        // 2. Chọn phép tính ngẫu nhiên
        List<string> optionsPhepToan = new List<string>();
        if (phepCong) optionsPhepToan.Add("+");
        if (phepTru) optionsPhepToan.Add("-");
        if (phepNhan) optionsPhepToan.Add("x");
        if (phepChia) optionsPhepToan.Add(":");
        if (haiPhepTinh) optionsPhepToan.Add("2");

        // Phòng hờ nếu không chọn phép nào thì mặc định là cộng
        if (optionsPhepToan.Count == 0) optionsPhepToan.Add("+");
        string phepToanHienTai = optionsPhepToan[Random.Range(0, optionsPhepToan.Count)];

        int n1, n2, n3;

        // 3. Logic tạo câu hỏi tương ứng
        switch (phepToanHienTai)
        {
            case "+":
                n1 = Random.Range(CTu, CDen + 1);
                n2 = Random.Range(CTu, CDen + 1);
                dapAnDung = n1 + n2;
                cauHoiText.text = $"{n1} + {n2} = ?";
                break;
            case "-":
                n1 = Random.Range(TTu, TDen + 1);
                n2 = Random.Range(TTu, TDen + 1);
                if (n1 < n2) { int t = n1; n1 = n2; n2 = t; }
                dapAnDung = n1 - n2;
                cauHoiText.text = $"{n1} - {n2} = ?";
                break;
            case "x":
                n1 = Random.Range(NTu, NDen + 1);
                n2 = Random.Range(NTu, NDen + 1);
                dapAnDung = n1 * n2;
                cauHoiText.text = $"{n1} x {n2} = ?";
                break;
            case ":":
                int ketQua = Random.Range(CCTu, CCDen + 1);
                int soChia = Random.Range(CCTu, CCDen + 1);
                dapAnDung = ketQua;
                cauHoiText.text = $"{soChia * ketQua} : {soChia} = ?";
                break;
            case "2":
                n1 = Random.Range(Tu, Den + 1);
                n2 = Random.Range(Tu, Den + 1);
                n3 = Random.Range(Tu, Den + 1);
                List<string> op2 = new List<string>();
                if (congHaiPhep) op2.Add("+");
                if (truHaiPhep) op2.Add("-");
                string d1 = op2[Random.Range(0, op2.Count)];
                string d2 = op2[Random.Range(0, op2.Count)];
                int mid = (d1 == "+") ? (n1 + n2) : Mathf.Max(0, n1 - n2);
                dapAnDung = (d2 == "+") ? (mid + n3) : Mathf.Max(0, mid - n3);
                cauHoiText.text = $"{n1} {d1} {n2} {d2} {n3} = ?";
                break;
        }

        // 4. Tạo danh sách đáp án ngẫu nhiên không trùng lặp
        List<int> choices = new List<int>();
        choices.Add(dapAnDung);

        int safetyBreak = 0;
        while (choices.Count < answerTexts.Length && safetyBreak < 100)
        {
            safetyBreak++;
            int offset = Random.Range(SaiTu, SaiDen + 1);
            int wrongAns = Mathf.Abs(dapAnDung + offset);

            // Chỉ thêm nếu chưa tồn tại và không phải đáp án đúng
            if (!choices.Contains(wrongAns))
            {
                choices.Add(wrongAns);
            }
        }

        // 5. Trộn vị trí và hiển thị lên UI
        ShuffleList(choices);
        for (int i = 0; i < answerTexts.Length; i++)
        {
            // Kiểm tra phòng hờ nếu số nút nhiều hơn số đáp án tạo được
            if (i < choices.Count)
                answerTexts[i].text = choices[i].ToString();
        }
    }

    public int GetCurrentCorrectAnswer() => dapAnDung;

    private void ShuffleList(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int temp = list[i];
            int r = Random.Range(i, list.Count);
            list[i] = list[r];
            list[r] = temp;
        }
    }
}