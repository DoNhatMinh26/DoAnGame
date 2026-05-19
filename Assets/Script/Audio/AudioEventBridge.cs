using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace DoAnGame.Audio
{
    /// <summary>
    /// AudioEventBridge: single-file, non-invasive bridge that hooks common UI/game objects
    /// and calls AudioManager APIs. Attach to the same GameObject as your AudioManager.
    /// - Does NOT modify other scripts.
    /// - Uses polling + button listeners + optional inspector references.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioEventBridge : MonoBehaviour
    {
        public enum SceneMusicType { None, MainMenu, Class, Defense, Space, Multiplayer }

        [Header("Scene music type (choose the role of this scene)")]
        public SceneMusicType sceneMusicType = SceneMusicType.None;
        [Tooltip("Optional override clip for background music for this scene")]
        public AudioClip backgroundMusicOverride;

        [Header("Per-scene SFX (optional; if empty, AudioManager fields used)")]
        public AudioClip sfxCorrect;
        public AudioClip sfxWrong;
        public AudioClip sfxWin;
        public AudioClip sfxLose;
        public AudioClip sfxCoin;
        public AudioClip timerWarningClip;
        [Header("Auto hooks")]
        public bool autoHookButtons = false;
        public bool autoHookBattleStatusText = true;
        public bool autoHookAnswerTimerWarning = true;
        public bool autoHookModePanels = true;

        [Header("Manual references (optional)")]
        public GameObject battleStatusTextObject; // shows "3","2","1","Ready","GO!"
        public GameObject answerTimerTextObject;  // timer text in AnswerSummaryUI (e.g. "10s")
        public GameObject answerResultTextObject; // optional: text that shows "Correct"/"Wrong"
        public string[] modePanelNames = new string[] { "UiClass", "UiTp", "UiSp" };

        [Header("Panel music switching (optional names)")]
        [Tooltip("If any of these panels is active, bridge uses menu music.")]
        public string[] menuPanelNames;
        [Tooltip("If any of these panels is active, bridge uses battle music.")]
        public string[] battlePanelNames;
        [Tooltip("If any of these panels is active (Win/Lose), bridge keeps music stopped.")]
        public string[] resultPanelNames;

        [Header("Inspector Music Overrides (assign if AudioManager doesn't expose clips)")]
        public AudioClip menuMusic;
        public AudioClip battleMusic;
        public AudioClip victoryMusic;
        public AudioClip defeatMusic;

        [Header("Polling")]
        [Range(0.05f, 1f)]
        public float pollInterval = 0.12f;

        // internal state
        string lastBattleText = null;
        bool timerWarningPlayed = false;
        string lastResultText = null;
        PanelMusicState lastPanelMusicState = PanelMusicState.None;
        bool defaultSfxCaptured = false;
        AudioClip defaultSfxCorrect;
        AudioClip defaultSfxWrong;
        AudioClip defaultSfxWin;
        AudioClip defaultSfxLose;
        AudioClip defaultSfxCoin;

        enum PanelMusicState
        {
            None,
            Menu,
            Battle,
            Result
        }

        void Start()
        {
            if (autoHookButtons) HookAllButtons();
            // initial bind
            RebindCurrentSceneObjects();

            if (autoHookBattleStatusText) StartCoroutine(WatchBattleStatusRoutine());
            if (autoHookAnswerTimerWarning) StartCoroutine(WatchAnswerTimerRoutine());
            if (autoHookModePanels) StartCoroutine(WatchModePanelsRoutine());
            if (answerResultTextObject != null) StartCoroutine(WatchAnswerResultRoutine());
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // rebind when a new scene loads
            RebindCurrentSceneObjects();
        }

        #region Buttons
        void HookAllButtons()
        {
            var all = FindObjectsOfType<Button>(true);
            foreach (var b in all)
            {
                b.onClick.RemoveListener(PlayButtonClick);
                b.onClick.AddListener(PlayButtonClick);
            }
            Debug.Log($"[AudioEventBridge] Hooked {all.Length} buttons");
        }

        void PlayButtonClick()
        {
            var am = AudioManager.Instance;
            if (am == null) return;
            // prefer explicit clip field if available
            var clipField = am.GetType().GetField("soundClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var clip = clipField != null ? clipField.GetValue(am) as AudioClip : null;
            if (clip != null) am.PlaySFX(clip);
            else am.PlaySFX(null);
        }
        #endregion

        #region Countdown (battle status)
        IEnumerator WatchBattleStatusRoutine()
        {
            while (true)
            {
                string txt = GetTextFromObject(battleStatusTextObject) ?? FindShortUIText();
                if (!string.IsNullOrEmpty(txt) && txt != lastBattleText)
                {
                    HandleBattleStatusTextChange(txt.Trim());
                    lastBattleText = txt;
                }
                yield return new WaitForSeconds(pollInterval);
            }
        }

        void HandleBattleStatusTextChange(string txt)
        {
            var t = txt.ToLowerInvariant();
            if (t == "3") AudioManager.Instance?.PlayCountdown3();
            else if (t == "2") AudioManager.Instance?.PlayCountdown2();
            else if (t == "1") AudioManager.Instance?.PlayCountdown1();
            else if (t.Contains("ready")) AudioManager.Instance?.PlayCountdownReady();
            else if (t.Contains("go")) AudioManager.Instance?.PlayCountdownGo();
        }

        string FindShortUIText()
        {
            // try common names
            string[] names = { "battleStatusText", "statusText", "BattleStatus", "txtBattleStatus" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null)
                {
                    var s = GetTextFromObject(go);
                    if (!string.IsNullOrEmpty(s) && s.Length <= 8) return s;
                }
            }

            // fallback find any short text/TMP_Text in scene
            foreach (var t in FindObjectsOfType<TMP_Text>(true))
            {
                if (!string.IsNullOrEmpty(t.text) && t.text.Length <= 8) return t.text;
            }
            foreach (var t in FindObjectsOfType<Text>(true))
            {
                if (!string.IsNullOrEmpty(t.text) && t.text.Length <= 8) return t.text;
            }
            return null;
        }

        string GetTextFromObject(GameObject go)
        {
            if (go == null) return null;
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp != null) return tmp.text;
            var txt = go.GetComponent<Text>();
            if (txt != null) return txt.text;
            var childTmp = go.GetComponentInChildren<TMP_Text>(true);
            if (childTmp != null) return childTmp.text;
            var childTxt = go.GetComponentInChildren<Text>(true);
            if (childTxt != null) return childTxt.text;
            return null;
        }
        #endregion

        #region Answer timer warning
        IEnumerator WatchAnswerTimerRoutine()
        {
            while (true)
            {
                if (answerTimerTextObject != null)
                {
                    string txt = GetTextFromObject(answerTimerTextObject);
                    if (TryParseSecondsFromText(txt, out int seconds))
                    {
                                if (seconds <= 5 && !timerWarningPlayed)
                                {
                                    var am = AudioManager.Instance;
                                    if (am != null)
                                    {
                                        AudioClip warn = timerWarningClip != null
                                            ? timerWarningClip
                                            : FindAudioClipField(am, new[] { "timerWarning", "timerWarningSound", "timer_warning", "timerWarningClip" });
                                        if (warn != null) am.PlaySFX(warn);
                                    }
                                    timerWarningPlayed = true;
                                }
                        if (seconds > 5) timerWarningPlayed = false;
                    }
                }
                yield return new WaitForSeconds(pollInterval);
            }
        }

        bool TryParseSecondsFromText(string txt, out int seconds)
        {
            seconds = 0;
            if (string.IsNullOrEmpty(txt)) return false;
            var s = txt.Trim().Replace("s", "").Replace("S", "").Trim();
            return int.TryParse(s, out seconds);
        }
        #endregion

        #region Answer result detection (optional inspector link)
        IEnumerator WatchAnswerResultRoutine()
        {
            while (true)
            {
                string txt = GetTextFromObject(answerResultTextObject);
                if (!string.IsNullOrEmpty(txt) && txt != lastResultText)
                {
                    var lower = txt.ToLowerInvariant();
                    if (lower.Contains("correct") || lower.Contains("đúng") || lower.Contains("true"))
                    {
                        var am = AudioManager.Instance;
                        if (am != null)
                        {
                            var c = sfxCorrect != null ? sfxCorrect : am.soundCorrect;
                            if (c != null) am.PlaySFX(c);
                        }
                    }
                    else if (lower.Contains("wrong") || lower.Contains("sai") || lower.Contains("false"))
                    {
                        var am = AudioManager.Instance;
                        if (am != null)
                        {
                            var c = sfxWrong != null ? sfxWrong : am.soundWrong;
                            if (c != null) am.PlaySFX(c);
                        }
                    }
                    lastResultText = txt;
                }
                yield return new WaitForSeconds(pollInterval);
            }
        }
        #endregion

        #region Mode panels / music switching
        IEnumerator WatchModePanelsRoutine()
        {
            while (true)
            {
                UpdatePanelMusicState();
                // Run each frame so music switches immediately when UI panels change.
                yield return null;
            }
        }

        void UpdatePanelMusicState()
        {
            var panelState = ResolvePanelMusicState();
            if (panelState != PanelMusicState.None && panelState != lastPanelMusicState)
            {
                ApplyPanelMusicState(panelState);
                lastPanelMusicState = panelState;
            }
            else if (panelState == PanelMusicState.None)
            {
                lastPanelMusicState = PanelMusicState.None;
            }
        }

        PanelMusicState ResolvePanelMusicState()
        {
            if (IsAnyPanelActive(resultPanelNames))
                return PanelMusicState.Result;

            var autoResult = GetAutoResultPanelNamesForScene();
            if (IsAnyPanelActive(autoResult))
                return PanelMusicState.Result;

            if (IsAnyPanelActive(battlePanelNames))
                return PanelMusicState.Battle;
            if (IsAnyPanelActive(menuPanelNames))
                return PanelMusicState.Menu;

            var autoBattle = GetAutoBattlePanelNamesForScene();
            if (IsAnyPanelActive(autoBattle))
                return PanelMusicState.Battle;

            var autoMenu = GetAutoMenuPanelNamesForScene();
            if (IsAnyPanelActive(autoMenu))
                return PanelMusicState.Menu;

            // Legacy fallback for existing modePanelNames setting
            foreach (var name in modePanelNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var go = GameObject.Find(name);
                if (go != null && go.activeInHierarchy)
                {
                    return name.ToLowerInvariant().Contains("class")
                        ? PanelMusicState.Menu
                        : PanelMusicState.Battle;
                }
            }

            return PanelMusicState.None;
        }

        bool IsAnyPanelActive(string[] panelNames)
        {
            if (panelNames == null || panelNames.Length == 0) return false;
            foreach (var name in panelNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                var go = GameObject.Find(name);
                if (go != null && go.activeInHierarchy) return true;
            }
            return false;
        }

        string[] GetAutoMenuPanelNamesForScene()
        {
            switch (sceneMusicType)
            {
                case SceneMusicType.Class:
                case SceneMusicType.Defense:
                case SceneMusicType.Space:
                    return new[] { "ShopPanel", "ChonManPanel", "panelHome", "panelChonBai", "panelChonMan" };
                case SceneMusicType.Multiplayer:
                    return new[] { "LobbyPanel", "LobbyBrowserPanel" };
                case SceneMusicType.MainMenu:
                    return new[] { "WELCOMESCREEN", "WellcomePanel", "ModSelectionPanel", "MainMenuPanel" };
                default:
                    return null;
            }
        }

        string[] GetAutoBattlePanelNamesForScene()
        {
            switch (sceneMusicType)
            {
                case SceneMusicType.Class:
                case SceneMusicType.Defense:
                case SceneMusicType.Space:
                    return new[] { "GamePlay", "QuesUi", "QuesUI", "panelGameplay" };
                case SceneMusicType.Multiplayer:
                    return new[] { "GameplayPanel" };
                default:
                    return null;
            }
        }

        string[] GetAutoResultPanelNamesForScene()
        {
            switch (sceneMusicType)
            {
                case SceneMusicType.Class:
                case SceneMusicType.Defense:
                case SceneMusicType.Space:
                    return new[] { "WinPanel", "LosePanel" };
                case SceneMusicType.Multiplayer:
                    return new[] { "Wins" };
                default:
                    return null;
            }
        }

        void ApplyPanelMusicState(PanelMusicState panelState)
        {
            var am = AudioManager.Instance;
            if (am == null) return;

            if (panelState == PanelMusicState.Result)
            {
                am.StopMusic();
                return;
            }

            if (panelState == PanelMusicState.Battle)
            {
                if (battleMusic != null)
                {
                    am.PlayMusicWithFade(battleMusic);
                }
                else
                {
                    PlaySceneTypeDefaultMusic(am);
                }
                return;
            }

            if (menuMusic != null)
            {
                am.PlayMusicWithFade(menuMusic);
            }
            else
            {
                PlaySceneTypeDefaultMusic(am);
            }
        }

        void PlaySceneTypeDefaultMusic(AudioManager am)
        {
            if (am == null) return;

            switch (sceneMusicType)
            {
                case SceneMusicType.MainMenu:
                    am.PlayMainMenuMusic();
                    break;
                case SceneMusicType.Class:
                    am.PlayClassModeMusic();
                    break;
                case SceneMusicType.Defense:
                    am.PlayDefenseModeMusic();
                    break;
                case SceneMusicType.Space:
                    am.PlaySpaceModeMusic();
                    break;
                case SceneMusicType.Multiplayer:
                    am.PlayMultiplayerModeMusic();
                    break;
            }
        }

        // Rebind helpers: find common objects in the current scene and set references
        void RebindCurrentSceneObjects()
        {
            if (autoHookButtons) HookAllButtons();

            // Try to find common battle status text objects
            if (battleStatusTextObject == null)
            {
                string[] commonBattleNames = { "Text (TMP) TrangThai", "StatusText", "battleStatusText", "TextTrangThai", "battleStatus" };
                foreach (var n in commonBattleNames)
                {
                    var go = GameObject.Find(n);
                    if (go != null)
                    {
                        battleStatusTextObject = go;
                        break;
                    }
                }
            }

            // Try to find common timer text objects
            if (answerTimerTextObject == null)
            {
                string[] commonTimerNames = { "Timertext", "TimerState", "timeText", "timerText", "Timertext (TMP)" };
                foreach (var n in commonTimerNames)
                {
                    var go = GameObject.Find(n);
                    if (go != null)
                    {
                        answerTimerTextObject = go;
                        break;
                    }
                }
            }

            ApplyPerSceneSfxOverrides();
            // Apply scene music selection
            ApplySceneMusicSelection();
            // Then resolve active panels immediately to avoid delayed menu/battle switch.
            UpdatePanelMusicState();
            Debug.Log("[AudioEventBridge] Rebound scene objects and applied music settings");
        }

        void ApplyPerSceneSfxOverrides()
        {
            var am = AudioManager.Instance;
            if (am == null) return;

            if (!defaultSfxCaptured)
            {
                defaultSfxCorrect = am.soundCorrect;
                defaultSfxWrong = am.soundWrong;
                defaultSfxWin = am.soundWin;
                defaultSfxLose = am.soundLose;
                defaultSfxCoin = am.soundCoin;
                defaultSfxCaptured = true;
            }

            am.soundCorrect = sfxCorrect != null ? sfxCorrect : defaultSfxCorrect;
            am.soundWrong = sfxWrong != null ? sfxWrong : defaultSfxWrong;
            am.soundWin = sfxWin != null ? sfxWin : defaultSfxWin;
            am.soundLose = sfxLose != null ? sfxLose : defaultSfxLose;
            am.soundCoin = sfxCoin != null ? sfxCoin : defaultSfxCoin;
        }

        void ApplySceneMusicSelection()
        {
            var am = AudioManager.Instance;
            if (am == null) return;

            if (backgroundMusicOverride != null)
            {
                am.PlayMusicWithFade(backgroundMusicOverride);
                return;
            }

            switch (sceneMusicType)
            {
                case SceneMusicType.MainMenu:
                    // try helper method then fallback
                    var m1 = am.GetType().GetMethod("PlayMainMenuMusic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (m1 != null) m1.Invoke(am, null);
                    else am.PlayMusicWithFade(menuMusic ?? menuMusicFallback());
                    break;
                case SceneMusicType.Class:
                    var mClass = am.GetType().GetMethod("PlayClassModeMusic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mClass != null) mClass.Invoke(am, null);
                    else am.PlayMusicWithFade(menuMusic ?? menuMusicFallback());
                    break;
                case SceneMusicType.Defense:
                    var mDef = am.GetType().GetMethod("PlayDefenseModeMusic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mDef != null) mDef.Invoke(am, null);
                    else am.PlayMusicWithFade(battleMusic ?? battleMusicFallback());
                    break;
                case SceneMusicType.Space:
                    var mSpace = am.GetType().GetMethod("PlaySpaceModeMusic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mSpace != null) mSpace.Invoke(am, null);
                    else am.PlayMusicWithFade(battleMusic ?? battleMusicFallback());
                    break;
                case SceneMusicType.Multiplayer:
                    var mMulti = am.GetType().GetMethod("PlayMultiplayerModeMusic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mMulti != null) mMulti.Invoke(am, null);
                    else am.PlayMusicWithFade(battleMusic ?? battleMusicFallback());
                    break;
                case SceneMusicType.None:
                default:
                    break;
            }
        }

        AudioClip menuMusicFallback()
        {
            try
            {
                var am = AudioManager.Instance;
                if (am == null) return null;
                var t = am.GetType();
                // try multiple common field names
                var fields = new[] { "musicMenu", "menuMusic", "musicMain", "music_main", "musicMenuClip" };
                foreach (var n in fields)
                {
                    var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null) return f.GetValue(am) as AudioClip;
                }
            }
            catch { }
            return null;
        }

        AudioClip battleMusicFallback()
        {
            try
            {
                var am = AudioManager.Instance;
                if (am == null) return null;
                var t = am.GetType();
                var fields = new[] { "musicClassMode", "musicDefenseMode", "musicSpaceMode", "musicMultiplayer", "battleMusic", "musicBattle", "musicClass" };
                foreach (var n in fields)
                {
                    var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null) return f.GetValue(am) as AudioClip;
                }
            }
            catch { }
            return null;
        }

        AudioClip FindAudioClipField(object amObj, string[] candidates)
        {
            if (amObj == null || candidates == null) return null;
            var t = amObj.GetType();
            foreach (var n in candidates)
            {
                try
                {
                    var f = t.GetField(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (f != null)
                    {
                        var v = f.GetValue(amObj) as AudioClip;
                        if (v != null) return v;
                    }
                }
                catch { }
            }
            return null;
        }
        #endregion
    }
}
