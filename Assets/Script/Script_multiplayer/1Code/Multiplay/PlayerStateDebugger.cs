using UnityEngine;
using DoAnGame.Multiplayer;

/// <summary>
/// Debug script để kiểm tra PlayerState references
/// </summary>
public class PlayerStateDebugger : MonoBehaviour
{
    [ContextMenu("Find All Player States")]
    public void FindAllPlayerStates()
    {
        Debug.Log("========== FINDING ALL PLAYER STATES ==========");

        var allStates = FindObjectsOfType<NetworkedPlayerState>();
        Debug.Log("Found " + allStates.Length + " NetworkedPlayerState objects");

        foreach (var state in allStates)
        {
            Debug.Log("PlayerState: " + state.name);
            Debug.Log("  PlayerId: " + state.PlayerId.Value);
            Debug.Log("  PlayerName: " + state.PlayerName.Value);
            Debug.Log("  CurrentHealth: " + state.CurrentHealth.Value);
            Debug.Log("  MaxHealth: " + state.MaxHealth.Value);

            var netObj = state.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null)
            {
                Debug.Log("  NetworkObject IsSpawned: " + netObj.IsSpawned);
                Debug.Log("  NetworkObject OwnerClientId: " + netObj.OwnerClientId);
            }
        }

        Debug.Log("=============================================");
    }

    [ContextMenu("Check BattleManager References")]
    public void CheckBattleManagerReferences()
    {
        Debug.Log("========== CHECKING BATTLEMANAGER REFERENCES ==========");

        var battleManager = NetworkedMathBattleManager.Instance;
        if (battleManager == null)
        {
            Debug.LogError("BattleManager is NULL!");
            return;
        }

        Debug.Log("BattleManager found: " + battleManager.name);

        var p1 = battleManager.GetPlayer1State();
        var p2 = battleManager.GetPlayer2State();

        if (p1 == null)
        {
            Debug.LogError("Player1State is NULL!");
        }
        else
        {
            Debug.Log("Player1State: " + p1.name);
            Debug.Log("  PlayerId: " + p1.PlayerId.Value);
            Debug.Log("  CurrentHealth: " + p1.CurrentHealth.Value + "/" + p1.MaxHealth.Value);
        }

        if (p2 == null)
        {
            Debug.LogError("Player2State is NULL!");
        }
        else
        {
            Debug.Log("Player2State: " + p2.name);
            Debug.Log("  PlayerId: " + p2.PlayerId.Value);
            Debug.Log("  CurrentHealth: " + p2.CurrentHealth.Value + "/" + p2.MaxHealth.Value);
        }

        Debug.Log("======================================================");
    }

    [ContextMenu("Force Reinit Health UI")]
    public void ForceReinitHealthUI()
    {
        var healthUI = FindObjectOfType<DoAnGame.UI.MultiplayerHealthUI>(true);
        if (healthUI != null)
        {
            Debug.Log("[PlayerStateDebugger] Forcing HealthUI re-init...");
            healthUI.SendMessage("RetryInit", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogError("[PlayerStateDebugger] MultiplayerHealthUI not found!");
        }
    }
}