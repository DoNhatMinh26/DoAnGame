using UnityEngine;

/// <summary>
/// Tool để xóa toàn bộ PlayerPrefs (session, cache)
/// Chạy từ Unity Editor: GameObject → Create Empty → Add Component → ClearPlayerPrefs
/// Hoặc: Tools → Clear All PlayerPrefs
/// </summary>
public class ClearPlayerPrefs : MonoBehaviour
{
    [ContextMenu("Clear All PlayerPrefs")]
    public void ClearAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("✅ Đã xóa toàn bộ PlayerPrefs!");
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Clear All PlayerPrefs")]
    public static void ClearAllPlayerPrefsMenu()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("✅ Đã xóa toàn bộ PlayerPrefs!");
    }
#endif
}
