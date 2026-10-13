using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NatchapholAunjai
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer & Groups")]
        public AudioMixer audioMixer;
        public AudioMixerGroup bgmGroup;
        public AudioMixerGroup sfxGroup;

        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        [Header("Default Audio Clips")]
        public AudioClip defaultBgm;
        public AudioClip buttonClickSfx;
        public AudioClip winSfx;
        public AudioClip loseSfx;

        [Header("Item Collection Clips")]
        public AudioClip itemAddTimeSfx;
        public AudioClip itemSubTimeSfx;
        public AudioClip itemAddScoreSfx;
        public AudioClip itemSubScoreSfx;

        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
                SetupAudioSources();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            ApplySavedVolumes();

            if (defaultBgm != null && bgmSource != null && !bgmSource.isPlaying)
            {
                PlayBGM(defaultBgm);
            }

            RegisterButtonsInCurrentScenes();
        }

        private void SetupAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
            }
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            if (bgmGroup != null)
            {
                bgmSource.outputAudioMixerGroup = bgmGroup;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            if (sfxGroup != null)
            {
                sfxSource.outputAudioMixerGroup = sfxGroup;
            }
        }

        private void ApplySavedVolumes()
        {
            SetBGMVolume(GetBGMVolume());
            SetSFXVolume(GetSFXVolume());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RegisterButtonsInScene(scene);

            if (scene.name == "Win")
            {
                PlayWinSound();
            }
            else if (scene.name == "Game Over")
            {
                PlayLoseSound();
            }
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || bgmSource == null) return;

            if (bgmSource.clip == clip && bgmSource.isPlaying) return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlaySFX(AudioClip clip, float volumeScale)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, volumeScale);
        }

        public void PlayButtonClick()
        {
            if (buttonClickSfx != null)
            {
                PlaySFX(buttonClickSfx);
            }
        }

        public void PlayWinSound()
        {
            if (winSfx != null)
            {
                PlaySFX(winSfx);
            }
        }

        public void PlayLoseSound()
        {
            if (loseSfx != null)
            {
                PlaySFX(loseSfx);
            }
        }

        public void PlayItemSound(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.AddTime:
                    if (itemAddTimeSfx != null) PlaySFX(itemAddTimeSfx);
                    break;
                case ItemType.SubtractTime:
                    if (itemSubTimeSfx != null) PlaySFX(itemSubTimeSfx);
                    break;
                case ItemType.AddScore:
                    if (itemAddScoreSfx != null) PlaySFX(itemAddScoreSfx);
                    break;
                case ItemType.SubtractScore:
                    if (itemSubScoreSfx != null) PlaySFX(itemSubScoreSfx);
                    break;
                default:
                    if (itemAddScoreSfx != null) PlaySFX(itemAddScoreSfx);
                    break;
            }
        }

        public void SetBGMVolume(float sliderValue)
        {
            float clamped = Mathf.Clamp(sliderValue, 0.0001f, 1f);
            float dB = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(clamped) * 20f;
            if (audioMixer != null)
            {
                audioMixer.SetFloat("BGMVolume", dB);
            }
            PlayerPrefs.SetFloat(BGM_VOLUME_KEY, sliderValue);
        }

        public void SetSFXVolume(float sliderValue)
        {
            float clamped = Mathf.Clamp(sliderValue, 0.0001f, 1f);
            float dB = (sliderValue <= 0.0001f) ? -80f : Mathf.Log10(clamped) * 20f;
            if (audioMixer != null)
            {
                audioMixer.SetFloat("SFXVolume", dB);
            }
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sliderValue);
        }

        public float GetBGMVolume()
        {
            return PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.75f);
        }

        public float GetSFXVolume()
        {
            return PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.75f);
        }

        public void RegisterButtonsInCurrentScenes()
        {
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                RegisterButtonsInScene(SceneManager.GetSceneAt(i));
            }
        }

        public void RegisterButtonsInScene(Scene scene)
        {
            if (!scene.isLoaded) return;

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                Button[] buttons = root.GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    if (btn.GetComponent<UIButtonSound>() == null)
                    {
                        btn.gameObject.AddComponent<UIButtonSound>();
                    }
                }
            }
        }
    }
}
