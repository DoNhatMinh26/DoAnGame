using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MathManager : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Health Systems")]
    public HealthSystem Health;
    
    [Header("Thành phần UI")]
    [SerializeField] private TextMeshProUGUI cauHoiText;
    [SerializeField] private Button[] dapAnBnt;

    [Header("Cài đặt phép tính")]
    [SerializeField] private bool phepCong = true;
    [SerializeField] private int CTu;
    [SerializeField] private int CDen;
    [SerializeField] private bool phepTru = true;
    [SerializeField] private int TTu;
    [SerializeField] private int TDen;
    [SerializeField] private bool phepNhan = false;
    [SerializeField] private int NTu;
    [SerializeField] private int NDen;
    [SerializeField] private bool phepChia = false;
    [SerializeField] private int CCTu;
    [SerializeField] private int CCDen;
    [Header("Chia VD:(C : (A->B) = (A->B))")]

    [Header("Cài đặt số phép tính")]
    [SerializeField] private bool haiPhepTinh = false;
    [Header("Chọn phép tính của 2 phép tính")]
    [SerializeField] private bool congHaiPhep = true;
    [SerializeField] private bool truHaiPhep = true;
    //[SerializeField] private bool nhanHaiPhep = false;
    //[SerializeField] private bool chiaHaiPhep = false;
    [SerializeField] private int Tu;
    [SerializeField] private int Den;

    private string phepToanHienTai;
    private int dapAnDung;

    [Header("Sai số của Đáp án sai")]
    [SerializeField] private int SaiTu;
    [SerializeField] private int SaiDen;
    void Start()
    {
        TaoCauHoi();
    }

    void Update()
    {
        
    }
    public void TaoCauHoi()
    {
        //bật lại botton
        SetButtonsInteractable(true);
        // Reset các nút về màu trắng ban đầu
        foreach (Button bnt in dapAnBnt)
        {
            bnt.image.color = Color.white;
        }
        int number1 ;
        int number2 ;
        int number3;
        // Chọn ngẫu nhiên phép tính dựa trên các lựa chọn đã bật
        List<string> optionsPhepToan = new List<string>();
        if (phepCong) optionsPhepToan.Add("+");
        if (phepTru) optionsPhepToan.Add("-");
        if (phepNhan) optionsPhepToan.Add("x");
        if (phepChia) optionsPhepToan.Add(":");
        if (haiPhepTinh) optionsPhepToan.Add("2");
        phepToanHienTai = optionsPhepToan[Random.Range(0, optionsPhepToan.Count)];

        // 2. Tính toán đáp án đúng dựa trên phép tính
        switch (phepToanHienTai)
        {
            case "+":
                number1 = Random.Range(CTu, CDen);
                number2 = Random.Range(CTu, CDen);
                cauHoiText.text = number1 + " " + phepToanHienTai + " " + number2 + " = ?";
                dapAnDung = number1 + number2;
                break;
            case "-":
                number1 = Random.Range(TTu, TDen);
                number2 = Random.Range(TTu, TDen);
                // Đảm bảo số thứ nhất lớn hơn để tránh đáp án âm 
                if (number1 < number2) { int temp = number1; number1 = number2; number2 = temp; }
                cauHoiText.text = number1 + " " + phepToanHienTai + " " + number2 + " = ?";
                dapAnDung = number1 - number2;
                break;
            case "x":
                // Với phép nhân nên giới hạn số nhỏ hơn để dễ tính
                number1 = Random.Range(NTu, NDen);
                number2 = Random.Range(NTu, NDen);
                cauHoiText.text = number1 + " " + phepToanHienTai + " " + number2 + " = ?";
                dapAnDung = number1 * number2;
                break;
            case ":":
                int soChia = Random.Range(CCTu, CCDen);
                int ketQua = Random.Range(CCTu, CCDen);
                int soBiChia = soChia * ketQua; // Ví dụ 2 * 5 = 10

                number1 = soBiChia;
                number2 = soChia;
                dapAnDung = ketQua;
                cauHoiText.text = number1 + " " + phepToanHienTai + " " + number2 + " = ?";
                break;
            case "2":
                 number1 = Random.Range(Tu,Den);
                 number2 = Random.Range(Tu, Den);
                 number3 = Random.Range(Tu, Den);

                // 1. Lấy danh sách phép tính khả dụng
                List<string> optionsHaiPhep = new List<string>();
                if (congHaiPhep) optionsHaiPhep.Add("+");
                if (truHaiPhep) optionsHaiPhep.Add("-");
                //if (nhanHaiPhep) optionsHaiPhep.Add("x");
                //if (chiaHaiPhep) optionsHaiPhep.Add(":");
                // Lưu ý: Tạm thời chỉ nên kết hợp + và - để tránh lỗi thứ tự thực hiện phép tính

                string dau1 = optionsHaiPhep[Random.Range(0, optionsHaiPhep.Count)];
                string dau2 = optionsHaiPhep[Random.Range(0, optionsHaiPhep.Count)];

                // 2. Tính toán kết quả trung gian và kết quả cuối
                int ketQuaTrungGian = 0;

                // Tính bước 1: number1 [dau1] number2
                if (dau1 == "+") ketQuaTrungGian = number1 + number2;
                else 
                {
                    // Đảm bảo kết quả trung gian không âm (nếu làm game cho trẻ em)
                    if (number1 < number2) { int tem = number1; number1 = number2; number2 = tem; }
                    ketQuaTrungGian = number1 - number2;
                }
                // Tính bước 2: ketQuaTrungGian [dau2] number3
                if (dau2 == "+") dapAnDung = ketQuaTrungGian + number3;
                else
                {
                    // Đảm bảo không ra đáp án âm
                    if (ketQuaTrungGian < number3) number3 = Random.Range(0, ketQuaTrungGian);
                    dapAnDung = ketQuaTrungGian - number3;
                }

                // 3. Hiển thị câu hỏi lên UI
                cauHoiText.text = $"{number1} {dau1} {number2} {dau2} {number3} = ?";
                break;
        }

        // 3. Hiển thị Đáp án
        int[] options = new int[3];
        options[0] = dapAnDung;
        options[1] = dapAnDung + Random.Range(SaiTu, SaiDen); // Đáp án sai 1
        options[2] = dapAnDung + Random.Range(SaiTu, SaiDen); // Đáp án sai 2
        // Đảm bảo đáp án sai không âm
        if (options[1] < 0) options[1]= Mathf.Abs(options[1]);
        if (options[2] < 0) options[2]= Mathf.Abs(options[2]);
        // Đảm bảo đáp án sai không trùng với nhau
        if (options[1] == options[0]) options[1] += Random.Range(1, SaiDen);
        if (options[2] == options[0]) options[2] += Random.Range(1, SaiDen);
        if (options[2] == options[1]) options[2] += Random.Range(1, SaiDen);
        // 4. Trộn ngẫu nhiên vị trí các nút (Shuffle)
        TronDapAn(options);
        // 5. Gán giá trị vào các nút UI
        for (int i = 0; i < dapAnBnt.Length; i++)
        {
            dapAnBnt[i].GetComponentInChildren<TextMeshProUGUI>().text = options[i].ToString();

            // Xóa sự kiện cũ để tránh cộng dồn
            dapAnBnt[i].onClick.RemoveAllListeners();

            int DapAnDaChon = options[i]; // Lưu giá trị tạm để dùng trong Lambda
            dapAnBnt[i].onClick.AddListener(() => CheckDapAn(DapAnDaChon));
        }
    }
    private void CheckDapAn(int DapAnDaChon)
    {
        //Khóa click
        SetButtonsInteractable(false);
        //Kiểm tra
        if (DapAnDaChon == dapAnDung)
        {
            Debug.Log("Đúng");
            // Gọi hàm trừ máu quái vật ở đây
            animator.SetTrigger("Win");
            Health.TakeDamageEnemy(20f);
            // Đổi màu nút đúng
            foreach (Button bnt in dapAnBnt)
            {
                if (bnt.GetComponentInChildren<TextMeshProUGUI>().text == DapAnDaChon.ToString())
                {
                    bnt.image.color = Color.green;
           
                }
            }

            // Đổi câu hỏi mới sau 1s
            Invoke("TaoCauHoi", 1f);
            
        }
        else
        {
            Debug.Log("sai");
            // Gọi hàm trừ máu người chơi ở đây
            animator.SetTrigger("Lose");
            Health.TakeDamagePlayer(20f);
            // Đổi màu nút sai
            foreach (Button bnt in dapAnBnt)
            {
                if (bnt.GetComponentInChildren<TextMeshProUGUI>().text == DapAnDaChon.ToString())
                {
                    bnt.image.color = Color.red;
                        
                }
            }
            //Mở click sau 1s
            Invoke("EnableButtonsAfterWrongAnswer", 1f);
        }
    }
    // Hàm hỗ trợ bật/tắt các nút bấm
    private void SetButtonsInteractable(bool state)
    {
        foreach (Button bnt in dapAnBnt)
        {
            bnt.interactable = state;
        }
    }

    // Hàm gọi lại khi trả lời sai
    private void EnableButtonsAfterWrongAnswer()
    {
        SetButtonsInteractable(true);
    }

    // Hàm trộn mảng đáp án
    private void TronDapAn(int[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int temp = array[i];
            int randomIndex = Random.Range(i, array.Length);
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
}
