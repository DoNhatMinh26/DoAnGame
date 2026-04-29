using UnityEngine;
using Unity.Netcode;
using DoAnGame.Multiplayer;

/// <summary>
/// Script đơn giản để test health sync
/// Attach vào BattleManager
/// </summary>
public class TestHealthSync : MonoBehaviour
{
    [ContextMenu("Test: Damage Player 1")]
    public void TestDamagePlayer1()
    {
        var battleManager = NetworkedMathBattleManager.Instance;
        if (battleManager == null)
        {
            Debug.LogError("[TestHealthSync] BattleManager is NULL!");
            return;
        }

        var net = NetworkManager.Singleton;
        if (net == null || !net.IsServer)
        {
            Debug.LogError("[TestHealthSync] Must run on Server!");
            return;
        }

        var p1 = battleManager.GetPlayer1State();
        if (p1 != null)
        {
            p1.TakeDamage(1);
            Debug.Log("[TestHealthSync] Damaged Player1 by 1 HP");
        }
        else
        {
            Debug.LogError("[TestHealthSync] Player1State is NULL!");
        }
    }

    [ContextMenu("Test: Damage Player 2")]
    public void TestDamagePlayer2()
    {
        var battleManager = NetworkedMathBattleManager.Instance;
        if (battleManager == null)
        {
            Debug.LogError("[TestHealthSync] BattleManager is NULL!");
            return;
        }

        var net = NetworkManager.Singleton;
        if (net == null || !net.IsServer)
        {
            Debug.LogError("[TestHealthSync] Must run on Server!");
            return;
        }

        var p2 = battleManager.GetPlayer2State();
        if (p2 != null)
        {
            p2.TakeDamage(1);
            Debug.Log("[TestHealthSync] Damaged Player2 by 1 HP");
        }
        else
        {
            Debug.LogError("[TestHealthSync] Player2State is NULL!");
        }
    }

    [ContextMenu("Check Player States")]
    public void CheckPlayerStates()
    {
        var battleManager = NetworkedMathBattleManager.Instance;
        if (battleManager == null)
        {
            Debug.LogError("[TestHealthSync] BattleManager is NULL!");
            return;
        }

        Debug.Log("========== PLAYER STATES CHECK ==========");

        var p1 = battleManager.GetPlayer1State();
        if (p1 == null)
        {
            Debug.LogError("Player1State is NULL!");
        }
        else
        {
            Debug.Log("Player1State:");
            Debug.Log("  PlayerId: " + p1.PlayerId.Value);
            Debug.Log("  PlayerName: " + p1.PlayerName.Value);
            Debug.Log("  CurrentHealth: " + p1.CurrentHealth.Value);
            Debug.Log("  MaxHealth: " + p1.MaxHealth.Value);
            Debug.Log("  Score: " + p1.Score.Value);
        }

        var p2 = battleManager.GetPlayer2State();
        if (p2 == null)
        {
            Debug.LogError("Player2State is NULL!");
        }
        else
        {
            Debug.Log("Player2State:");
            Debug.Log("  PlayerId: " + p2.PlayerId.Value);
            Debug.Log("  PlayerName: " + p2.PlayerName.Value);
            Debug.Log("  CurrentHealth: " + p2.CurrentHealth.Value);
            Debug.Log("  MaxHealth: " + p2.MaxHealth.Value);
            Debug.Log("  Score: " + p2.Score.Value);
        }

        Debug.Log("=========================================");
    }
}