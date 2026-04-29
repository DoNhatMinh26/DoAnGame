using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static Enemy Instance;

    [Header("Cấu hình vị trí")]
    [SerializeField] private float baseDistanceX = -15f; // Khoảng cách ban đầu (ngoài màn hình bên trái)
    [SerializeField] private float approachStep = 4f;    // Mỗi lần sai tiến lại gần thêm 4 đơn vị
    [SerializeField] private float moveSpeed = 5f;       // Tốc độ di chuyển mượt mà của kẻ địch

    private int saiCount = 0;
    private Vector3 targetPosition;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Đặt vị trí ban đầu cho kẻ địch
        ResetPosition();
    }

    private void Update()
    {
        // Di chuyển mượt mà tới vị trí mục tiêu thay vì nhảy cóc
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
    }

    // Hàm gọi khi người chơi trả lời sai
    public void MoveCloser()
    {
        saiCount++;

        // Tính toán vị trí X mới tiến dần về phía phi thuyền người chơi
        float newX = baseDistanceX + (saiCount * approachStep);
        targetPosition = new Vector3(newX, transform.localPosition.y, transform.localPosition.z);

        Debug.Log("Kẻ địch đang áp sát! Lần sai: " + saiCount);

        // Kiểm tra điều kiện thua cuộc (3 lần sai)
        if (saiCount >= 3)
        {
            // Gọi Coroutine để đợi 2 giây rồi mới hiện bảng thua
            StartCoroutine(WaitAndShowLose(2f));
        }
    }

    // Coroutine đợi x giây rồi gọi bảng thua từ UiSp
    private System.Collections.IEnumerator WaitAndShowLose(float delay)
    {
        // Thông báo cho người chơi biết họ đã bị bắt
        if (UiSp.Instance != null)
        {
            UiSp.Instance.ShowShopNotification("Cảnh báo: Kẻ địch đã bắt kịp!");
        }

        // Đợi 2 giây thực tế (không bị ảnh hưởng bởi Time.timeScale nếu có)
        yield return new WaitForSecondsRealtime(delay);

        // Sau khi đợi xong mới gọi hàm hiện bảng thua
        if (UiSp.Instance != null)
        {
            UiSp.Instance.ShowLose();
        }
    }

    // Reset lại vị trí khi bắt đầu màn mới hoặc chơi lại
    public void ResetPosition()
    {
        saiCount = 0;
        targetPosition = new Vector3(baseDistanceX, transform.localPosition.y, transform.localPosition.z);
        transform.localPosition = targetPosition;
    }
}