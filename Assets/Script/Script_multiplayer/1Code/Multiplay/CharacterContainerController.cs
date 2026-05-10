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

            SetBattleCharacters(false);
            SetWinsCharacters(false);

            Debug.Log("[CharContainer] Initialized - tat ca characters da an");
        }

        private void Update()
        {
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
            if (player1Character != null)
            {
                player1Character.SetActive(active);
                Debug.Log("[CharContainer] Player1Character.SetActive(" + active + ")");
            }
            if (player2Character != null)
            {
                player2Character.SetActive(active);
                Debug.Log("[CharContainer] Player2Character.SetActive(" + active + ")");
            }
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
