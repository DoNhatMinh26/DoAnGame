using UnityEngine;
using Unity.Netcode;
using DoAnGame.Multiplayer;
using DoAnGame.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// Script debug nâng cao để kiểm tra TẤT CẢ references và trạng thái multiplayer battle system
/// Attach vào BattleManager GameObject để debug
/// </summary>
public class MultiplayerBattleDebugger : MonoBehaviour
{
    [Header("Auto References")]
    private NetworkedMathBattleManager battleManager;
    private UIMultiplayerBattleController battleController;

    [Header("Debug Info")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showGUI = true;
    [SerializeField] private bool autoCheckOnStart = true;
    
    [Header("Validation Results")]
    [SerializeField] private List<string> errors = new List<string>();
    [SerializeField] private List<string> warnings = new List<string>();
    [SerializeField] private List<string> success = new List<string>();

    [Header("Export Settings")]
    [SerializeField] private string exportFileName = "BattleDebugReport.txt";
    [SerializeField] private bool autoExportOnValidate = true;
    [SerializeField] private bool captureConsoleLogs = true;
    [SerializeField] private int maxLogEntries = 500;

    // Console log capture
    private List<string> capturedLogs = new List<string>();
    private bool isCapturingLogs = false;

    private void Start()
    {
        battleManager = GetComponent<NetworkedMathBattleManager>();
        if (battleManager == null)
        {
            battleManager = FindObjectOfType<NetworkedMathBattleManager>(true); // Include inactive
        }

        // KHÔNG validate ngay trong Start vì GameplayPanel có thể inactive
        // Sẽ validate khi user bấm F3 hoặc khi panel active

        if (showDebugLogs)
        {
            Debug.Log("=== MULTIPLAYER BATTLE DEBUGGER ===");
            Debug.Log($"BattleManager found: {battleManager != null}");
            Debug.Log("Bấm F3 để validate sau khi vào battle scene");
        }

        // Subscribe vào events để debug
        if (battleManager != null)
        {
            battleManager.OnQuestionGenerated += OnQuestionGeneratedDebug;
            battleManager.OnAnswerResult += OnAnswerResultDebug;
            battleManager.OnMatchEnded += OnMatchEndedDebug;
            
            Debug.Log("✅ Subscribed to BattleManager events for debugging");
        }

        // Start capturing console logs
        if (captureConsoleLogs)
        {
            StartCapturingLogs();
        }
    }

    /// <summary>
    /// Bắt đầu capture console logs
    /// </summary>
    private void StartCapturingLogs()
    {
        if (isCapturingLogs) return;

        isCapturingLogs = true;
        capturedLogs.Clear();
        Application.logMessageReceived += HandleLog;
        
        Debug.Log("📝 [Debugger] Started capturing console logs...");
    }

    /// <summary>
    /// Dừng capture console logs
    /// </summary>
    private void StopCapturingLogs()
    {
        if (!isCapturingLogs) return;

        isCapturingLogs = false;
        Application.logMessageReceived -= HandleLog;
        
        Debug.Log($"📝 [Debugger] Stopped capturing logs. Total captured: {capturedLogs.Count}");
    }

    /// <summary>
    /// Handler để capture mỗi log message
    /// </summary>
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (capturedLogs.Count >= maxLogEntries)
        {
            // Remove oldest log to keep within limit
            capturedLogs.RemoveAt(0);
        }

        string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
        string typePrefix = type switch
        {
            LogType.Error => "[ERROR]",
            LogType.Assert => "[ASSERT]",
            LogType.Warning => "[WARNING]",
            LogType.Log => "[LOG]",
            LogType.Exception => "[EXCEPTION]",
            _ => "[UNKNOWN]"
        };

        string logEntry = $"[{timestamp}] {typePrefix} {logString}";
        
        // Nếu là error/exception, thêm stack trace
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (!string.IsNullOrEmpty(stackTrace))
            {
                logEntry += $"\n{stackTrace}";
            }
        }

