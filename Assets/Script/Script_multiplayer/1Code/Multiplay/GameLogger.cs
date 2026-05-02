using System;
using System.IO;
using UnityEngine;
using Unity.Netcode;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Ghi log ra file txt riêng cho Host và Client
    /// Tự động xoá log cũ khi chạy lại
    /// </summary>
    public class GameLogger : MonoBehaviour
    {
        private static GameLogger instance;
        private StreamWriter logWriter;
        private string logFilePath;
        private bool isInitialized = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Đợi NetworkManager ready để biết Host hay Client
            StartCoroutine(InitializeLogger());
        }

        private System.Collections.IEnumerator InitializeLogger()
        {
            // Đợi NetworkManager spawn
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                yield return new WaitForSeconds(0.5f);
            }

            // Xác định Host hay Client
            string role = NetworkManager.Singleton.IsServer ? "HOST" : "CLIENT";
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            // Lưu vào thư mục project
            string logDir = Path.Combine(Application.dataPath, "Script", "Script_multiplayer", "1Code", "Multiplay");
            logFilePath = Path.Combine(logDir, $"GameLog_{role}_{timestamp}.txt");

            // Xoá log cũ (nếu có)
            DeleteOldLogs(role, logDir);

            // Tạo file log mới
            try
            {
                logWriter = new StreamWriter(logFilePath, false);
                logWriter.AutoFlush = true;
                isInitialized = true;

                WriteLog($"=== GAME LOG START ({role}) ===");
                WriteLog($"Time: {DateTime.Now}");
                WriteLog($"Unity Version: {Application.unityVersion}");
                WriteLog($"Platform: {Application.platform}");
                WriteLog($"Log Path: {logFilePath}");
                WriteLog("================================\n");

                Debug.Log($"[GameLogger] Log file created: {logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameLogger] Failed to create log file: {ex.Message}");
            }

            // Subscribe vào Unity log
            Application.logMessageReceived += HandleLog;
        }

        private void DeleteOldLogs(string role, string logDir)
        {
            try
            {
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                string[] oldLogs = Directory.GetFiles(logDir, $"GameLog_{role}_*.txt");

                foreach (string oldLog in oldLogs)
                {
                    File.Delete(oldLog);
                    Debug.Log($"[GameLogger] Deleted old log: {oldLog}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameLogger] Failed to delete old logs: {ex.Message}");
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!isInitialized) return;

            string prefix = type switch
            {
                LogType.Error => "[ERROR]",
                LogType.Warning => "[WARNING]",
                LogType.Exception => "[EXCEPTION]",
                _ => "[INFO]"
            };

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logLine = $"[{timestamp}] {prefix} {logString}";

            WriteLog(logLine);

            // Ghi stack trace cho error và exception
            if (type == LogType.Error || type == LogType.Exception)
            {
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    WriteLog($"Stack Trace:\n{stackTrace}");
                }
            }
        }

        private void WriteLog(string message)
        {
            try
            {
                logWriter?.WriteLine(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameLogger] Failed to write log: {ex.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            if (isInitialized && logWriter != null)
            {
                try
                {
                    WriteLog("\n=== GAME LOG END ===");
                    WriteLog($"Time: {DateTime.Now}");
                    WriteLog("====================");

                    Application.logMessageReceived -= HandleLog;
                    
                    logWriter.Close();
                    logWriter.Dispose();
                    logWriter = null;
                    
                    isInitialized = false;

                    Debug.Log($"[GameLogger] Log saved to: {logFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameLogger] Error closing log: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            if (instance == this && isInitialized)
            {
                OnApplicationQuit();
            }
        }

        /// <summary>
        /// Ghi log thủ công (không qua Unity Debug.Log)
        /// </summary>
        public static void Log(string message)
        {
            if (instance != null && instance.isInitialized)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                instance.WriteLog($"[{timestamp}] [CUSTOM] {message}");
            }
        }

        /// <summary>
        /// Mở thư mục chứa log
        /// </summary>
        [ContextMenu("Open Log Folder")]
        public void OpenLogFolder()
        {
            string logDir = Path.Combine(Application.dataPath, "Script", "Script_multiplayer", "1Code", "Multiplay");
            Application.OpenURL(logDir);
        }
    }
}
