using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Multiplayer;

/// <summary>
/// Script để FORCE ENABLE raycastTarget trên Answer objects
/// Chạy SAU KHI các script khác đã disable
/// Attach vào GameplayPanel
/// </summary>
public class ForceEnableAnswerRaycast : MonoBehaviour
{
    [SerializeField] private float checkInterval = 0.5f;
    private float nextCheckTime;

    private void Update()
    {
        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;

        // Find all Answer objects
        var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
        
        foreach (var answer in answers)
        {
            var image = answer.GetComponent<Image>();
            if (image != null && !image.raycastTarget)
            {
                image.raycastTarget = true;
                Debug.Log($"[ForceEnableAnswerRaycast] Re-enabled raycastTarget on {answer.name}");
            }
        }
    }
}
