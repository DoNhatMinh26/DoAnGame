using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script test để debug LeaderboardEntry
/// Gắn vào LeaderboardEntry prefab để test
/// </summary>
public class TestLeaderboardEntry : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== TEST LEADERBOARD ENTRY ===");
        
        // Tìm tất cả Image components
        var images = GetComponentsInChildren<Image>(true);
        Debug.Log($"Found {images.Length} Image components:");
        foreach (var img in images)
        {
            Debug.Log($"  - {img.gameObject.name}: Color={img.color}, Active={img.gameObject.activeSelf}");
        }
        
        // Tìm tất cả Text components
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        Debug.Log($"Found {texts.Length} Text components:");
        foreach (var txt in texts)
        {
            Debug.Log($"  - {txt.gameObject.name}: Text='{txt.text}', Active={txt.gameObject.activeSelf}");
        }
        
        // Kiểm tra LeaderboardEntryWidget
        var widget = GetComponent<DoAnGame.UI.LeaderboardEntryWidget>();
        if (widget != null)
        {
            Debug.Log("LeaderboardEntryWidget found!");
            
            // Test set data
            widget.SetData(1, "TestPlayer", 1000, 5);
            widget.SetHighlight(true);
            
            Debug.Log("Test data set successfully!");
        }
        else
        {
            Debug.LogError("LeaderboardEntryWidget NOT FOUND!");
        }
    }
}
