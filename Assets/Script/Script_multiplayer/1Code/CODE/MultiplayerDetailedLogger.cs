using System;
using System.IO;
using System.Text;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MultiplayerDetailedLogger
{
    private static readonly object SyncRoot = new object();
    private static bool initialized;
    private static bool shuttingDown;
    private static string logFilePath;
    private static StreamWriter writer;

    public static string CurrentLogFilePath => logFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        Initialize();
    }

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            if (initialized)
                return;

            initialized = true;
            shuttingDown = false;

            logFilePath = BuildPreferredLogPath();
            TryOpenWriter(logFilePath);

            if (writer == null)
            {
                logFilePath = BuildFallbackLogPath();
                TryOpenWriter(logFilePath);
            }

            Application.logMessageReceivedThreaded += HandleUnityLog;
            Application.quitting += HandleApplicationQuitting;
            SceneManager.sceneLoaded += HandleSceneLoaded;

            WriteRawLine("=== Multiplayer Detailed Logger Started ===");
            WriteRawLine($"SessionStartUtc={DateTime.UtcNow:O}");
            WriteRawLine($"ProjectDataPath={Application.dataPath}");
            WriteRawLine($"PersistentDataPath={Application.persistentDataPath}");
            WriteRawLine($"LogFile={logFilePath}");
            Trace("INIT", "Logger initialized successfully");
            Debug.Log("[MP-LOGGER] Detailed log file: " + logFilePath);
        }
    }

    public static void Trace(string category, string message)
    {
        if (!initialized)
            Initialize();

        string line = BuildLine("INFO", category, message);
        WriteRawLine(line);
    }

    public static void TraceWarning(string category, string message)
    {
        if (!initialized)
            Initialize();

        string line = BuildLine("WARN", category, message);
        WriteRawLine(line);
    }

    public static void TraceError(string category, string message)
    {
        if (!initialized)
            Initialize();

        string line = BuildLine("ERROR", category, message);
        WriteRawLine(line);
    }

    public static void TraceException(string category, Exception ex, string note = null)
    {
        if (!initialized)
            Initialize();

        string msg = note == null ? ex.ToString() : note + " | " + ex;
        string line = BuildLine("EXCEPTION", category, msg);
        WriteRawLine(line);
    }

    public static void TraceNetworkSnapshot(string category, string note = null)
    {
        string context = BuildRuntimeContext();
        string message = string.IsNullOrWhiteSpace(note) ? context : (note + " | " + context);
        Trace(category, message);
    }

    private static string BuildPreferredLogPath()
    {
        string root = Path.Combine(Application.dataPath, "Script", "Script_multiplayer", "1Code", "CODE", "log lỗi chi tiết");
        Directory.CreateDirectory(root);

        string fileName = $"mp_log_{DateTime.Now:yyyyMMdd_HHmmss}_{Mathf.Abs(Application.dataPath.GetHashCode())}.log";
        return Path.Combine(root, fileName);
    }

    private static string BuildFallbackLogPath()
    {
        string root = Path.Combine(Application.persistentDataPath, "log_loi_chi_tiet");
        Directory.CreateDirectory(root);

        string fileName = $"mp_log_{DateTime.Now:yyyyMMdd_HHmmss}_{Mathf.Abs(Application.dataPath.GetHashCode())}.log";
        return Path.Combine(root, fileName);
    }

    private static void TryOpenWriter(string path)
    {
        try
        {
            var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            writer = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
        }
        catch (Exception ex)
        {
            writer = null;
            Debug.LogWarning("[MP-LOGGER] Cannot open writer at path: " + path + " | " + ex.Message);
        }
    }

    private static void HandleUnityLog(string condition, string stackTrace, LogType type)
    {
        if (!initialized || shuttingDown)
            return;

        string level = type.ToString().ToUpperInvariant();
        string msg = condition ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(stackTrace) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            msg += " | stack=" + stackTrace.Replace('\n', ' ').Replace('\r', ' ');
        }

        string line = BuildLine(level, "UNITY", msg);
        WriteRawLine(line);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Trace("SCENE", $"Loaded scene={scene.name}, mode={mode}");
        TraceNetworkSnapshot("SCENE");
    }

    private static void HandleApplicationQuitting()
    {
        lock (SyncRoot)
        {
            if (shuttingDown)
                return;

            shuttingDown = true;
            WriteRawLine(BuildLine("INFO", "APP", "Application quitting"));

            Application.logMessageReceivedThreaded -= HandleUnityLog;
            Application.quitting -= HandleApplicationQuitting;
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            try
            {
                writer?.Flush();
                writer?.Dispose();
                writer = null;
            }
            catch
            {
                // ignore
            }
        }
    }

    private static string BuildLine(string level, string category, string message)
    {
        string utc = DateTime.UtcNow.ToString("O");
        string context = BuildRuntimeContext();
        return $"[{utc}] [{level}] [{category}] {message} | {context}";
    }

    private static string BuildRuntimeContext()
    {
        string scene = "unknown";
        try
        {
            scene = SceneManager.GetActiveScene().name;
        }
        catch
        {
            // ignore
        }

        string auth = "auth=none";
        try
        {
            if (AuthenticationService.Instance != null)
            {
                auth = AuthenticationService.Instance.IsSignedIn
                    ? $"auth=signed:{AuthenticationService.Instance.PlayerId}"
                    : "auth=unsigned";
            }
        }
        catch
        {
            // ignore
        }

        string net = "net=none";
        try
        {
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                net = $"net:listening={nm.IsListening},server={nm.IsServer},client={nm.IsClient},host={nm.IsHost},localId={nm.LocalClientId},connected={nm.ConnectedClientsIds.Count}";
            }
        }
        catch
        {
            // ignore
        }

        return $"scene={scene} | {auth} | {net}";
    }

    private static void WriteRawLine(string line)
    {
        lock (SyncRoot)
        {
            if (writer == null)
                return;

            try
            {
                writer.WriteLine(line);
            }
            catch
            {
                // ignore file I/O hiccups in diagnostics path
            }
        }
    }
}
