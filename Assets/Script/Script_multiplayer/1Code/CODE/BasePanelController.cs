using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// Base class cho mọi UI panel. Chịu trách nhiệm bật/tắt GameObject và chuẩn hóa hook show/hide.
    /// </summary>
    public class BasePanelController : MonoBehaviour
    {
        [SerializeField]
        private bool showOnStart;

        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            if (!showOnStart)
            {
                gameObject.SetActive(false);
                IsVisible = false;
            }
        }

        public virtual void Show()
        {
            bool wasActive = gameObject.activeSelf;
            bool wasVisible = IsVisible;

            if (wasVisible && wasActive)
                return;

            gameObject.SetActive(true);
            IsVisible = true;

            if (!wasVisible || !wasActive)
            {
                OnShow();
            }
        }

        public virtual void Hide()
        {
            bool wasActive = gameObject.activeSelf;
            bool wasVisible = IsVisible;

            if (!wasVisible && !wasActive)
                return;

            OnHide();
            gameObject.SetActive(false);
            IsVisible = false;
        }

        /// <summary>
        /// Override nếu cần chạy animation/logic khi panel hiển thị.
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// Override nếu cần chạy animation/logic khi panel đóng.
        /// </summary>
        protected virtual void OnHide()
        {
        }
    }
}
