using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AudioBridgeSetup
{
    [MenuItem("Audio/Setup Bridge For Current Scene")]
    public static void SetupBridgeForScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogError("No scene loaded.");
            return;
        }

        // Try find AudioManager in scene
        var audioManagerGO = GameObject.Find("AudioManager");
        GameObject target;
        if (audioManagerGO != null)
        {
            target = audioManagerGO;
        }
        else
        {
            // create a bridge GameObject
            target = GameObject.Find("AudioBridge");
            if (target == null) target = new GameObject("AudioBridge");
        }

        // Add AudioEventBridge if missing
        var bridge = target.GetComponent<DoAnGame.Audio.AudioEventBridge>();
        if (bridge == null)
        {
            bridge = target.AddComponent<DoAnGame.Audio.AudioEventBridge>();
            Debug.Log("AudioEventBridge added to " + target.name);
        }
        bridge.autoHookButtons = false;
        bridge.autoHookBattleStatusText = true;
        bridge.autoHookAnswerTimerWarning = true;
        bridge.autoHookModePanels = true;

        // map scene name to SceneMusicType
        string s = scene.name.ToLowerInvariant();
        if (s.Contains("gameuiplay") || s.Contains("menu")) bridge.sceneMusicType = DoAnGame.Audio.AudioEventBridge.SceneMusicType.MainMenu;
        else if (s.Contains("chonda") || s.Contains("class")) bridge.sceneMusicType = DoAnGame.Audio.AudioEventBridge.SceneMusicType.Class;
        else if (s.Contains("keotha") || s.Contains("keotha") || s.Contains("defense") || s.Contains("tp")) bridge.sceneMusicType = DoAnGame.Audio.AudioEventBridge.SceneMusicType.Defense;
        else if (s.Contains("phi") || s.Contains("phithuyen") || s.Contains("phithuyen") || s.Contains("space")) bridge.sceneMusicType = DoAnGame.Audio.AudioEventBridge.SceneMusicType.Space;
        else if (s.Contains("test_firebase_multi") || s.Contains("multiplayer") || s.Contains("battle")) bridge.sceneMusicType = DoAnGame.Audio.AudioEventBridge.SceneMusicType.Multiplayer;
        else bridge.sceneMusicType = DoAnGame.Audio.AudioEventBridge.SceneMusicType.None;

        ConfigurePanelMusicSwitching(scene, bridge);
        ConfigureManualReferences(scene, bridge);

        // Add UIButtonAudioHelper to each Canvas in the scene (including inactive ones)
        var hooked = 0;
        var canvases = new System.Collections.Generic.List<UnityEngine.Canvas>();
        foreach (var root in scene.GetRootGameObjects())
        {
            canvases.AddRange(root.GetComponentsInChildren<UnityEngine.Canvas>(true));
        }
        foreach (var c in canvases)
        {
            var go = c.gameObject;
            var helper = go.GetComponent<DoAnGame.UI.UIButtonAudioHelper>();
            if (helper == null)
            {
                helper = go.AddComponent<DoAnGame.UI.UIButtonAudioHelper>();
                hooked++;
            }
            helper.autoSetupOnStart = true;
            helper.hookChildrenOnly = true;
        }

        var globalListener = target.GetComponent<DoAnGame.Audio.GlobalClickAudioListener>();
        if (globalListener == null)
        {
            globalListener = target.AddComponent<DoAnGame.Audio.GlobalClickAudioListener>();
            Debug.Log("GlobalClickAudioListener added to " + target.name);
        }
        globalListener.enableGlobalClick = true;
        globalListener.onlyNonUIClicks = true;

        EditorUtility.SetDirty(bridge);
        EditorUtility.SetDirty(globalListener);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"AudioBridge setup complete for scene '{scene.name}'. SceneMusicType={bridge.sceneMusicType}. Added UIButtonAudioHelper to {hooked} Canvas(es). Button and outside-screen clicks use AudioManager.soundClick.");
    }

    private static void ConfigurePanelMusicSwitching(Scene scene, DoAnGame.Audio.AudioEventBridge bridge)
    {
        bridge.menuPanelNames = null;
        bridge.battlePanelNames = null;
        bridge.resultPanelNames = null;

        string s = scene.name.ToLowerInvariant();
        if (s.Contains("gameuiplay") || s.Contains("menu"))
        {
            bridge.menuPanelNames = new[] { "WELCOMESCREEN", "WellcomePanel", "ModSelectionPanel", "MainMenuPanel" };
            return;
        }

        if (s.Contains("chonda") || s.Contains("class"))
        {
            bridge.menuPanelNames = new[] { "ShopPanel", "ChonManPanel" };
            bridge.battlePanelNames = new[] { "GamePlay", "QuesUi" };
            bridge.resultPanelNames = new[] { "WinPanel", "LosePanel" };
            return;
        }

        if (s.Contains("keotha") || s.Contains("defense") || s.Contains("tp"))
        {
            bridge.menuPanelNames = new[] { "ShopPanel", "ChonManPanel" };
            bridge.battlePanelNames = new[] { "GamePlay", "QuesUI" };
            bridge.resultPanelNames = new[] { "WinPanel", "LosePanel" };
            return;
        }

        if (s.Contains("phi") || s.Contains("phithuyen") || s.Contains("space"))
        {
            bridge.menuPanelNames = new[] { "ShopPanel", "ChonManPanel" };
            bridge.battlePanelNames = new[] { "GamePlay", "QuesUI" };
            bridge.resultPanelNames = new[] { "WinPanel", "LosePanel" };
            return;
        }

        if (s.Contains("test_firebase_multi") || s.Contains("multiplayer") || s.Contains("battle"))
        {
            bridge.menuPanelNames = new[] { "LobbyPanel", "LobbyBrowserPanel" };
            bridge.battlePanelNames = new[] { "GameplayPanel" };
            bridge.resultPanelNames = new[] { "Wins" };
        }
    }

    private static void ConfigureManualReferences(Scene scene, DoAnGame.Audio.AudioEventBridge bridge)
    {
        bridge.battleStatusTextObject = null;
        bridge.answerTimerTextObject = null;
        bridge.answerResultTextObject = null;

        string s = scene.name.ToLowerInvariant();
        if (!(s.Contains("test_firebase_multi") || s.Contains("multiplayer") || s.Contains("battle")))
            return;

        bridge.battleStatusTextObject = FindInScene(scene, "Text (TMP) TrangThai");
        bridge.answerTimerTextObject = FindInScene(scene, "Timertext");

        // Answer result audio is played directly in UIMultiplayerBattleController,
        // so keep this null to avoid double correct/wrong sounds.
        bridge.answerResultTextObject = null;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var match = FindInChildren(root.transform, objectName);
            if (match != null) return match.gameObject;
        }

        return null;
    }

    private static Transform FindInChildren(Transform parent, string objectName)
    {
        if (parent.name == objectName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            var match = FindInChildren(parent.GetChild(i), objectName);
            if (match != null) return match;
        }

        return null;
    }
}
