using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ARChess.Scripts.Chess
{
    public class ChessInteractable : MonoBehaviour
    {
        /// <summary>
        /// The type of trigger to use to spawn an object.
        /// </summary>
        public enum SpawnTriggerType
        {
            /// <summary>
            /// Spawn an object when the interactor activates its select input
            /// but no selection actually occurs.
            /// </summary>
            SelectAttempt,

            /// <summary>
            /// Spawn an object when an input is performed.
            /// </summary>
            InputAction,
        }
        
        private ARPlaneManager _arPlaneManager;
        private GameObject m_ObjectInstance;

        bool m_AttemptSpawn;
        bool m_AttemptHadSelection;
        bool m_EverHadSelection;
        private bool _mAttemptEdit = false;

        [SerializeField] [Tooltip("The AR ray interactor that determines where to spawn the object.")]
        XRRayInteractor m_ARInteractor;

        /// <summary>
        /// The AR ray interactor that determines where to spawn the object.
        /// </summary>
        public XRRayInteractor arInteractor
        {
            get => m_ARInteractor;
            set => m_ARInteractor = value;
        }

        [SerializeField]
        [Tooltip(
            "The type of trigger to use to spawn an object, either when the Interactor's select action occurs or " +
            "when a button input is performed.")]
        SpawnTriggerType m_SpawnTriggerType;

        /// <summary>
        /// The type of trigger to use to spawn an object.
        /// </summary>
        public SpawnTriggerType spawnTriggerType
        {
            get => m_SpawnTriggerType;
            set => m_SpawnTriggerType = value;
        }

        [SerializeField] [Tooltip("When enabled, spawn will not be triggered if an object is currently selected.")]
        bool m_BlockSpawnWhenInteractorHasSelection = true;

        /// <summary>
        /// When enabled, spawn will not be triggered if an object is currently selected.
        /// </summary>
        public bool blockSpawnWhenInteractorHasSelection
        {
            get => m_BlockSpawnWhenInteractorHasSelection;
            set => m_BlockSpawnWhenInteractorHasSelection = value;
        }

        [SerializeField] XRInputButtonReader m_SpawnObjectInput;

        /// <summary>
        /// The input used to trigger spawn, if <see cref="spawnTriggerType"/> is set to <see cref="SpawnTriggerType.InputAction"/>.
        /// </summary>
        public XRInputButtonReader spawnObjectInput
        {
            get => m_SpawnObjectInput;
            set => XRInputReaderUtility.SetInputProperty(ref m_SpawnObjectInput, value, this);
        }

        [SerializeField] PlaceObject m_PlaceObject;

        void OnEnable()
        {
            m_SpawnObjectInput.EnableDirectActionIfModeUsed();
        }

        void OnDisable()
        {
            m_SpawnObjectInput.DisableDirectActionIfModeUsed();
        }

        private void Start()
        {
            if (_arPlaneManager == null)
            {
                _arPlaneManager = FindFirstObjectByType<ARPlaneManager>();
                EnableOrDisableInteractive(false);
            }
            
            if (m_ARInteractor == null)
            {
                Debug.LogError("Missing AR Interactor reference, disabling component.", this);
                enabled = false;
            }
        }


        private void Update()
        {

            // If attempt edit is false, don't spawn anything!
            if (!_mAttemptEdit)
            {
                Grab(0);
                return;
            }
            // Wait a frame after the Spawn Object input is triggered to actually cast against AR planes and spawn
            // in order to ensure the touchscreen gestures have finished processing to allow the ray pose driver
            // to update the pose based on the touch position of the gestures.
            if (m_AttemptSpawn)
            {
                m_AttemptSpawn = false;

                // Cancel the spawn if the select was delayed until the frame after the spawn trigger.
                // This can happen if the select action uses a different input source than the spawn trigger.
                if (m_ARInteractor.hasSelection)
                    return;

                SpawnOrEdit();

                return;
            }

            if (m_AttemptHadSelection)
            {
                SpawnOrEdit();
            }

            var selectState = m_ARInteractor.logicalSelectState;

            if (m_BlockSpawnWhenInteractorHasSelection)
            {
                if (selectState.wasPerformedThisFrame)
                    m_EverHadSelection = m_ARInteractor.hasSelection;
                else if (selectState.active)
                    m_EverHadSelection |= m_ARInteractor.hasSelection;
            }

            m_AttemptSpawn = false;
            switch (m_SpawnTriggerType)
            {
                case SpawnTriggerType.SelectAttempt:
                    if (selectState.wasCompletedThisFrame)
                        m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
                    break;

                case SpawnTriggerType.InputAction:
                    if (m_SpawnObjectInput.ReadWasPerformedThisFrame())
                        m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
                    break;
            }

            if (!m_AttemptSpawn && m_ARInteractor.hasSelection) m_AttemptHadSelection = true;
            else m_AttemptHadSelection = false;
        }

        private void SpawnOrEdit()
        {
            // Don't spawn the object if the tap was over screen space UI.
            var isPointerOverUI = EventSystem.current && EventSystem.current.IsPointerOverGameObject(-1);
            if (!isPointerOverUI && _mAttemptEdit && m_ARInteractor.TryGetCurrentARRaycastHit(out var raycastHit))
            {
                if (raycastHit.trackable is not ARPlane arPlane)
                    return;

                m_ObjectInstance = m_PlaceObject.ClonePrefab(raycastHit.pose.position, arPlane.normal);
            }

            if (_mAttemptEdit)
            {
                Grab("Chess");
            }
        }

        private void Grab(params string[] layerMask)
        {
            if (!m_ObjectInstance) return;
            m_ObjectInstance.TryGetComponent(out XRGrabInteractable interactable);
            // Set Interaction Layer Mask to 'layerMask' so that it enable the grab interaction
            if (interactable && interactable.interactionLayers != LayerMask.GetMask(layerMask)) interactable.interactionLayers = LayerMask.GetMask(layerMask);
        }

        private void Grab(int layerMask)
        {
            if (!m_ObjectInstance) return;
            m_ObjectInstance.TryGetComponent(out XRGrabInteractable interactable);
            // Set Interaction Layer Mask to nothing so that it disable the grab interaction
            if (interactable && interactable.interactionLayers != layerMask) interactable.interactionLayers = layerMask;
        }

        public void EnableOrDisableInteractive(bool state)
        {
            if (_arPlaneManager == null) return;
            if (state)
            {
                // Enable AR Plane Manager before set all trackable planes active
                _arPlaneManager.enabled = true;
                foreach (var plane in _arPlaneManager.trackables)
                {
                    plane.gameObject.SetActive(true); // Show the GameObject of the plane
                }
                _mAttemptEdit = true;
            }
            else
            {
                foreach (var plane in _arPlaneManager.trackables)
                {
                    plane.gameObject.SetActive(false); // Hide the GameObject of the plane
                }
                    
                // Disable AR Plane Manager after set all trackable inactive
                _arPlaneManager.enabled = false;
                _mAttemptEdit = false;
            }
        }
    }
}