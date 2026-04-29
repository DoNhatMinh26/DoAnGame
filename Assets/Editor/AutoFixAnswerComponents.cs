using UnityEngine;
using UnityEngine.UI;
using DoAnGame.Multiplayer;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Script tự động fix Answer objects - thêm MultiplayerDragAndDrop nếu thiếu
/// Menu: Tools → Auto Fix Answer Components
/// </summary>
public class AutoFixAnswerComponents
{
    [MenuItem("Tools/Auto Fix Answer Components")]
    public static void AutoFix()
    {
        Debug.Log("=== AUTO FIXING ANSWER COMPONENTS ===");
        
        int fixedCount = 0;
        
        // Tìm tất cả GameObject có tên Answer_0, Answer_1, Answer_2, Answer_3
        string[] answerNames = { "Answer_0", "Answer_1", "Answer_2", "Answer_3" };
        
        foreach (string answerName in answerNames)
        {
            var answerObj = GameObject.Find(answerName);
            
            if (answerObj == null)
            {
                Debug.LogWarning($"⚠️ Không tìm thấy {answerName}");
                continue;
            }
            
            Debug.Log($"\n--- Checking {answerName} ---");
            
            // Check MultiplayerDragAndDrop
            var multiDrag = answerObj.GetComponent<MultiplayerDragAndDrop>();
            var singleDrag = answerObj.GetComponent<DragAndDrop>();
            
            if (singleDrag != null)
            {
                Debug.LogWarning($"⚠️ {answerName} có DragAndDrop (SAI!) - Đang xóa...");
                Object.DestroyImmediate(singleDrag);
                fixedCount++;
            }
            
            if (multiDrag == null)
            {
                Debug.Log($"✅ Thêm MultiplayerDragAndDrop vào {answerName}");
                multiDrag = answerObj.AddComponent<MultiplayerDragAndDrop>();
                fixedCount++;
            }
            else
            {
                Debug.Log($"✅ {answerName} đã có MultiplayerDragAndDrop");
            }
            
            // Check CanvasGroup
            var canvasGroup = answerObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.Log($"✅ Thêm CanvasGroup vào {answerName}");
                canvasGroup = answerObj.AddComponent<CanvasGroup>();
                fixedCount++;
            }
            
            // Fix CanvasGroup settings
            if (canvasGroup.alpha != 1f || !canvasGroup.blocksRaycasts || !canvasGroup.interactable)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
                Debug.Log($"✅ Fixed CanvasGroup settings");
                fixedCount++;
            }
            
            // Check Image
            var image = answerObj.GetComponent<Image>();
            if (image == null)
            {
                Debug.Log($"✅ Thêm Image vào {answerName}");
                image = answerObj.AddComponent<Image>();
                fixedCount++;
            }
            
            // Fix Image raycastTarget
            if (!image.raycastTarget)
            {
                image.raycastTarget = true;
                Debug.Log($"✅ Enabled raycastTarget");
                fixedCount++;
            }
            
            // Check Text child
            var textChild = answerObj.transform.Find("AnswerText");
            if (textChild == null)
            {
                // Try to find any TMP_Text child
                var tmpText = answerObj.GetComponentInChildren<TMP_Text>();
                if (tmpText != null)
                {
                    textChild = tmpText.transform;
                    Debug.Log($"✅ Found text child: {textChild.name}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ {answerName} không có Text child! Cần tạo TextMeshPro child");
                }
            }
            
            // Gán myText
            if (textChild != null && multiDrag != null)
            {
                var tmpText = textChild.GetComponent<TMP_Text>();
                if (tmpText != null)
                {
                    // Use reflection to set private field
                    var field = typeof(MultiplayerDragAndDrop).GetField("myText", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        field.SetValue(multiDrag, tmpText);
                        Debug.Log($"✅ Gán myText = {textChild.name}");
                        fixedCount++;
                    }
                }
            }
            
