using UnityEngine;
using UnityEngine.EventSystems;
using DoAnGame.UI;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Adapter để kết nối DragAndDrop system với Multiplayer Battle System.
    /// Attach script này vào Slot GameObject để detect khi player thả đáp án.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MultiplayerDragDropAdapter : MonoBehaviour, IDropHandler
    {
        [Header("References")]
        [SerializeField] private UIMultiplayerBattleController battleController;
        
        [Header("Settings")]
        [SerializeField] private bool autoFindBattleController = true;

        private void Start()
        {
            if (autoFindBattleController && battleController == null)
            {
                battleController = FindObjectOfType<UIMultiplayerBattleController>();
                
                if (battleController != null)
                {
                    Debug.Log($"[MultiplayerDragDropAdapter] Auto-found BattleController: {battleController.name}");
                }
                else
                {
                    Debug.LogWarning("[MultiplayerDragDropAdapter] BattleController not found!");
                }
            }
        }

        /// <summary>
        /// Được gọi khi player thả đáp án vào slot này
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (battleController == null)
            {
                Debug.LogWarning("[MultiplayerDragDropAdapter] BattleController is null!");
                return;
            }

            // Lấy DragAndDrop component từ object được thả
            GameObject droppedObject = eventData.pointerDrag;
            if (droppedObject == null)
                return;

            DragAndDrop dragComponent = droppedObject.GetComponent<DragAndDrop>();
            if (dragComponent == null || dragComponent.myText == null)
                return;

            // Parse đáp án từ text
            string answerText = dragComponent.myText.text;
            if (!int.TryParse(answerText, out int answer))
            {
                Debug.LogWarning($"[MultiplayerDragDropAdapter] Cannot parse answer: {answerText}");
                return;
            }

            Debug.Log($"[MultiplayerDragDropAdapter] Player dropped answer: {answer}");

            // Notify BattleController
            battleController.OnAnswerDropped(answer);
        }
    }
}
