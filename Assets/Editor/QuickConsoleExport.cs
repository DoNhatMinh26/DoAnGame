using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Reflection;
using System;

/// <summary>
/// Quick export console log với 1 click
/// Menu: Tools/Quick Export Console Log (Ctrl+Shift+L)
/// </summary>
public class QuickConsoleExport
{
    [MenuItem("Tools/Quick Export Console Log %#l")] // Ctrl+Shift+L
    public static void QuickExport()
    {
        try
        {
            string exportPath = "D:/app/GameDoan/DoAnGame/Assets/Editor";
            
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

            // Refresh AssetDatabase
            if (fullPath.StartsWith(Application.dataPath))
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"[QuickExport] ✅ Exported {logs.Count} console logs to: {fullPath}");
            
            // Mở file trong Explorer
            EditorUtility.RevealInFinder(fullPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QuickExport] Export failed: {ex.Message}");
        }
    }

    private static System.Collections.Generic.List<string> GetConsoleLogs()
    {
        var logs = new System.Collections.Generic.List<string>();

        try
        {
            var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
            var logEntriesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");

            if (logEntriesType == null) return logs;

            var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
            if (getCountMethod == null) return logs;

            int count = (int)getCountMethod.Invoke(null, null);

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

                    var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public);
                    var fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
                    var lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
                    var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);

                    string message = messageField?.GetValue(logEntry)?.ToString() ?? "";
                    string file = fileField?.GetValue(logEntry)?.ToString() ?? "";
                    int line = (int)(lineField?.GetValue(logEntry) ?? 0);
                    int mode = (int)(modeField?.GetValue(logEntry) ?? 0);

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
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QuickExport] Error getting console logs: {ex}");
        }

        return logs;
    }

    private static string GetLogTypeString(int mode)
    {
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
