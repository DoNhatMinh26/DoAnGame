using UnityEngine;
using Unity.Netcode;
using DoAnGame.Multiplayer;
using DoAnGame.UI;

/// <summary>
/// Script debug health sync issues
/// Attach vào GameplayPanel để kiểm tra
/// </summary>
public class HealthSyncDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float debugInterval = 2f;

    private float nextDebugTime;

    private void Update()
    {
        if (Time.time < nextDebugTime)
            return;

        nextDebugTime = Time.time + debugInterval;
        DebugHealthSync();
    }

    [ContextMenu("Debug Health Sync")]
    public void DebugHealthSync()
    {
        if (!enableDebugLogs)
            return;

        Log("========== HEALTH SYNC DEBUG ==========");

        // 1. Check NetworkManager
        var net = NetworkManager.Singleton;
        if (net == null)
        {
            LogError("NetworkManager is NULL!");
            return;
        }

        Log("NetworkManager: IsHost=" + net.IsHost + ", IsClient=" + net.IsClient + ", IsServer=" + net.IsServer);
        Log("Connected clients: " + net.ConnectedClientsIds.Count);

        // 2. Check BattleManager
        var battleManager = NetworkedMathBattleManager.Instance;
        if (battleManager == null)
        {
            LogError("BattleManager is NULL!");
            return;
        }

        Log("BattleManager found");

        // Check NetworkObject
        var netObj = battleManager.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            LogError("BattleManager missing NetworkObject!");
            return;
        }

        Log("BattleManager NetworkObject IsSpawned: " + netObj.IsSpawned);

        // 3. Check Player States
        var p1 = battleManager.GetPlayer1State();
        var p2 = battleManager.GetPlayer2State();

        if (p1 == null)
        {
            LogError("Player1State is NULL!");
        }
        else
        {
            var p1NetObj = p1.GetComponent<NetworkObject>();
            Log("Player1State found:");
            Log("  NetworkObject IsSpawned: " + (p1NetObj != null ? p1NetObj.IsSpawned.ToString() : "NO_NETOBJ"));
            Log("  PlayerId: " + p1.PlayerId.Value);
            Log("  PlayerName: " + p1.PlayerName.Value);
            Log("  CurrentHealth: " + p1.CurrentHealth.Value);
            Log("  MaxHealth: " + p1.MaxHealth.Value);
            Log("  Score: " + p1.Score.Value);
        }

        if (p2 == null)
        {
            LogError("Player2State is NULL!");
        }
        else
        {
            var p2NetObj = p2.GetComponent<NetworkObject>();
            Log("Player2State found:");
            Log("  NetworkObject IsSpawned: " + (p2NetObj != null ? p2NetObj.IsSpawned.ToString() : "NO_NETOBJ"));
            Log("  PlayerId: " + p2.PlayerId.Value);
            Log("  PlayerName: " + p2.PlayerName.Value);
            Log("  CurrentHealth: " + p2.CurrentHealth.Value);
            Log("  MaxHealth: " + p2.MaxHealth.Value);
            Log("  Score: " + p2.Score.Value);
        }

        // 4. Check HealthUI
        var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
        if (healthUI == null)
        {
            LogError("MultiplayerHealthUI not found!");
        }
        else
        {
            Log("MultiplayerHealthUI found: " + healthUI.name);
            Log("  GameObject active: " + healthUI.gameObject.activeInHierarchy);
            Log("  Component enabled: " + healthUI.enabled);
        }

        Log("=======================================");
    }

    [ContextMenu("Force Health UI Reinit")]
    public void ForceHealthUIReinit()
    {
        var healthUI = FindObjectOfType<MultiplayerHealthUI>(true);
        if (healthUI != null)
        {
            Log("Forcing HealthUI re-initialization...");
            healthUI.SendMessage("RetryInit", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            LogError("MultiplayerHealthUI not found!");
        }
    }

    [ContextMenu("Force Player State Init")]
    public void ForcePlayerStateInit()
    {
        var battleManager = NetworkedMathBattleManager.Instance;
        if (battleManager == null)
        {
            LogError("BattleManager is NULL!");
            return;
        }

        var net = NetworkManager.Singleton;
        if (net == null || !net.IsServer)
        {
            LogError("Not running on server!");
            return;
        }

        Log("Force initializing player states...");

        // Force init Player 1
        var p1 = battleManager.GetPlayer1State();
        if (p1 != null)
        {
            p1.InitializeServerRpc(0, "Player 1", 10);
            Log("Force initialized Player1");
        }

        // Force init Player 2
        var p2 = battleManager.GetPlayer2State();
        if (p2 != null)
        {
            p2.InitializeServerRpc(1, "Player 2", 10);
            Log("Force initialized Player2");
        }
    }

    [ContextMenu("Test Damage Player 1")]
    public void TestDamagePlayer1()
    {
        var battleManager = NetworkedMathBattleManager.Instance;
        if (battleManager == null || !NetworkManager.Singleton.IsServer)
        {
            LogError("Must be on server!");
            return;
        }

        var p1 = battleManager.GetPlayer1State();
        if (p1 != null)
        {
            p1.TakeDamage(1);
            Log("Damaged Player1 by 1 HP");
        }
    }

    [ContextMenu("Test Damage Player 2")]
    public void TestDamagePlayer2()
    {
        var battleManager = NetworkedMathBattleManager.Instance;
        if (battleManager == null || !NetworkManager.Singleton.IsServer)
        {
            LogError("Must be on server!");
            return;
        }

        var p2 = battleManager.GetPlayer2State();
        if (p2 != null)
        {
            p2.TakeDamage(1);
            Log("Damaged Player2 by 1 HP");
        }
    }

    private void Log(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log("[HealthSyncDebugger] " + message);
    }

    private void LogError(string message)
    {
        Debug.LogError("[HealthSyncDebugger] " + message);
    }
}