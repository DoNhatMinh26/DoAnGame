using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Gan truc tiep vao moi nut Settings tren tung screen.
    /// Khi click se mo (hoac toggle) popup Settings.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UISettingsOpenButton : MonoBehaviour
    {
        [Header("Liên kết Popup")]
        [LocalizedLabel("Controller popup Settings")]
        [SerializeField] private SettingsPopupController popupController;

        [LocalizedLabel("Dùng chế độ bật/tắt")]
        [SerializeField] private bool useToggle;

        [Header("Debug")]
        [LocalizedLabel("Bật log debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private Button cachedButton;

        private void Awake()
        {
            cachedButton = GetComponent<Button>();
            cachedButton.onClick.AddListener(HandleOpen);
            LogDebug($"Awake | button={cachedButton.name} | popupController={(popupController != null ? popupController.name : "null")} | mode={(useToggle ? "Toggle" : "Open")}");
        }

        private void OnDestroy()
        {
            cachedButton?.onClick.RemoveListener(HandleOpen);
        }

        private void HandleOpen()
        {
            if (popupController == null)
            {
                Debug.LogError($"[{nameof(UISettingsOpenButton)}] Chưa gán popupController trên {name}");
                return;
            }

            LogDebug($"HandleOpen | clicked by {name} | popup={popupController.name} | mode={(useToggle ? "Toggle" : "Open")}");

            if (useToggle)
            {
                popupController.Toggle();
            }
            else
            {
                popupController.Open();
            }
        }

        private void LogDebug(string message)
        {
            if (!enableDebugLogs)
                return;

            Debug.Log($"[{nameof(UISettingsOpenButton)}:{name}] {message}");
        }
    }
}
