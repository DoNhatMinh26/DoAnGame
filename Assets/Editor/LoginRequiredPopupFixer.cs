using UnityEngine;
using UnityEditor;
using DoAnGame.UI;

/// <summary>
/// Tool tự động fix các vấn đề phổ biến với LoginRequiredPopup
/// Menu: Tools → Fix Login Required Popup
/// </summary>
public class LoginRequiredPopupFixer : EditorWindow
{
    [MenuItem("Tools/Fix Login Required Popup")]
    public static void ShowWindow()
    {
        GetWindow<LoginRequiredPopupFixer>("Popup Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Login Required Popup Fixer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("1. Find Popup in Scene", GUILayout.Height(30)))
        {
            FindPopup();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("2. Fix Hierarchy Order", GUILayout.Height(30)))
        {
            FixHierarchyOrder();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("3. Fix RectTransform", GUILayout.Height(30)))
        {
            FixRectTransform();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("4. Activate All Children", GUILayout.Height(30)))
        {
            ActivateAllChildren();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("5. Link to ModSelectionPanel", GUILayout.Height(30)))
        {
            LinkToModSelectionPanel();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("🔧 FIX ALL", GUILayout.Height(40)))
        {
            FixAll();
        }
    }

    private static void FindPopup()
    {
        var popup = GameObject.Find("LoginRequiredPopup");
        if (popup == null)
        {
            Debug.LogError("[PopupFixer] LoginRequiredPopup not found in scene!");
            EditorUtility.DisplayDialog("Error", "LoginRequiredPopup not found in scene!", "OK");
            return;
        }

        Selection.activeGameObject = popup;
        EditorGUIUtility.PingObject(popup);
        Debug.Log($"[PopupFixer] Found popup: {popup.name}");
    }

    private static void FixHierarchyOrder()
    {
        var popup = GameObject.Find("LoginRequiredPopup");
        if (popup == null)
        {
            Debug.LogError("[PopupFixer] LoginRequiredPopup not found!");
            return;
        }

        // Đưa popup xuống cuối cùng
        popup.transform.SetAsLastSibling();
        
        EditorUtility.SetDirty(popup);
        Debug.Log($"[PopupFixer] Moved popup to last sibling (index: {popup.transform.GetSiblingIndex()})");
    }

    private static void FixRectTransform()
    {
        var popup = GameObject.Find("LoginRequiredPopup");
        if (popup == null)
        {
            Debug.LogError("[PopupFixer] LoginRequiredPopup not found!");
            return;
        }

        var rect = popup.GetComponent<RectTransform>();
        if (rect == null)
        {
            Debug.LogError("[PopupFixer] RectTransform not found!");
            return;
        }

        // Set to stretch full-screen
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;

        EditorUtility.SetDirty(popup);
        Debug.Log("[PopupFixer] Fixed RectTransform to full-screen stretch");
    }

    private static void ActivateAllChildren()
    {
        var popup = GameObject.Find("LoginRequiredPopup");
        if (popup == null)
        {
            Debug.LogError("[PopupFixer] LoginRequiredPopup not found!");
            return;
        }

        int count = 0;
        foreach (Transform child in popup.transform)
        {
            if (!child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
                count++;
                Debug.Log($"[PopupFixer] Activated: {child.name}");
            }
        }

        EditorUtility.SetDirty(popup);
        Debug.Log($"[PopupFixer] Activated {count} children");
    }

    private static void LinkToModSelectionPanel()
    {
        var popup = GameObject.Find("LoginRequiredPopup");
        if (popup == null)
        {
            Debug.LogError("[PopupFixer] LoginRequiredPopup not found!");
            return;
        }

        var popupController = popup.GetComponent<UILoginRequiredPopupController>();
        if (popupController == null)
        {
            Debug.LogError("[PopupFixer] UILoginRequiredPopupController component not found!");
            return;
        }

        var modPanel = GameObject.Find("ModSelectionPanel");
        if (modPanel == null)
        {
            Debug.LogError("[PopupFixer] ModSelectionPanel not found!");
            return;
        }

        var modController = modPanel.GetComponent<UIModSelectionPanelController>();
        if (modController == null)
        {
            Debug.LogError("[PopupFixer] UIModSelectionPanelController component not found!");
            return;
        }

        // Gán popup vào ModSelectionPanel
        SerializedObject so = new SerializedObject(modController);
        SerializedProperty popupProp = so.FindProperty("loginRequiredPopup");
        if (popupProp != null)
        {
            popupProp.objectReferenceValue = popupController;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(modPanel);
            Debug.Log("[PopupFixer] Linked popup to ModSelectionPanel");
        }
        else
        {
            Debug.LogError("[PopupFixer] Cannot find 'loginRequiredPopup' field!");
        }
    }

    private static void FixAll()
    {
        Debug.Log("=== [PopupFixer] Starting full fix ===");
        
        FindPopup();
        FixHierarchyOrder();
        FixRectTransform();
        ActivateAllChildren();
        LinkToModSelectionPanel();
        
        Debug.Log("=== [PopupFixer] Fix complete! ===");
        EditorUtility.DisplayDialog("Success", "All fixes applied!\nCheck Console for details.", "OK");
    }
}
