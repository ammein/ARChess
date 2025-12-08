using UnityEngine;
using UnityEngine.UI;

namespace ARChess.Scripts
{
    public class ToggleReposition : MonoBehaviour
    {
        [Header("UI Images")]
        [Tooltip("Icon that Toggle On State")]
        public Sprite scanIconOn;
        [Tooltip("Icon that Toggle Off State")]
        public Sprite scanIconOff;
        [Tooltip("Background that Toggle On State")]
        public Sprite backgroundToggleOn;
        [Tooltip("Background that Toggle Off State")]
        public Sprite backgroundToggleOff;
        
        [Header("UI References")]
        [Tooltip("GameObject that has Image Component to switch icon states")]
        public Image iconImage;
        [Tooltip("GameObject that has Image Component to switch background states")]
        public Image backgroundImage;
        [Tooltip("GameObject that has TextMeshPro Component to switch string states")]
        public Text buttonText;

        private void Start()
        {
            if (TryGetComponent(out Toggle toggle))
            {
                iconImage.sprite = ScanIconSprite(toggle.isOn);
                backgroundImage.sprite = BackgroundIconSprite(toggle.isOn);
                buttonText.text = ChangeToggleText(toggle.isOn);
                toggle.onValueChanged.AddListener(SwitchIcon);
            }
            else Debug.LogError("No Toggle component found on " + gameObject.name);
        }

        private void SwitchIcon(bool isOn)
        {
            iconImage.sprite = ScanIconSprite(isOn);
            backgroundImage.sprite = BackgroundIconSprite(isOn);
            buttonText.text = ChangeToggleText(isOn);
        }
        
        private Sprite ScanIconSprite(bool isOn) => isOn ? scanIconOn : scanIconOff;
        private Sprite BackgroundIconSprite(bool isOn) => isOn ? backgroundToggleOn : backgroundToggleOff;
        private string ChangeToggleText(bool isOn) => isOn ? buttonText.text.Replace("OFF", "ON") : buttonText.text.Replace("ON", "OFF");
    }
}
