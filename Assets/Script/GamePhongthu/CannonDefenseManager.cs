using UnityEngine;

public class CannonDefenseManager : MonoBehaviour
{
    public static CannonDefenseManager Instance;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public string enemyTag = "Enemy";

    void Awake() => Instance = this;

    public void FireAtClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = enemy;
            }
        }

        // THAY ĐỔI: Luôn tạo đạn khi chọn đúng, bất kể có quái hay chưa
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        DanVaCham scriptDan = bullet.GetComponent<DanVaCham>();

        if (closest != null)
        {
            // Có quái thì bắn ngay
            scriptDan.SetTarget(closest.transform);
        }
        else
        {
            // Chưa có quái thì truyền null để viên đạn vào trạng thái chờ (isWaitingForTarget)
            scriptDan.SetTarget(null);
        }
    }
}