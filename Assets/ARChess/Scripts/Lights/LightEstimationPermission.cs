using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARChess.Scripts.Lights
{
    public class LightEstimationPermission : MonoBehaviour
    {
        const string k_Permission = "android.permission.SCENE_UNDERSTANDING_COARSE";

        [SerializeField]
        ARCameraManager m_ARCameraManager;

#if UNITY_ANDROID
        void Start()
        {
            if (!Permission.HasUserAuthorizedPermission(k_Permission))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += OnPermissionDenied;
                callbacks.PermissionGranted += OnPermissionGranted;

                Permission.RequestUserPermission(k_Permission, callbacks);
            }
            else
            {
                // enable the AR Camera Manager component if permission is already granted
                m_ARCameraManager.enabled = true;
            }
        }

        void OnPermissionDenied(string permission)
        {
            // handle denied permission
        }

        void OnPermissionGranted(string permission)
        {
            // enable the AR Camera Manager component if permission is already granted
            m_ARCameraManager.enabled = true;
            m_ARCameraManager.subsystem.Stop();
            m_ARCameraManager.subsystem.Start();
        }
#endif // UNITY_ANDROID

    }
}
