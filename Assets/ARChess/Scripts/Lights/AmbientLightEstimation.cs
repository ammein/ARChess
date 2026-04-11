using ARChess.Scripts.Project;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARChess.Scripts.Lights
{
    /// <summary>
    /// A component that can be used to access the most recently received HDR light estimation information
    /// for the physical environment as observed by an AR device.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class AmbientLightEstimation : MonoBehaviour
    {
        [Header("Lighting Settings")]
        [SerializeField]
        [Tooltip("The ARCameraManager which will produce frame events containing light estimation information.")]
        private ARCameraManager m_CameraManager;

        [SerializeField]
        Transform m_Arrow;

        [Header("Project Settings")]
        [SerializeField]
        [Tooltip("Project settings for the light estimation information.")]
        private ProjectStateOptions globalSettings;

        [Header("Transform Values of Dynamic Lighting")]
        [SerializeField] 
        [Tooltip("Position of the light if the dynamic lighting is on.")]
        private Vector3 dynamicLightPosition;
        
        [SerializeField] 
        [Tooltip("Rotation of the light if the dynamic lighting is on.")]
        private Quaternion dynamicLightRotation;

        
        [Header("Default Value (View Only)")]
        [SerializeField]
        [Tooltip("The position of the light if the dynamic lighting is off.")]
        private Vector3 defaultPosition;
        
        [SerializeField]
        [Tooltip("The rotation of the light if the dynamic lighting is off.")]
        private Vector3 defaultRotation;

        public Transform arrow
        {
            get => m_Arrow;
            set => m_Arrow = value;
        }

        public Vector3 DynamicLightPosition
        {
            get => defaultPosition;
            set => defaultPosition = value;
        }

        public Vector3 DynamicLightRotation
        {
            get => defaultRotation;
            set => defaultRotation = value;
        }

        /// <summary>
        /// Get or set the <c>ARCameraManager</c>.
        /// </summary>
        public ARCameraManager cameraManager
        {
            get => m_CameraManager;
            set
            {
                if (m_CameraManager == value)
                    return;

                if (m_CameraManager != null)
                    m_CameraManager.frameReceived -= FrameChanged;

                m_CameraManager = value;

                if (m_CameraManager != null & enabled)
                    m_CameraManager.frameReceived += FrameChanged;
            }
        }
        
        /// <summary>
        /// The estimated brightness of the physical environment, if available.
        /// </summary>
        public float? brightness { get; private set; }

        /// <summary>
        /// The estimated color temperature of the physical environment, if available.
        /// </summary>
        public float? colorTemperature { get; private set; }

        /// <summary>
        /// The estimated color correction value of the physical environment, if available.
        /// </summary>
        public Color? colorCorrection { get; private set; }

        void Awake ()
        {
            m_Light = GetComponent<Light>();
        }

        void OnEnable()
        {
            if (m_CameraManager != null)
                m_CameraManager.frameReceived += FrameChanged;

            // Disable the arrow to start; enable it later if we get directional light info
            if (arrow)
            {
                arrow.gameObject.SetActive(false);
            }
            Application.onBeforeRender += OnBeforeRender;
        }

        void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;

            if (m_CameraManager != null)
                m_CameraManager.frameReceived -= FrameChanged;
        }

        void OnBeforeRender()
        {
            if (arrow && m_CameraManager)
            {
                var cameraTransform = m_CameraManager.GetComponent<Camera>().transform;
                arrow.position = cameraTransform.position + cameraTransform.forward * .25f;

                if (globalSettings.dynamicLighting)
                {
                    cameraTransform.position = dynamicLightPosition;
                    cameraTransform.rotation = dynamicLightRotation;
                }
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if(EditorApplication.isUpdating)
                    EditorApplication.update -= UpdateTransformEditor;
                EditorApplication.update += UpdateTransformEditor;
            }
        }

        private void UpdateTransformEditor()
        {
            if (this == null) return;
            defaultPosition = transform.localPosition;
            defaultRotation = transform.localEulerAngles;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= UpdateTransformEditor;
        }
#endif

        private void Update()
        {
            // Turn on Dynamic Lighting
            if (m_CameraManager && globalSettings && globalSettings.dynamicLighting)
            {
                if (m_CameraManager.currentLightEstimation is not
                    (LightEstimation.MainLightDirection | LightEstimation.MainLightIntensity |
                     LightEstimation.AmbientSphericalHarmonics))
                {
                    m_CameraManager.requestedLightEstimation =
                        LightEstimation.AmbientIntensity | LightEstimation.AmbientColor;
                    m_CameraManager.requestedFacingDirection = CameraFacingDirection.World;
                }
            }
            // Turn off Dynamic Lighting
            else if(m_CameraManager && globalSettings && !globalSettings.dynamicLighting)
            {
                if(m_CameraManager.currentLightEstimation is not LightEstimation.None)
                    m_CameraManager.requestedLightEstimation = LightEstimation.None;
            }

            // Reset Light Intensity to 1f
            if (!globalSettings.dynamicLighting && m_Light && m_Light.intensity < 1f)
            {
                m_Light.intensity = 1f;
            }
        }

        void FrameChanged(ARCameraFrameEventArgs args)
        {
            if (args.lightEstimation.averageBrightness.HasValue)
            {
                brightness = args.lightEstimation.averageBrightness.Value;
                m_Light.intensity = brightness.Value;
            }
            else
            {
                brightness = null;
            }

            if (args.lightEstimation.averageColorTemperature.HasValue)
            {
                colorTemperature = args.lightEstimation.averageColorTemperature.Value;
                m_Light.colorTemperature = colorTemperature.Value;
            }
            else
            {
                colorTemperature = null;
            }

            if (args.lightEstimation.colorCorrection.HasValue)
            {
                colorCorrection = args.lightEstimation.colorCorrection.Value;
                m_Light.color = colorCorrection.Value;
            }
            else
            {
                colorCorrection = null;
            }
        }

        Light m_Light;
    }
}