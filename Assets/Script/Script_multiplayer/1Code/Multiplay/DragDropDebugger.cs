using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;

/// <summary>
/// Debug tool để kiểm tra drag-drop system setup
/// Attach vào bất kỳ GameObject nào trong scene
/// </summary>
public class DragDropDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showGUI = true;
    [SerializeField] private bool logDragEvents = true;
    [SerializeField] private KeyCode debugKey = KeyCode.F5;

    private StringBuilder debugInfo = new StringBuilder();

    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            CheckDragDropSetup();
        }
    }

    [ContextMenu("Check Drag-Drop Setup")]
    public void CheckDragDropSetup()
    {
        debugInfo.Clear();
        debugInfo.AppendLine("=== DRAG-DROP SYSTEM DEBUG ===");
        debugInfo.AppendLine();

        // 1. Check DragAndDrop components
        var dragComponents = FindObjectsOfType<DragAndDrop>(true);
        var multiplayerDragComponents = FindObjectsOfType<DoAnGame.Multiplayer.MultiplayerDragAndDrop>(true);
        
        debugInfo.AppendLine($"--- DRAG COMPONENTS ---");
        debugInfo.AppendLine($"Single-player (DragAndDrop): {dragComponents.Length}");
        debugInfo.AppendLine($"Multiplayer (MultiplayerDragAndDrop): {multiplayerDragComponents.Length}");
        debugInfo.AppendLine();
        
        if (multiplayerDragComponents.Length > 0)
        {
            debugInfo.AppendLine("=== MULTIPLAYER MODE ===");
            foreach (var drag in multiplayerDragComponents)
            {
                debugInfo.AppendLine($"• {drag.gameObject.name}");
                debugInfo.AppendLine($"  - Active: {drag.gameObject.activeInHierarchy}");
                debugInfo.AppendLine($"  - Enabled: {drag.enabled}");
                debugInfo.AppendLine($"  - Has CanvasGroup: {drag.GetComponent<CanvasGroup>() != null}");
                debugInfo.AppendLine($"  - Has Image: {drag.GetComponent<UnityEngine.UI.Image>() != null}");
                debugInfo.AppendLine($"  - Has RectTransform: {drag.GetComponent<RectTransform>() != null}");
                
                if (drag.myText != null)
                {
                    debugInfo.AppendLine($"  - Text: \"{drag.myText.text}\"");
                }
                else
                {
                    debugInfo.AppendLine($"  - Text: NULL ❌");
                }
                debugInfo.AppendLine();
            }
        }
        else if (dragComponents.Length > 0)
        {
            debugInfo.AppendLine("=== SINGLE-PLAYER MODE ===");
            foreach (var drag in dragComponents)
            {
                debugInfo.AppendLine($"• {drag.gameObject.name}");
                debugInfo.AppendLine($"  - Active: {drag.gameObject.activeInHierarchy}");
                debugInfo.AppendLine($"  - Enabled: {drag.enabled}");
                
                if (drag.myText != null)
                {
                    debugInfo.AppendLine($"  - Text: \"{drag.myText.text}\"");
                }
                else
                {
                    debugInfo.AppendLine($"  - Text: NULL ❌");
                }

                // Check if locked
                var lockField = typeof(DragAndDrop).GetField("isLocked", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (lockField != null)
                {
                    bool isLocked = (bool)lockField.GetValue(null);
                    debugInfo.AppendLine($"  - Global Lock: {isLocked} {(isLocked ? "❌ LOCKED!" : "✅")}");
                }

                debugInfo.AppendLine();
            }
        }

        // 2. Check Slot (drop target)
        var slots = GameObject.FindGameObjectsWithTag("Slot");
        debugInfo.AppendLine($"--- SLOT OBJECTS ({slots.Length}) ---");
        
        foreach (var slot in slots)
        {
            debugInfo.AppendLine($"• {slot.name}");
            debugInfo.AppendLine($"  - Active: {slot.activeInHierarchy}");
            debugInfo.AppendLine($"  - Tag: {slot.tag}");
            debugInfo.AppendLine($"  - Has RectTransform: {slot.GetComponent<RectTransform>() != null}");
            
            // Check IDropHandler
            var dropHandlers = slot.GetComponents<IDropHandler>();
            debugInfo.AppendLine($"  - IDropHandler count: {dropHandlers.Length}");
            
            foreach (var handler in dropHandlers)
            {
                debugInfo.AppendLine($"    → {handler.GetType().Name}");
            }

            // Check MultiplayerDragDropAdapter
            var adapter = slot.GetComponent<DoAnGame.Multiplayer.MultiplayerDragDropAdapter>();
            if (adapter != null)
            {
                debugInfo.AppendLine($"  - Has MultiplayerDragDropAdapter: ✅");
                debugInfo.AppendLine($"    - Enabled: {adapter.enabled}");
            }
            else
            {
                debugInfo.AppendLine($"  - Has MultiplayerDragDropAdapter: ❌ MISSING!");
            }

            debugInfo.AppendLine();
        }

        // 3. Check EventSystem
        debugInfo.AppendLine("--- EVENT SYSTEM ---");
        var eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            debugInfo.AppendLine($"• EventSystem: FOUND");
            debugInfo.AppendLine($"  - Active: {eventSystem.gameObject.activeInHierarchy}");
            debugInfo.AppendLine($"  - Enabled: {eventSystem.enabled}");
            debugInfo.AppendLine($"  - Current: {EventSystem.current != null}");
        }
        else
        {
            debugInfo.AppendLine($"• EventSystem: NULL ❌");
        }
        debugInfo.AppendLine();

        // 4. Check Canvas
        debugInfo.AppendLine("--- CANVAS ---");
        var canvases = FindObjectsOfType<Canvas>(true);
        foreach (var canvas in canvases)
        {
            if (canvas.gameObject.name.Contains("Cauhoi") || canvas.gameObject.name.Contains("Gameplay"))
            {
                debugInfo.AppendLine($"• {canvas.gameObject.name}");
                debugInfo.AppendLine($"  - Active: {canvas.gameObject.activeInHierarchy}");
                debugInfo.AppendLine($"  - Render Mode: {canvas.renderMode}");
                debugInfo.AppendLine($"  - Has GraphicRaycaster: {canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null}");
                debugInfo.AppendLine();
            }
        }

        // 5. Check DragQuizManager (single-player)
        debugInfo.AppendLine("--- MANAGERS ---");
        var dragQuizManager = FindObjectOfType<DragQuizManager>();
        if (dragQuizManager != null)
        {
            debugInfo.AppendLine($"• DragQuizManager: FOUND (single-player mode)");
            debugInfo.AppendLine($"  - Active: {dragQuizManager.gameObject.activeInHierarchy}");
        }
        else
        {
            debugInfo.AppendLine($"• DragQuizManager: NULL (multiplayer mode ✅)");
        }

        var battleManager = FindObjectOfType<DoAnGame.Multiplayer.NetworkedMathBattleManager>(true);
        if (battleManager != null)
        {
            debugInfo.AppendLine($"• NetworkedMathBattleManager: FOUND");
            debugInfo.AppendLine($"  - Active: {battleManager.gameObject.activeInHierarchy}");
        }
        else
        {
            debugInfo.AppendLine($"• NetworkedMathBattleManager: NULL ❌");
        }
        debugInfo.AppendLine();

        // 6. Recommendations
        debugInfo.AppendLine("--- RECOMMENDATIONS ---");
        
        bool hasIssues = false;

        if (dragComponents.Length == 0)
        {
            debugInfo.AppendLine("❌ No DragAndDrop components found!");
            hasIssues = true;
        }

        if (slots.Length == 0)
        {
            debugInfo.AppendLine("❌ No Slot objects found! Make sure Slot has tag 'Slot'");
            hasIssues = true;
        }

        foreach (var slot in slots)
        {
            var adapter = slot.GetComponent<DoAnGame.Multiplayer.MultiplayerDragDropAdapter>();
            if (adapter == null)
            {
                debugInfo.AppendLine($"❌ Slot '{slot.name}' missing MultiplayerDragDropAdapter!");
                hasIssues = true;
            }
        }

        if (eventSystem == null)
        {
            debugInfo.AppendLine("❌ EventSystem not found! Add one to scene.");
            hasIssues = true;
        }

        // Check global lock
        var lockField2 = typeof(DragAndDrop).GetField("isLocked", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (lockField2 != null)
        {
            bool isLocked = (bool)lockField2.GetValue(null);
            if (isLocked)
            {
                debugInfo.AppendLine("❌ DragAndDrop is GLOBALLY LOCKED! Call DragAndDrop.SetGlobalLock(false)");
                hasIssues = true;
            }
        }

        if (!hasIssues)
        {
            debugInfo.AppendLine("✅ All checks passed!");
        }

        debugInfo.AppendLine();
        debugInfo.AppendLine("=== END DEBUG ===");

        Debug.Log(debugInfo.ToString());

        // Export to file
        string filePath = System.IO.Path.Combine(Application.dataPath, "..", "DragDropDebugReport.txt");
        System.IO.File.WriteAllText(filePath, debugInfo.ToString());
        Debug.Log($"📄 Debug report exported to: {filePath}");
    }

    private void OnGUI()
    {
        if (!showGUI) return;

        GUILayout.BeginArea(new Rect(Screen.width - 310, 10, 300, 400));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== DRAG-DROP DEBUGGER ===", GUI.skin.box);
        GUILayout.Space(5);

        // Check global lock
        var lockField = typeof(DragAndDrop).GetField("isLocked", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (lockField != null)
        {
            bool isLocked = (bool)lockField.GetValue(null);
            
            if (isLocked)
            {
                GUI.color = Color.red;
                GUILayout.Label("❌ DRAG-DROP LOCKED!");
                GUI.color = Color.white;
                
                if (GUILayout.Button("Unlock Drag-Drop"))
                {
                    DragAndDrop.SetGlobalLock(false);
                    Debug.Log("✅ Unlocked drag-drop!");
                }
            }
            else
            {
                GUI.color = Color.green;
                GUILayout.Label("✅ Drag-Drop Unlocked");
                GUI.color = Color.white;
            }
        }

        GUILayout.Space(10);

        // Quick stats
        var dragComponents = FindObjectsOfType<DragAndDrop>(true);
        var slots = GameObject.FindGameObjectsWithTag("Slot");
        
        GUILayout.Label($"Drag Components: {dragComponents.Length}");
        GUILayout.Label($"Slot Objects: {slots.Length}");
        
        var eventSystem = FindObjectOfType<EventSystem>();
        GUILayout.Label($"EventSystem: {(eventSystem != null ? "✅" : "❌")}");

        GUILayout.Space(10);

        if (GUILayout.Button($"Full Check ({debugKey})"))
        {
            CheckDragDropSetup();
        }

        if (GUILayout.Button("Force Unlock All"))
        {
            DragAndDrop.SetGlobalLock(false);
            DragAndDrop.ReleaseAllLocks();
            
            foreach (var drag in dragComponents)
            {
                drag.ForceResetPosition();
            }
            
            Debug.Log("✅ Force unlocked all drag-drop!");
        }

        GUILayout.Space(5);
        GUILayout.Label($"Press {debugKey} for full report", GUI.skin.box);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
