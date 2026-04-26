using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DoAnGame.UI
{
    /// <summary>
    /// Tool debug Leaderboard UI
    /// Menu: Tools → Debug Leaderboard UI
    /// </summary>
    public class LeaderboardDebugTool
    {
#if UNITY_EDITOR
        [MenuItem("Tools/Debug Leaderboard UI")]
        public static void DebugLeaderboardUI()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== LEADERBOARD UI DEBUG REPORT ===\n");
            
            bool hasIssues = false;

            // 1. Tìm BangXepHang panel
            var panel = GameObject.Find("BangXepHang");
            if (panel == null)
            {
                report.AppendLine("❌ KHÔNG TÌM THẤY BangXepHang panel!");
                report.AppendLine("Hãy mở scene có BangXepHang hoặc bật panel trong Hierarchy");
                Debug.LogError(report.ToString());
                return;
            }

            report.AppendLine($"✅ Tìm thấy BangXepHang panel: {panel.name}");
            report.AppendLine($"   Active: {panel.activeSelf}");
            report.AppendLine();

            // 2. Kiểm tra UILeaderboardPanelController
            var controller = panel.GetComponent<UILeaderboardPanelController>();
            if (controller == null)
            {
                report.AppendLine("❌ KHÔNG có UILeaderboardPanelController component!");
            }
            else
            {
                report.AppendLine("✅ UILeaderboardPanelController found");
                // Dùng reflection để đọc private fields
                var type = controller.GetType();
                
                var entryPrefabField = type.GetField("entryPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var contentField = type.GetField("contentContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (entryPrefabField != null)
                {
                    var prefab = entryPrefabField.GetValue(controller);
                    report.AppendLine($"   Entry Prefab: {(prefab != null ? "✅ Assigned" : "❌ NULL")}");
                }
                
                if (contentField != null)
                {
                    var content = contentField.GetValue(controller);
                    report.AppendLine($"   Content Container: {(content != null ? "✅ Assigned" : "❌ NULL")}");
                }
            }
            report.AppendLine();

            // 3. Tìm Content container
            Transform contentTransform = FindChildRecursive(panel.transform, "Content");
            if (contentTransform == null)
            {
                report.AppendLine("❌ KHÔNG tìm thấy Content container!");
            }
            else
            {
                report.AppendLine($"✅ Content container: {contentTransform.name}");
                report.AppendLine($"   Child count: {contentTransform.childCount}");
                
                // Kiểm tra Layout
                var verticalLayout = contentTransform.GetComponent<VerticalLayoutGroup>();
                if (verticalLayout != null)
                {
                    report.AppendLine($"   ✅ VerticalLayoutGroup:");
                    report.AppendLine($"      Spacing: {verticalLayout.spacing}");
                    report.AppendLine($"      Child Force Expand Width: {verticalLayout.childForceExpandWidth}");
                    report.AppendLine($"      Child Force Expand Height: {verticalLayout.childForceExpandHeight}");
                }
                else
                {
                    report.AppendLine("   ❌ THIẾU VerticalLayoutGroup!");
                }
                
                var contentSizeFitter = contentTransform.GetComponent<ContentSizeFitter>();
                if (contentSizeFitter != null)
                {
                    report.AppendLine($"   ✅ ContentSizeFitter:");
                    report.AppendLine($"      Vertical Fit: {contentSizeFitter.verticalFit}");
                }
                
                report.AppendLine();
                
                // Kiểm tra các entry đã spawn
                if (contentTransform.childCount > 0)
                {
                    report.AppendLine($"📋 Đã spawn {contentTransform.childCount} entries:");
                    for (int i = 0; i < Mathf.Min(contentTransform.childCount, 3); i++)
                    {
                        var entry = contentTransform.GetChild(i);
                        report.AppendLine($"\n   Entry {i + 1}: {entry.name}");
                        DebugEntry(entry, report);
                    }
                    if (contentTransform.childCount > 3)
                    {
                        report.AppendLine($"   ... và {contentTransform.childCount - 3} entries khác");
                    }
                }
                else
                {
                    report.AppendLine("⚠️ Chưa có entry nào được spawn");
                    report.AppendLine("   Hãy Play game và click 'Load' để spawn entries");
                }
            }
            report.AppendLine();

            // 4. Tìm LeaderboardEntry template (inactive)
            Transform template = FindChildRecursive(panel.transform.parent, "LeaderboardEntry");
            if (template == null)
            {
                report.AppendLine("⚠️ KHÔNG tìm thấy LeaderboardEntry template");
            }
            else
            {
                report.AppendLine($"📄 LeaderboardEntry template: {template.name}");
                report.AppendLine($"   Active: {template.gameObject.activeSelf} (phải FALSE)");
                if (template.gameObject.activeSelf)
                {
                    report.AppendLine("   ⚠️ WARNING: Template đang active, nên tắt đi!");
                }
                report.AppendLine();
                DebugEntry(template, report);
            }

            // 5. Tổng kết
            report.AppendLine("\n=== TỔNG KẾT ===");
            if (report.ToString().Contains("❌") || report.ToString().Contains("⚠️"))
            {
                hasIssues = true;
                report.AppendLine("⚠️ Phát hiện lỗi! Hãy chạy 'Tools → Auto-Fix Leaderboard Entry' để sửa tự động!");
            }
            else
            {
                report.AppendLine("✅ Tất cả đều OK!");
            }
            
            Debug.Log(report.ToString());
            
            // Hiển thị popup
            if (hasIssues)
            {
                bool autoFix = EditorUtility.DisplayDialog("Leaderboard Debug Report", 
                    "Phát hiện lỗi trong Leaderboard UI!\n\nBạn có muốn tự động sửa không?", 
                    "Auto-Fix", "Cancel");
                
                if (autoFix)
                {
                    AutoFixLeaderboardEntry();
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Leaderboard Debug Report", 
                    "✅ Tất cả đều OK! Không có lỗi.", 
                    "OK");
            }
        }
        
        [MenuItem("Tools/Auto-Fix Leaderboard Entry")]
        public static void AutoFixLeaderboardEntry()
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine("=== AUTO-FIX LEADERBOARD ENTRY ===\n");
            
            // 1. Tìm BangXepHang panel
            var panel = GameObject.Find("BangXepHang");
            if (panel == null)
            {
                log.AppendLine("❌ KHÔNG TÌM THẤY BangXepHang panel!");
                log.AppendLine("Hãy mở scene có BangXepHang");
                Debug.LogError(log.ToString());
                EditorUtility.DisplayDialog("Auto-Fix Failed", 
                    "Không tìm thấy BangXepHang panel!\nHãy mở scene có BangXepHang.", 
                    "OK");
                return;
            }
            
            // 2. Tìm Content container
            Transform contentTransform = FindChildRecursive(panel.transform, "Content");
            if (contentTransform == null)
            {
                log.AppendLine("❌ KHÔNG tìm thấy Content container!");
                Debug.LogError(log.ToString());
                return;
            }
            
            int fixedCount = 0;
            
            // 3. Fix Content VerticalLayoutGroup spacing
            var verticalLayout = contentTransform.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout != null)
            {
                if (verticalLayout.spacing < 5)
                {
                    verticalLayout.spacing = 10;
                    log.AppendLine("✅ Set Content spacing = 10");
                    fixedCount++;
                }
                
                // Đảm bảo Child Control Size đúng
                if (!verticalLayout.childControlWidth || verticalLayout.childControlHeight)
                {
                    verticalLayout.childControlWidth = true;
                    verticalLayout.childControlHeight = false;
                    log.AppendLine("✅ Fixed Content Child Control Size");
                    fixedCount++;
                }
            }
            
            // 4. Fix tất cả entries đã spawn
            for (int i = 0; i < contentTransform.childCount; i++)
            {
                var entry = contentTransform.GetChild(i);
                if (FixEntry(entry, log))
                {
                    fixedCount++;
                }
            }
            
            // 4. Tìm và fix template
            Transform template = FindChildRecursive(panel.transform.parent, "LeaderboardEntry");
            if (template != null)
            {
                log.AppendLine("\n📄 Fixing LeaderboardEntry template...");
                if (FixEntry(template, log))
                {
                    fixedCount++;
                }
                
                // Đảm bảo template inactive
                if (template.gameObject.activeSelf)
                {
                    template.gameObject.SetActive(false);
                    log.AppendLine("   ✅ Đã tắt template");
                }
            }
            
            log.AppendLine($"\n✅ Đã fix {fixedCount} entries!");
            log.AppendLine("\n💾 Nhớ Save scene: Ctrl+S");
            
            Debug.Log(log.ToString());
            
            EditorUtility.DisplayDialog("Auto-Fix Complete", 
                $"Đã fix {fixedCount} entries!\n\nNhớ Save scene (Ctrl+S) để lưu thay đổi.", 
                "OK");
        }
        
        private static bool FixEntry(Transform entry, StringBuilder log)
        {
            bool hasFixed = false;
            log.AppendLine($"\n🔧 Fixing: {entry.name}");
            
            // 1. Fix RectTransform Anchors
            var rectTransform = entry.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Kiểm tra nếu width = 0
                if (rectTransform.rect.width < 10)
                {
                    // Set anchors để stretch ngang
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    rectTransform.offsetMin = new Vector2(0, rectTransform.offsetMin.y);
                    rectTransform.offsetMax = new Vector2(0, rectTransform.offsetMax.y);
                    log.AppendLine("   ✅ Fixed RectTransform anchors (stretch horizontal)");
                    hasFixed = true;
                }
            }
            
            // 2. Fix Layout Element
            var layoutElement = entry.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = entry.gameObject.AddComponent<LayoutElement>();
                log.AppendLine("   ✅ Added LayoutElement");
                hasFixed = true;
            }
            
            if (layoutElement.preferredHeight < 60)
            {
                layoutElement.preferredHeight = 80;
                log.AppendLine("   ✅ Set preferredHeight = 80");
                hasFixed = true;
            }
            
            if (layoutElement.flexibleWidth < 1)
            {
                layoutElement.flexibleWidth = 1;
                log.AppendLine("   ✅ Set flexibleWidth = 1");
                hasFixed = true;
            }
            
            // 3. Fix Horizontal Layout Group
            var horizontalLayout = entry.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                if (horizontalLayout.childForceExpandWidth || horizontalLayout.childForceExpandHeight)
                {
                    horizontalLayout.childForceExpandWidth = false;
                    horizontalLayout.childForceExpandHeight = false;
                    log.AppendLine("   ✅ Disabled Child Force Expand");
                    hasFixed = true;
                }
            }
            
            // 4. Fix Background Image
            Transform bg = entry.Find("Background");
            if (bg != null)
            {
                var bgImage = bg.GetComponent<Image>();
                if (bgImage != null && bgImage.sprite == null)
                {
                    // Tìm UISprite
                    var uiSprite = FindUISprite();
                    if (uiSprite != null)
                    {
                        bgImage.sprite = uiSprite;
                        log.AppendLine("   ✅ Assigned UISprite to Background");
                        hasFixed = true;
                    }
                    else
                    {
                        log.AppendLine("   ⚠️ Không tìm thấy UISprite, Background vẫn NULL");
                    }
                }
            }
            
            // 5. Fix HighlightBorder
            Transform border = entry.Find("HighlightBorder");
            if (border != null)
            {
                var borderImage = border.GetComponent<Image>();
                if (borderImage != null && borderImage.sprite == null)
                {
                    var uiSprite = FindUISprite();
                    if (uiSprite != null)
                    {
                        borderImage.sprite = uiSprite;
                        log.AppendLine("   ✅ Assigned UISprite to HighlightBorder");
                        hasFixed = true;
                    }
                }
                
                // Đảm bảo HighlightBorder inactive
                if (border.gameObject.activeSelf)
                {
                    border.gameObject.SetActive(false);
                    log.AppendLine("   ✅ Disabled HighlightBorder");
                    hasFixed = true;
                }
            }
            
            if (!hasFixed)
            {
                log.AppendLine("   ℹ️ Không cần fix gì");
            }
            
            return hasFixed;
        }
        
        private static Sprite FindUISprite()
        {
            // Tìm UISprite trong project
            string[] guids = AssetDatabase.FindAssets("UISprite t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            
            // Fallback: tìm bất kỳ sprite nào
            guids = AssetDatabase.FindAssets("t:Sprite");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            
            return null;
        }

        private static void DebugEntry(Transform entry, StringBuilder report)
        {
            // Kiểm tra LeaderboardEntryWidget
            var widget = entry.GetComponent<LeaderboardEntryWidget>();
            if (widget == null)
            {
                report.AppendLine("      ❌ THIẾU LeaderboardEntryWidget component!");
                return;
            }
            
            report.AppendLine("      ✅ LeaderboardEntryWidget found");
            
            // Kiểm tra RectTransform
            var rectTransform = entry.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                report.AppendLine($"      RectTransform:");
                report.AppendLine($"         Size: {rectTransform.rect.width} x {rectTransform.rect.height}");
                report.AppendLine($"         Anchors: Min({rectTransform.anchorMin.x}, {rectTransform.anchorMin.y}) Max({rectTransform.anchorMax.x}, {rectTransform.anchorMax.y})");
            }
            
            // Kiểm tra Layout Element
            var layoutElement = entry.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                report.AppendLine($"      ✅ LayoutElement:");
                report.AppendLine($"         Preferred Height: {layoutElement.preferredHeight}");
                report.AppendLine($"         Flexible Width: {layoutElement.flexibleWidth}");
            }
            else
            {
                report.AppendLine("      ⚠️ THIẾU LayoutElement!");
            }
            
            // Kiểm tra Horizontal Layout Group
            var horizontalLayout = entry.GetComponent<HorizontalLayoutGroup>();
            if (horizontalLayout != null)
            {
                report.AppendLine($"      ✅ HorizontalLayoutGroup:");
                report.AppendLine($"         Spacing: {horizontalLayout.spacing}");
                report.AppendLine($"         Child Force Expand Width: {horizontalLayout.childForceExpandWidth} (phải FALSE)");
                report.AppendLine($"         Child Force Expand Height: {horizontalLayout.childForceExpandHeight} (phải FALSE)");
                
                if (horizontalLayout.childForceExpandWidth || horizontalLayout.childForceExpandHeight)
                {
                    report.AppendLine("         ⚠️ WARNING: Child Force Expand nên TẮT!");
                }
            }
            
            // Kiểm tra Background
            Transform bg = entry.Find("Background");
            if (bg == null)
            {
                report.AppendLine("      ❌ THIẾU Background child!");
            }
            else
            {
                var bgImage = bg.GetComponent<Image>();
                if (bgImage == null)
                {
                    report.AppendLine("      ❌ Background THIẾU Image component!");
                }
                else
                {
                    report.AppendLine($"      ✅ Background Image:");
                    report.AppendLine($"         Sprite: {(bgImage.sprite != null ? bgImage.sprite.name : "❌ NULL")}");
                    report.AppendLine($"         Color: {bgImage.color}");
                    report.AppendLine($"         Active: {bg.gameObject.activeSelf}");
                    
                    if (bgImage.sprite == null)
                    {
                        report.AppendLine("         ❌ THIẾU SPRITE! Đây là lý do không hiển thị màu!");
                    }
                    if (bgImage.color.a < 0.01f)
                    {
                        report.AppendLine("         ⚠️ Alpha quá thấp, gần như trong suốt!");
                    }
                }
            }
            
            // Kiểm tra HighlightBorder
            Transform border = entry.Find("HighlightBorder");
            if (border != null)
            {
                var borderImage = border.GetComponent<Image>();
                if (borderImage != null)
                {
                    report.AppendLine($"      ✅ HighlightBorder:");
                    report.AppendLine($"         Sprite: {(borderImage.sprite != null ? borderImage.sprite.name : "❌ NULL")}");
                    report.AppendLine($"         Color: {borderImage.color}");
                    report.AppendLine($"         Active: {border.gameObject.activeSelf} (phải FALSE khi chưa highlight)");
                }
            }
            
            // Kiểm tra Text components
            string[] textNames = { "RankText", "NameText", "ScoreText", "LevelText" };
            foreach (var textName in textNames)
            {
                Transform textTransform = entry.Find(textName);
                if (textTransform == null)
                {
                    report.AppendLine($"      ❌ THIẾU {textName}!");
                }
                else
                {
                    var tmp = textTransform.GetComponent<TextMeshProUGUI>();
                    if (tmp == null)
                    {
                        report.AppendLine($"      ❌ {textName} THIẾU TextMeshProUGUI!");
                    }
                    else
                    {
                        report.AppendLine($"      ✅ {textName}: '{tmp.text}' (Size: {tmp.fontSize}, Color: {tmp.color})");
                    }
                }
            }
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            
            Transform found = parent.Find(name);
            if (found != null) return found;
            
            for (int i = 0; i < parent.childCount; i++)
            {
                found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            
            return null;
        }
#endif
    }
}
