using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using NatchapholAunjai;

namespace NatchapholAunjai.EditorTools
{
    public static class AudioSystemSetup
    {
        private const string MIXER_PATH = "Assets/MainAudioMixer.mixer";
        private const string BGM_PATH = "Assets/Sounds/BGM/Chunky_Monkey.wav";
        private const string CLICK_PATH = "Assets/Sounds/SFX/Coin Pickup 4.wav";
        private const string WIN_PATH = "Assets/Sounds/SFX/Win sound 1.wav";
        private const string LOSE_PATH = "Assets/Sounds/SFX/Game Over 2.wav";
        private const string ADD_TIME_PATH = "Assets/Sounds/SFX/Power up 1.wav";
        private const string SUB_TIME_PATH = "Assets/Sounds/SFX/Debuff Downgrade 1.wav";
        private const string ADD_SCORE_PATH = "Assets/Sounds/SFX/Coins.wav";
        private const string SUB_SCORE_PATH = "Assets/Sounds/SFX/Debuff Downgrade 2.wav";

        [MenuItem("Tools/Setup Audio System")]
        public static void RunSetup()
        {
            Debug.Log("[AudioSystemSetup] Starting Audio System Setup...");

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MIXER_PATH);
            if (mixer == null)
            {
                Debug.LogError("MainAudioMixer not found at " + MIXER_PATH);
                return;
            }

            AudioMixerGroup[] bgmGroups = mixer.FindMatchingGroups("BGM");
            AudioMixerGroup[] sfxGroups = mixer.FindMatchingGroups("SFX");
            AudioMixerGroup bgmGroup = (bgmGroups != null && bgmGroups.Length > 0) ? bgmGroups[0] : null;
            AudioMixerGroup sfxGroup = (sfxGroups != null && sfxGroups.Length > 0) ? sfxGroups[0] : null;

            AudioClip defaultBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(BGM_PATH);
            AudioClip clickSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(CLICK_PATH);
            AudioClip winSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(WIN_PATH);
            AudioClip loseSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(LOSE_PATH);
            AudioClip addTimeSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(ADD_TIME_PATH);
            AudioClip subTimeSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(SUB_TIME_PATH);
            AudioClip addScoreSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(ADD_SCORE_PATH);
            AudioClip subScoreSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(SUB_SCORE_PATH);

            // 1. Setup Main Menu Scene with persistent AudioManager
            SetupMainMenuScene(mixer, bgmGroup, sfxGroup, defaultBgm, clickSfx, winSfx, loseSfx, addTimeSfx, subTimeSfx, addScoreSfx, subScoreSfx);

            // 2. Setup Options Scene with OptionsMenuUI and Sliders
            SetupOptionsScene();

            // 3. Setup UIButtonSound on all Canvases across all scenes in Build Settings
            SetupAllScenesButtons();

            Debug.Log("[AudioSystemSetup] Audio System Setup Completed Successfully!");
        }

