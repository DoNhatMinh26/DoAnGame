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

        if (closest != null)
        {
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            bullet.GetComponent<DanVaCham>().SetTarget(closest.transform);
        }
    }
}