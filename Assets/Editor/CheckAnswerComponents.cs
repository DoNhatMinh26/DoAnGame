using UnityEngine;
using UnityEditor;
using DoAnGame.Multiplayer;

/// <summary>
/// Tool đơn giản để kiểm tra Answer objects có component MultiplayerDragAndDrop chưa
/// </summary>
public class CheckAnswerComponents : EditorWindow
{
    [MenuItem("Tools/Multiplayer Battle/Check Answer Components")]
    public static void CheckAnswers()
    {
        Debug.Log("=== CHECKING ANSWER COMPONENTS ===\n");

        string[] answerNames = { "Answer_0", "Answer_1", "Answer_2", "Answer_3" };
        int foundCount = 0;
        int hasComponentCount = 0;

        foreach (string name in answerNames)
        {
            GameObject obj = GameObject.Find(name);
            
            if (obj == null)
            {
                Debug.LogError($"❌ {name}: KHÔNG TÌM THẤY trong scene!");
                continue;
            }

            foundCount++;
            Debug.Log($"✅ {name}: Tìm thấy");

            // Check MultiplayerDragAndDrop
            var multiDrag = obj.GetComponent<MultiplayerDragAndDrop>();
            if (multiDrag != null)
            {
                hasComponentCount++;
                Debug.Log($"   ✅ Có MultiplayerDragAndDrop component");
                
                // Check myText
                if (multiDrag.myText != null)
                {
                    Debug.Log($"   ✅ myText: {multiDrag.myText.name}");
                }
                else
                {
                    Debug.LogWarning($"   ⚠️ myText: NULL (cần gán)");
                }
            }
            else
            {
                Debug.LogError($"   ❌ THIẾU MultiplayerDragAndDrop component!");
            }

            // Check required components
            if (obj.GetComponent<CanvasGroup>() == null)
            {
                Debug.LogWarning($"   ⚠️ Thiếu CanvasGroup");
            }
            if (obj.GetComponent<UnityEngine.UI.Image>() == null)
            {
                Debug.LogWarning($"   ⚠️ Thiếu Image");
            }

            Debug.Log("");
        }

        // Summary
        Debug.Log("=== SUMMARY ===");
        Debug.Log($"Found: {foundCount}/4 Answer objects");
        Debug.Log($"Has MultiplayerDragAndDrop: {hasComponentCount}/4");

        if (hasComponentCount == 4)
        {
            Debug.Log("\n✅ TẤT CẢ ĐÃ ĐÚNG! Bây giờ gán vào Answer Choices:");
            Debug.Log("1. Chọn GameplayPanel");
            Debug.Log("2. Inspector → UIMultiplayerBattleController");
            Debug.Log("3. Answer Choices → Size = 4");
            Debug.Log("4. Kéo Answer_0/1/2/3 vào Element 0/1/2/3");
        }
        else
        {
            Debug.LogError($"\n❌ Thiếu {4 - hasComponentCount} component!");
            Debug.LogError("Chạy: Tools → Multiplayer Battle → Auto-Setup References");
        }
    }

    [MenuItem("Tools/Multiplayer Battle/Add MultiplayerDragAndDrop to Answers")]
    public static void AddComponentsToAnswers()
    {
        if (!EditorUtility.DisplayDialog(
            "Add MultiplayerDragAndDrop",
            "Tool này sẽ thêm MultiplayerDragAndDrop component vào Answer_0/1/2/3\n\n" +
            "Bạn có muốn tiếp tục?",
            "Có", "Không"))
        {
            return;
        }

        Debug.Log("=== ADDING MULTIPLAYERDRAGANDDROP ===\n");

        string[] answerNames = { "Answer_0", "Answer_1", "Answer_2", "Answer_3" };
        int addedCount = 0;

        foreach (string name in answerNames)
        {
            GameObject obj = GameObject.Find(name);
            
            if (obj == null)
            {
                Debug.LogError($"❌ {name}: Không tìm thấy!");
                continue;
            }

            // Check if already has component
            var existing = obj.GetComponent<MultiplayerDragAndDrop>();
            if (existing != null)
            {
                Debug.Log($"⏭️ {name}: Đã có MultiplayerDragAndDrop, skip");
                continue;
            }

            // Add component
            var component = obj.AddComponent<MultiplayerDragAndDrop>();
            Debug.Log($"➕ {name}: Thêm MultiplayerDragAndDrop");

            // Auto-assign myText
            var text = obj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                SerializedObject so = new SerializedObject(component);
                so.FindProperty("myText").objectReferenceValue = text;
                so.ApplyModifiedProperties();
                Debug.Log($"   ✅ Gán myText: {text.name}");
            }

            // Ensure required components
            if (obj.GetComponent<CanvasGroup>() == null)
            {
                obj.AddComponent<CanvasGroup>();
                Debug.Log($"   ➕ Thêm CanvasGroup");
            }
            if (obj.GetComponent<UnityEngine.UI.Image>() == null)
            {
                obj.AddComponent<UnityEngine.UI.Image>();
                Debug.Log($"   ➕ Thêm Image");
            }

            addedCount++;
            EditorUtility.SetDirty(obj);
        }

        Debug.Log($"\n=== HOÀN THÀNH ===");
        Debug.Log($"✅ Đã thêm component vào {addedCount} Answer objects");
        Debug.Log($"Nhớ Save scene (Ctrl+S)!");

        EditorUtility.DisplayDialog(
            "Hoàn thành!",
            $"Đã thêm MultiplayerDragAndDrop vào {addedCount} Answer objects!\n\n" +
            "BÂY GIỜ:\n" +
            "1. Save scene (Ctrl+S)\n" +
            "2. Chọn GameplayPanel\n" +
            "3. Gán Answer_0/1/2/3 vào Answer Choices\n" +
            "4. Save lại",
            "OK");
    }
}
