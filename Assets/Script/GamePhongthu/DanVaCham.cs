using UnityEngine;

public class DanVaCham : MonoBehaviour
{
    [Header("Cấu hình bay")]
    public float speed = 2.0f;        // Tốc độ bay (thời gian bay tỉ lệ nghịch với số này)
    public float arcHeight = 4.0f;      // Độ cao của hình vòng cung

    private Vector3 startPos;
    private Transform target;
    private float timer = 0f;
    private bool isLaunching = false;

    // Hàm khởi tạo mục tiêu từ CannonDefenseManager
    public void SetTarget(Transform _target)
    {
        target = _target;
        startPos = transform.position;
        timer = 0f;
        isLaunching = true;
    }

    void Update()
    {
        // Chỉ bay khi có mục tiêu và đã được kích hoạt
        if (!isLaunching || target == null) return;

        timer += Time.deltaTime * speed;

        // 1. Tính toán vị trí Lerp theo trục thẳng từ điểm đầu đến kẻ địch
        // Việc Lerp trực tiếp đến target.position giúp đạn "đuổi" theo nếu kẻ địch di chuyển
        Vector3 currentPos = Vector3.Lerp(startPos, target.position, timer);

        // 2. Tính toán độ cao (Y) cộng thêm để tạo hình vòng cung (Parabol)
        // Công thức: y = h * sin(π * t) tạo ra một đường cong đều từ 0 lên đỉnh rồi về 0
        float parabola = Mathf.Sin(timer * Mathf.PI) * arcHeight;
        currentPos.y += parabola;

        // 3. Xoay đầu đạn hướng theo hướng bay
        Vector3 direction = currentPos - transform.position;
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // Cập nhật vị trí thực tế
        transform.position = currentPos;

        // 4. Khi timer chạm 1 (hoặc vượt quá), đạn đã trúng đích
        if (timer >= 1.0f)
        {
            HandleCollision();
        }
    }

    void HandleCollision()
    {
        if (target != null)
        {
            // Tiêu diệt kẻ địch (con chuột)
            Destroy(target.gameObject);
        }

        // Tự hủy viên đạn
        Destroy(gameObject);

        // Bạn có thể thêm hiệu ứng nổ tại đây
    }
}