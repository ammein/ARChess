using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARChess.Scripts.UI
{
    public class ToggleCheckbox : MonoBehaviour
    {
        [Header("Checkbox Art Stuff")]
        [Tooltip("ON State")]
        public Sprite backgroundOn;
        [Tooltip("OFF State")]
        public Sprite backgroundOff;
        [Tooltip("Icon Raw Image")] 
        [CanBeNull] 
        public RawImage iconRawImage;
        [Tooltip("Icon Image")] 
        [CanBeNull] 
        public UnityEngine.UI.Image iconImage;
        [Tooltip("Icon Active Color")]
        public Color iconActiveColor;
        [Tooltip("Icon Inactive Color")]
        public Color iconInactiveColor;
        [Tooltip("Text")]
        [CanBeNull]
        public Text text;
        [Tooltip("TextMeshPro")]
        [CanBeNull]
        public TextMeshPro textMeshPro;
        [Tooltip("TextMeshProUGUI")]
        [CanBeNull]
        public TextMeshProUGUI textMeshProUGUI;
        [Tooltip("Text Active Color")]
        public Color textActiveColor;
        [Tooltip("Text Inactive Color")]
        public Color textInactiveColor;
        
        private Toggle _toggle;
        private UnityEngine.UI.Image _background;

        void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _background = _toggle.targetGraphic.gameObject.GetComponent<UnityEngine.UI.Image>();
            if (!_toggle)
            {
                Debug.LogError($"Toggle {name} has no Toggle component!");
            }
        }

        void Start()
        {
            _toggle.onValueChanged.AddListener(ToggleHandler);
        }

        void Update()
        {
            // Fix when programatically handled that detect either toggle is on or off
            if (_toggle.isOn)
            {
                if(_background.sprite != backgroundOn)
                    _background.sprite = backgroundOn;
                
                if(iconRawImage && iconRawImage.color != iconActiveColor)
                    iconRawImage.color = iconActiveColor;
                
                if(iconImage && iconImage.color != iconActiveColor)
                    iconImage.color = iconActiveColor;
                
                if(text && text.color != textActiveColor)
                    text.color = textActiveColor;
                
                if(textMeshPro && textMeshPro.color != textActiveColor)
                    textMeshPro.color = textActiveColor;
                
                if(textMeshProUGUI && textMeshProUGUI.color != textActiveColor)
                    textMeshProUGUI.color = textActiveColor;
            } else if (!_toggle.isOn)
            {
                if(_background.sprite != backgroundOff)
                    _background.sprite = backgroundOff;
                
                if(iconRawImage && iconRawImage.color != iconInactiveColor)
                    iconRawImage.color = iconInactiveColor;
                
                if(iconImage && iconImage.color != iconInactiveColor)
                    iconImage.color = iconInactiveColor;
                
                if(text && text.color != textInactiveColor)
                    text.color = textInactiveColor;
                
                if(textMeshPro && textMeshPro.color != textInactiveColor)
                    textMeshPro.color = textInactiveColor;
                
                if(textMeshProUGUI && textMeshProUGUI.color != textInactiveColor)
                    textMeshProUGUI.color = textInactiveColor;
            }
        }

        void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(ToggleHandler);
        }

        private void ToggleHandler(bool isOn)
        {
            _background.sprite = isOn ? backgroundOn : backgroundOff;
        }
    }
}
