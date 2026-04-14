using System;
using UnityEngine;
using UnityEngine.UI;

namespace DoAnGame.UI
{
    /// <summary>
    /// Cho phép cấu hình đích điều hướng cho từng Button ngay trong Inspector.
    /// </summary>
    [Serializable]
    public struct FlowButtonConfig
    {
        [SerializeField]
        [LocalizedLabel("Button (Nút)")]
        private Button button;

        [SerializeField]
        [LocalizedLabel("Action (Hành động)")]
        private FlowButtonAction action;

        [SerializeField]
        [LocalizedLabel("Target Screen (Màn hình đích)")]
        [Tooltip("Đích UI muốn chuyển tới khi action = ShowScreen")]
        private UIFlowManager.Screen targetScreen;

        [SerializeField]
        [LocalizedLabel("Origin Screen (Màn hình gốc)")]
        [Tooltip("Panel gọi Settings sẽ được push để có thể quay lại")]
        private UIFlowManager.Screen originScreen;

        [SerializeField]
        [LocalizedLabel("Override Push History (Ghi đè lịch sử)")]
        [Tooltip("Ghi đè tham số pushHistory khi gọi ShowScreen")]
        private bool overridePushHistory;

        [SerializeField]
        [LocalizedLabel("Push History Value (Giá trị push)")]
        private bool pushHistoryValue;

        public Button Button => button;
        public FlowButtonAction Action => action;
        public UIFlowManager.Screen TargetScreen => targetScreen;
        public UIFlowManager.Screen OriginScreen => originScreen;
        public bool OverridePushHistory => overridePushHistory;
        public bool PushHistoryValue => pushHistoryValue;

        public bool IsValid => button != null;
    }

    public enum FlowButtonAction
    {
        [InspectorName("ShowScreen (Đổi màn hình)")]
        ShowScreen,

        [InspectorName("Back (Quay lại)")]
        Back,

        [InspectorName("ShowSettings (Mở cài đặt)")]
        ShowSettings,

        [InspectorName("ReturnFromSettings (Đóng cài đặt)")]
        ReturnFromSettings
    }
}