            // Mark dirty
            EditorUtility.SetDirty(answerObj);
        }
        
        Debug.Log($"\n=== DONE: Fixed {fixedCount} issues ===");
        
        if (fixedCount > 0)
        {
            Debug.Log("⚠️ Nhớ SAVE SCENE (Ctrl+S)!");
            Debug.Log("⚠️ Sau đó GÁN LẠI vào UIMultiplayerBattleController.answerChoices!");
        }
        else
        {
            Debug.Log("✅ Tất cả Answer objects đã OK!");
        }
    }
    
    [MenuItem("Tools/Check Answer Components")]
    public static void CheckAnswers()
    {
        Debug.Log("=== CHECKING ANSWER COMPONENTS ===");
        
        string[] answerNames = { "Answer_0", "Answer_1", "Answer_2", "Answer_3" };
        int okCount = 0;
        
        foreach (string answerName in answerNames)
        {
            var answerObj = GameObject.Find(answerName);
            
            if (answerObj == null)
            {
                Debug.LogError($"❌ {answerName}: KHÔNG TÌM THẤY");
                continue;
            }
            
            Debug.Log($"\n--- {answerName} ---");
            Debug.Log($"  Active: {answerObj.activeInHierarchy}");
            
            // Check components
            var multiDrag = answerObj.GetComponent<MultiplayerDragAndDrop>();
            var singleDrag = answerObj.GetComponent<DragAndDrop>();
            var canvasGroup = answerObj.GetComponent<CanvasGroup>();
            var image = answerObj.GetComponent<Image>();
            
            if (singleDrag != null)
            {
                Debug.LogError($"  ❌ Có DragAndDrop (SAI! Phải là MultiplayerDragAndDrop)");
            }
            
            if (multiDrag != null)
            {
                Debug.Log($"  ✅ MultiplayerDragAndDrop: OK");
                
                // Check myText
                var field = typeof(MultiplayerDragAndDrop).GetField("myText", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var myText = field.GetValue(multiDrag) as TMP_Text;
                    if (myText != null)
                    {
                        Debug.Log($"  ✅ myText: {myText.name}");
                    }
                    else
                    {
                        Debug.LogError($"  ❌ myText: CHƯA GÁN");
                    }
                }
            }
            else
            {
                Debug.LogError($"  ❌ MultiplayerDragAndDrop: THIẾU");
            }
            
            if (canvasGroup != null)
            {
                Debug.Log($"  ✅ CanvasGroup: alpha={canvasGroup.alpha}, blocksRaycasts={canvasGroup.blocksRaycasts}");
            }
            else
            {
                Debug.LogError($"  ❌ CanvasGroup: THIẾU");
            }
            
            if (image != null)
            {
                Debug.Log($"  ✅ Image: raycastTarget={image.raycastTarget}");
            }
            else
            {
                Debug.LogError($"  ❌ Image: THIẾU");
            }
            
            // Check text child
            var textChild = answerObj.GetComponentInChildren<TMP_Text>();
            if (textChild != null)
            {
                Debug.Log($"  ✅ Text child: {textChild.name}");
            }
            else
            {
                Debug.LogError($"  ❌ Text child: THIẾU");
            }
            
            // Count OK
            if (multiDrag != null && canvasGroup != null && image != null && textChild != null && singleDrag == null)
            {
                okCount++;
            }
        }
        
        Debug.Log($"\n=== SUMMARY: {okCount}/4 Answer objects OK ===");
        
        if (okCount == 4)
        {
            Debug.Log("✅ TẤT CẢ ANSWER OBJECTS ĐÃ ĐÚNG!");
            Debug.Log("→ Bây giờ GÁN vào UIMultiplayerBattleController.answerChoices");
        }
        else
        {
            Debug.LogWarning($"⚠️ Còn {4 - okCount} Answer objects cần fix");
            Debug.Log("→ Chạy Tools → Auto Fix Answer Components");
        }
    }
}
#endif
