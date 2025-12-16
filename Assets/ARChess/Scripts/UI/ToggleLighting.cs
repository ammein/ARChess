using ARChess.Scripts.Project;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

namespace ARChess.Scripts.UI
{
    public class ToggleLighting : MonoBehaviour
    {
        private Toggle _toggle;
        
        [Header("UI References")]
        public UnityEngine.UI.Image backgroundImage;
        public GameObject iconLottie;
        public GameObject icon;
        
        [Header("Background Images")]
        public Sprite lightOn;
        public Sprite lightOff;
        
        [Header("Project Settings")]
        [SerializeField]
        private ProjectStateOptions projectStateOptions;

        private Texture _jsonImage;
        private XRLoader _loader;
        private XRCameraSubsystem _cameraSubsystem;
        
        
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
            _toggle.onValueChanged.AddListener(SwitchLight);
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(SwitchLight);
        }
        
        private void EnableCameraTorch(bool enable)
        {
            _loader = LoaderUtility.GetActiveLoader();
            _cameraSubsystem = _loader?.GetLoadedSubsystem<XRCameraSubsystem>();
            if (_cameraSubsystem != null && _cameraSubsystem.DoesCurrentCameraSupportTorch())
                _cameraSubsystem.requestedCameraTorchMode = enable ? XRCameraTorchMode.On : XRCameraTorchMode.Off;
        }
        
        private void SwitchLight(bool isOn)
        {
            if (_toggle.isOn && backgroundImage.sprite != lightOn)
            {
                backgroundImage.sprite = lightOn;
                icon.SetActive(false);
                iconLottie.transform.localScale = new Vector3(2, -2, 2);
                EnableCameraTorch(isOn);
            }
            else if(!_toggle.isOn && backgroundImage.sprite != lightOff)
            {
                backgroundImage.sprite = lightOff;
                icon.SetActive(true);
                iconLottie.transform.localScale = new Vector3(0, 0, 0);
                EnableCameraTorch(isOn);
            }
        }
    }
}
