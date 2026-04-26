using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 2.0f;
    public float damage = 10f; // Sát thương của con chuột
    private bool isAttacking = false;

    void Update()
    {
        if (!isAttacking)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra nếu chạm vào tường thành (Tag là "Tuong")
        if (collision.CompareTag("Tuong") && !isAttacking)
        {
            isAttacking = true;
            speed = 0; // Dừng di chuyển để đứng lại đánh

            // Bắt đầu đánh liên tục: 
            // Tham số 1: Tên hàm cần gọi ("GaySatThuong")
            // Tham số 2: Thời gian chờ trước lần đánh đầu tiên (0 giây - đánh luôn)
            // Tham số 3: Khoảng thời gian giữa các lần đánh (2 giây)
            InvokeRepeating("GaySatThuong", 0f, 2f);
        }
    }

    // Hàm bổ trợ để thực hiện việc trừ máu
    void GaySatThuong()
    {
        if (WallHealth.Instance != null)
        {
            WallHealth.Instance.TakeDamage(damage);
        }

        // Bạn có thể thêm hiệu ứng hoạt họa (Animation) đánh tại đây
    }

    private void OnDestroy()
    {
        // Quan trọng: Phải hủy lệnh lặp khi kẻ địch bị tiêu diệt
        CancelInvoke("GaySatThuong");
    }
}