        capturedLogs.Add(logEntry);
    }

    /// <summary>
    /// Kiểm tra TẤT CẢ references và báo cáo chi tiết
    /// </summary>
    [ContextMenu("Validate All References")]
    public void ValidateAllReferences()
    {
        errors.Clear();
        warnings.Clear();
        success.Clear();

        Debug.Log("=== 🔍 VALIDATING ALL REFERENCES ===");

        // Find components (include inactive)
        if (battleController == null)
        {
            battleController = FindObjectOfType<UIMultiplayerBattleController>(true);
        }

        // 1. Validate BattleManager
        ValidateBattleManager();

        // 2. Validate BattleController
        ValidateBattleController();

        // 3. Validate Network
        ValidateNetwork();

        // Print summary
        PrintValidationSummary();
    }

    private void ValidateBattleManager()
    {
        Debug.Log("\n--- VALIDATING BATTLE MANAGER ---");

        if (battleManager == null)
        {
            errors.Add("❌ BattleManager: GameObject không tồn tại hoặc thiếu component NetworkedMathBattleManager");
            Debug.LogError("❌ BattleManager: NULL");
            return;
        }

        success.Add("✅ BattleManager: GameObject tồn tại");

        // Check NetworkObject (CẦN CÓ để sync NetworkVariable!)
        var netObj = battleManager.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            success.Add("✅ BattleManager: Có NetworkObject component (cần thiết để sync)");
            Debug.Log($"✅ BattleManager có NetworkObject (IsSpawned={netObj.IsSpawned})");
            
            // Check NetworkObject configuration
            if (netObj.AlwaysReplicateAsRoot)
            {
                success.Add("✅ NetworkObject: AlwaysReplicateAsRoot = TRUE");
            }
            else
            {
                warnings.Add("⚠️ NetworkObject: AlwaysReplicateAsRoot = FALSE (nên bật)");
            }
        }
        else
        {
            errors.Add("❌ BattleManager: Thiếu NetworkObject component!");
            Debug.LogError("❌ BattleManager thiếu NetworkObject - NetworkVariable sẽ không sync!");
        }

        // Check GameRules reference
        var gameRulesField = battleManager.GetType().GetField("gameRules", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (gameRulesField != null)
        {
            var gameRules = gameRulesField.GetValue(battleManager) as GameRulesConfig;
            if (gameRules == null)
            {
                errors.Add("❌ BattleManager.gameRules: Chưa gán DefaultGameRules.asset");
                Debug.LogError("❌ GameRules chưa gán!");
            }
            else
            {
                success.Add($"✅ BattleManager.gameRules: {gameRules.name}");
            }
        }

        // Check LevelData reference
        var levelDataField = battleManager.GetType().GetField("levelData", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (levelDataField != null)
        {
            var levelData = levelDataField.GetValue(battleManager) as LevelGenerate;
            if (levelData == null)
            {
                errors.Add("❌ BattleManager.levelData: Chưa gán LevelGenerate asset");
                Debug.LogError("❌ LevelData chưa gán!");
            }
            else
            {
                success.Add($"✅ BattleManager.levelData: {levelData.name}");
            }
        }

        // Check PlayerStatePrefab reference
        var prefabField = battleManager.GetType().GetField("playerStatePrefab", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (prefabField != null)
        {
            var prefab = prefabField.GetValue(battleManager) as GameObject;
            if (prefab == null)
            {
                errors.Add("❌ BattleManager.playerStatePrefab: Chưa gán NetworkedPlayerState.prefab");
                Debug.LogError("❌ PlayerStatePrefab chưa gán!");
            }
            else
            {
                success.Add($"✅ BattleManager.playerStatePrefab: {prefab.name}");

                // Check prefab có NetworkObject không
                var prefabNetObj = prefab.GetComponent<NetworkObject>();
                if (prefabNetObj == null)
                {
                    errors.Add("❌ PlayerStatePrefab: Thiếu NetworkObject component");
                    Debug.LogError("❌ PlayerStatePrefab thiếu NetworkObject!");
                }
                else
                {
                    success.Add("✅ PlayerStatePrefab: Có NetworkObject component");
                }

                // Check prefab có NetworkedPlayerState không
                var prefabState = prefab.GetComponent<NetworkedPlayerState>();
                if (prefabState == null)
                {
                    errors.Add("❌ PlayerStatePrefab: Thiếu NetworkedPlayerState component");
                    Debug.LogError("❌ PlayerStatePrefab thiếu NetworkedPlayerState!");
                }
                else
                {
                    success.Add("✅ PlayerStatePrefab: Có NetworkedPlayerState component");
                }
            }
        }

        // Check Instance
        if (NetworkedMathBattleManager.Instance == null)
        {
            warnings.Add("⚠️ BattleManager.Instance: NULL (chưa Awake hoặc bị destroy)");
            Debug.LogWarning("⚠️ BattleManager.Instance NULL");
        }
        else
        {
            success.Add("✅ BattleManager.Instance: Đã khởi tạo");
        }
    }

    private void ValidateBattleController()
    {
        Debug.Log("\n--- VALIDATING BATTLE CONTROLLER ---");

        if (battleController == null)
        {
            errors.Add("❌ UIMultiplayerBattleController: Không tìm thấy trong scene");
            Debug.LogError("❌ BattleController: NULL");
            return;
        }

        success.Add($"✅ UIMultiplayerBattleController: Tìm thấy trên {battleController.gameObject.name}");

        // Check active
        if (!battleController.gameObject.activeInHierarchy)
        {
            warnings.Add("⚠️ BattleController: GameObject không active");
            Debug.LogWarning("⚠️ BattleController không active");
        }
        else
        {
            success.Add("✅ BattleController: GameObject active");
        }

        // Check enabled
        if (!battleController.enabled)
        {
            warnings.Add("⚠️ BattleController: Component bị disable");
            Debug.LogWarning("⚠️ BattleController bị disable");
        }
        else
        {
            success.Add("✅ BattleController: Component enabled");
        }

        // Check battleManager reference
        var bmField = battleController.GetType().GetField("battleManager", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (bmField != null)
        {
            var bm = bmField.GetValue(battleController) as NetworkedMathBattleManager;
            if (bm == null)
            {
                warnings.Add("⚠️ BattleController.battleManager: Chưa gán (sẽ auto-find)");
                Debug.LogWarning("⚠️ BattleController.battleManager chưa gán");
            }
            else
            {
                success.Add("✅ BattleController.battleManager: Đã gán");
            }
        }

        // Check questionText reference
        var qtField = battleController.GetType().GetField("questionText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (qtField != null)
        {
            var qt = qtField.GetValue(battleController) as TMP_Text;
            if (qt == null)
            {
                errors.Add("❌ BattleController.questionText: Chưa gán (câu hỏi sẽ không hiển thị)");
                Debug.LogError("❌ BattleController.questionText chưa gán!");
            }
            else
            {
                success.Add($"✅ BattleController.questionText: {qt.gameObject.name}");
            }
        }

        // Check answerSlot reference
        var slotField = battleController.GetType().GetField("answerSlot", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (slotField != null)
        {
            var slot = slotField.GetValue(battleController) as GameObject;
            if (slot == null)
            {
                errors.Add("❌ BattleController.answerSlot: Chưa gán");
                Debug.LogError("❌ BattleController.answerSlot chưa gán!");
            }
            else
            {
                success.Add($"✅ BattleController.answerSlot: {slot.name}");
            }
        }

        // Check answerChoices array
        var choicesField = battleController.GetType().GetField("answerChoices", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (choicesField != null)
        {
            var choices = choicesField.GetValue(battleController) as DoAnGame.Multiplayer.MultiplayerDragAndDrop[];
            if (choices == null || choices.Length == 0)
            {
                errors.Add("❌ BattleController.answerChoices: Chưa gán (đáp án sẽ không hiển thị)");
                Debug.LogError("❌ BattleController.answerChoices chưa gán!");
            }
            else
            {
                success.Add($"✅ BattleController.answerChoices: {choices.Length} đáp án");
                
                // Check từng choice
                for (int i = 0; i < choices.Length; i++)
                {
                    if (choices[i] == null)
                    {
                        errors.Add($"❌ BattleController.answerChoices[{i}]: NULL");
                        Debug.LogError($"❌ answerChoices[{i}] NULL!");
                    }
                    else
                    {
                        success.Add($"✅ answerChoices[{i}]: {choices[i].gameObject.name}");
                    }
                }
            }
        }
    }

    private void ValidateNetwork()
    {
        Debug.Log("\n--- VALIDATING NETWORK ---");

        if (NetworkManager.Singleton == null)
        {
            warnings.Add("⚠️ NetworkManager: NULL (chưa start multiplayer)");
            Debug.LogWarning("⚠️ NetworkManager NULL");
            return;
        }

        success.Add("✅ NetworkManager: Tồn tại");

        if (!NetworkManager.Singleton.IsListening)
        {
            warnings.Add("⚠️ NetworkManager: Chưa listening (chưa connect)");
            Debug.LogWarning("⚠️ NetworkManager chưa listening");
        }
        else
        {
            success.Add("✅ NetworkManager: Đang listening");
            success.Add($"✅ Connected Clients: {NetworkManager.Singleton.ConnectedClientsIds.Count}");
        }
    }

    private void PrintValidationSummary()
    {
        Debug.Log("\n=== 📊 VALIDATION SUMMARY ===");
        
        Debug.Log($"\n✅ SUCCESS ({success.Count}):");
        foreach (var s in success)
        {
            Debug.Log(s);
        }

        if (warnings.Count > 0)
        {
            Debug.Log($"\n⚠️ WARNINGS ({warnings.Count}):");
            foreach (var w in warnings)
            {
                Debug.LogWarning(w);
            }
        }

        if (errors.Count > 0)
        {
            Debug.Log($"\n❌ ERRORS ({errors.Count}):");
            foreach (var e in errors)
            {
                Debug.LogError(e);
            }
        }

        // Final verdict
        if (errors.Count == 0 && warnings.Count == 0)
        {
            Debug.Log("\n🎉 TẤT CẢ REFERENCES ĐÃ ĐÚNG! Hệ thống sẵn sàng!");
        }
        else if (errors.Count == 0)
        {
            Debug.Log($"\n⚠️ Có {warnings.Count} cảnh báo nhưng hệ thống vẫn có thể hoạt động");
        }
        else
        {
            Debug.LogError($"\n❌ Có {errors.Count} lỗi PHẢI SỬA trước khi test!");
        }

        // Auto export to file
        if (autoExportOnValidate)
        {
            ExportDebugReport();
        }
    }

    /// <summary>
    /// Export toàn bộ debug info ra file TXT
    /// </summary>
    [ContextMenu("Export Debug Report to TXT")]
    public void ExportDebugReport()
    {
        StringBuilder report = new StringBuilder();
        
        report.AppendLine("=".PadRight(80, '='));
        report.AppendLine("MULTIPLAYER BATTLE DEBUG REPORT");
        report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine("=".PadRight(80, '='));
        report.AppendLine();

        // 1. NETWORK STATUS
        report.AppendLine("--- NETWORK STATUS ---");
        if (NetworkManager.Singleton != null)
        {
            report.AppendLine($"NetworkManager: EXISTS");
            report.AppendLine($"  IsServer: {NetworkManager.Singleton.IsServer}");
            report.AppendLine($"  IsClient: {NetworkManager.Singleton.IsClient}");
            report.AppendLine($"  IsHost: {NetworkManager.Singleton.IsHost}");
            report.AppendLine($"  IsListening: {NetworkManager.Singleton.IsListening}");
            report.AppendLine($"  Connected Clients: {NetworkManager.Singleton.ConnectedClientsIds.Count}");
            
            if (NetworkManager.Singleton.ConnectedClientsIds.Count > 0)
            {
                report.AppendLine($"  Client IDs: [{string.Join(", ", NetworkManager.Singleton.ConnectedClientsIds)}]");
            }
        }
        else
        {
            report.AppendLine("NetworkManager: NULL");
        }
        report.AppendLine();

        // 2. BATTLE MANAGER STATUS
        report.AppendLine("--- BATTLE MANAGER STATUS ---");
        if (battleManager != null)
        {
            report.AppendLine($"BattleManager GameObject: {battleManager.gameObject.name}");
            report.AppendLine($"  Active: {battleManager.gameObject.activeInHierarchy}");
            report.AppendLine($"  Enabled: {battleManager.enabled}");
            
            var netObj = battleManager.GetComponent<NetworkObject>();
            report.AppendLine($"  Has NetworkObject: {netObj != null} {(netObj != null ? "✅ ĐÚNG (cần để sync NetworkVariable)" : "❌ SAI! PHẢI THÊM!")}");
            if (netObj != null)
            {
                report.AppendLine($"    - IsSpawned: {netObj.IsSpawned}");
                report.AppendLine($"    - AlwaysReplicateAsRoot: {netObj.AlwaysReplicateAsRoot}");
            }
            
            report.AppendLine($"  Instance: {NetworkedMathBattleManager.Instance != null}");
            report.AppendLine($"  IsSpawned: {battleManager.IsSpawned}");
            report.AppendLine($"  Match Started: {battleManager.MatchStarted.Value}");
            report.AppendLine($"  Match Ended: {battleManager.MatchEnded.Value}");
            report.AppendLine($"  Current Grade: {battleManager.CurrentGrade.Value}");
            report.AppendLine($"  Current Difficulty: {battleManager.CurrentDifficulty.Value}");
            report.AppendLine($"  Current Question: '{battleManager.CurrentQuestion.Value}'");
            report.AppendLine($"  Correct Answer: {battleManager.CorrectAnswer.Value}");
            report.AppendLine($"  Time Remaining: {battleManager.TimeRemaining.Value:F2}s");
            report.AppendLine($"  Winner ID: {battleManager.WinnerId.Value}");
            
            var p1 = battleManager.GetPlayer1State();
            var p2 = battleManager.GetPlayer2State();
            report.AppendLine($"  Player 1 State: {(p1 != null ? "FOUND" : "NULL")}");
            if (p1 != null)
            {
                report.AppendLine($"    - Health: {p1.CurrentHealth.Value}/{p1.MaxHealth.Value}");
                report.AppendLine($"    - Score: {p1.Score.Value}");
                report.AppendLine($"    - IsAlive: {p1.IsAlive()}");
            }
            report.AppendLine($"  Player 2 State: {(p2 != null ? "FOUND" : "NULL")}");
            if (p2 != null)
            {
                report.AppendLine($"    - Health: {p2.CurrentHealth.Value}/{p2.MaxHealth.Value}");
                report.AppendLine($"    - Score: {p2.Score.Value}");
                report.AppendLine($"    - IsAlive: {p2.IsAlive()}");
            }

            // Check references using reflection
            var gameRulesField = battleManager.GetType().GetField("gameRules", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (gameRulesField != null)
            {
                var gameRules = gameRulesField.GetValue(battleManager) as GameRulesConfig;
                report.AppendLine($"  GameRules Reference: {(gameRules != null ? gameRules.name : "NULL ❌")}");
            }

            var levelDataField = battleManager.GetType().GetField("levelData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (levelDataField != null)
            {
                var levelData = levelDataField.GetValue(battleManager) as LevelGenerate;
                report.AppendLine($"  LevelData Reference: {(levelData != null ? levelData.name : "NULL ❌")}");
            }

            var prefabField = battleManager.GetType().GetField("playerStatePrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prefabField != null)
            {
                var prefab = prefabField.GetValue(battleManager) as GameObject;
                report.AppendLine($"  PlayerStatePrefab Reference: {(prefab != null ? prefab.name : "NULL ❌")}");
                if (prefab != null)
                {
                    var prefabNetObj = prefab.GetComponent<NetworkObject>();
                    var prefabState = prefab.GetComponent<NetworkedPlayerState>();
                    report.AppendLine($"    - Has NetworkObject: {prefabNetObj != null}");
                    report.AppendLine($"    - Has NetworkedPlayerState: {prefabState != null}");
                }
            }
        }
        else
        {
            report.AppendLine("BattleManager: NULL ❌");
        }
        report.AppendLine();

        // 3. BATTLE CONTROLLER STATUS
        report.AppendLine("--- BATTLE CONTROLLER STATUS ---");
        if (battleController != null)
        {
            report.AppendLine($"UIMultiplayerBattleController: FOUND on {battleController.gameObject.name}");
            report.AppendLine($"  Active: {battleController.gameObject.activeInHierarchy}");
            report.AppendLine($"  Enabled: {battleController.enabled}");

            var bmField = battleController.GetType().GetField("battleManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (bmField != null)
            {
                var bm = bmField.GetValue(battleController) as NetworkedMathBattleManager;
                report.AppendLine($"  BattleManager Reference: {(bm != null ? "ASSIGNED" : "NULL (will auto-find)")}");
            }

            var qtField = battleController.GetType().GetField("questionText", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (qtField != null)
            {
                var qt = qtField.GetValue(battleController) as TMP_Text;
                report.AppendLine($"  QuestionText Reference: {(qt != null ? qt.gameObject.name : "NULL ❌")}");
            }

            var slotField = battleController.GetType().GetField("answerSlot", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (slotField != null)
            {
                var slot = slotField.GetValue(battleController) as GameObject;
                report.AppendLine($"  AnswerSlot Reference: {(slot != null ? slot.name : "NULL ❌")}");
            }

            var choicesField = battleController.GetType().GetField("answerChoices", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (choicesField != null)
            {
                var choices = choicesField.GetValue(battleController) as DoAnGame.Multiplayer.MultiplayerDragAndDrop[];
                report.AppendLine($"  AnswerChoices Array: {(choices != null ? $"{choices.Length} elements" : "NULL ❌")}");
                if (choices != null)
                {
                    for (int i = 0; i < choices.Length; i++)
                    {
                        report.AppendLine($"    [{i}]: {(choices[i] != null ? choices[i].gameObject.name : "NULL ❌")}");
                    }
                }
            }
        }
        else
        {
            report.AppendLine("UIMultiplayerBattleController: NULL ❌");
        }
        report.AppendLine();

        // 4. DRAG DROP SYSTEM STATUS
        report.AppendLine("--- DRAG DROP SYSTEM STATUS ---");
        report.AppendLine("ℹ️ Multiplayer uses MultiplayerDragAndDrop component (integrated logic)");
        report.AppendLine("ℹ️ MultiplayerDragDropAdapter is NOT needed - logic is in MultiplayerDragAndDrop.cs");
        
        // Check MultiplayerDragAndDrop components
        var dragComponents = FindObjectsOfType<DoAnGame.Multiplayer.MultiplayerDragAndDrop>(true);
        report.AppendLine($"MultiplayerDragAndDrop components found: {dragComponents.Length}");
        foreach (var drag in dragComponents)
        {
            report.AppendLine($"  - {drag.gameObject.name} (Active: {drag.gameObject.activeInHierarchy})");
        }
        report.AppendLine();

        // 5. VALIDATION RESULTS
        report.AppendLine("--- VALIDATION RESULTS ---");
        report.AppendLine($"Total Success: {success.Count}");
        report.AppendLine($"Total Warnings: {warnings.Count}");
        report.AppendLine($"Total Errors: {errors.Count}");
        report.AppendLine();

        if (success.Count > 0)
        {
            report.AppendLine("SUCCESS LIST:");
            foreach (var s in success)
            {
                report.AppendLine($"  {s}");
            }
            report.AppendLine();
        }

        if (warnings.Count > 0)
        {
            report.AppendLine("WARNINGS LIST:");
            foreach (var w in warnings)
            {
                report.AppendLine($"  {w}");
            }
            report.AppendLine();
        }

        if (errors.Count > 0)
        {
            report.AppendLine("ERRORS LIST:");
            foreach (var e in errors)
            {
                report.AppendLine($"  {e}");
            }
            report.AppendLine();
        }

        // 6. FINAL VERDICT
        report.AppendLine("--- FINAL VERDICT ---");
        if (errors.Count == 0 && warnings.Count == 0)
        {
            report.AppendLine("✅ TẤT CẢ REFERENCES ĐÃ ĐÚNG! Hệ thống sẵn sàng!");
        }
        else if (errors.Count == 0)
        {
            report.AppendLine($"⚠️ Có {warnings.Count} cảnh báo nhưng hệ thống vẫn có thể hoạt động");
        }
        else
        {
            report.AppendLine($"❌ Có {errors.Count} lỗi PHẢI SỬA trước khi test!");
        }
        report.AppendLine();

        // 7. RECOMMENDATIONS
        if (errors.Count > 0)
        {
            report.AppendLine("--- RECOMMENDED ACTIONS ---");
            
            if (errors.Exists(e => e.Contains("Thiếu NetworkObject")))
            {
                report.AppendLine("1. THÊM NetworkObject vào BattleManager GameObject");
                report.AppendLine("   - Chọn BattleManager trong Hierarchy");
                report.AppendLine("   - Inspector → Add Component → Network Object");
                report.AppendLine("   - Cấu hình: Always Replicate As Root = TRUE");
                report.AppendLine("   - Cấu hình: Dont Destroy With Owner = TRUE");
            }

            if (errors.Exists(e => e.Contains("gameRules")))
            {
                report.AppendLine("2. GÁN DefaultGameRules.asset vào BattleManager");
                report.AppendLine("   - Vị trí: Assets/Script/.../Resources/DefaultGameRules.asset");
            }

            if (errors.Exists(e => e.Contains("levelData")))
            {
                report.AppendLine("3. GÁN LevelGenerate asset vào BattleManager");
                report.AppendLine("   - Search 'LevelGenerate' trong Project window");
            }

            if (errors.Exists(e => e.Contains("playerStatePrefab")))
            {
                report.AppendLine("4. GÁN NetworkedPlayerState.prefab vào BattleManager");
                report.AppendLine("   - Vị trí: Assets/Script/.../Resources/NetworkedPlayerState.prefab");
            }

            if (errors.Exists(e => e.Contains("questionText")))
            {
                report.AppendLine("5. GÁN cauhoiText vào UIMultiplayerBattleController");
            }

            if (errors.Exists(e => e.Contains("answerSlot")))
            {
                report.AppendLine("6. GÁN Slot GameObject vào UIMultiplayerBattleController");
            }

            if (errors.Exists(e => e.Contains("answerChoices")))
            {
                report.AppendLine("7. GÁN Answer_0/1/2/3 vào UIMultiplayerBattleController");
                report.AppendLine("   - Mỗi Answer phải có MultiplayerDragAndDrop component");
            }
        }

        report.AppendLine();
        report.AppendLine("=".PadRight(80, '='));
        report.AppendLine("END OF REPORT");
        report.AppendLine("=".PadRight(80, '='));

        // 8. CONSOLE LOGS (if captured)
        if (captureConsoleLogs && capturedLogs.Count > 0)
        {
            report.AppendLine();
            report.AppendLine();
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine("CAPTURED CONSOLE LOGS");
            report.AppendLine($"Total Entries: {capturedLogs.Count} (Max: {maxLogEntries})");
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine();

            // Group logs by type
            var errorLogs = capturedLogs.Where(l => l.Contains("[ERROR]") || l.Contains("[EXCEPTION]")).ToList();
            var warningLogs = capturedLogs.Where(l => l.Contains("[WARNING]")).ToList();
            var infoLogs = capturedLogs.Where(l => l.Contains("[LOG]")).ToList();

            // Print errors first (most important)
            if (errorLogs.Count > 0)
            {
                report.AppendLine($"--- ERRORS & EXCEPTIONS ({errorLogs.Count}) ---");
                foreach (var log in errorLogs)
                {
                    report.AppendLine(log);
                    report.AppendLine();
                }
            }

            // Then warnings
            if (warningLogs.Count > 0)
            {
                report.AppendLine($"--- WARNINGS ({warningLogs.Count}) ---");
                foreach (var log in warningLogs)
                {
                    report.AppendLine(log);
                }
                report.AppendLine();
            }

            // Finally info logs (filtered to show only relevant ones)
            var relevantInfoLogs = infoLogs.Where(l => 
                l.Contains("[UIRoom]") ||
                l.Contains("[BattleManager]") ||
                l.Contains("[DEBUG]") ||
                l.Contains("Initialized") ||
                l.Contains("Question") ||
                l.Contains("Answer") ||
                l.Contains("Match") ||
                l.Contains("Player") ||
                l.Contains("Spawned") ||
                l.Contains("Started") ||
                l.Contains("Subscribed")
            ).ToList();

            if (relevantInfoLogs.Count > 0)
            {
                report.AppendLine($"--- RELEVANT INFO LOGS ({relevantInfoLogs.Count}/{infoLogs.Count}) ---");
                report.AppendLine("(Filtered to show only battle-related logs)");
                report.AppendLine();
                foreach (var log in relevantInfoLogs)
                {
                    report.AppendLine(log);
                }
                report.AppendLine();
            }

            // Show all logs if requested
            if (infoLogs.Count > relevantInfoLogs.Count)
            {
                report.AppendLine($"--- ALL INFO LOGS ({infoLogs.Count}) ---");
                report.AppendLine("(Complete log history)");
                report.AppendLine();
                foreach (var log in infoLogs)
                {
                    report.AppendLine(log);
                }
                report.AppendLine();
            }

            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine("END OF CONSOLE LOGS");
            report.AppendLine("=".PadRight(80, '='));
        }
        else if (captureConsoleLogs && capturedLogs.Count == 0)
        {
            report.AppendLine();
            report.AppendLine("--- CONSOLE LOGS ---");
            report.AppendLine("⚠️ No logs captured yet. Make sure 'Capture Console Logs' is enabled.");
            report.AppendLine();
        }

        // Write to file
        string filePath = System.IO.Path.Combine(Application.dataPath, "..", exportFileName);
        try
        {
            System.IO.File.WriteAllText(filePath, report.ToString());
            Debug.Log($"✅ Debug report exported to: {filePath}");
            Debug.Log($"📄 File size: {new System.IO.FileInfo(filePath).Length} bytes");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Failed to export debug report: {ex.Message}");
        }
    }

    private void OnQuestionGeneratedDebug(string question, int[] choices)
    {
        Debug.Log($"🎯 [DEBUG] Question Generated: {question}");
        Debug.Log($"🎯 [DEBUG] Choices: [{string.Join(", ", choices)}]");
    }

    private void OnAnswerResultDebug(int playerId, bool isCorrect, long responseTime)
    {
        Debug.Log($"✅ [DEBUG] Answer Result: Player {playerId}, Correct={isCorrect}, Time={responseTime}ms");
    }

    private void OnMatchEndedDebug(int winnerId, int winnerHealth)
    {
        Debug.Log($"🏆 [DEBUG] Match Ended: Winner={winnerId}, Health={winnerHealth}");
    }

    private void Update()
    {
        // Kiểm tra mỗi frame
        if (Input.GetKeyDown(KeyCode.F1))
        {
            PrintDebugInfo();
        }

        // Validate all references
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ValidateAllReferences();
        }

        // Export debug report
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ExportDebugReport();
        }

        // Test sinh câu hỏi (chỉ Host)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TestGenerateQuestion();
        }
    }

    private void PrintDebugInfo()
    {
        Debug.Log("=== BATTLE SYSTEM DEBUG INFO ===");
        
        // Network status
        if (NetworkManager.Singleton != null)
        {
            Debug.Log($"Network Status:");
            Debug.Log($"  - IsServer: {NetworkManager.Singleton.IsServer}");
            Debug.Log($"  - IsClient: {NetworkManager.Singleton.IsClient}");
            Debug.Log($"  - IsHost: {NetworkManager.Singleton.IsHost}");
            Debug.Log($"  - IsListening: {NetworkManager.Singleton.IsListening}");
            Debug.Log($"  - Connected Clients: {NetworkManager.Singleton.ConnectedClientsIds.Count}");
        }
        else
        {
            Debug.LogError("❌ NetworkManager.Singleton is NULL!");
        }

        // BattleManager status
        if (battleManager != null)
        {
            Debug.Log($"BattleManager Status:");
            Debug.Log($"  - Instance: {NetworkedMathBattleManager.Instance != null}");
            Debug.Log($"  - IsSpawned: {battleManager.IsSpawned}");
            Debug.Log($"  - Match Started: {battleManager.MatchStarted.Value}");
            Debug.Log($"  - Match Ended: {battleManager.MatchEnded.Value}");
            Debug.Log($"  - Current Grade: {battleManager.CurrentGrade.Value}");
            Debug.Log($"  - Current Difficulty: {battleManager.CurrentDifficulty.Value}");
            Debug.Log($"  - Current Question: {battleManager.CurrentQuestion.Value}");
            Debug.Log($"  - Time Remaining: {battleManager.TimeRemaining.Value}");
            
            var p1 = battleManager.GetPlayer1State();
            var p2 = battleManager.GetPlayer2State();
            Debug.Log($"  - Player 1 State: {(p1 != null ? "Found" : "NULL")}");
            Debug.Log($"  - Player 2 State: {(p2 != null ? "Found" : "NULL")}");
        }
        else
        {
            Debug.LogError("❌ BattleManager is NULL!");
        }

        // BattleController status
        if (battleController != null)
        {
            Debug.Log($"BattleController Status:");
            Debug.Log($"  - GameObject: {battleController.gameObject.name}");
            Debug.Log($"  - Active: {battleController.gameObject.activeInHierarchy}");
            Debug.Log($"  - Enabled: {battleController.enabled}");
        }
        else
        {
            Debug.LogError("❌ BattleController is NULL!");
        }
    }

    private void TestGenerateQuestion()
    {
        if (battleManager == null)
        {
            Debug.LogError("❌ Cannot test - BattleManager is NULL!");
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("⚠️ Only Host can generate questions!");
            return;
        }

        Debug.Log("🧪 Testing question generation...");
        battleManager.GenerateQuestionServerRpc();
    }

    private void OnGUI()
    {
        if (!showGUI) return;

        GUILayout.BeginArea(new Rect(10, 10, 450, 700));
        GUILayout.BeginVertical("box");

        GUILayout.Label("=== MULTIPLAYER BATTLE DEBUGGER ===", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Validation Summary
        if (errors.Count > 0 || warnings.Count > 0)
        {
            GUILayout.Label("VALIDATION STATUS:", EditorStyles.boldLabel);
            
            if (errors.Count > 0)
            {
                GUI.color = Color.red;
                GUILayout.Label($"❌ ERRORS: {errors.Count}");
                GUI.color = Color.white;
            }
            
            if (warnings.Count > 0)
            {
                GUI.color = Color.yellow;
                GUILayout.Label($"⚠️ WARNINGS: {warnings.Count}");
                GUI.color = Color.white;
            }
            
            if (errors.Count == 0 && warnings.Count == 0)
            {
                GUI.color = Color.green;
                GUILayout.Label("✅ ALL GOOD!");
                GUI.color = Color.white;
            }
            
            GUILayout.Space(10);
        }

        // Network Status
        GUILayout.Label("NETWORK STATUS:", EditorStyles.boldLabel);
        if (NetworkManager.Singleton != null)
        {
            GUILayout.Label($"IsServer: {NetworkManager.Singleton.IsServer}");
            GUILayout.Label($"IsClient: {NetworkManager.Singleton.IsClient}");
            GUILayout.Label($"Connected: {NetworkManager.Singleton.ConnectedClientsIds.Count}/2");
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Label("❌ NetworkManager NULL");
            GUI.color = Color.white;
        }
        GUILayout.Space(10);

        // BattleManager Status
        GUILayout.Label("BATTLE MANAGER:", EditorStyles.boldLabel);
        if (battleManager != null)
        {
            var netObj = battleManager.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                GUI.color = Color.green;
                GUILayout.Label("✅ Có NetworkObject (cần để sync)");
                GUI.color = Color.white;
                GUILayout.Label($"  IsSpawned: {netObj.IsSpawned}");
            }
            else
            {
                GUI.color = Color.red;
                GUILayout.Label("❌ Thiếu NetworkObject!");
                GUI.color = Color.white;
            }
            
            GUILayout.Label($"Instance: {(NetworkedMathBattleManager.Instance != null ? "✅" : "❌")}");
            GUILayout.Label($"IsSpawned: {battleManager.IsSpawned}");
            GUILayout.Label($"Match Started: {battleManager.MatchStarted.Value}");
            GUILayout.Label($"Grade: {battleManager.CurrentGrade.Value}");
            GUILayout.Label($"Difficulty: {battleManager.CurrentDifficulty.Value}");
            
            var question = battleManager.CurrentQuestion.Value.ToString();
            if (string.IsNullOrEmpty(question))
            {
                GUI.color = Color.yellow;
                GUILayout.Label("Question: (rỗng)");
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label($"Question: {question}");
            }
            
            GUILayout.Label($"Timer: {battleManager.TimeRemaining.Value:F1}s");
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Label("❌ BattleManager NULL");
            GUI.color = Color.white;
        }
        GUILayout.Space(10);

        // BattleController Status
        GUILayout.Label("BATTLE CONTROLLER:", EditorStyles.boldLabel);
        if (battleController != null)
        {
            GUILayout.Label($"GameObject: {battleController.gameObject.name}");
            GUILayout.Label($"Active: {battleController.gameObject.activeInHierarchy}");
            GUILayout.Label($"Enabled: {battleController.enabled}");
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Label("❌ BattleController NULL");
            GUI.color = Color.white;
        }
        GUILayout.Space(10);

        // Debug Buttons
        GUILayout.Label("DEBUG ACTIONS:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("🔍 Validate All References (F3)"))
        {
            ValidateAllReferences();
        }
        
        if (GUILayout.Button("📊 Print Debug Info (F1)"))
        {
            PrintDebugInfo();
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("🧪 Test Generate Question (F2)"))
            {
                TestGenerateQuestion();
            }
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button("🧪 Test Generate Question (Host only)");
            GUI.enabled = true;
        }

        if (GUILayout.Button("📄 Export Debug Report (F4)"))
        {
            ExportDebugReport();
        }

        GUILayout.Space(10);
        
        // Console log status
        if (captureConsoleLogs)
        {
            GUI.color = Color.green;
            GUILayout.Label($"📝 Capturing logs: {capturedLogs.Count}/{maxLogEntries}");
            GUI.color = Color.white;
        }
        
        // Quick tips
        if (errors.Count > 0)
        {
            GUI.color = Color.red;
            GUILayout.Label("⚠️ Bấm F3 để xem chi tiết lỗi!");
            GUI.color = Color.white;
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.OnQuestionGenerated -= OnQuestionGeneratedDebug;
            battleManager.OnAnswerResult -= OnAnswerResultDebug;
            battleManager.OnMatchEnded -= OnMatchEndedDebug;
        }

        // Stop capturing logs
        StopCapturingLogs();
    }

    // Helper class for EditorStyles in runtime
    private static class EditorStyles
    {
        public static GUIStyle boldLabel
        {
            get
            {
                var style = new GUIStyle(GUI.skin.label);
                style.fontStyle = FontStyle.Bold;
                return style;
            }
        }
    }
}
