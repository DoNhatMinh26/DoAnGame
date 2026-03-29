using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// Cầu nối tạm để chuyển yêu cầu UI giữa các scene (VD: từ Welcome Screen sang GameUIPlay).
    /// </summary>
    public static class SceneFlowBridge
    {
        private static bool hasRequest;
        private static UIFlowManager.Screen requestedScreen;

        /// <summary>
        /// Tạo yêu cầu mở một UI cụ thể sau khi scene mục tiêu được load.
        /// </summary>
        public static void RequestScreen(UIFlowManager.Screen screen)
        {
            requestedScreen = screen;
            hasRequest = true;
        }

        /// <summary>
        /// Lấy yêu cầu hiện tại (nếu có) rồi reset trạng thái.
        /// </summary>
        public static bool TryConsume(out UIFlowManager.Screen screen)
        {
            if (hasRequest)
            {
                screen = requestedScreen;
                hasRequest = false;
                return true;
            }

            screen = default;
            return false;
        }
    }
}
