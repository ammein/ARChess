using ARChess.Scripts.Project;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

namespace ARChess.Scripts.UI
{
    public class ToggleLighting : MonoBehaviour
    {
        private Toggle _toggle;
        
        [Header("UI References")]
        public GameObject iconLottie;
        public GameObject icon;
        
        [Header("Background Images")]
        public Sprite lightOn;
        public Sprite lightOff;
        
        [Header("Project Settings")]
        [SerializeField]
        private ProjectStateOptions projectStateOptions;

        private ARCameraManager _cameraManager;
        private UnityEngine.UI.Image _backgroundImage;
        
        private void Awake()
        {
            if (TryGetComponent(out Toggle toggle))
            {
                _toggle = toggle;
            }
            
            _backgroundImage = GetComponent<UnityEngine.UI.Image>();
        }

        private void Start()
        {
            _cameraManager = Object.FindObjectOfType<ARCameraManager>();
            UpdateUI(_toggle.isOn);
            _toggle.onValueChanged.AddListener(SwitchLight);
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(SwitchLight);
        }
        
        private void SwitchLight(bool isOn)
        {
            UpdateUI(isOn);

            if (_cameraManager != null && _cameraManager.subsystem != null)
            {
                if (_cameraManager.subsystem.DoesCurrentCameraSupportTorch())
                {
                    _cameraManager.subsystem.requestedCameraTorchMode = isOn ? XRCameraTorchMode.On : XRCameraTorchMode.Off;
                }
                else
                {
                    Debug.LogWarning("Torch is not supported by the current AR Configuration.");
                }
            }
        }

        private void UpdateUI(bool isOn)
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = isOn ? lightOn : lightOff;
            }
            if (icon != null) icon.SetActive(!isOn);
            if (iconLottie != null) iconLottie.SetActive(isOn);
        }
    }
}