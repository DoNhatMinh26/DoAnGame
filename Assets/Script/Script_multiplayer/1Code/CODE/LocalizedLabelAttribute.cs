using System;
using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// Dùng để thay thế nhãn của field trong Inspector bằng chuỗi tuỳ chọn (ví dụ song ngữ).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LocalizedLabelAttribute : PropertyAttribute
    {
        public string Label { get; }

        public LocalizedLabelAttribute(string label)
        {
            Label = label;
        }
    }
}
