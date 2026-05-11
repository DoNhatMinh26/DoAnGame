using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System;
using System.Collections.Generic;

/// <summary>
/// Editor tool để export console log ra file txt.
/// Sử dụng Application.logMessageReceived để capture logs runtime.
/// Menu: Tools/Export Console Log
/// </summary>
public class ConsoleLogExporter : EditorWindow
{
    private static string exportPath = "Assets/Editor";
    private Vector2 scrollPosition;
    private string previewText = "";
    private int logCount = 0;
    
    // Static list để lưu logs (persist qua domain reload)
    private static List<LogEntry> capturedLogs = new List<LogEntry>();
    private static bool isCapturing = false;

    [System.Serializable]
    private class LogEntry
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public string timestamp;

        public LogEntry(string msg, string stack, LogType logType)
        {
            message = msg;
            stackTrace = stack;
            type = logType;
            timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        }
    }

    [MenuItem("Tools/Export Console Log")]
    public static void ShowWindow()
    {
        var window = GetWindow<ConsoleLogExporter>("Console Log Exporter");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        // Start capturing logs khi window mở
        StartCapturing();
        RefreshPreview();
    }

    private void OnDisable()
    {
        // Không stop capturing khi window đóng - để logs vẫn được capture
    }

    private void OnGUI()
    {
        GUILayout.Label("Console Log Exporter", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Capture status
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Capturing: {(isCapturing ? "ON" : "OFF")}", EditorStyles.boldLabel);
        if (GUILayout.Button(isCapturing ? "Stop Capturing" : "Start Capturing", GUILayout.Width(120)))
        {
            if (isCapturing)
                StopCapturing();
            else
                StartCapturing();
        }
        if (GUILayout.Button("Clear Logs", GUILayout.Width(100)))
        {
            ClearLogs();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Export path
        GUILayout.Label("Export Path:", EditorStyles.label);
        EditorGUILayout.BeginHorizontal();
        exportPath = EditorGUILayout.TextField(exportPath);
        if (GUILayout.Button("Browse...", GUILayout.Width(80)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Export Folder", exportPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                exportPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Preview
        GUILayout.Label($"Captured Logs Preview ({logCount} entries):", EditorStyles.boldLabel);
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
        
        // Help box
        string helpText = isCapturing 
            ? "✅ Đang capture logs. Chạy game để thu thập logs, sau đó click 'Export to File'."
            : "⚠️ Không capture logs. Click 'Start Capturing' để bắt đầu.";
        
        EditorGUILayout.HelpBox(helpText, isCapturing ? MessageType.Info : MessageType.Warning);
    }

    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        // Auto-start capturing khi Unity khởi động
        if (!isCapturing)
        {
            StartCapturing();
        }
    }

    private static void StartCapturing()
    {
        if (isCapturing) return;
        
        Application.logMessageReceived += HandleLog;
        isCapturing = true;
        Debug.Log("[ConsoleLogExporter] ✅ Started capturing logs");
    }

    private static void StopCapturing()
    {
        if (!isCapturing) return;
        
        Application.logMessageReceived -= HandleLog;
        isCapturing = false;
        Debug.Log("[ConsoleLogExporter] ⏸️ Stopped capturing logs");
    }

    private static void HandleLog(string logString, string stackTrace, LogType type)
    {
        capturedLogs.Add(new LogEntry(logString, stackTrace, type));
    }

    private void ClearLogs()
    {
        capturedLogs.Clear();
        RefreshPreview();
        Debug.Log("[ConsoleLogExporter] 🗑️ Cleared all captured logs");
    }

    private void RefreshPreview()
    {
        logCount = capturedLogs.Count;
        
        StringBuilder preview = new StringBuilder();
        int previewLimit = Mathf.Min(20, capturedLogs.Count);
        
        // Lấy 20 logs gần nhất
        int startIndex = Mathf.Max(0, capturedLogs.Count - previewLimit);
        for (int i = startIndex; i < capturedLogs.Count; i++)
        {
            var log = capturedLogs[i];
            string typeStr = GetLogTypeString(log.type);
            preview.AppendLine($"[{log.timestamp}] [{typeStr}] {log.message}");
        }
        
        if (capturedLogs.Count > previewLimit)
        {
            preview.Insert(0, $"... and {capturedLogs.Count - previewLimit} more entries\n\n");
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

            // Ghi ra file
            StringBuilder content = new StringBuilder();
            content.AppendLine("=== UNITY CONSOLE LOG EXPORT ===");
            content.AppendLine($"Export Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Unity Version: {Application.unityVersion}");
            content.AppendLine($"Total Entries: {capturedLogs.Count}");
            content.AppendLine($"Capture Started: {(isCapturing ? "Active" : "Stopped")}");
            content.AppendLine("================================\n");

            foreach (var log in capturedLogs)
            {
                string typeStr = GetLogTypeString(log.type);
                content.AppendLine($"[{log.timestamp}] [{typeStr}] {log.message}");
                
                // Thêm stack trace nếu có (cho errors và exceptions)
                if (!string.IsNullOrEmpty(log.stackTrace) && 
                    (log.type == LogType.Error || log.type == LogType.Exception || log.type == LogType.Assert))
                {
                    content.AppendLine($"  Stack Trace:");
                    content.AppendLine($"  {log.stackTrace.Replace("\n", "\n  ")}");
                }
                
                content.AppendLine(); // Blank line giữa các logs
            }

            File.WriteAllText(fullPath, content.ToString(), Encoding.UTF8);

            // Refresh AssetDatabase nếu export vào Assets folder
            if (fullPath.StartsWith(Application.dataPath))
            {
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog(
                "Export Successful",
                $"Console log exported to:\n{fullPath}\n\nTotal entries: {capturedLogs.Count}",
                "OK"
            );

            // Mở file trong Explorer
            EditorUtility.RevealInFinder(fullPath);
            
            Debug.Log($"[ConsoleLogExporter] ✅ Exported {capturedLogs.Count} logs to: {fullPath}");
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

    private string GetLogTypeString(LogType type)
    {
        switch (type)
        {
            case LogType.Error: return "ERROR";
            case LogType.Assert: return "ASSERT";
            case LogType.Warning: return "WARNING";
            case LogType.Log: return "INFO";
            case LogType.Exception: return "EXCEPTION";
            default: return "UNKNOWN";
        }
    }
}
