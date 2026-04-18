using UnityEngine;
using TMPro;

public class SpaceShipPhysics : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float verticalSpeed = 15f;
    public float minY = -4.5f, maxY = 4.5f;

    void Update()
    {
        // Điều khiển phi thuyền lên xuống tại chỗ
        float moveY = Input.GetAxis("Vertical") * verticalSpeed * Time.deltaTime;
        transform.position += new Vector3(0, moveY, 0);

        // Giới hạn biên màn hình
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);
        transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra va chạm với các đối tượng có Tag là Gate
        if (other.CompareTag("Gate"))
        {
            // 1. Lấy nội dung chữ hiển thị trên cổng
            string selectedVal = other.GetComponentInChildren<TextMeshProUGUI>().text;

            // 2. Truy cập vào script QuestionZone ở đối tượng cha (Canvas)
            QuestionZone zone = other.GetComponentInParent<QuestionZone>();

            if (zone != null)
            {
                // 3. So sánh với đáp án đúng và in ra Console
                if (selectedVal == zone.dapAnDung)
                {
                    Debug.Log("<color=green>ĐÚNG RỒI!</color> Bạn đã chọn: " + selectedVal + " | Đáp án đúng là: " + zone.dapAnDung);
                }
                else
                {
                    Debug.Log("<color=red>SAI RỒI!</color> Bạn đã chọn: " + selectedVal + " | Đáp án đúng là: " + zone.dapAnDung);
                }
            }
            else
            {
                Debug.LogWarning("Không tìm thấy script QuestionZone trên Canvas cha của Gate này!");
            }
        }
    }
}