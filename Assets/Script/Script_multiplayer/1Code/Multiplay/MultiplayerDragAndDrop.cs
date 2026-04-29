using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DoAnGame.UI;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Drag-and-drop component cho MULTIPLAYER mode.
    /// Copy từ DragAndDrop.cs nhưng tích hợp với NetworkedMathBattleManager.
    /// 
    /// CÁCH DÙNG:
    /// 1. Thay thế DragAndDrop component bằng MultiplayerDragAndDrop trên Answer_0/1/2/3
    /// 2. Đảm bảo Slot có tag "Slot"
    /// 3. Slot KHÔNG cần MultiplayerDragDropAdapter nữa (logic đã tích hợp sẵn)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class MultiplayerDragAndDrop : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Image image;
        private Canvas canvas;
        private Color originalColor;

        // Vị trí gốc
        private Vector2 originalPosition;

        // Global lock
        private static bool isLocked = false;

        public TextMeshProUGUI myText;

        [Header("Cài đặt màu sắc")]
        public Color colorCorrect = Color.green;
        public Color colorWrong = Color.red;
        [SerializeField] private float thoiGianKhoa = 1.0f; // Ngắn hơn single-player

        [Header("Multiplayer References")]
        [SerializeField] private UIMultiplayerBattleController battleController;
        [SerializeField] private bool autoFindBattleController = true;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            image = GetComponent<Image>();
            canvas = GetComponentInParent<Canvas>();
            originalColor = image.color;

            // KHÔNG lưu vị trí ở đây nếu panel inactive
            // Sẽ lưu ở OnEnable hoặc khi panel active
        }

        private void Start()
        {
            // Lưu vị trí gốc SAU KHI panel đã active
            if (originalPosition == Vector2.zero)
            {
                originalPosition = rectTransform.anchoredPosition;
                Debug.Log($"[MultiplayerDragAndDrop] Saved original position for {name}: {originalPosition}");
            }
            
            if (autoFindBattleController && battleController == null)
            {
                battleController = FindObjectOfType<UIMultiplayerBattleController>(true);
                
                if (battleController != null)
                {
                    Debug.Log($"[MultiplayerDragAndDrop] Auto-found BattleController: {battleController.name}");
                }
                else
                {
                    Debug.LogWarning("[MultiplayerDragAndDrop] BattleController not found!");
                }
            }
        }

        // Tự động mở khóa khi màn hình được kích hoạt
        private void OnEnable()
        {
            isLocked = false;
            if (image != null) image.color = originalColor;
            
            // Lưu vị trí gốc KHI PANEL ACTIVE (fix cho panel inactive lúc start)
            if (rectTransform != null && originalPosition == Vector2.zero)
            {
                originalPosition = rectTransform.anchoredPosition;
                Debug.Log($"[MultiplayerDragAndDrop] OnEnable: Saved original position for {name}: {originalPosition}");
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[MultiplayerDragAndDrop] OnBeginDrag CALLED for {name}");
            Debug.Log($"  isLocked: {isLocked}");
            Debug.Log($"  canvasGroup: {canvasGroup != null}");
            Debug.Log($"  image: {image != null}");
            Debug.Log($"  rectTransform: {rectTransform != null}");
            
            if (isLocked)
            {
                Debug.LogWarning($"[MultiplayerDragAndDrop] Drag BLOCKED by lock!");
                eventData.pointerDrag = null;
                return;
            }

            StopAllCoroutines();
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
            image.color = originalColor;

            Debug.Log($"[MultiplayerDragAndDrop] ✅ Begin drag: {myText?.text}");
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isLocked)
            {
                Debug.LogWarning($"[MultiplayerDragAndDrop] OnDrag blocked by lock");
                return;
            }
            
            // Debug first drag event
            if (Time.frameCount % 10 == 0) // Log every 10 frames to avoid spam
            {
                Debug.Log($"[MultiplayerDragAndDrop] OnDrag: delta={eventData.delta}, position={rectTransform.anchoredPosition}");
            }
            
            // FIX: Tính đến CanvasScaler khi drag
            // Nếu Canvas có CanvasScaler với ScaleWithScreenSize, cần adjust delta
            if (canvas != null)
            {
                // Sử dụng anchoredPosition thay vì position để tránh vấn đề với CanvasScaler
                rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
            }
            else
            {
                // Fallback: Sử dụng position trực tiếp
                transform.position += (Vector3)eventData.delta;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isLocked) return;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            // Phương pháp 1: Thử dùng pointerEnter (cách cũ)
            GameObject droppedOn = eventData.pointerEnter;
            
            // Phương pháp 2: Nếu pointerEnter không hoạt động, dùng RaycastAll
            if (droppedOn == null || !droppedOn.CompareTag("Slot"))
            {
                droppedOn = FindSlotAtPointer(eventData.position);
            }

            Debug.Log($"[MultiplayerDragAndDrop] End drag: {myText?.text}, dropped on: {droppedOn?.name}");

            if (droppedOn != null && droppedOn.CompareTag("Slot"))
            {
                // Parse đáp án
                if (myText == null || !int.TryParse(myText.text, out int answer))
                {
                    Debug.LogWarning($"[MultiplayerDragAndDrop] Cannot parse answer: {myText?.text}");
                    StartCoroutine(SmoothReturn());
                    return;
                }

                Debug.Log($"[MultiplayerDragAndDrop] Player dropped answer: {answer}");

                // Submit đáp án qua BattleController
                if (battleController != null)
                {
                    battleController.OnAnswerDropped(answer);

                    // Lock tất cả choices
                    SetGlobalLock(true);

                    // Visual feedback: Di chuyển vào slot
                    rectTransform.anchoredPosition = droppedOn.GetComponent<RectTransform>().anchoredPosition;
                    image.color = Color.yellow; // Màu chờ kết quả

                    Debug.Log($"[MultiplayerDragAndDrop] Answer submitted: {answer}");
                }
                else
                {
                    Debug.LogError("[MultiplayerDragAndDrop] BattleController is NULL!");
                    StartCoroutine(SmoothReturn());
                }
            }
            else
            {
                // Không thả vào slot → Trả về vị trí cũ
                StartCoroutine(SmoothReturn());
                image.color = originalColor;
            }
        }

        /// <summary>
        /// Tìm Slot object tại vị trí con trỏ chuột bằng RaycastAll
        /// Bỏ qua Answer objects vì chúng có thể che khuất Slot
        /// Tìm Slot ngay cả khi nó bị disable
        /// </summary>
        private GameObject FindSlotAtPointer(Vector2 screenPosition)
        {
            // Phương pháp 1: Thử RaycastAll bình thường
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Debug.Log($"[MultiplayerDragAndDrop] RaycastAll found {results.Count} objects at {screenPosition}");

            foreach (RaycastResult result in results)
            {
                Debug.Log($"  - {result.gameObject.name} (tag: {result.gameObject.tag})");
                
                // Bỏ qua Answer objects (chúng có MultiplayerDragAndDrop component)
                if (result.gameObject.GetComponent<MultiplayerDragAndDrop>() != null)
                {
                    Debug.Log($"    → Bỏ qua (là Answer object)");
                    continue;
                }
                
                // Tìm Slot
                if (result.gameObject.CompareTag("Slot"))
                {
                    Debug.Log($"[MultiplayerDragAndDrop] ✅ Found Slot: {result.gameObject.name}");
                    return result.gameObject;
                }
            }

            // Phương pháp 2: Nếu không tìm thấy, tìm Slot trực tiếp theo tag
            // (Cách này hoạt động ngay cả khi Slot bị disable)
            Debug.Log("[MultiplayerDragAndDrop] RaycastAll không tìm thấy, thử tìm trực tiếp...");
            
            GameObject[] allSlots = GameObject.FindGameObjectsWithTag("Slot");
            if (allSlots.Length > 0)
            {
                GameObject slot = allSlots[0];
                
                // Kiểm tra xem Slot có nằm trong vùng thả không
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    // Chuyển screen position sang local position của Slot
                    Canvas canvas = FindObjectOfType<Canvas>();
                    if (canvas != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        slotRect, screenPosition, canvas.worldCamera, out Vector2 localPoint))
                    {
                        // Kiểm tra xem point có nằm trong rect không
                        if (slotRect.rect.Contains(localPoint))
                        {
                            Debug.Log($"[MultiplayerDragAndDrop] ✅ Found Slot (direct): {slot.name}");
                            return slot;
                        }
                    }
                }
            }

            Debug.Log("[MultiplayerDragAndDrop] ❌ No Slot found at pointer position");
            return null;
        }

        /// <summary>
        /// Hiển thị kết quả đúng/sai (gọi từ UIMultiplayerBattleController)
        /// </summary>
        public void ShowResult(bool isCorrect)
        {
            if (isCorrect)
            {
                image.color = colorCorrect;
                Debug.Log($"[MultiplayerDragAndDrop] Correct answer!");
            }
            else
            {
                image.color = colorWrong;
                Debug.Log($"[MultiplayerDragAndDrop] Wrong answer!");
            }

            // Tự động reset sau delay
            StartCoroutine(ResetAfterDelay(1.5f));
        }

        /// <summary>
        /// Trả về vị trí gốc mượt mà
        /// </summary>
        private IEnumerator SmoothReturn()
        {
            float time = 0;
            Vector2 currentPos = rectTransform.anchoredPosition;
            
            while (time < 0.2f)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(currentPos, originalPosition, time / 0.2f);
                time += Time.deltaTime;
                yield return null;
            }
            
            rectTransform.anchoredPosition = originalPosition;
        }

        /// <summary>
        /// Reset sau khi hiển thị kết quả
        /// </summary>
        private IEnumerator ResetAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            image.color = originalColor;
            rectTransform.anchoredPosition = originalPosition;
            
            // Mở khóa nếu không còn menu nào
            SetGlobalLock(false);
        }

        /// <summary>
        /// Force reset về vị trí gốc
        /// </summary>
        public void ForceResetPosition()
        {
            StopAllCoroutines();
            isLocked = false;
            
            // Nếu originalPosition chưa được set (panel inactive lúc Awake)
            // Thì lưu vị trí hiện tại làm original
            if (originalPosition == Vector2.zero && rectTransform != null)
            {
                originalPosition = rectTransform.anchoredPosition;
                Debug.Log($"[MultiplayerDragAndDrop] ForceResetPosition: Saved original position for {name}: {originalPosition}");
            }
            
            if (image != null) 
                image.color = originalColor;
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
        }

        /// <summary>
        /// Set global lock cho tất cả MultiplayerDragAndDrop
        /// </summary>
        public static void SetGlobalLock(bool locked)
        {
            isLocked = locked;
            Debug.Log($"[MultiplayerDragAndDrop] Global lock: {locked}");
        }

        /// <summary>
        /// Release tất cả locks
        /// </summary>
        public static void ReleaseAllLocks()
        {
            isLocked = false;
            Debug.Log("[MultiplayerDragAndDrop] Released all locks");
        }

        /// <summary>
        /// Reset tất cả MultiplayerDragAndDrop trong scene
        /// </summary>
        public static void ResetAll()
        {
            var allDrags = FindObjectsOfType<MultiplayerDragAndDrop>();
            foreach (var drag in allDrags)
            {
                drag.ForceResetPosition();
            }
            
            SetGlobalLock(false);
            Debug.Log($"[MultiplayerDragAndDrop] Reset all ({allDrags.Length} objects)");
        }
    }
}
