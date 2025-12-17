using System;
using ARChess.Scripts.Project;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

namespace ARChess.Scripts.Lights
{
    /// <summary>
    /// A component that can be used to access the most recently received HDR light estimation information
    /// for the physical environment as observed by an AR device.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class HDRLightEstimation : MonoBehaviour
    {
        [Header("Lighting Settings")]
        [SerializeField]
        [Tooltip("The ARCameraManager which will produce frame events containing light estimation information.")]
        ARCameraManager m_CameraManager;

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
        
        /// <summary>
        /// The estimated direction of the main light of the physical environment, if available.
        /// </summary>
        public Vector3? mainLightDirection { get; private set; }

        /// <summary>
        /// The estimated color of the main light of the physical environment, if available.
        /// </summary>
        public Color? mainLightColor { get; private set; }

        /// <summary>
        /// The estimated intensity in lumens of main light of the physical environment, if available.
        /// </summary>
        public float? mainLightIntensityLumens { get; private set; }

        /// <summary>
        /// The estimated spherical harmonics coefficients of the physical environment, if available.
        /// </summary>
        public SphericalHarmonicsL2? sphericalHarmonics { get; private set; }

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
            if (m_CameraManager && globalSettings && globalSettings.dynamicLighting){
                if(m_CameraManager.requestedLightEstimation is not
                   (LightEstimation.AmbientSphericalHarmonics | LightEstimation.MainLightDirection | LightEstimation.MainLightIntensity))
                    m_CameraManager.requestedLightEstimation = LightEstimation.AmbientSphericalHarmonics | LightEstimation.MainLightDirection | LightEstimation.MainLightIntensity;
                
                if(m_CameraManager.requestedFacingDirection is not CameraFacingDirection.World)
                    m_CameraManager.requestedFacingDirection = CameraFacingDirection.World;
            }
            else if(m_CameraManager && globalSettings && !globalSettings.dynamicLighting)
            {
                if(m_CameraManager.requestedLightEstimation is not LightEstimation.None)
                    m_CameraManager.requestedLightEstimation = LightEstimation.None;
            }
        }

        public void ToggleLightEstimation(bool toggle)
        {
            if (toggle)
            {
                transform.position = dynamicLightPosition;
                transform.rotation = dynamicLightRotation;
            }
            else
            {
                transform.position = defaultPosition;
                transform.rotation = Quaternion.Euler(defaultRotation);
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
            
            if (args.lightEstimation.mainLightDirection.HasValue)
            {
                mainLightDirection = args.lightEstimation.mainLightDirection;
                m_Light.transform.rotation = Quaternion.LookRotation(mainLightDirection.Value);
                if (arrow)
                {
                    arrow.gameObject.SetActive(true);
                    arrow.rotation = Quaternion.LookRotation(mainLightDirection.Value);
                }
            }
            else if (arrow)
            {
                arrow.gameObject.SetActive(false);
                mainLightDirection = null;
            }

            if (args.lightEstimation.mainLightColor.HasValue)
            {
                mainLightColor = args.lightEstimation.mainLightColor;
                m_Light.color = mainLightColor.Value;
            }
            else
            {
                mainLightColor = null;
            }

            if (args.lightEstimation.mainLightIntensityLumens.HasValue)
            {
                mainLightIntensityLumens = args.lightEstimation.mainLightIntensityLumens;
                m_Light.intensity = args.lightEstimation.averageMainLightBrightness.Value;
            }
            else
            {
                mainLightIntensityLumens = null;
            }

            if (args.lightEstimation.ambientSphericalHarmonics.HasValue)
            {
                sphericalHarmonics = args.lightEstimation.ambientSphericalHarmonics;
                RenderSettings.ambientMode = AmbientMode.Skybox;
                RenderSettings.ambientProbe = sphericalHarmonics.Value;
            }
            else
            {
                sphericalHarmonics = null;
            }
        }

        Light m_Light;
    }
}