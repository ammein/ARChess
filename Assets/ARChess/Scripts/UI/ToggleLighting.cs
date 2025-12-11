using LottiePlugin.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ARChess.Scripts.UI
{
    public class ToggleLighting : MonoBehaviour
    {
        private Toggle _toggle;
        
        [Header("UI References")]
        public UnityEngine.UI.Image BackgroundImage;
        public GameObject IconLottie;
        public GameObject Icon;
        
        [Header("Background Images")]
        public Sprite LightON;
        public Sprite LightOFF;

        private Texture _jsonImage;
        private AnimatedImage _lottie;
        
        private void Awake()
        {
            if (TryGetComponent(out Toggle toggle))
            {
                if (toggle)
                {
                    _toggle = toggle;
                }
            }
            
            _lottie = IconLottie.GetComponent<AnimatedImage>();
        }

        private void Start()
        {
            _toggle.onValueChanged.AddListener(SwitchLight);
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(SwitchLight);
        }
        
        private void SwitchLight(bool isOn)
        {
            if (_toggle.isOn && BackgroundImage.sprite != LightON)
            {
                BackgroundImage.sprite = LightON;
                Icon.SetActive(false);
                IconLottie.transform.localScale = new Vector3(2, -2, 2);
            }
            else if(!_toggle.isOn && BackgroundImage.sprite != LightOFF)
            {
                BackgroundImage.sprite = LightOFF;
                Icon.SetActive(true);
                IconLottie.transform.localScale = new Vector3(0, 0, 0);
            }
        }
    }
}
