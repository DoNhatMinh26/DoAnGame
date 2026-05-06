using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

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
        [SerializeField] private SettingsPopupController settingsPopupController;
        [Tooltip("Popup xác nhận quit trong battle — nếu gán thì hiện popup thay vì quit thẳng")]
        [SerializeField] private UIBattleQuitConfirmPopup battleQuitConfirmPopup;

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
        [SerializeField] private bool rebindOnEnable = true;
        [SerializeField] private bool autoResolveSettingsPopup = true;
        [SerializeField] private bool backActsAsQuitRoom = true;
        [SerializeField] private bool bindOptionalRoomButtons;
        [SerializeField] private bool autoRepairInputIssues = true;
        [SerializeField] private bool navigateToRoomAfterQuit = true;
        [SerializeField] private float quitNavigateDelaySeconds = 0.08f;
        [SerializeField] private bool enableDebugLogs;

        [Header("Events")]
        [SerializeField] private UnityEvent onAnyActionTriggered;
        [SerializeField] private UnityEvent onQuitTriggered;

        public UIMultiplayerRoomController RoomController => roomController;
        public UIButtonScreenNavigator BackToRoomNavigator => backToRoomNavigator;
        public UIButtonScreenNavigator FallbackQuitNavigator => fallbackQuitNavigator;

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

            ValidateUiInputReadiness();
        }

        private void OnEnable()
        {
            if (rebindOnEnable)
            {
                BindButtons();
            }

            // Ensure UI16 action buttons are usable each time panel becomes visible.
            SetNavigationActionsInteractable(true);

            if (autoRepairInputIssues)
            {
                RepairUiInputIfNeeded();
            }

            ValidateUiInputReadiness();
        }

        private void Update()
        {
            if (!enableDebugLogs)
                return;

            if (Input.GetMouseButtonDown(0))
            {
                LogPointerRaycastUnderMouse();
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
            Log("OnClickSetting invoked");

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
            var all = Resources.FindObjectsOfTypeAll<SettingsPopupController>();
            if (all == null || all.Length == 0)
            {
                Log("ResolveSettingsPopup | no SettingsPopupController found");
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
            Log("OnClickBackToUI15 invoked");
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
            Debug.Log($"[UI16Hub] OnClickQuitRoom called. battleQuitConfirmPopup={(battleQuitConfirmPopup != null ? battleQuitConfirmPopup.gameObject.name : "NULL")}");

            // Nếu có popup xác nhận quit (trong battle) → hiện popup, không quit thẳng
            // onAnyActionTriggered/onQuitTriggered sẽ được invoke khi người dùng xác nhận trong popup
            if (battleQuitConfirmPopup != null)
            {
                Debug.Log("[UI16Hub] ✅ Showing BattleQuitConfirmPopup");
                battleQuitConfirmPopup.Show();
                return;
            }

            // Không có popup → quit thẳng (LobbyPanel hoặc ngoài battle)
            Debug.LogWarning("[UI16Hub] battleQuitConfirmPopup is NULL → quitting directly. Assign it in Inspector on GameplayPanel → UI16ButtonActionHub.");
            onAnyActionTriggered?.Invoke();
            onQuitTriggered?.Invoke();
            Log("OnClickQuitRoom invoked");

            ScheduleQuitNavigation();

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

        private void ScheduleQuitNavigation()
        {
            if (!navigateToRoomAfterQuit)
                return;

            if (quitNavigateDelaySeconds <= 0f)
            {
                NavigateAfterQuitNow();
                return;
            }

            if (isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                StartCoroutine(NavigateAfterQuitDelay());
            }
            else
            {
                Log("ScheduleQuitNavigation -> GameplayPanel inactive, navigate immediately");
                NavigateAfterQuitNow();
            }
        }

        private IEnumerator NavigateAfterQuitDelay()
        {
            if (quitNavigateDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(quitNavigateDelaySeconds);
            }

            NavigateAfterQuitNow();
        }

        private void NavigateAfterQuitNow()
        {
            if (fallbackQuitNavigator != null)
            {
                fallbackQuitNavigator.NavigateNow();
                Log("QuitNavigateDelay -> fallbackQuitNavigator.NavigateNow");
                return;
            }

            if (backToRoomNavigator != null)
            {
                backToRoomNavigator.NavigateNow();
                Log("QuitNavigateDelay -> backToRoomNavigator.NavigateNow");
            }
            else
            {
                Log("QuitNavigateDelay -> no navigator assigned");
            }
        }

        public void OnClickStartMatch()
        {
            onAnyActionTriggered?.Invoke();
            Log("OnClickStartMatch invoked");

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
            Log("OnClickCreateRoom invoked");

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
            Log("OnClickQuickJoin invoked");

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
            Log("OnClickJoinByCode invoked");

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

        [ContextMenu("Debug/Validate UI Input")]
        public void ValidateUiInputReadiness()
        {
            if (!enableDebugLogs)
                return;

            var currentEventSystem = EventSystem.current;
            string esState = currentEventSystem == null
                ? "EventSystem=NULL"
                : $"EventSystem={currentEventSystem.name}, active={currentEventSystem.gameObject.activeInHierarchy}, enabled={currentEventSystem.enabled}";
            Log($"InputCheck | {esState}");

            LogButtonState("Setting", settingButton);
            LogButtonState("Back", backButton);
            LogButtonState("Quit", quitRoomButton);
        }

        private void LogButtonState(string label, Button button)
        {
            if (!enableDebugLogs)
                return;

            if (button == null)
            {
                Log($"InputCheck | {label} button=NULL");
                return;
            }

            bool active = button.gameObject.activeInHierarchy;
            bool interactable = button.interactable;
            bool enabled = button.enabled;
            Log($"InputCheck | {label} button={button.name}, active={active}, enabled={enabled}, interactable={interactable}");
        }

        [ContextMenu("Debug/Repair UI Input")]
        public void RepairUiInputIfNeeded()
        {
            EnsureEventSystemReady();
            EnsureGraphicRaycasterReady();
            DisableNonInteractiveRaycastTargets();
            EnsureButtonReady(settingButton, "Setting");
            EnsureButtonReady(backButton, "Back");
            EnsureButtonReady(quitRoomButton, "Quit");
        }

        private void DisableNonInteractiveRaycastTargets()
        {
            var graphics = GetComponentsInChildren<Graphic>(true);
            int disabledCount = 0;

            for (int i = 0; i < graphics.Length; i++)
            {
                var g = graphics[i];
                if (g == null)
                    continue;

                // Keep raycast for actual button visuals.
                bool underButton = g.GetComponentInParent<Button>() != null;
                if (underButton)
                    continue;

                // ✅ FIX: Skip Answer objects (MultiplayerDragAndDrop)
                if (g.GetComponent<DoAnGame.Multiplayer.MultiplayerDragAndDrop>() != null)
                    continue;

                if (g.raycastTarget)
                {
                    g.raycastTarget = false;
                    disabledCount++;
                }
            }

            Log($"Repair | Disabled non-interactive raycast targets: {disabledCount}");
        }

        private void EnsureEventSystemReady()
        {
            var current = EventSystem.current;
            if (current != null)
            {
                current.gameObject.SetActive(true);
                current.enabled = true;
                if (current.GetComponent<StandaloneInputModule>() == null)
                {
                    current.gameObject.AddComponent<StandaloneInputModule>();
                }
                EnableInputModules(current);
                Log($"Repair | EventSystem ready: {current.name}");
                return;
            }

            var any = FindObjectOfType<EventSystem>(true);
            if (any != null)
            {
                any.gameObject.SetActive(true);
                any.enabled = true;
                if (any.GetComponent<StandaloneInputModule>() == null)
                {
                    any.gameObject.AddComponent<StandaloneInputModule>();
                }
                EnableInputModules(any);
                Log($"Repair | Activated existing EventSystem: {any.name}");
                return;
            }

            var go = new GameObject("EventSystem (AutoCreated)");
            var es = go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
            EnableInputModules(es);
            Log("Repair | Created EventSystem (AutoCreated)");
        }

        private void EnableInputModules(EventSystem es)
        {
            if (es == null)
                return;

            var modules = es.GetComponents<BaseInputModule>();
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] != null)
                {
                    modules[i].enabled = true;
                    Log($"Repair | InputModule enabled: {modules[i].GetType().Name}");
                }
            }
        }

        private void EnsureGraphicRaycasterReady()
        {
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
                return;

            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Log($"Repair | Added GraphicRaycaster on {canvas.name}");
            }
            raycaster.enabled = true;
        }

        private void EnsureButtonReady(Button button, string label)
        {
            if (button == null)
                return;

            button.enabled = true;
            button.interactable = true;

            var image = button.GetComponent<Graphic>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            // Make button root graphic the only click target to avoid child TMP overlay intercept.
            var childGraphics = button.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < childGraphics.Length; i++)
            {
                var g = childGraphics[i];
                if (g == null)
                    continue;

                // ✅ FIX: Skip Answer objects (MultiplayerDragAndDrop)
                if (g.GetComponent<DoAnGame.Multiplayer.MultiplayerDragAndDrop>() != null)
                    continue;

                g.raycastTarget = false;
            }

            if (button.targetGraphic != null)
            {
                button.targetGraphic.raycastTarget = true;
            }
            else if (image != null)
            {
                image.raycastTarget = true;
            }

            var groups = button.GetComponentsInParent<CanvasGroup>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                if (groups[i] == null)
                    continue;

                groups[i].interactable = true;
                groups[i].blocksRaycasts = true;
            }

            Log($"Repair | {label} button ensured ready: {button.name}");
        }

        private void LogPointerRaycastUnderMouse()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                Log("ClickProbe | EventSystem is NULL");
                return;
            }

            var pointerData = new PointerEventData(es)
            {
                position = Input.mousePosition
            };

            var results = new List<RaycastResult>();
            es.RaycastAll(pointerData, results);

            if (results.Count == 0)
            {
                Log($"ClickProbe | no raycast hit at {Input.mousePosition}");
                return;
            }

            int take = Mathf.Min(5, results.Count);
            for (int i = 0; i < take; i++)
            {
                var hit = results[i];
                string hitName = hit.gameObject != null ? hit.gameObject.name : "null";
                Log($"ClickProbe | hit[{i}]={hitName}, depth={hit.depth}, dist={hit.distance}");
            }
        }
    }
}
