using UnityEngine;

public class DanVaCham : MonoBehaviour
{
    [Header("Cấu hình bay")]
    public float speed = 2.0f;
    public float arcHeight = 4.0f;

    private Vector3 startPos;
    private Transform target;
    private float timer = 0f;
    private bool isLaunching = false;
    private bool isWaitingForTarget = false; // Trạng thái chờ kẻ địch

    // Thay đổi: Hàm này giờ đây có thể được gọi mà không cần target ngay lập tức
    public void SetTarget(Transform _target)
    {
        startPos = transform.position;
        timer = 0f;

        if (_target != null)
        {
            target = _target;
            isLaunching = true;
            isWaitingForTarget = false;
        }
        else
        {
            // Nếu chưa có mục tiêu, đưa đạn vào trạng thái chờ
            isLaunching = false;
            isWaitingForTarget = true;
        }
    }

    void Update()
    {
        // TRƯỜNG HỢP 1: Đang chờ kẻ địch xuất hiện
        if (isWaitingForTarget)
        {
            FindNewTarget();
            return; // Chưa bay cho đến khi tìm thấy mục tiêu
        }

        // TRƯỜNG HỢP 2: Đang bay đến mục tiêu
        if (!isLaunching || target == null)
        {
            // Nếu đang bay mà mục tiêu biến mất (ví dụ bị tiêu diệt bởi đạn khác)
            if (isLaunching && target == null) isWaitingForTarget = true;
            return;
        }

        timer += Time.deltaTime * speed;

        Vector3 currentPos = Vector3.Lerp(startPos, target.position, timer);
        float parabola = Mathf.Sin(timer * Mathf.PI) * arcHeight;
        currentPos.y += parabola;

        Vector3 direction = currentPos - transform.position;
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        transform.position = currentPos;

        if (timer >= 1.0f)
        {
            HandleCollision();
        }
    }

    // Hàm tự động tìm mục tiêu gần nhất khi đang ở trạng thái chờ
    void FindNewTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length > 0)
        {
            // Tìm kẻ địch gần nhất hoặc lấy kẻ địch đầu tiên trong danh sách
            target = enemies[0].transform;
            isLaunching = true;
            isWaitingForTarget = false;
        }
    }

    void HandleCollision()
    {
        if (target != null)
        {
            // Xác nhận quái bị tiêu diệt bởi người chơi trước khi Destroy
            if (target.TryGetComponent(out EnemyMovement enemy))
            {
                enemy.biTieuDietBoiNguoiChoi = true;
            }
            Destroy(target.gameObject);
        }
        Destroy(gameObject);
    }
}