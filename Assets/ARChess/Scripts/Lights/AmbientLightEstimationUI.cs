using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace ARChess.Scripts.Lights
{
    /// <summary>
    /// A simple UI controller to display HDR light estimation information.
    /// </summary>
    [RequireComponent(typeof(AmbientLightEstimation))]
    public class HDRLightEstimationUI : MonoBehaviour
    {
        [Tooltip("The UI Text element used to display the estimated ambient intensity in the physical environment.")]
        [SerializeField]
        Text m_AmbientIntensityText;

        /// <summary>
        /// The UI Text element used to display the estimated ambient intensity value.
        /// </summary>
        public Text ambientIntensityText
        {
            get => m_AmbientIntensityText;
            set => m_AmbientIntensityText = ambientIntensityText;
        }

        [Tooltip("The UI Text element used to display the estimated ambient color in the physical environment.")]
        [SerializeField]
        Text m_AmbientColorText;

        /// <summary>
        /// The UI Text element used to display the estimated ambient color in the scene.
        /// </summary>
        public Text ambientColorText
        {
            get => m_AmbientColorText;
            set => m_AmbientColorText = value;
        }
        
        [Tooltip("The UI Text element used to display the estimated facing direction of the AR Camera")]
        [SerializeField]
        [CanBeNull]
        Text m_facingDirectionText;

        public Text facingDirectionText
        {
            get => m_facingDirectionText;
            set => m_facingDirectionText = value;
        }
        
        [Tooltip("The UI Text element used to display the estimation light mode")]
        [SerializeField]
        [CanBeNull]
        Text m_LightModeText;

        public Text lightModeText
        {
            get => m_LightModeText;
            set => m_LightModeText = value;
        }

        void Awake()
        {
            m_HDRLightEstimation = GetComponent<AmbientLightEstimation>();
        }

        void Update()
        {
            //Display basic light estimation info
            SetUIValue(m_HDRLightEstimation.brightness, ambientIntensityText);
            //Display color temperature or color correction if supported
            if (m_HDRLightEstimation.colorTemperature != null)
                SetUIValue(m_HDRLightEstimation.colorTemperature, ambientColorText);
            else if (m_HDRLightEstimation.colorCorrection != null)
                SetUIValue(m_HDRLightEstimation.colorCorrection, ambientColorText);
            else
                SetUIValue<float>(null, ambientColorText);
            
            if(facingDirectionText)
                SetCameraValue(m_HDRLightEstimation.cameraManager.currentFacingDirection, facingDirectionText);
            
            if(lightModeText)
                SetLightModeValue(m_HDRLightEstimation.cameraManager.currentLightEstimation, lightModeText);
        }

        void SetUIValue<T>(T? displayValue, Text text) where T : struct
        {
            if (text != null)
                text.text = displayValue.HasValue ? displayValue.Value.ToString(): k_UnavailableText;
        }

        void SetCameraValue(CameraFacingDirection displayValue, Text text)
        {
            text.text = displayValue.ToString();
        }

        void SetLightModeValue(LightEstimation lightEstimation, Text text)
        {
            text.text = lightEstimation.ToString();
        }

        const string k_UnavailableText = "Unavailable";

        AmbientLightEstimation m_HDRLightEstimation;
    }
}