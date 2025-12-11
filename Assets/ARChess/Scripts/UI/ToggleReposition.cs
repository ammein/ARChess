using LottiePlugin.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ARChess.Scripts.UI
{
    public class ToggleReposition : MonoBehaviour
    {
        [Header("UI Images")]
        [Tooltip("Background that Toggle On State")]
        public Sprite backgroundToggleOn;
        [Tooltip("Background that Toggle Off State")]
        public Sprite backgroundToggleOff;
        
        [Header("UI References")]
        [Tooltip("GameObject that has Image Component to switch background states")]
        public UnityEngine.UI.Image backgroundImage;
        [Tooltip("GameObject that has TextMeshPro Component to switch string states")]
        public Text buttonText;
        [Tooltip("GameObject that has AnimatedImage (Lottie) Component to switch icon states")]
        public GameObject lottieIcon;
        [Tooltip("GameObject that has RawImage/Image Component to switch button states")]
        public GameObject Icon;

        [Header("Text Color")] 
        [Tooltip("Text color for active state")]
        public Color activeTextColor;
        [Tooltip("Text color for inactive state")]
        public Color inactiveTextColor;

        private RawImage _iconImage;
        private Toggle _toggle;

        private void Awake()
        {
            if (TryGetComponent(out Toggle toggle))
            {
                if (toggle)
                {
                    _toggle = toggle;
                }
            }
        }

        private void Start()
        {
            if (_toggle)
            {
                backgroundImage.sprite = BackgroundIconSprite(_toggle.isOn);
                buttonText.text = ChangeToggleText(_toggle.isOn);
                buttonText.color = ChangeTextColor(_toggle.isOn);
                _toggle.onValueChanged.AddListener(SwitchIcon);
            }
            else Debug.LogError("No Toggle component found on " + gameObject.name);
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(SwitchIcon);
        }

        private void SwitchIcon(bool isOn)
        {
            ScanIconSprite(isOn);
            backgroundImage.sprite = BackgroundIconSprite(isOn);
            buttonText.text = ChangeToggleText(isOn);
            buttonText.color = ChangeTextColor(isOn);
        }
        
        private void ScanIconSprite(bool isOn) {
            if (isOn)
            {
                lottieIcon.SetActive(true);
                Icon.SetActive(false);
                lottieIcon.transform.localScale = new Vector3(2f, -2f, 2f);
            }
            else
            {
                lottieIcon.transform.localScale = new Vector3(0, 0, 0);
                lottieIcon.SetActive(false);
                Icon.SetActive(true);
            }
        }
        private Sprite BackgroundIconSprite(bool isOn) => isOn ? backgroundToggleOn : backgroundToggleOff;
        private string ChangeToggleText(bool isOn) => isOn ? buttonText.text.Replace("OFF", "ON") : buttonText.text.Replace("ON", "OFF");
        private Color ChangeTextColor(bool isOn) => isOn ? activeTextColor : inactiveTextColor;
    }
}
