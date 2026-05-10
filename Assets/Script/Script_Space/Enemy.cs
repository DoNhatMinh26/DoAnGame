using UnityEngine;

public class Enemy : MonoBehaviour
{
    public static Enemy Instance;

    [Header("Cấu hình vị trí áp sát")]
    [SerializeField] private float baseDistanceX = -15f;
    [SerializeField] private float approachStep = 4f;
    [SerializeField] private float moveSpeedX = 5f; // Tốc độ áp sát theo trục X

    [Header("Cấu hình bay lơ lửng (Lên xuống)")]
    [SerializeField] private float floatAmplitude = 1.5f; // Độ cao bay lên xuống
    [SerializeField] private float floatFrequency = 2f;   // Tốc độ bay lên xuống nhanh hay chậm

    private int saiCount = 0;
    private float targetX; // Chỉ lưu mục tiêu trục X
    private float startY;  // Lưu vị trí Y ban đầu để dao động quanh đó

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        startY = transform.localPosition.y; // Lấy vị trí Y gốc
        ResetPosition();
    }

    private void Update()
    {
        // 1. Tính toán vị trí X mượt mà (áp sát khi trả lời sai)
        float newX = Mathf.Lerp(transform.localPosition.x, targetX, moveSpeedX * Time.deltaTime);

        // 2. Tính toán vị trí Y dao động hình Sin (bay lên xuống)
        float newY = startY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;

        // 3. Cập nhật vị trí mới
        transform.localPosition = new Vector3(newX, newY, transform.localPosition.z);
    }

    public void MoveCloser()
    {
        saiCount++;
        targetX = baseDistanceX + (saiCount * approachStep);

        Debug.Log("Kẻ địch đang áp sát! Lần sai: " + saiCount);

        if (saiCount >= 3)
        {
            StartCoroutine(WaitAndShowLose(2f));
        }
    }

    private System.Collections.IEnumerator WaitAndShowLose(float delay)
    {
        if (UiSp.Instance != null)
        {
            UiSp.Instance.ShowShopNotification("Cảnh báo: Kẻ địch đã bắt kịp!");
        }

        yield return new WaitForSecondsRealtime(delay);

        if (UiSp.Instance != null)
        {
            UiSp.Instance.ShowLose();
        }
    }

    public void ResetPosition()
    {
        saiCount = 0;
        targetX = baseDistanceX;
        // Reset X ngay lập tức, Y sẽ tự chạy theo Sin trong Update
        transform.localPosition = new Vector3(targetX, startY, transform.localPosition.z);
    }
}