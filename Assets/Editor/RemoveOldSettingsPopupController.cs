using UnityEngine;
using UnityEditor;
using DoAnGame.UI;

/// <summary>
/// Tool xóa SettingsPopupController cũ trên GameUICanvas
/// Menu: Tools → Remove Old Settings Popup Controller
/// </summary>
public class RemoveOldSettingsPopupController : EditorWindow
{
    [MenuItem("Tools/Remove Old Settings Popup Controller")]
    public static void RemoveOldController()
    {
        // Tìm GameUICanvas
        GameObject canvas = GameObject.Find("GameUICanvas");
        if (canvas == null)
        {
            Debug.LogError("[RemoveOld] GameUICanvas not found!");
            EditorUtility.DisplayDialog("Error", "GameUICanvas not found in scene!", "OK");
            return;
        }

        // Tìm component SettingsPopupController trên canvas
        var oldController = canvas.GetComponent<SettingsPopupController>();
        if (oldController == null)
        {
            Debug.Log("[RemoveOld] No SettingsPopupController found on GameUICanvas");
            EditorUtility.DisplayDialog("Info", "No old controller found on GameUICanvas", "OK");
            return;
        }

        // Xóa component
        DestroyImmediate(oldController);
        EditorUtility.SetDirty(canvas);
        
        Debug.Log("[RemoveOld] ✅ Removed SettingsPopupController from GameUICanvas");
        EditorUtility.DisplayDialog("Success", "Removed old SettingsPopupController from GameUICanvas!", "OK");
    }
}
