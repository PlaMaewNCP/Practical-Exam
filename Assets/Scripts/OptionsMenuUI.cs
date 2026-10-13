using UnityEngine;
using UnityEngine.UI;

namespace NatchapholAunjai
{
    public class OptionsMenuUI : MonoBehaviour
    {
        [Header("Volume Sliders")]
        public Slider bgmSlider;
        public Slider sfxSlider;

        private void Awake()
        {
            FindSlidersIfNull();
        }

        private void Start()
        {
            FindSlidersIfNull();
            InitializeSliders();
        }

        private void OnEnable()
        {
            FindSlidersIfNull();
            InitializeSliders();
        }

        private void FindSlidersIfNull()
        {
            if (bgmSlider == null)
            {
                GameObject bGo = GameObject.Find("BGMSlider");
                if (bGo != null) bgmSlider = bGo.GetComponent<Slider>();
            }

            if (sfxSlider == null)
            {
                GameObject sGo = GameObject.Find("SFXSlider");
                if (sGo != null) sfxSlider = sGo.GetComponent<Slider>();
            }
        }

        private void InitializeSliders()
        {
            if (AudioManager.Instance != null)
            {
                if (bgmSlider != null)
                {
                    bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);
                    bgmSlider.value = AudioManager.Instance.GetBGMVolume();
                    bgmSlider.onValueChanged.AddListener(OnBGMChanged);
                }

                if (sfxSlider != null)
                {
                    sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
                    sfxSlider.value = AudioManager.Instance.GetSFXVolume();
                    sfxSlider.onValueChanged.AddListener(OnSFXChanged);
                }
            }
        }

        public void OnBGMChanged(float val)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBGMVolume(val);
            }
        }

        public void OnSFXChanged(float val)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(val);
            }
        }
    }
}
