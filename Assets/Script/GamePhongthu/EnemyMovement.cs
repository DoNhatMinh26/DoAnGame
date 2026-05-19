using UnityEngine;
using UnityEngine.UI;

public class EnemyMovement : MonoBehaviour
{
    public float speed = 2.0f;
    public float damage = 10f; // Sát thương của con chuột
    private bool isAttacking = false;
    public bool biTieuDietBoiNguoiChoi = false;

    [Header("Cài đặt rơi tiền")]
    public GameObject coinPrefab;

    [Header("Cài đặt Nhịp đánh")]
    [Tooltip("Khoảng thời gian giữa các lần vung gậy đánh tiếp theo (giây)")]
    public float mauSau = 2f;

    // Khai báo biến để lưu Coroutine đánh, giúp dọn dẹp khi quái chết
    private Coroutine attackCoroutine;

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

            // Kích hoạt vòng lặp vung gậy bằng Coroutine
            attackCoroutine = StartCoroutine(VongLapTanCongRoutine());
        }
    }

    // Coroutine này BÂY GIỜ CHỈ điều khiển nhịp vung gậy (Animation)
    private System.Collections.IEnumerator VongLapTanCongRoutine()
    {
        // --- LẦN ĐẦU TIÊN CHẠM TƯỜNG ---
        // Vung gậy đánh luôn phát đầu tiên không cần đợi
        YeuCauVungGayTanCong();

        // --- CÁC LẦN TIẾP THEO ---
        while (true)
        {
            // Đợi tiếp 2 giây (mauSau) rồi mới vung gậy phát tiếp theo
            yield return new WaitForSeconds(mauSau);

            YeuCauVungGayTanCong();
        }
    }

    // Hàm chỉ làm đúng 1 nhiệm vụ: Bật hoạt họa vung gậy
    void YeuCauVungGayTanCong()
    {
        if (biTieuDietBoiNguoiChoi) return;

        // Kích hoạt hiệu ứng hoạt họa đánh (Animation Attack) ngay lập tức
        if (TryGetComponent(out Animator anim))
        {
            anim.SetTrigger("TriggerDanh");
        }
    }

    // HÀM QUAN TRỌNG: Bạn cắm hàm này vào Animation Event trên cửa sổ Animation
    // Đúng khung hình cái gậy đập trúng thành, Unity sẽ gọi hàm này để trừ máu
    public void SatThuongThucTeTuAnimationEvent()
    {
        // Nếu quái đã bị bắn chết trong lúc đang vung gậy thì không trừ máu thành nữa
        if (biTieuDietBoiNguoiChoi) return;

        // Tiến hành trừ máu thành
        if (WallHealth.Instance != null)
        {
            WallHealth.Instance.TakeDamage(damage);
        }
    }

    private void OnDestroy()
    {
        // Dọn dẹp Coroutine khi Object bị hủy để tránh lỗi ngầm
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

    }

    void SpawnCoins()
    {
        if (coinPrefab != null)
        {
            Instantiate(coinPrefab, transform.position, Quaternion.identity);
        }
    }

    public void ThucHienHieuUngChet()
    {
        // Nếu đã bị tính là chết trước đó rồi thì bỏ qua để tránh trùng lặp phần thưởng
        if (biTieuDietBoiNguoiChoi) return;

        biTieuDietBoiNguoiChoi = true;
        isAttacking = true;
        speed = 0;

        // Dừng hoàn toàn Coroutine nhịp vung gậy cắn thành
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }

        // Tắt toàn bộ va chạm ngay lập tức để đạn khác không bắn trúng xác quái nữa
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in allColliders)
        {
            col.enabled = false;
        }

        // ===================================================================
        // 🔥 ĐƯA LOGIC PHẦN THƯỞNG LÊN ĐÂY ĐỂ KÍCH HOẠT NGAY KHI ĐẠN VỪA TRÚNG
        // ===================================================================
        if (!AppQuitting.isQuitting && coinPrefab != null)
        {
            SpawnCoins(); // Rơi tiền ngay lập tức
        }

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.OnEnemyKilled(); // Cộng số lượng chuột chết trên UI ngay
            GameUIManager.Instance.AddScore(10);    // Cộng 10 điểm ngay lập tức
        }
        // ===================================================================

        // Vẫn chạy Animation chết mượt mà ngầm dưới nền trước khi hủy Object hẳn
        StartCoroutine(KichHoatChetRoutine());
    }

    private System.Collections.IEnumerator KichHoatChetRoutine()
    {
        if (TryGetComponent(out Animator anim))
        {
            anim.SetTrigger("TriggerChet");
        }

        yield return new WaitForSeconds(1.2f);
        Destroy(gameObject);
    }
}