using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace ARChess.Scripts.Lights
{
    public class LightEstimationPermission : MonoBehaviour
    {
        #if UNITY_ANDROID
        const string k_Permission = Permission.Camera;
        #endif

        [SerializeField]
        ARCameraManager m_ARCameraManager;

        [Header("Custom Permission UI")]
        [Tooltip("Assign your custom in-game UI panel explaining why AR needs this permission.")]
        [SerializeField]
        GameObject m_CustomDenialDialogPanel;

        private bool isCheckingPermission = false;

#if UNITY_ANDROID
        void Awake()
        {
            // Force-disable the AR Camera Manager at launch to prevent 
            // the subsystem from starting before permission is verified.
            if (m_ARCameraManager != null)
            {
                m_ARCameraManager.enabled = false;
            }

            // Ensure the custom explanation dialog is hidden at the very start
            if (m_CustomDenialDialogPanel != null)
            {
                m_CustomDenialDialogPanel.SetActive(false);
            }
        }
        
        void Start()
        {
            TryRequestPermission();
        }

        public void TryRequestPermission()
        {
            if (!Permission.HasUserAuthorizedPermission(k_Permission))
            {
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += OnPermissionDenied;
                callbacks.PermissionGranted += OnPermissionGranted;

                Permission.RequestUserPermission(k_Permission, callbacks);
                
                // Fallback check in case the user previously tapped "Don't ask again"
                StartCoroutine(WaitForPermissionResult());
            }
            else
            {
                EnableARCamera();
            }
        }

        void OnPermissionDenied(string permission)
        {
            ShowCustomDenialDialog();
        }

        void OnPermissionGranted(string permission)
        {
            EnableARCamera();
        }

        private IEnumerator WaitForPermissionResult()
        {
            if (isCheckingPermission) yield break;
            isCheckingPermission = true;

            // Give the OS dialog a brief moment to process, then wait until the game is back in focus
            yield return new WaitForSeconds(0.5f);
            yield return new WaitUntil(() => Application.isFocused);

            if (Permission.HasUserAuthorizedPermission(k_Permission))
            {
                EnableARCamera();
            }
            else
            {
                // Triggered if "Don't Ask Again" prevented the OS pop-up from appearing
                ShowCustomDenialDialog();
            }

            isCheckingPermission = false;
        }

        private void ShowCustomDenialDialog()
        {
            if (m_CustomDenialDialogPanel != null)
            {
                m_CustomDenialDialogPanel.SetActive(true);
            }
        }

        private void EnableARCamera()
        {
            if (m_CustomDenialDialogPanel != null)
            {
                m_CustomDenialDialogPanel.SetActive(false);
            }

            if (m_ARCameraManager != null)
            {
                m_ARCameraManager.enabled = true;
                if (m_ARCameraManager.subsystem != null)
                {
                    m_ARCameraManager.subsystem.Stop();
                    m_ARCameraManager.subsystem.Start();
                }
            }
        }

        /// <summary>
        /// LINK THIS FUNCTION TO YOUR IN-GAME "YES" / "RETRY" BUTTON ON THE DIALOG PANEL
        /// </summary>
        public void OnUserClickedYesToRetry()
        {
            if (m_CustomDenialDialogPanel != null)
            {
                m_CustomDenialDialogPanel.SetActive(false);
            }

            OpenAndroidAppSettings();
        }

        private void OpenAndroidAppSettings()
        {
            #if !UNITY_EDITOR
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent"))
                    {
                        intent.Call<AndroidJavaObject>("setAction", "android.settings.APPLICATION_DETAILS_SETTINGS");
                        
                        using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
                        {
                            string packageString = "package:" + Application.identifier;
                            using (AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", packageString))
                            {
                                intent.Call<AndroidJavaObject>("setData", uri);
                                currentActivity.Call("startActivity", intent);
                            }
                        }
                    }
                }
            }
            #endif
        }

        // Re-check automatically when the user returns to the game from Android Settings
        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                if (Permission.HasUserAuthorizedPermission(k_Permission))
                {
                    EnableARCamera();
                }
                else
                {
                    // If they return and still haven't allowed it, keep the warning prompt up
                    ShowCustomDenialDialog();
                }
            }
        }
#endif // UNITY_ANDROID
    }
}
