using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 16: Multiplayer Battle - gán đúng vai trò local player và đối thủ theo Netcode.
    /// </summary>
    public class UIMultiplayerBattleController : BasePanelController
    {
        [Header("Role Texts")]
        [SerializeField] private TMP_Text topPlayerText;
        [SerializeField] private TMP_Text bottomPlayerText;
        [SerializeField] private TMP_Text battleStatusText;

        [Header("Optional Texts")]
        [SerializeField] private TMP_Text roomInfoText;

        [Header("Fallback")]
        [SerializeField] private string localPlayerLabel = "Player 1 - người chơi";
        [SerializeField] private string enemyPlayerLabel = "Player 2 - đối thủ";
        [SerializeField] private string aiEnemyLabel = "Máy AI - đối thủ";

        protected override void OnShow()
        {
            base.OnShow();
            BindRoles();
        }

        private void BindRoles()
        {
            var net = NetworkManager.Singleton;
            if (net == null || (!net.IsClient && !net.IsServer))
            {
                // Trường hợp test UI offline: vẫn hiển thị đúng bố cục player dưới - đối thủ trên.
                SetBottom(localPlayerLabel);
                SetTop(aiEnemyLabel);
                battleStatusText?.SetText("Chế độ test offline");
                roomInfoText?.SetText("Room: Local Test");
                return;
            }

            int count = net.ConnectedClientsIds.Count;
            bool hasOpponent = count >= 2;

            if (net.IsHost)
            {
                SetBottom("Player 1 - chủ phòng");
                SetTop(hasOpponent ? "Player 2 - người chơi" : aiEnemyLabel);
            }
            else
            {
                SetBottom("Player 2 - bạn");
                SetTop("Player 1 - chủ phòng");
            }

            battleStatusText?.SetText(hasOpponent ? "Đang đấu 1v1" : "Đang chờ đối thủ...");
            roomInfoText?.SetText($"Connected: {count}/2");
        }

        private void SetTop(string text)
        {
            topPlayerText?.SetText(text);
        }

        private void SetBottom(string text)
        {
            bottomPlayerText?.SetText(text);
        }
    }
}
