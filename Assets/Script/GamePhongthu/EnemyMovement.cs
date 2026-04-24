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
        if (collision.CompareTag("Tuong")) // Chạm vào tường thành
        {
            isAttacking = true;
            speed = 0;

            // Gọi hàm trừ máu của thành
            if (WallHealth.Instance != null)
            {
                WallHealth.Instance.TakeDamage(damage);
            }

            // Sau khi tấn công xong thì kẻ địch tự hủy (hoặc bạn có thể dùng InvokeRepeating để đánh liên tục)
            Destroy(gameObject, 0.5f);
        }
    }
}