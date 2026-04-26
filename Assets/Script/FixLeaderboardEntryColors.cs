using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Script tự động fix màu sắc cho LeaderboardEntry
/// Chạy từ menu: Tools → Fix Leaderboard Entry Colors
/// </summary>
public class FixLeaderboardEntryColors : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Fix Leaderboard Entry Colors")]
    public static void FixColors()
    {
        // Tìm LeaderboardEntry trong scene
        var entries = FindObjectsOfType<DoAnGame.UI.LeaderboardEntryWidget>(true);
        
        if (entries.Length == 0)
        {
            Debug.LogWarning("Không tìm thấy LeaderboardEntryWidget nào trong scene!");
            Debug.Log("Hãy mở scene có LeaderboardEntry hoặc mở prefab LeaderboardEntry");
            return;
        }
        
        Debug.Log($"Tìm thấy {entries.Length} LeaderboardEntry, đang fix...");
        
        foreach (var entry in entries)
        {
            FixEntry(entry.gameObject);
        }
        
        Debug.Log("✅ Đã fix xong! Nhớ lưu scene/prefab (Ctrl+S)");
    }
    
    private static void FixEntry(GameObject entryObj)
    {
        Debug.Log($"Fixing: {entryObj.name}");
        
        // Fix Background
        Transform bgTransform = entryObj.transform.Find("Background");
        if (bgTransform != null)
        {
            Image bgImage = bgTransform.GetComponent<Image>();
            if (bgImage != null)
            {
                // Set màu trắng mờ
                bgImage.color = new Color(1f, 1f, 1f, 0.1f); // Alpha = 0.1 (10%)
                
                // Đảm bảo có sprite (dùng sprite trắng mặc định)
                if (bgImage.sprite == null)
                {
                    // Tạo sprite trắng 1x1
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    bgImage.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                }
                
                bgTransform.gameObject.SetActive(true);
                Debug.Log("  ✅ Fixed Background");
            }
        }
        else
        {
            Debug.LogWarning("  ⚠️ Background not found!");
        }
        
        // Fix HighlightBorder
        Transform borderTransform = entryObj.transform.Find("HighlightBorder");
        if (borderTransform != null)
        {
            Image borderImage = borderTransform.GetComponent<Image>();
            if (borderImage != null)
            {
                // Set màu vàng
                borderImage.color = new Color(1f, 0.84f, 0f, 1f); // Vàng gold
                
                // Đảm bảo có sprite
                if (borderImage.sprite == null)
                {
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, Color.white);
                    tex.Apply();
                    borderImage.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                }
                
                // Tắt border (sẽ bật khi cần)
                borderTransform.gameObject.SetActive(false);
                Debug.Log("  ✅ Fixed HighlightBorder");
            }
        }
        else
        {
            Debug.LogWarning("  ⚠️ HighlightBorder not found!");
        }
        
        // Fix Text colors
        FixTextColor(entryObj, "RankText", Color.white);
        FixTextColor(entryObj, "NameText", Color.white);
        FixTextColor(entryObj, "ScoreText", new Color(1f, 0.84f, 0f, 1f)); // Vàng
        FixTextColor(entryObj, "LevelText", new Color(0f, 1f, 0.5f, 1f)); // Xanh lá
        
        // Mark dirty để Unity save changes
        EditorUtility.SetDirty(entryObj);
    }
    
    private static void FixTextColor(GameObject parent, string childName, Color color)
    {
        Transform textTransform = parent.transform.Find(childName);
        if (textTransform != null)
        {
            var tmp = textTransform.GetComponent<TMPro.TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = color;
                textTransform.gameObject.SetActive(true);
                Debug.Log($"  ✅ Fixed {childName}");
            }
        }
        else
        {
            Debug.LogWarning($"  ⚠️ {childName} not found!");
        }
    }
#endif
}
