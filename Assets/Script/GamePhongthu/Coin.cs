using UnityEngine;
using System.Collections;

public class Coin : MonoBehaviour
{
    private bool isMoving = false;
    public float speed = 8f;
    public float delayBeforeFly = 0.5f;

    void Start()
    {
        // Hiệu ứng văng nhẹ khi rơi
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 pushDir = new Vector2(Random.Range(-2f, 2f), Random.Range(3f, 6f));
            rb.AddForce(pushDir, ForceMode2D.Impulse);
        }

        StartCoroutine(WaitAndFly());
    }

    IEnumerator WaitAndFly()
    {
        yield return new WaitForSeconds(delayBeforeFly);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false; // Tắt vật lý để bay thẳng

        isMoving = true;
    }

    void Update()
    {
        if (isMoving && GameUIManager.Instance != null && GameUIManager.Instance.coinTarget != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            // 1. Lấy vị trí tọa độ màn hình của mục tiêu UI
            // Dùng Camera.WorldToScreenPoint để lấy tọa độ Pixel chính xác của coinTarget
            Vector3 screenPos = mainCam.WorldToScreenPoint(GameUIManager.Instance.coinTarget.position);

            // 2. Thiết lập Z bằng Plane Distance của Canvas để đồng nhất không gian
            // Theo ảnh của bạn, Plane Distance đang là 100
            screenPos.z = 100f;

            // 3. Chuyển ngược về vị trí thế giới mà đồng xu có thể hiểu được
            Vector3 targetWorldPos = mainCam.ScreenToWorldPoint(screenPos);

            // 4. Di chuyển mượt mà
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);

            // 5. Kiểm tra va chạm để cộng tiền
            if (Vector2.Distance(transform.position, targetWorldPos) < 0.3f)
            {
                GameUIManager.Instance.AddCoins(1);
                Destroy(gameObject);
            }
        }
    }
}