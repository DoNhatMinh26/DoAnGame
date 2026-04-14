using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoAnGame.UI
{
    /// <summary>
    /// Quản lý luồng chuyển đổi giữa các UI panels dựa trên cấu trúc 14 màn hình.
    /// </summary>
    public class UIFlowManager : MonoBehaviour
    {
        public enum Screen
        {
            [InspectorName("UI 1 · Welcome Intro")]
            WelcomeIntro = 1,

            [InspectorName("UI 2 · Welcome Auth")]
            WelcomeAuth = 2,

            [InspectorName("UI 3 · Login")]
            Login = 3,

            [InspectorName("UI 4 · Register")]
            Register = 4,

            [InspectorName("UI 5 · Main Menu")]
            MainMenu = 5,

            [InspectorName("UI 6 · Mode Selection")]
            ModeSelection = 6,

            [InspectorName("UI 7 · Level Selection")]
            LevelSelection = 7,

            [InspectorName("UI 8 · Difficulty")]
            Difficulty = 8,

            [InspectorName("UI 9 · In Game")]
            InGame = 9,

            [InspectorName("UI 10 · Pause Menu")]
            PauseMenu = 10,

            [InspectorName("UI 11 · Game Result")]
            GameResult = 11,

            [InspectorName("UI 12 · Leaderboard")]
            Leaderboard = 12,

            [InspectorName("UI 13 · Profile")]
            Profile = 13,

            [InspectorName("UI 14 · Settings")]
            Settings = 14,

            [InspectorName("UI 15 · Multiplayer Room")]
            MultiplayerRoom = 15,

            [InspectorName("UI 16 · Multiplayer Battle")]
            MultiplayerBattle = 16
        }

        [Serializable]
        private struct PanelEntry
        {
            public Screen screen;
            public BasePanelController panel;
        }

        [Header("Panel Mapping (Danh sách panel)")]
        [SerializeField] private Screen startScreen = Screen.WelcomeIntro;
        [SerializeField] private PanelEntry[] panels;

        private readonly Dictionary<Screen, BasePanelController> panelLookup = new Dictionary<Screen, BasePanelController>();
        private readonly Stack<Screen> history = new Stack<Screen>();

        public Screen CurrentScreen { get; private set; }

        private void Awake()
        {
            panelLookup.Clear();
            foreach (var entry in panels)
            {
                if (entry.panel == null)
                {
                    Debug.LogWarning($"[UIFlow] Panel cho screen {entry.screen} bị null");
                    continue;
                }

                if (!panelLookup.ContainsKey(entry.screen))
                {
                    panelLookup.Add(entry.screen, entry.panel);
                }
            }
        }

        private void Start()
        {
            if (SceneFlowBridge.TryConsume(out var requestedScreen))
            {
                ShowScreen(requestedScreen, false);
            }
            else
            {
                ShowScreen(startScreen, false);
            }
        }

        public void ShowScreen(Screen targetScreen, bool pushHistory = true)
        {
            TryShowScreen(targetScreen, pushHistory);
        }

        public bool TryShowScreen(Screen targetScreen, bool pushHistory = true)
        {
            if (!panelLookup.TryGetValue(targetScreen, out var nextPanel))
            {
                Debug.LogError($"[UIFlow] Không tìm thấy panel cho screen {targetScreen}");
                return false;
            }

            if (CurrentScreen.Equals(targetScreen) && nextPanel.IsVisible)
            {
                return true;
            }

            if (pushHistory && CurrentScreen != 0)
            {
                history.Push(CurrentScreen);
            }

            if (panelLookup.TryGetValue(CurrentScreen, out var currentPanel))
            {
                currentPanel.Hide();
            }

            nextPanel.Show();
            CurrentScreen = targetScreen;
            return true;
        }

        public void Back()
        {
            if (history.Count == 0)
            {
                Debug.Log("[UIFlow] Không còn màn hình để back.");
                return;
            }

            var previous = history.Pop();
            ShowScreen(previous, false);
        }

        public void ShowSettings(Screen originScreen)
        {
            history.Push(originScreen);
            ShowScreen(Screen.Settings, false);
        }

        public void ReturnFromSettings()
        {
            if (history.Count == 0)
            {
                ShowScreen(startScreen, false);
                return;
            }

            var previous = history.Pop();
            ShowScreen(previous, false);
        }
    }
}