        private static void SetupMainMenuScene(
            AudioMixer mixer,
            AudioMixerGroup bgmGroup,
            AudioMixerGroup sfxGroup,
            AudioClip defaultBgm,
            AudioClip clickSfx,
            AudioClip winSfx,
            AudioClip loseSfx,
            AudioClip addTimeSfx,
            AudioClip subTimeSfx,
            AudioClip addScoreSfx,
            AudioClip subScoreSfx)
        {
            string scenePath = "Assets/Scenes/Main Menu.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Find or create AudioManager GameObject
            GameObject audioMgrGo = GameObject.Find("AudioManager");
            if (audioMgrGo == null)
            {
                audioMgrGo = new GameObject("AudioManager");
            }

            AudioManager audioMgr = audioMgrGo.GetComponent<AudioManager>();
            if (audioMgr == null)
            {
                audioMgr = audioMgrGo.AddComponent<AudioManager>();
            }

            // Assign mixer and groups
            audioMgr.audioMixer = mixer;
            audioMgr.bgmGroup = bgmGroup;
            audioMgr.sfxGroup = sfxGroup;

            // Audio sources
            AudioSource[] sources = audioMgrGo.GetComponents<AudioSource>();
            AudioSource bgmSource = null;
            AudioSource sfxSource = null;

            if (sources.Length >= 2)
            {
                bgmSource = sources[0];
                sfxSource = sources[1];
            }
            else if (sources.Length == 1)
            {
                bgmSource = sources[0];
                sfxSource = audioMgrGo.AddComponent<AudioSource>();
            }
            else
            {
                bgmSource = audioMgrGo.AddComponent<AudioSource>();
                sfxSource = audioMgrGo.AddComponent<AudioSource>();
            }

            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.outputAudioMixerGroup = bgmGroup;

            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.outputAudioMixerGroup = sfxGroup;

            audioMgr.bgmSource = bgmSource;
            audioMgr.sfxSource = sfxSource;

            // Assign clips
            audioMgr.defaultBgm = defaultBgm;
            audioMgr.buttonClickSfx = clickSfx;
            audioMgr.winSfx = winSfx;
            audioMgr.loseSfx = loseSfx;
            audioMgr.itemAddTimeSfx = addTimeSfx;
            audioMgr.itemSubTimeSfx = subTimeSfx;
            audioMgr.itemAddScoreSfx = addScoreSfx;
            audioMgr.itemSubScoreSfx = subScoreSfx;

            // Attach UIButtonSound to all buttons in Main Menu
            AddButtonSoundsToSceneGameObjects();

            EditorUtility.SetDirty(audioMgrGo);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AudioSystemSetup] Main Menu scene updated and saved with AudioManager.");
        }

        private static void SetupOptionsScene()
        {
            string scenePath = "Assets/Scenes/Options.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            GameObject optionsPanel = GameObject.Find("OptionsPanel");
            if (optionsPanel == null)
            {
                optionsPanel = GameObject.Find("Canvas");
            }

            OptionsMenuUI optionsUI = null;
            if (optionsPanel != null)
            {
                optionsUI = optionsPanel.GetComponent<OptionsMenuUI>();
                if (optionsUI == null)
                {
                    optionsUI = optionsPanel.AddComponent<OptionsMenuUI>();
                }
            }

            // Find Sliders
            Slider bgmSlider = null;
            Slider sfxSlider = null;

            GameObject bgmSliderGo = GameObject.Find("BGMSlider");
            if (bgmSliderGo != null)
            {
                bgmSlider = bgmSliderGo.GetComponent<Slider>();
                if (bgmSlider != null)
                {
                    bgmSlider.minValue = 0.0001f;
                    bgmSlider.maxValue = 1f;
                    bgmSlider.value = 0.75f;
                    EditorUtility.SetDirty(bgmSlider);
                }
            }

            GameObject sfxSliderGo = GameObject.Find("SFXSlider");
            if (sfxSliderGo != null)
            {
                sfxSlider = sfxSliderGo.GetComponent<Slider>();
                if (sfxSlider != null)
                {
                    sfxSlider.minValue = 0.0001f;
                    sfxSlider.maxValue = 1f;
                    sfxSlider.value = 0.75f;
                    EditorUtility.SetDirty(sfxSlider);
                }
            }

            if (optionsUI != null)
            {
                optionsUI.bgmSlider = bgmSlider;
                optionsUI.sfxSlider = sfxSlider;
                EditorUtility.SetDirty(optionsUI);
            }

            // Also configure persistent slider listeners if preferred
            if (bgmSlider != null && optionsUI != null)
            {
                while (bgmSlider.onValueChanged.GetPersistentEventCount() > 0)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(bgmSlider.onValueChanged, 0);
                }
                UnityEditor.Events.UnityEventTools.AddPersistentListener(bgmSlider.onValueChanged, optionsUI.OnBGMChanged);
                EditorUtility.SetDirty(bgmSlider);
            }

            if (sfxSlider != null && optionsUI != null)
            {
                while (sfxSlider.onValueChanged.GetPersistentEventCount() > 0)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(sfxSlider.onValueChanged, 0);
                }
                UnityEditor.Events.UnityEventTools.AddPersistentListener(sfxSlider.onValueChanged, optionsUI.OnSFXChanged);
                EditorUtility.SetDirty(sfxSlider);
            }

            // Attach UIButtonSound to all buttons in Options
            AddButtonSoundsToSceneGameObjects();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[AudioSystemSetup] Options scene updated and saved with OptionsMenuUI and Sliders.");
        }

        private static void SetupAllScenesButtons()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/Main Menu.unity",
                "Assets/Scenes/Stage Selection.unity",
                "Assets/Scenes/Options.unity",
                "Assets/Scenes/Stage 1.unity",
                "Assets/Scenes/Stage 2.unity",
                "Assets/Scenes/Game Over.unity",
                "Assets/Scenes/Win.unity",
                "Assets/Scenes/Credits.unity",
                "Assets/Scenes/Pause.unity"
            };

            foreach (string scenePath in scenePaths)
            {
                if (!System.IO.File.Exists(scenePath)) continue;

                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int added = AddButtonSoundsToSceneGameObjects();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[AudioSystemSetup] Scene '{scene.name}' updated ({added} buttons with UIButtonSound).");
            }
        }

        private static int AddButtonSoundsToSceneGameObjects()
        {
            int addedCount = 0;
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button btn in buttons)
            {
                if (btn.GetComponent<UIButtonSound>() == null)
                {
                    btn.gameObject.AddComponent<UIButtonSound>();
                    EditorUtility.SetDirty(btn.gameObject);
                    addedCount++;
                }
            }
            return addedCount;
        }
    }
}
