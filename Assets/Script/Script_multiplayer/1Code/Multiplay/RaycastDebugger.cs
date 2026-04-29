using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

namespace DoAnGame.Multiplayer
{
    /// <summary>
    /// Debug tool để kiểm tra raycast issues
    /// Hiển thị tất cả objects dưới con trỏ chuột
    /// </summary>
    public class RaycastDebugger : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enableDebug = true;
        [SerializeField] private KeyCode debugKey = KeyCode.F1;
        [SerializeField] private bool showOnScreenLog = true;

        [Header("UI")]
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private GameObject debugPanel;

        private List<string> logMessages = new List<string>();
        private const int maxLogMessages = 20;

        private void Update()
        {
            if (!enableDebug)
                return;

            // Toggle debug panel
            if (Input.GetKeyDown(debugKey))
            {
                if (debugPanel != null)
                {
                    debugPanel.SetActive(!debugPanel.activeSelf);
                }
            }

            // Check raycast on mouse click
            if (Input.GetMouseButtonDown(0))
            {
                CheckRaycastUnderMouse();
            }
        }

        private void CheckRaycastUnderMouse()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                AddLog("❌ EventSystem is NULL!");
                return;
            }

            var pointerData = new PointerEventData(es)
            {
                position = Input.mousePosition
            };

            var results = new List<RaycastResult>();
            es.RaycastAll(pointerData, results);

            AddLog($"========== RAYCAST at {Input.mousePosition} ==========");
            AddLog($"EventSystem: {es.name}, enabled={es.enabled}");
            AddLog($"Hits: {results.Count}");

            if (results.Count == 0)
            {
                AddLog("❌ NO HITS!");
                CheckWhyNoHits();
            }
            else
            {
                for (int i = 0; i < Mathf.Min(10, results.Count); i++)
                {
                    var hit = results[i];
                    string hitInfo = $"[{i}] {hit.gameObject.name}";
                    
                    // Check components
                    var image = hit.gameObject.GetComponent<Image>();
                    var button = hit.gameObject.GetComponent<Button>();
                    var dragDrop = hit.gameObject.GetComponent<MultiplayerDragAndDrop>();
                    var canvasGroup = hit.gameObject.GetComponent<CanvasGroup>();

                    if (image != null)
                        hitInfo += $" | Image(raycast={image.raycastTarget})";
                    if (button != null)
                        hitInfo += $" | Button(interactable={button.interactable})";
                    if (dragDrop != null)
                        hitInfo += $" | DragDrop";
                    if (canvasGroup != null)
                        hitInfo += $" | CanvasGroup(blocksRaycasts={canvasGroup.blocksRaycasts})";

                    AddLog(hitInfo);
                }
            }

            AddLog("=================================================");
            UpdateDebugUI();
        }

        private void CheckWhyNoHits()
        {
            // Check Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                AddLog("❌ No Canvas found!");
                return;
            }

            AddLog($"Canvas: {canvas.name}, renderMode={canvas.renderMode}");

            // Check GraphicRaycaster
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                AddLog("❌ No GraphicRaycaster on Canvas!");
                return;
            }

            AddLog($"GraphicRaycaster: enabled={raycaster.enabled}");

            // Check Answer objects
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            AddLog($"Found {answers.Length} Answer objects:");

            foreach (var answer in answers)
            {
                var image = answer.GetComponent<Image>();
                var canvasGroup = answer.GetComponent<CanvasGroup>();
                
                string status = $"  {answer.name}: ";
                status += $"active={answer.gameObject.activeInHierarchy}, ";
                status += $"raycast={image?.raycastTarget}, ";
                status += $"blocksRaycasts={canvasGroup?.blocksRaycasts}";

                AddLog(status);
            }
        }

        [ContextMenu("Check All Answer Objects")]
        public void CheckAllAnswerObjects()
        {
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            
            AddLog($"========== CHECKING {answers.Length} ANSWER OBJECTS ==========");

            foreach (var answer in answers)
            {
                AddLog($"--- {answer.name} ---");
                AddLog($"  GameObject active: {answer.gameObject.activeInHierarchy}");
                AddLog($"  GameObject layer: {LayerMask.LayerToName(answer.gameObject.layer)}");

                var image = answer.GetComponent<Image>();
                if (image != null)
                {
                    AddLog($"  Image: raycastTarget={image.raycastTarget}, enabled={image.enabled}, color={image.color}");
                }
                else
                {
                    AddLog($"  ❌ NO IMAGE COMPONENT!");
                }

                var canvasGroup = answer.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    AddLog($"  CanvasGroup: blocksRaycasts={canvasGroup.blocksRaycasts}, alpha={canvasGroup.alpha}, interactable={canvasGroup.interactable}");
                }
                else
                {
                    AddLog($"  ❌ NO CANVASGROUP COMPONENT!");
                }

                var rectTransform = answer.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    AddLog($"  RectTransform: anchoredPosition={rectTransform.anchoredPosition}, sizeDelta={rectTransform.sizeDelta}");
                }

                // Check parent Canvas
                var parentCanvas = answer.GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    AddLog($"  Parent Canvas: {parentCanvas.name}");
                    var raycaster = parentCanvas.GetComponent<GraphicRaycaster>();
                    if (raycaster != null)
                    {
                        AddLog($"    GraphicRaycaster: enabled={raycaster.enabled}");
                    }
                    else
                    {
                        AddLog($"    ❌ NO GRAPHICRAYCASTER!");
                    }
                }
                else
                {
                    AddLog($"  ❌ NO PARENT CANVAS!");
                }
            }

            AddLog("=================================================");
            UpdateDebugUI();
        }

        [ContextMenu("Force Enable All Answer Raycasts")]
        public void ForceEnableAllAnswerRaycasts()
        {
            var answers = FindObjectsOfType<MultiplayerDragAndDrop>(true);
            int fixedCount = 0;

            foreach (var answer in answers)
            {
                var image = answer.GetComponent<Image>();
                if (image != null)
                {
                    if (!image.raycastTarget)
                    {
                        image.raycastTarget = true;
                        fixedCount++;
                        AddLog($"✅ Enabled raycast on {answer.name}");
                    }
                }

                var canvasGroup = answer.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    if (!canvasGroup.blocksRaycasts)
                    {
                        canvasGroup.blocksRaycasts = true;
                        fixedCount++;
                        AddLog($"✅ Enabled blocksRaycasts on {answer.name}");
                    }
                }
            }

            AddLog($"✅ Fixed {fixedCount} components");
            UpdateDebugUI();
        }

        private void AddLog(string message)
        {
            logMessages.Add(message);
            if (logMessages.Count > maxLogMessages)
            {
                logMessages.RemoveAt(0);
            }

            Debug.Log($"[RaycastDebugger] {message}");
        }

        private void UpdateDebugUI()
        {
            if (!showOnScreenLog || debugText == null)
                return;

            debugText.text = string.Join("\n", logMessages);
        }

        private void OnGUI()
        {
            if (!enableDebug || !showOnScreenLog || debugPanel != null)
                return;

            // Fallback: Draw on screen if no debug panel
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"<b>Raycast Debugger</b> (Press {debugKey} to toggle)", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("Click anywhere to check raycast");
            
            GUILayout.Space(10);
            
            foreach (var msg in logMessages)
            {
                GUILayout.Label(msg);
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
