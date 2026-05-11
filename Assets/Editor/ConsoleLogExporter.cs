using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Reflection;
using System;

/// <summary>
/// Editor tool để export toàn bộ console log ra file txt
/// Menu: Tools/Export Console Log
/// </summary>
public class ConsoleLogExporter : EditorWindow
{
    private static string exportPath = "D:/app/GameDoan/DoAnGame/Assets/Editor";
    private Vector2 scrollPosition;
    private string previewText = "";
    private int logCount = 0;

    [MenuItem("Tools/Export Console Log")]
    public static void ShowWindow()
    {
        var window = GetWindow<ConsoleLogExporter>("Console Log Exporter");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Console Log Exporter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Export path
        GUILayout.Label("Export Path:", EditorStyles.label);
        exportPath = EditorGUILayout.TextField(exportPath);
        
        if (GUILayout.Button("Browse..."))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Export Folder", exportPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                exportPath = selectedPath;
            }
        }

        GUILayout.Space(10);

        // Preview
        GUILayout.Label($"Console Logs Preview ({logCount} entries):", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
        EditorGUILayout.TextArea(previewText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        // Buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Refresh Preview", GUILayout.Height(30)))
        {
            RefreshPreview();
        }

        if (GUILayout.Button("Export to File", GUILayout.Height(30)))
        {
            ExportConsoleLog();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Lưu ý: Tool này sử dụng reflection để đọc Console log. " +
            "Nếu không thấy log, hãy mở Console window trước (Window → General → Console).",
            MessageType.Info
        );
    }

    private void OnEnable()
    {
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        var logs = GetConsoleLogs();
        logCount = logs.Count;
        
        StringBuilder preview = new StringBuilder();
        int previewLimit = Mathf.Min(20, logs.Count);
        
        for (int i = 0; i < previewLimit; i++)
        {
            preview.AppendLine(logs[i]);
        }
        
        if (logs.Count > previewLimit)
        {
            preview.AppendLine($"\n... and {logs.Count - previewLimit} more entries");
        }
        
        previewText = preview.ToString();
        Repaint();
    }

    private void ExportConsoleLog()
    {
        try
        {
            // Tạo thư mục nếu chưa tồn tại
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            // Tạo tên file với timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"ConsoleLog_{timestamp}.txt";
            string fullPath = Path.Combine(exportPath, fileName);

            // Lấy toàn bộ console logs
            var logs = GetConsoleLogs();

            // Ghi ra file
            StringBuilder content = new StringBuilder();
            content.AppendLine("=== UNITY CONSOLE LOG EXPORT ===");
            content.AppendLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Unity Version: {Application.unityVersion}");
            content.AppendLine($"Total Entries: {logs.Count}");
            content.AppendLine("================================\n");

            foreach (var log in logs)
            {
                content.AppendLine(log);
            }

            File.WriteAllText(fullPath, content.ToString(), Encoding.UTF8);

            // Refresh AssetDatabase nếu export vào Assets folder
            if (fullPath.StartsWith(Application.dataPath))
            {
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog(
                "Export Successful",
                $"Console log exported to:\n{fullPath}\n\nTotal entries: {logs.Count}",
                "OK"
            );

            // Mở file trong Explorer
            EditorUtility.RevealInFinder(fullPath);
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog(
                "Export Failed",
                $"Failed to export console log:\n{ex.Message}",
                "OK"
            );
            Debug.LogError($"[ConsoleLogExporter] Export failed: {ex}");
        }
    }

    /// <summary>
    /// Lấy toàn bộ console logs bằng reflection
    /// </summary>
    private System.Collections.Generic.List<string> GetConsoleLogs()
    {
        var logs = new System.Collections.Generic.List<string>();

        try
        {
            // Sử dụng reflection để truy cập Console window
            var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            if (consoleWindowType == null)
            {
                Debug.LogWarning("[ConsoleLogExporter] Cannot find ConsoleWindow type");
                return logs;
            }

            var logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            if (logEntriesType == null)
            {
                Debug.LogWarning("[ConsoleLogExporter] Cannot find LogEntries type");
                return logs;
            }

            // Lấy số lượng log entries
            var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
            if (getCountMethod == null)
            {
                Debug.LogWarning("[ConsoleLogExporter] Cannot find GetCount method");
                return logs;
            }

            int count = (int)getCountMethod.Invoke(null, null);

            // Lấy từng log entry
            var startGettingEntriesMethod = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Static | BindingFlags.Public);
            var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
            var endGettingEntriesMethod = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Static | BindingFlags.Public);

            if (startGettingEntriesMethod != null && getEntryInternalMethod != null && endGettingEntriesMethod != null)
            {
                startGettingEntriesMethod.Invoke(null, null);

                var logEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
                object logEntry = Activator.CreateInstance(logEntryType);

                for (int i = 0; i < count; i++)
                {
                    getEntryInternalMethod.Invoke(null, new object[] { i, logEntry });

                    // Lấy thông tin từ LogEntry
                    var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                    var fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
                    var lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
                    var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);

                    string message = messageField?.GetValue(logEntry)?.ToString() ?? "";
                    string file = fileField?.GetValue(logEntry)?.ToString() ?? "";
                    int line = (int)(lineField?.GetValue(logEntry) ?? 0);
                    int mode = (int)(modeField?.GetValue(logEntry) ?? 0);

                    // Format log entry
                    string logType = GetLogTypeString(mode);
                    string logLine = $"[{logType}] {message}";
                    
                    if (!string.IsNullOrEmpty(file))
                    {
                        logLine += $"\n  at {file}:{line}";
                    }

                    logs.Add(logLine);
                }

                endGettingEntriesMethod.Invoke(null, null);
            }
            else
            {
                Debug.LogWarning("[ConsoleLogExporter] Cannot find log entry methods");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ConsoleLogExporter] Error getting console logs: {ex}");
        }

        return logs;
    }

    private string GetLogTypeString(int mode)
    {
        // LogType enum values
        // 0 = Error, 1 = Assert, 2 = Warning, 3 = Log, 4 = Exception
        switch (mode)
        {
            case 0: return "ERROR";
            case 1: return "ASSERT";
            case 2: return "WARNING";
            case 3: return "INFO";
            case 4: return "EXCEPTION";
            default: return "UNKNOWN";
        }
    }
}
