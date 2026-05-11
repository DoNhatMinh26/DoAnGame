using UnityEngine;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Gán lên CharacterContainer trong Canvas.
    /// Tự động bật/tắt Player1/2Character theo GameplayPanel,
    /// và WinnerCharacter/LoserCharacter theo Wins panel.
    /// CharacterContainer phải luôn active — chỉ ẩn các child bên trong.
    /// </summary>
    public class CharacterContainerController : MonoBehaviour
    {
        private string GetCallStack()
        {
            var st = new System.Diagnostics.StackTrace(true);
            string stack = "";
            for (int i = 1; i < Mathf.Min(4, st.FrameCount); i++)
            {
                var frame = st.GetFrame(i);
                stack += frame.GetMethod().DeclaringType?.Name + "." + frame.GetMethod().Name + "() → ";
            }
            return stack.TrimEnd('→', ' ');
        }
        [Header("Battle Characters")]
        [SerializeField] private GameObject player1Character;
        [SerializeField] private GameObject player2Character;

        [Header("Wins Characters")]
        [SerializeField] private GameObject winnerCharacter;
        [SerializeField] private GameObject loserCharacter;

        [Header("Panels để theo dõi")]
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject winsPanel;

        private bool lastGameplayActive;
        private bool lastWinsActive;

        private void Awake()
        {
            if (player1Character == null) Debug.LogWarning("[CharContainer] player1Character chua gan!");
            if (player2Character == null) Debug.LogWarning("[CharContainer] player2Character chua gan!");
            if (winnerCharacter  == null) Debug.LogWarning("[CharContainer] winnerCharacter chua gan!");
            if (loserCharacter   == null) Debug.LogWarning("[CharContainer] loserCharacter chua gan!");
            if (gameplayPanel    == null) Debug.LogWarning("[CharContainer] gameplayPanel chua gan!");
            if (winsPanel        == null) Debug.LogWarning("[CharContainer] winsPanel chua gan!");

            // ✅ CHANGED: Don't deactivate battle characters in Awake
            // They will be managed by UIMultiplayerBattleController.ApplyAvatarCharacters()
            // SetBattleCharacters(false); // DISABLED
            
            // Only manage wins characters
            SetWinsCharacters(false);

            Debug.Log("[CharContainer] Initialized - wins characters hidden, battle characters managed by BattleController");
        }

        private void OnEnable()
        {
            Debug.Log($"[CharContainer] ⏫ OnEnable - GameObject.activeSelf={gameObject.activeSelf}, parent={gameObject.transform.parent?.name}, Stack: {GetCallStack()}");
        }

        private void OnDisable()
        {
            Debug.Log($"[CharContainer] ⏬ OnDisable - GameObject.activeSelf={gameObject.activeSelf}, parent={gameObject.transform.parent?.name}, Stack: {GetCallStack()}");
        }

        private void Update()
        {
            // ✅ DISABLED: CharacterContainerController không còn quản lý battle characters
            // Battle characters được quản lý bởi UIMultiplayerBattleController.ApplyAvatarCharacters()
            // Chỉ giữ logic cho WinsPanel
            
            /*
            if (gameplayPanel != null)
            {
                bool isActive = gameplayPanel.activeInHierarchy;
                if (isActive != lastGameplayActive)
                {
                    lastGameplayActive = isActive;
                    SetBattleCharacters(isActive);
                    Debug.Log("[CharContainer] GameplayPanel " + (isActive ? "ACTIVE" : "INACTIVE") + " - Battle characters " + (isActive ? "bat" : "tat"));
                }
            }
            */

            if (winsPanel != null)
            {
                bool isActive = winsPanel.activeInHierarchy;
                if (isActive != lastWinsActive)
                {
                    lastWinsActive = isActive;
                    SetWinsCharacters(isActive);
                    Debug.Log("[CharContainer] WinsPanel " + (isActive ? "ACTIVE" : "INACTIVE") + " - Wins characters " + (isActive ? "bat" : "tat"));
                }
            }
        }

        private void SetBattleCharacters(bool active)
        {
            // ✅ DISABLED: Battle characters are now managed by UIMultiplayerBattleController
            // This method is no longer used
            Debug.Log("[CharContainer] SetBattleCharacters called but DISABLED - BattleController manages characters now");
        }

        private void SetWinsCharacters(bool active)
        {
            if (winnerCharacter != null)
            {
                winnerCharacter.SetActive(active);
                Debug.Log("[CharContainer] WinnerCharacter.SetActive(" + active + ")");
            }
            if (loserCharacter != null)
            {
                loserCharacter.SetActive(active);
                Debug.Log("[CharContainer] LoserCharacter.SetActive(" + active + ")");
            }
        }
    }
}
