using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

namespace ARChess.Scripts.UI
{
    public class FlashLighting : MonoBehaviour
    {
        [Header("Flash Lighting Settings")]
        [SerializeField]
        [Tooltip("Toggle Button for torchlight/flashlight")]
        private GameObject toggleButton;
        
        private XRLoader _loader;
        private XRCameraSubsystem _cameraSubsystem;
        private bool _supportTorch = true;

        private void Awake()
        {
            _loader = LoaderUtility.GetActiveLoader();
            _cameraSubsystem = _loader != null ? _loader.GetLoadedSubsystem<XRCameraSubsystem>() : null;
            if (_cameraSubsystem == null || !_cameraSubsystem.DoesCurrentCameraSupportTorch())
            {
                Debug.Log("Torchlight not supported!");
                _supportTorch = false;
                DisableParentButton();
            }
        }
        
        public void EnableCameraTorch(bool enable)
        {
            if (!_supportTorch || _cameraSubsystem == null) return;
            _cameraSubsystem.requestedCameraTorchMode = enable ? XRCameraTorchMode.On : XRCameraTorchMode.Off;
        }

        private void DisableParentButton()
        {
            toggleButton.SetActive(false);
        }

    }
}
