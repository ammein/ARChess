using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARChess.Scripts.UI
{
    public class FlashLighting : MonoBehaviour
    {
        public void EnableCameraTorch(bool enable)
        {
            var loader = LoaderUtility.GetActiveLoader();
            var cameraSubsystem = loader != null ? loader.GetLoadedSubsystem<XRCameraSubsystem>() : null;
            if (cameraSubsystem == null || !cameraSubsystem.DoesCurrentCameraSupportTorch())
            {
                Debug.Log("Torchlight not supported!");
                return;
            }

            cameraSubsystem.requestedCameraTorchMode = enable ? XRCameraTorchMode.On : XRCameraTorchMode.Off;
        }

    }
}
