using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

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
                totalObjects += ExportGameObject(sb, root, 0, false);
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

        [MenuItem("Tools/Export Current Scene (Detailed) to TXT")]
        public static void ExportCurrentSceneDetailed()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Lỗi", "Không có scene nào đang mở.", "OK");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine($"SCENE: {scene.name} (DETAILED EXPORT)");
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
                totalObjects += ExportGameObject(sb, root, 0, true);
            }

            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════");
            sb.AppendLine($"TỔNG KẾT");
            sb.AppendLine($"Tổng số GameObjects: {totalObjects}");
            sb.AppendLine("═══════════════════════════════════════════════════════════");

            // Lưu vào thư mục Editor
            string editorPath = Path.Combine(Application.dataPath, "Editor");
            string fileName = $"{scene.name}_Export_Detailed.txt";
            string finalPath = Path.Combine(editorPath, fileName);

            File.WriteAllText(finalPath, sb.ToString(), Encoding.UTF8);
            
            EditorUtility.DisplayDialog("Thành công!", 
                $"Đã export scene chi tiết thành công!\n\nFile: {fileName}\nTổng số Objects: {totalObjects}\n\nĐường dẫn: {finalPath}", 
                "OK");
            
            // Mở thư mục chứa file
            EditorUtility.RevealInFinder(finalPath);
        }

        private static int ExportGameObject(StringBuilder sb, GameObject obj, int depth, bool detailed)
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

                // Export serialized fields if detailed mode
                if (detailed)
                {
                    ExportSerializedFields(sb, component, indent + "     ");
                }
            }

            // Children
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                count += ExportGameObject(sb, obj.transform.GetChild(i).gameObject, depth + 1, detailed);
            }

            return count;
        }

        private static void ExportSerializedFields(StringBuilder sb, Component component, string indent)
        {
            if (component == null) return;

            SerializedObject so = new SerializedObject(component);
            SerializedProperty prop = so.GetIterator();

            List<string> fields = new List<string>();

            // Iterate through all serialized properties
            if (prop.NextVisible(true))
            {
                do
                {
                    // Skip script reference
                    if (prop.name == "m_Script") continue;

                    string fieldInfo = GetPropertyInfo(prop);
                    if (!string.IsNullOrEmpty(fieldInfo))
                    {
                        fields.Add($"{indent}[{prop.name}] = {fieldInfo}");
                    }
                }
                while (prop.NextVisible(false));
            }

            // Only print if there are fields
            if (fields.Count > 0)
            {
                foreach (var field in fields)
                {
                    sb.AppendLine(field);
                }
            }
        }

        private static string GetPropertyInfo(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return prop.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return prop.floatValue.ToString("F2");
                case SerializedPropertyType.String:
                    return $"\"{prop.stringValue}\"";
                case SerializedPropertyType.Color:
                    return prop.colorValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    if (prop.objectReferenceValue != null)
                        return $"→ {prop.objectReferenceValue.name} ({prop.objectReferenceValue.GetType().Name})";
                    else
                        return "NULL";
                case SerializedPropertyType.Enum:
                    if (prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumNames.Length)
                        return prop.enumNames[prop.enumValueIndex];
                    else
                        return $"Enum({prop.enumValueIndex})";

                case SerializedPropertyType.Vector2:
                    return prop.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return prop.vector3Value.ToString();
                case SerializedPropertyType.Rect:
                    return prop.rectValue.ToString();
                case SerializedPropertyType.ArraySize:
                    return $"Size: {prop.intValue}";
                case SerializedPropertyType.Generic:
                    if (prop.isArray)
                        return $"Array[{prop.arraySize}]";
                    return $"(Complex type)";
                default:
                    return "";
            }
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
