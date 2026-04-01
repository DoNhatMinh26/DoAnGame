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
        [SerializeField] private UISettingsPopupController popupController;

        [LocalizedLabel("Dùng chế độ bật/tắt")]
        [SerializeField] private bool useToggle;

        private Button cachedButton;

        private void Awake()
        {
            cachedButton = GetComponent<Button>();
            cachedButton.onClick.AddListener(HandleOpen);
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

            if (useToggle)
            {
                popupController.Toggle();
            }
            else
            {
                popupController.Open();
            }
        }
    }
}
