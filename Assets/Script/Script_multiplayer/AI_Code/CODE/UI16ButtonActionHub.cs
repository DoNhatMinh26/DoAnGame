using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Centralizes UI16 button wiring so each button can be handled individually
    /// while still allowing synchronized group behavior (lock/unlock all actions).
    /// </summary>
    public class UI16ButtonActionHub : MonoBehaviour
    {
        [Header("Controllers")]
        [SerializeField] private UIMultiplayerRoomController roomController;
        [SerializeField] private UISettingsPopupController settingsPopupController;

        [Header("Navigation")]
        [SerializeField] private UIButtonScreenNavigator backToRoomNavigator;
        [SerializeField] private UIButtonScreenNavigator fallbackQuitNavigator;

        [Header("UI16 Buttons")]
        [SerializeField] private Button settingButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button quitRoomButton;

        [Header("Optional: UI15 Room Buttons")]
        [SerializeField] private Button startMatchButton;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button quickJoinButton;
        [SerializeField] private Button joinByCodeButton;

        [Header("Behavior")]
        [SerializeField] private bool autoBindOnAwake = true;
        [SerializeField] private bool autoResolveSettingsPopup = true;
        [SerializeField] private bool backActsAsQuitRoom = true;
        [SerializeField] private bool bindOptionalRoomButtons;
        [SerializeField] private bool enableDebugLogs;

        [Header("Events")]
        [SerializeField] private UnityEvent onAnyActionTriggered;
        [SerializeField] private UnityEvent onQuitTriggered;

        private void Awake()
        {
            if (autoResolveSettingsPopup)
            {
                TryResolveSettingsPopup();
            }

            if (autoBindOnAwake)
            {
                BindButtons();
            }
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        [ContextMenu("Bind Buttons")]
        public void BindButtons()
        {
            UnbindButtons();

            settingButton?.onClick.AddListener(OnClickSetting);
            backButton?.onClick.AddListener(OnClickBackToUI15);
            quitRoomButton?.onClick.AddListener(OnClickQuitRoom);

            if (bindOptionalRoomButtons)
            {
                startMatchButton?.onClick.AddListener(OnClickStartMatch);
                createRoomButton?.onClick.AddListener(OnClickCreateRoom);
                quickJoinButton?.onClick.AddListener(OnClickQuickJoin);
                joinByCodeButton?.onClick.AddListener(OnClickJoinByCode);
            }

            Log("Buttons bound");
        }

        [ContextMenu("Unbind Buttons")]
        public void UnbindButtons()
        {
            settingButton?.onClick.RemoveListener(OnClickSetting);
            backButton?.onClick.RemoveListener(OnClickBackToUI15);
            quitRoomButton?.onClick.RemoveListener(OnClickQuitRoom);
            startMatchButton?.onClick.RemoveListener(OnClickStartMatch);
            createRoomButton?.onClick.RemoveListener(OnClickCreateRoom);
            quickJoinButton?.onClick.RemoveListener(OnClickQuickJoin);
            joinByCodeButton?.onClick.RemoveListener(OnClickJoinByCode);
        }

        public void SetAllActionsInteractable(bool interactable)
        {
            SetRoomActionsInteractable(interactable);
            SetNavigationActionsInteractable(interactable);
        }

        public void SetRoomActionsInteractable(bool interactable)
        {
            if (!bindOptionalRoomButtons)
                return;

            if (createRoomButton != null) createRoomButton.interactable = interactable;
            if (quickJoinButton != null) quickJoinButton.interactable = interactable;
            if (joinByCodeButton != null) joinByCodeButton.interactable = interactable;
            if (startMatchButton != null) startMatchButton.interactable = interactable;
            if (quitRoomButton != null) quitRoomButton.interactable = interactable;
        }

        public void SetNavigationActionsInteractable(bool interactable)
        {
            if (settingButton != null) settingButton.interactable = interactable;
            if (backButton != null) backButton.interactable = interactable;
            if (quitRoomButton != null) quitRoomButton.interactable = interactable;
        }

        public void OnClickSetting()
        {
            onAnyActionTriggered?.Invoke();

            if (settingsPopupController == null)
            {
                TryResolveSettingsPopup();
            }

            if (settingsPopupController != null)
            {
                settingsPopupController.Open();
                Log("Action: Setting -> Open popup");
                return;
            }

            Log("Action: Setting ignored (settingsPopupController is null)");
        }

        [ContextMenu("Resolve Settings Popup")]
        public void TryResolveSettingsPopup()
        {
            var all = Resources.FindObjectsOfTypeAll<UISettingsPopupController>();
            if (all == null || all.Length == 0)
            {
                Log("ResolveSettingsPopup | no UISettingsPopupController found");
                return;
            }

            for (int i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null)
                    continue;

                var scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                settingsPopupController = candidate;
                Log($"ResolveSettingsPopup | resolved: {candidate.name} in scene {scene.name}");
                return;
            }

            Log("ResolveSettingsPopup | found objects but none in a loaded scene");
        }

        public void OnClickBackToUI15()
        {
            if (backActsAsQuitRoom)
            {
                Log("Action: Back -> acts as QuitRoom");
                OnClickQuitRoom();
                return;
            }

            onAnyActionTriggered?.Invoke();

            if (backToRoomNavigator != null)
            {
                backToRoomNavigator.NavigateNow();
                Log("Action: Back -> Navigate");
                return;
            }

            Log("Action: Back ignored (backToRoomNavigator is null)");
        }

        public void OnClickQuitRoom()
        {
            onAnyActionTriggered?.Invoke();
            onQuitTriggered?.Invoke();

            if (roomController != null)
            {
                roomController.RequestQuitRoom();
                Log("Action: Quit -> RoomController.RequestQuitRoom");
                return;
            }

            if (fallbackQuitNavigator != null)
            {
                fallbackQuitNavigator.NavigateNow();
                Log("Action: Quit -> Fallback navigate only");
                return;
            }

            Log("Action: Quit ignored (no controller, no navigator)");
        }

        public void OnClickStartMatch()
        {
            onAnyActionTriggered?.Invoke();

            if (roomController != null)
            {
                roomController.RequestStartMatch();
                Log("Action: StartMatch -> RoomController");
            }
            else
            {
                Log("Action: StartMatch ignored (roomController is null)");
            }
        }

        public void OnClickCreateRoom()
        {
            onAnyActionTriggered?.Invoke();

            if (roomController != null)
            {
                roomController.RequestCreateRoom();
                Log("Action: CreateRoom -> RoomController");
            }
            else
            {
                Log("Action: CreateRoom ignored (roomController is null)");
            }
        }

        public void OnClickQuickJoin()
        {
            onAnyActionTriggered?.Invoke();

            if (roomController != null)
            {
                roomController.RequestQuickJoin();
                Log("Action: QuickJoin -> RoomController");
            }
            else
            {
                Log("Action: QuickJoin ignored (roomController is null)");
            }
        }

        public void OnClickJoinByCode()
        {
            onAnyActionTriggered?.Invoke();

            if (roomController != null)
            {
                roomController.RequestJoinByCode();
                Log("Action: JoinByCode -> RoomController");
            }
            else
            {
                Log("Action: JoinByCode ignored (roomController is null)");
            }
        }

        private void Log(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[{nameof(UI16ButtonActionHub)}:{name}] {message}");
        }
    }
}
