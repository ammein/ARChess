using System;
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

        [Header("Text Color")] 
        [Tooltip("Text color for active state")]
        public Color activeTextColor;
        [Tooltip("Text color for inactive state")]
        public Color inactiveTextColor;
        
        [Header("Icon")]
        [Tooltip("GameObject that has RawImage/Image Component to switch button states")]
        public GameObject Icon;
        public GameObject AnimatedIcon;

        private RawImage _iconImage;
        private Toggle _toggle;
        private GameObject _animatedIconImage;
        private AnimatedImage _animatedIcon;

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
            Icon.SetActive(!isOn);
            AnimatedIcon.SetActive(isOn);
            
            _animatedIconImage = _animatedIcon == null && _animatedIconImage == null ? AnimatedIcon : _animatedIconImage;
            _animatedIconImage.TryGetComponent(out _animatedIcon);

            // If found _animatedIcon, Run Play/Stop Immediately
            if (_animatedIcon != null)
            {
                LottiePlay(isOn);
            }
            // Else find recursive children on wherever it might have "AnimatedImage" and assign to the variable so that it would only run recursive once.
            else if (_animatedIcon == null)
            {
                FindAnimatedIconInChildren(_animatedIconImage, 0, isOn);
            }
        }

        private void FindAnimatedIconInChildren(GameObject currentGameObject, int childIndex, bool isOn)
        {
            while (!_animatedIcon)
            {
                if (currentGameObject.transform.GetChild(childIndex) == null) break;
                GameObject childGameObject = currentGameObject.transform.GetChild(childIndex).gameObject;
                if (childGameObject.TryGetComponent(out _animatedIcon))
                {
                    LottiePlay(isOn);
                    _animatedIconImage = childGameObject;
                    break;
                }
                
                // Recurse find in next children
                if(childGameObject.transform.childCount > 0) FindAnimatedIconInChildren(childGameObject , 0, isOn);

                childIndex++;
            }
        }

        private void LottiePlay(bool isOn)
        {
            if (isOn)
            {
                try
                {
                    _animatedIcon.Play();
                }
                catch (NullReferenceException e)
                {
                    if (e.InnerException != null)
                    {
                        _animatedIcon.Stop();
                        _animatedIcon.Play();
                    }
                }
            }
            else
                _animatedIcon.Stop();
        }
        
        private Sprite BackgroundIconSprite(bool isOn) => isOn ? backgroundToggleOn : backgroundToggleOff;
        private string ChangeToggleText(bool isOn) => isOn ? buttonText.text.Replace("OFF", "ON") : buttonText.text.Replace("ON", "OFF");
        private Color ChangeTextColor(bool isOn) => isOn ? activeTextColor : inactiveTextColor;
    }
}
