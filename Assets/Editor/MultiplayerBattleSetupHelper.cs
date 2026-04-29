using UnityEngine;
using UnityEditor;
using DoAnGame.UI;
using DoAnGame.Multiplayer;

/// <summary>
/// Helper tool để tự động gán references cho Multiplayer Battle System
/// </summary>
public class MultiplayerBattleSetupHelper : EditorWindow
{
    [MenuItem("Tools/Multiplayer Battle/Auto-Setup References")]
    public static void AutoSetupReferences()
    {
        if (!EditorUtility.DisplayDialog(
            "Auto-Setup Multiplayer Battle",
            "Tool này sẽ tự động tìm và gán references cho:\n\n" +
            "• UIMultiplayerBattleController\n" +
            "• MultiplayerHealthUI\n" +
            "• MultiplayerDragDropAdapter\n\n" +
            "Bạn có muốn tiếp tục?",
            "Có", "Không"))
        {
            return;
        }

        int fixedCount = 0;

        // 1. Setup UIMultiplayerBattleController
        var battleController = FindObjectOfType<UIMultiplayerBattleController>(true);
        if (battleController != null)
        {
            Debug.Log("=== SETTING UP UIMultiplayerBattleController ===");
            
            SerializedObject so = new SerializedObject(battleController);
            
            // Find BattleManager
            var battleManager = FindObjectOfType<NetworkedMathBattleManager>(true);
            if (battleManager != null)
            {
                so.FindProperty("battleManager").objectReferenceValue = battleManager;
                Debug.Log($"✅ Gán BattleManager: {battleManager.name}");
                fixedCount++;
            }

            // Find questionText
            var questionText = GameObject.Find("cauhoiText")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (questionText != null)
            {
                so.FindProperty("questionText").objectReferenceValue = questionText;
                Debug.Log($"✅ Gán questionText: {questionText.name}");
                fixedCount++;
            }

            // Find answerSlot
            var slot = GameObject.FindGameObjectWithTag("Slot");
            if (slot != null)
            {
                so.FindProperty("answerSlot").objectReferenceValue = slot;
                Debug.Log($"✅ Gán answerSlot: {slot.name}");
                fixedCount++;
            }

            // Find answerChoices
            var answer0Obj = GameObject.Find("Answer_0");
            var answer1Obj = GameObject.Find("Answer_1");
            var answer2Obj = GameObject.Find("Answer_2");
            var answer3Obj = GameObject.Find("Answer_3");

            if (answer0Obj != null && answer1Obj != null && answer2Obj != null && answer3Obj != null)
            {
                // Ensure each Answer has MultiplayerDragAndDrop component
                var answer0 = answer0Obj.GetComponent<MultiplayerDragAndDrop>();
                var answer1 = answer1Obj.GetComponent<MultiplayerDragAndDrop>();
                var answer2 = answer2Obj.GetComponent<MultiplayerDragAndDrop>();
                var answer3 = answer3Obj.GetComponent<MultiplayerDragAndDrop>();

                // Add MultiplayerDragAndDrop if missing
                if (answer0 == null)
                {
                    answer0 = answer0Obj.AddComponent<MultiplayerDragAndDrop>();
                    Debug.Log($"➕ Thêm MultiplayerDragAndDrop vào Answer_0");
                    
                    // Auto-assign myText
                    var text0 = answer0Obj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text0 != null)
                    {
                        SerializedObject so0 = new SerializedObject(answer0);
                        so0.FindProperty("myText").objectReferenceValue = text0;
                        so0.ApplyModifiedProperties();
                    }
                }
                if (answer1 == null)
                {
                    answer1 = answer1Obj.AddComponent<MultiplayerDragAndDrop>();
                    Debug.Log($"➕ Thêm MultiplayerDragAndDrop vào Answer_1");
                    
                    var text1 = answer1Obj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text1 != null)
                    {
                        SerializedObject so1 = new SerializedObject(answer1);
                        so1.FindProperty("myText").objectReferenceValue = text1;
                        so1.ApplyModifiedProperties();
                    }
                }
                if (answer2 == null)
                {
                    answer2 = answer2Obj.AddComponent<MultiplayerDragAndDrop>();
                    Debug.Log($"➕ Thêm MultiplayerDragAndDrop vào Answer_2");
                    
                    var text2 = answer2Obj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text2 != null)
                    {
                        SerializedObject so2 = new SerializedObject(answer2);
                        so2.FindProperty("myText").objectReferenceValue = text2;
                        so2.ApplyModifiedProperties();
                    }
                }
                if (answer3 == null)
                {
                    answer3 = answer3Obj.AddComponent<MultiplayerDragAndDrop>();
                    Debug.Log($"➕ Thêm MultiplayerDragAndDrop vào Answer_3");
                    
                    var text3 = answer3Obj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (text3 != null)
                    {
                        SerializedObject so3 = new SerializedObject(answer3);
                        so3.FindProperty("myText").objectReferenceValue = text3;
                        so3.ApplyModifiedProperties();
                    }
                }

