using UnityEngine;
using System.Collections;

public class CoinSpace : MonoBehaviour
{
    private bool isCollected = false;
    public float flySpeed = 20f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra nếu chạm vào phi thuyền (Player)
        if (!isCollected && other.CompareTag("Player"))
        {
            isCollected = true;
            GetComponent<Collider2D>().enabled = false; // Tắt va chạm để tránh nhặt 2 lần

            // Tách khỏi cha (Portal) để không bị xóa khi Portal trôi quá xa
            transform.SetParent(null);

            StartCoroutine(FlyToUI());
        }
    }

    private IEnumerator FlyToUI()
    {
        if (UiSp.Instance != null && UiSp.Instance.coinTarget != null)
        {
            Transform target = UiSp.Instance.coinTarget;

            // Hiệu ứng bay về phía Icon tiền trên màn hình
            while (Vector3.Distance(transform.position, target.position) > 0.5f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target.position, flySpeed * Time.deltaTime);
                yield return null;
            }

            // Cộng tiền khi đã bay tới nơi
            UiSp.Instance.AddCoins(1);
        }

        Destroy(gameObject);
    }
}