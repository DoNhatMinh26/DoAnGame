using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;

public class SceneExporter
{
        [MenuItem("Tools/Export Current Scene to TXT")]
        public static void ExportCurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Lỗi", "Không có scene nào đang mở.", "OK");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine($"SCENE: {scene.name}");
            sb.AppendLine($"Đường dẫn: {scene.path}");
            sb.AppendLine($"Thời gian export: {System.DateTime.Now}");
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine();

            // Get all root objects
            var rootObjects = scene.GetRootGameObjects();
            sb.AppendLine($"Tổng số Root GameObjects: {rootObjects.Length}");
            sb.AppendLine();

            int totalObjects = 0;
            foreach (var root in rootObjects)
            {
                totalObjects += ExportGameObject(sb, root, 0);
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine($"TỔNG KẾT");
            sb.AppendLine($"Tổng số GameObjects: {totalObjects}");
            sb.AppendLine("═══════════════════════════════════════════════════════════");

            // Lưu vào thư mục Editor
            string editorPath = Path.Combine(Application.dataPath, "Editor");
            string fileName = $"{scene.name}_Export.txt";
            string finalPath = Path.Combine(editorPath, fileName);

            File.WriteAllText(finalPath, sb.ToString(), Encoding.UTF8);
            
            EditorUtility.DisplayDialog("Thành công!", 
                $"Đã export scene thành công!\n\nFile: {fileName}\nTổng số Objects: {totalObjects}\n\nĐường dẫn: {finalPath}", 
                "OK");
            
            // Mở thư mục chứa file
            EditorUtility.RevealInFinder(finalPath);
        }

        private static int ExportGameObject(StringBuilder sb, GameObject obj, int depth)
        {
            int count = 1;
            string indent = new string('│', depth);
            string prefix = depth > 0 ? "├─ " : "";

            // GameObject name and active state
            string activeState = obj.activeSelf ? "" : " [INACTIVE]";
            string tag = obj.tag != "Untagged" ? $" (Tag: {obj.tag})" : "";
            string layer = LayerMask.LayerToName(obj.layer) != "Default" ? $" [Layer: {LayerMask.LayerToName(obj.layer)}]" : "";
            
            sb.AppendLine($"{indent}{prefix}{obj.name}{activeState}{tag}{layer}");

            // Components
            var components = obj.GetComponents<Component>();
            foreach (var component in components)
            {
                if (component == null) continue;
                if (component is Transform) continue; // Skip Transform

                string componentInfo = GetComponentInfo(component);
                sb.AppendLine($"{indent}   • {component.GetType().Name}{componentInfo}");
            }

            // Children
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                count += ExportGameObject(sb, obj.transform.GetChild(i).gameObject, depth + 1);
            }

            return count;
        }

        private static string GetComponentInfo(Component component)
        {
            // Add specific info for common components
            if (component is MonoBehaviour mb)
            {
                return $" ({mb.GetType().Namespace})";
            }
            else if (component is Canvas canvas)
            {
                return $" (RenderMode: {canvas.renderMode})";
            }
            else if (component is UnityEngine.UI.Text text)
            {
                return $" (\"{text.text.Substring(0, Mathf.Min(30, text.text.Length))}{(text.text.Length > 30 ? "..." : "")}\")";
            }
            else if (component is TMPro.TextMeshProUGUI tmp)
            {
                return $" (\"{tmp.text.Substring(0, Mathf.Min(30, tmp.text.Length))}{(tmp.text.Length > 30 ? "..." : "")}\")";
            }
            else if (component is UnityEngine.UI.Image img)
            {
                return img.sprite != null ? $" (Sprite: {img.sprite.name})" : "";
            }
            else if (component is UnityEngine.UI.Button)
            {
                return " (Interactable)";
            }

            return "";
        }
    }
