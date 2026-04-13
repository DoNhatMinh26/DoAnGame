using TMPro;
using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// Helper hiển thị message nhất quán cho các panel auth.
    /// </summary>
    public static class UIMessagePresenter
    {
        public static void Clear(TMP_Text target, bool normalizeRect = false)
        {
            Show(target, string.Empty, Color.red, normalizeRect);
        }

        public static void ShowError(TMP_Text target, string message, bool normalizeRect = false)
        {
            Show(target, message, Color.red, normalizeRect);
        }

        public static void ShowSuccess(TMP_Text target, string message, bool normalizeRect = false)
        {
            Show(target, message, Color.green, normalizeRect);
        }

        private static void Show(TMP_Text target, string message, Color color, bool normalizeRect)
        {
            if (target == null)
                return;

            EnsureVisible(target, normalizeRect);
            target.text = message;
            target.color = color;
            target.enabled = true;
            target.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private static void EnsureVisible(TMP_Text target, bool normalizeRect)
        {
            if (!target.gameObject.activeSelf)
            {
                target.gameObject.SetActive(true);
            }

            if (!normalizeRect)
                return;

            RectTransform rect = target.rectTransform;
            if (rect == null)
                return;

            RectTransform parent = rect.parent as RectTransform;
            if (parent == null)
                return;

            // Chỉ auto-fix khi text bị văng quá xa khỏi vùng parent.
            Vector2 anchored = rect.anchoredPosition;
            float maxX = Mathf.Max(parent.rect.width * 1.2f, 200f);
            float maxY = Mathf.Max(parent.rect.height * 1.2f, 120f);
            bool isOffscreen = Mathf.Abs(anchored.x) > maxX || Mathf.Abs(anchored.y) > maxY;

            if (isOffscreen)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }
        }
    }
}