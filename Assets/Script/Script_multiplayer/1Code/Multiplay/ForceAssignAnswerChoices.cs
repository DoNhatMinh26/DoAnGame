using UnityEngine;
using DoAnGame.UI;
using DoAnGame.Multiplayer;

/// <summary>
/// Script tạm thời để force assign answerChoices trong runtime
/// Attach vào GameplayPanel để test
/// </summary>
public class ForceAssignAnswerChoices : MonoBehaviour
{
    private void Start()
    {
        // Delay để đảm bảo mọi thứ đã init
        Invoke(nameof(AssignAnswerChoices), 0.5f);
    }

    private void AssignAnswerChoices()
    {
        Debug.Log("=== FORCE ASSIGNING ANSWER CHOICES ===");

        // Find BattleController
        var battleController = GetComponent<UIMultiplayerBattleController>();
        if (battleController == null)
        {
            Debug.LogError("❌ UIMultiplayerBattleController not found on this GameObject!");
            return;
        }

        // Find ALL MultiplayerDragAndDrop components in scene (including inactive)
        var allAnswers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
        
        Debug.Log($"📝 Tìm thấy {allAnswers.Length} MultiplayerDragAndDrop components:");
        foreach (var answer in allAnswers)
        {
            Debug.Log($"  - {answer.name} (Active: {answer.gameObject.activeInHierarchy})");
        }

        if (allAnswers.Length < 4)
        {
            Debug.LogError($"❌ Chỉ tìm thấy {allAnswers.Length}/4 Answer objects với MultiplayerDragAndDrop component!");
            Debug.LogError("→ Đảm bảo có ít nhất 4 objects có component MultiplayerDragAndDrop");
            return;
        }

        // Take first 4 answers
        var choices = new MultiplayerDragAndDrop[4];
        for (int i = 0; i < 4; i++)
        {
            choices[i] = allAnswers[i];
        }

        // Use reflection to assign private field
        var field = typeof(UIMultiplayerBattleController).GetField("answerChoices", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field == null)
        {
            Debug.LogError("❌ Không tìm thấy field 'answerChoices'!");
            return;
        }

        // Assign
        field.SetValue(battleController, choices);

        Debug.Log("✅ Force assigned answerChoices:");
        for (int i = 0; i < choices.Length; i++)
        {
            Debug.Log($"  - Element {i}: {choices[i].name}");
        }

        // Verify
        var assigned = field.GetValue(battleController) as MultiplayerDragAndDrop[];
        if (assigned != null && assigned.Length == 4)
        {
            Debug.Log($"✅ Verification: answerChoices.Length = {assigned.Length}");
            for (int i = 0; i < assigned.Length; i++)
            {
                Debug.Log($"  [{i}]: {(assigned[i] != null ? assigned[i].name : "NULL")}");
            }
        }
        else
        {
            Debug.LogError("❌ Verification failed!");
        }
    }
}