                // Ensure required components
                EnsureRequiredComponents(answer0Obj);
                EnsureRequiredComponents(answer1Obj);
                EnsureRequiredComponents(answer2Obj);
                EnsureRequiredComponents(answer3Obj);

                // Assign to array
                var choicesProp = so.FindProperty("answerChoices");
                choicesProp.arraySize = 4;
                choicesProp.GetArrayElementAtIndex(0).objectReferenceValue = answer0;
                choicesProp.GetArrayElementAtIndex(1).objectReferenceValue = answer1;
                choicesProp.GetArrayElementAtIndex(2).objectReferenceValue = answer2;
                choicesProp.GetArrayElementAtIndex(3).objectReferenceValue = answer3;
                
                Debug.Log($"✅ Gán answerChoices: Answer_0/1/2/3 (MultiplayerDragAndDrop)");
                fixedCount += 4;
            }
            else
            {
                Debug.LogWarning("⚠️ Không tìm thấy đủ 4 Answer objects!");
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(battleController);
        }
        else
        {
            Debug.LogWarning("⚠️ Không tìm thấy UIMultiplayerBattleController!");
        }

        // 2. Setup MultiplayerHealthUI
        var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
        if (healthUI != null)
        {
            Debug.Log("\n=== SETTING UP MultiplayerHealthUI ===");
            
            SerializedObject so = new SerializedObject(healthUI);

            // Player 1
            var p1HealthFill = GameObject.Find("healFill1")?.GetComponent<UnityEngine.UI.Image>();
            var p1HealthText = GameObject.Find("healText 1")?.GetComponent<TMPro.TextMeshProUGUI>();
            var p1NameText = GameObject.Find("NamePL1")?.GetComponent<TMPro.TextMeshProUGUI>();
            var p1ScoreText = GameObject.Find("Player1Score")?.GetComponent<TMPro.TextMeshProUGUI>();

            if (p1HealthFill != null)
            {
                so.FindProperty("player1HealthFill").objectReferenceValue = p1HealthFill;
                Debug.Log($"✅ Gán player1HealthFill: {p1HealthFill.name}");
                fixedCount++;
            }
            if (p1HealthText != null)
            {
                so.FindProperty("player1HealthText").objectReferenceValue = p1HealthText;
                Debug.Log($"✅ Gán player1HealthText: {p1HealthText.name}");
                fixedCount++;
            }
            if (p1NameText != null)
            {
                so.FindProperty("player1NameText").objectReferenceValue = p1NameText;
                Debug.Log($"✅ Gán player1NameText: {p1NameText.name}");
                fixedCount++;
            }
            if (p1ScoreText != null)
            {
                so.FindProperty("player1ScoreText").objectReferenceValue = p1ScoreText;
                Debug.Log($"✅ Gán player1ScoreText: {p1ScoreText.name}");
                fixedCount++;
            }

            // Player 2
            var p2HealthFill = GameObject.Find("healFill2")?.GetComponent<UnityEngine.UI.Image>();
            var p2HealthText = GameObject.Find("healText 2")?.GetComponent<TMPro.TextMeshProUGUI>();
            var p2NameText = GameObject.Find("NamePL2")?.GetComponent<TMPro.TextMeshProUGUI>();
            var p2ScoreText = GameObject.Find("Player2Score")?.GetComponent<TMPro.TextMeshProUGUI>();

            if (p2HealthFill != null)
            {
                so.FindProperty("player2HealthFill").objectReferenceValue = p2HealthFill;
                Debug.Log($"✅ Gán player2HealthFill: {p2HealthFill.name}");
                fixedCount++;
            }
            if (p2HealthText != null)
            {
                so.FindProperty("player2HealthText").objectReferenceValue = p2HealthText;
                Debug.Log($"✅ Gán player2HealthText: {p2HealthText.name}");
                fixedCount++;
            }
            if (p2NameText != null)
            {
                so.FindProperty("player2NameText").objectReferenceValue = p2NameText;
                Debug.Log($"✅ Gán player2NameText: {p2NameText.name}");
                fixedCount++;
            }
            if (p2ScoreText != null)
            {
                so.FindProperty("player2ScoreText").objectReferenceValue = p2ScoreText;
                Debug.Log($"✅ Gán player2ScoreText: {p2ScoreText.name}");
                fixedCount++;
            }

            // Timer
            var timerText = GameObject.Find("TimerText")?.GetComponent<TMPro.TextMeshProUGUI>();
            var timerFill = GameObject.Find("TimerFill")?.GetComponent<UnityEngine.UI.Image>();

            if (timerText != null)
            {
                so.FindProperty("timerText").objectReferenceValue = timerText;
                Debug.Log($"✅ Gán timerText: {timerText.name}");
                fixedCount++;
            }
            if (timerFill != null)
            {
                so.FindProperty("timerFillImage").objectReferenceValue = timerFill;
                Debug.Log($"✅ Gán timerFillImage: {timerFill.name}");
                fixedCount++;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(healthUI);
        }
        else
        {
            Debug.LogWarning("⚠️ Không tìm thấy MultiplayerHealthUI!");
        }

        // Summary
        Debug.Log($"\n=== HOÀN THÀNH ===");
        Debug.Log($"✅ Đã gán {fixedCount} references!");
        Debug.Log($"💡 Multiplayer dùng MultiplayerDragAndDrop (không cần MultiplayerDragDropAdapter)");
        Debug.Log($"Nhớ Save scene (Ctrl+S)!");

        EditorUtility.DisplayDialog(
            "Hoàn thành!",
            $"Đã tự động gán {fixedCount} references!\n\n" +
            "✅ Dùng MultiplayerDragAndDrop (script riêng cho multiplayer)\n" +
            "✅ Không cần MultiplayerDragDropAdapter\n\n" +
            "Nhớ Save scene (Ctrl+S) để lưu thay đổi!",
            "OK");
    }

    [MenuItem("Tools/Multiplayer Battle/Validate Setup")]
    public static void ValidateSetup()
    {
        Debug.Log("=== VALIDATING MULTIPLAYER BATTLE SETUP ===\n");

        int errors = 0;
        int warnings = 0;

        // Check UIMultiplayerBattleController
        var battleController = FindObjectOfType<UIMultiplayerBattleController>(true);
        if (battleController == null)
        {
            Debug.LogError("❌ UIMultiplayerBattleController not found!");
            errors++;
        }
        else
        {
            SerializedObject so = new SerializedObject(battleController);
            
            if (so.FindProperty("battleManager").objectReferenceValue == null)
            {
                Debug.LogError("❌ BattleController.battleManager is NULL!");
                errors++;
            }
            if (so.FindProperty("questionText").objectReferenceValue == null)
            {
                Debug.LogError("❌ BattleController.questionText is NULL!");
                errors++;
            }
            if (so.FindProperty("answerSlot").objectReferenceValue == null)
            {
                Debug.LogError("❌ BattleController.answerSlot is NULL!");
                errors++;
            }
            
            var choices = so.FindProperty("answerChoices");
            if (choices.arraySize != 4)
            {
                Debug.LogError($"❌ BattleController.answerChoices size = {choices.arraySize} (expected 4)!");
                errors++;
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    if (choices.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        Debug.LogError($"❌ BattleController.answerChoices[{i}] is NULL!");
                        errors++;
                    }
                }
            }
        }

        // Check MultiplayerHealthUI
        var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
        if (healthUI == null)
        {
            Debug.LogWarning("⚠️ MultiplayerHealthUI not found!");
            warnings++;
        }
        else
        {
            SerializedObject so = new SerializedObject(healthUI);
            
            if (so.FindProperty("timerFillImage").objectReferenceValue == null)
            {
                Debug.LogWarning("⚠️ HealthUI.timerFillImage is NULL (timer sẽ không hoạt động)!");
                warnings++;
            }
        }

        // Summary
        Debug.Log($"\n=== VALIDATION SUMMARY ===");
        if (errors == 0 && warnings == 0)
        {
            Debug.Log("✅ TẤT CẢ ĐÃ ĐÚNG!");
        }
        else
        {
            Debug.Log($"❌ Errors: {errors}");
            Debug.Log($"⚠️ Warnings: {warnings}");
            Debug.Log("\nChạy 'Tools → Multiplayer Battle → Auto-Setup References' để tự động sửa!");
        }
    }

    /// <summary>
    /// Đảm bảo GameObject có đủ required components cho DragAndDrop
    /// </summary>
    private static void EnsureRequiredComponents(GameObject obj)
    {
        if (obj == null) return;

        // CanvasGroup
        if (obj.GetComponent<CanvasGroup>() == null)
        {
            obj.AddComponent<CanvasGroup>();
            Debug.Log($"➕ Thêm CanvasGroup vào {obj.name}");
        }

        // Image
        if (obj.GetComponent<UnityEngine.UI.Image>() == null)
        {
            obj.AddComponent<UnityEngine.UI.Image>();
            Debug.Log($"➕ Thêm Image vào {obj.name}");
        }

        // RectTransform (should already exist on UI objects)
        if (obj.GetComponent<RectTransform>() == null)
        {
            Debug.LogWarning($"⚠️ {obj.name} thiếu RectTransform! Đây phải là UI object.");
        }
    }
}
