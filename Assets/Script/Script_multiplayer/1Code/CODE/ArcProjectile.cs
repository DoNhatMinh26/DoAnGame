using System.Collections;
using UnityEngine;

public class ArcProjectile : MonoBehaviour
{
    private Vector3 startPoint;
    private Vector3 endPoint;
    private Vector3 impactPoint;
    private Vector3 controlPoint;
    private float duration;
    private float spinSpeed;
    private GameObject hitVfxPrefab;
    private Coroutine flightRoutine;

    private void OnEnable()
    {
        Debug.Log($"[ArcProjectile] OnEnable: name={gameObject.name}, layer={gameObject.layer}, parent={(transform.parent != null ? transform.parent.name : "NULL")}");

        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[ArcProjectile] {gameObject.name}: SpriteRenderer missing");
            return;
        }

        Debug.Log($"[ArcProjectile] {gameObject.name}: sprite={(spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "NULL")}, enabled={spriteRenderer.enabled}, sortingLayerID={spriteRenderer.sortingLayerID}, orderInLayer={spriteRenderer.sortingOrder}, size={spriteRenderer.size}, color={spriteRenderer.color}");
    }

    public void Launch(Vector3 start, Vector3 end, Vector3 impact, float arcHeight, float flightTime, float spinDegreesPerSecond, GameObject hitVfx)
    {
        startPoint = start;
        endPoint = end;
        impactPoint = impact;
        duration = Mathf.Max(0.01f, flightTime);
        spinSpeed = spinDegreesPerSecond;
        hitVfxPrefab = hitVfx;

        float travelDistance = Vector3.Distance(startPoint, endPoint);
        float dynamicArcHeight = arcHeight + (travelDistance * 0.25f);
        controlPoint = (startPoint + endPoint) * 0.5f + Vector3.up * dynamicArcHeight;
        transform.position = startPoint;

        Debug.Log($"[ArcProjectile] Launch: start={startPoint}, end={endPoint}, impact={impactPoint}, control={controlPoint}, duration={duration}, arcHeight={arcHeight}, dynamicArcHeight={dynamicArcHeight}, spin={spinSpeed}, parent={(transform.parent != null ? transform.parent.name : "NULL")}");

        if (flightRoutine != null)
        {
            StopCoroutine(flightRoutine);
        }

        flightRoutine = StartCoroutine(FlyRoutine());
    }

    private IEnumerator FlyRoutine()
    {
        float elapsed = 0f;
        bool loggedFirstFrame = false;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = GetQuadraticBezier(startPoint, controlPoint, endPoint, t);

            if (!loggedFirstFrame)
            {
                Debug.Log($"[ArcProjectile] First frame pos={transform.position}, t={t}");
                loggedFirstFrame = true;
            }

            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPoint;

        if (hitVfxPrefab != null)
        {
            Debug.Log($"[ArcProjectile] Impact VFX spawn at {impactPoint}: {hitVfxPrefab.name}");
            Instantiate(hitVfxPrefab, impactPoint, Quaternion.identity);
        }

        Debug.Log($"[ArcProjectile] Destroy projectile at {endPoint}");

        Destroy(gameObject);
    }

    private static Vector3 GetQuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        Vector3 ab = Vector3.Lerp(a, b, t);
        Vector3 bc = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(ab, bc, t);
    }
}
