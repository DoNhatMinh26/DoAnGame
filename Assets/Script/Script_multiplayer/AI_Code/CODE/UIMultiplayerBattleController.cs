using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// UI 16: Multiplayer Battle - gán đúng vai trò + Avatar/HP/Score tracking.
    /// Load dữ liệu từ Firebase khi panel show.
    /// </summary>
    public class UIMultiplayerBattleController : BasePanelController
    {
        [Header("Role Texts")]
        [SerializeField] private TMP_Text topPlayerText;
        [SerializeField] private TMP_Text bottomPlayerText;
        [SerializeField] private TMP_Text battleStatusText;

        [Header("Opponent Display (TOP)")]
        [SerializeField] private Image topPlayerAvatarImage;
        [SerializeField] private TMP_Text topPlayerNameText;
        [SerializeField] private Image topPlayerHpBar;
        [SerializeField] private TMP_Text topPlayerHpText;
        [SerializeField] private TMP_Text topPlayerScoreText;

        [Header("Question & Answers")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TMP_Text[] answerTexts = new TMP_Text[4];

        [Header("Local Player Display (BOTTOM)")]
        [SerializeField] private Image bottomPlayerAvatarImage;
        [SerializeField] private TMP_Text bottomPlayerNameText;
        [SerializeField] private Image bottomPlayerHpBar;
        [SerializeField] private TMP_Text bottomPlayerHpText;
        [SerializeField] private TMP_Text bottomPlayerScoreText;

        [Header("Fallback")]
        [SerializeField] private Sprite defaultAvatarSprite;
        [SerializeField] private TMP_Text roomInfoText;
        [SerializeField] private string localPlayerLabel = "Player 1 - người chơi";
        [SerializeField] private string enemyPlayerLabel = "Player 2 - đối thủ";
        [SerializeField] private string aiEnemyLabel = "Máy AI - đối thủ";

        // Runtime data
        private PlayerData localPlayerData;
        private int localHp = 100;
        private int opponentHp = 100;

        protected override void OnShow()
        {
            base.OnShow();
            BindRolesAndLoadData();
        }

        private async void BindRolesAndLoadData()
        {
            var net = NetworkManager.Singleton;
            if (net == null || (!net.IsClient && !net.IsServer))
            {
                // Test offline mode
                SetOfflineUI();
                return;
            }

            // Load local player data từ Firebase
            var auth = AuthManager.Instance;
            var firebase = FirebaseManager.Instance;
            
            if (auth != null && firebase != null)
            {
                var currentUser = firebase.GetCurrentUser();
                if (currentUser != null)
                {
                    // Load player data (score, level, etc)
                    localPlayerData = await firebase.LoadPlayerDataAsync(currentUser.UserId);
                    
                    // Display local player (bottom)
                    DisplayLocalPlayer();
                }
            }

            // Setup opponent + check connection
            int count = net.ConnectedClientsIds.Count;
            bool hasOpponent = count >= 2;

            if (net.IsHost)
            {
                SetBottom("Player 1 - chủ phòng");
                SetTop(hasOpponent ? "Player 2 - đối thủ" : aiEnemyLabel);
            }
            else
            {
                SetBottom("Player 2 - bạn");
                SetTop("Player 1 - chủ phòng");
            }

            battleStatusText?.SetText(hasOpponent ? "Đang đấu 1v1" : "Đang chờ đối thủ...");
            roomInfoText?.SetText($"Connected: {count}/2");
        }

        private void DisplayLocalPlayer()
        {
            if (localPlayerData == null)
                return;

            // Bottom player display (local)
            bottomPlayerNameText?.SetText(localPlayerData.username ?? "Player");
            bottomPlayerScoreText?.SetText($"Score: {localPlayerData.totalScore}");
            bottomPlayerHpText?.SetText($"{localHp}/100");
            
            // Use default avatar (avatar loading can be added later)
            bottomPlayerAvatarImage.sprite = defaultAvatarSprite;

            // Update HP bar
            UpdateHpBar(bottomPlayerHpBar, localHp);
        }

        private void SetOfflineUI()
        {
            SetBottom(localPlayerLabel);
            SetTop(aiEnemyLabel);
            battleStatusText?.SetText("Chế độ test offline");
            roomInfoText?.SetText("Room: Local Test");
            
            // Placeholder
            bottomPlayerNameText?.SetText("Player");
            bottomPlayerScoreText?.SetText("Score: 0");
            topPlayerNameText?.SetText("AI");
            topPlayerScoreText?.SetText("Score: 0");
            
            bottomPlayerAvatarImage.sprite = defaultAvatarSprite;
            topPlayerAvatarImage.sprite = defaultAvatarSprite;
        }

        /// <summary>
        /// Async load avatar từ URL
        /// </summary>
        private IEnumerator LoadAvatarFromUrl(string url, Image targetImage)
        {
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                var downloadHandler = new UnityEngine.Networking.DownloadHandlerTexture();
                request.downloadHandler = downloadHandler;
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = downloadHandler.texture;
                    targetImage.sprite = Sprite.Create(texture, 
                        new Rect(0, 0, texture.width, texture.height), 
                        new Vector2(0.5f, 0.5f));
                    
                    Debug.Log("[UI16] ✅ Avatar loaded from URL");
                }
                else
                {
                    Debug.LogWarning($"[UI16] ⚠️ Failed to load avatar: {request.error}");
                    targetImage.sprite = defaultAvatarSprite;
                }
            }
        }

        /// <summary>
        /// Update HP khi trả lời sai hoặc bị tấn công
        /// </summary>
        public void UpdateLocalPlayerHp(int hpLoss)
        {
            localHp = Mathf.Max(0, localHp - hpLoss);
            bottomPlayerHpText?.SetText($"{localHp}/100");
            UpdateHpBar(bottomPlayerHpBar, localHp);
            
            if (localHp <= 0)
            {
                Debug.Log("[UI16] ❌ Local player defeated!");
                // TODO: Trigger game over behavior
            }
        }

        /// <summary>
        /// Update score khi trả lời đúng
        /// </summary>
        public void UpdateLocalPlayerScore(int scoreAdd)
        {
            if (localPlayerData != null)
            {
                localPlayerData.totalScore += scoreAdd;
                bottomPlayerScoreText?.SetText($"Score: {localPlayerData.totalScore}");
            }
        }

        /// <summary>
        /// Update opponent HP display
        /// </summary>
        public void UpdateOpponentHp(int hpValue)
        {
            opponentHp = Mathf.Clamp(hpValue, 0, 100);
            topPlayerHpText?.SetText($"{opponentHp}/100");
            UpdateHpBar(topPlayerHpBar, opponentHp);
        }

        /// <summary>
        /// Update opponent score display
        /// </summary>
        public void UpdateOpponentScore(int scoreValue)
        {
            topPlayerScoreText?.SetText($"Score: {scoreValue}");
        }

        /// <summary>
        /// Visual update HP bar (color change based on HP%)
        /// </summary>
        private void UpdateHpBar(Image hpBar, int currentHp)
        {
            if (hpBar != null)
            {
                hpBar.fillAmount = currentHp / 100f;
                
                // Color: green → yellow → red
                if (currentHp > 50)
                    hpBar.color = Color.green;
                else if (currentHp > 20)
                    hpBar.color = Color.yellow;
                else
                    hpBar.color = Color.red;
            }
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
