using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if AR_FOUNDATION_REMOTE_INSTALLED
using ARFoundationRemote.Editor;
#endif
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ARChess.Scripts.Chess
{

    [RequireComponent(typeof(PlaceObject))]
    public class ChessInteractable : MonoBehaviour
    {
        private ARPlaneManager _arPlaneManager;
        private GameObject m_ObjectInstance;

        bool m_AttemptSpawn;
        bool m_AttemptHadSelection;
        bool m_EverHadSelection;
        private bool _mAttemptEdit = false;
        private Chessboard m_Chessboard;
        
        [Header("Raycast Settings")]
        [SerializeField]
        [Tooltip("The raycast manager for Trackable detector")]
        private ARRaycastManager raycastManager;

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

        [Header("Input Actions")]
        [SerializeField] 
        [Tooltip("For Spawn Chess Input")]
        InputActionReference m_ARFoundationObjectInput;
        [SerializeField]
        [Tooltip("For Spawn Chess Input in XR Simulation")]
        InputActionReference m_SimulationObjectInput;

        /// <summary>
        /// The input used to trigger spawn, if <see cref="spawnTriggerType"/> is set to <see cref="InputAction"/>.
        /// </summary>
        public InputActionReference spawnObjectInput
        {
            #if AR_FOUNDATION_REMOTE_INSTALLED
            get => m_ARFoundationObjectInput;
            set => m_ARFoundationObjectInput = value;
            #else
            get => m_SimulationObjectInput;
            set => m_SimulationObjectInput = value;
            #endif
        }

        private PlaceObject m_PlaceObject;

        void OnEnable()
        {
            #if UNITY_EDITOR && AR_COMPANION
            m_ARFoundationObjectInput.action.performed += SpawnOrEdit;
            #elif UNITY_EDITOR
            m_SimulationObjectInput.action.performed += SpawnOrEdit;
            #else
            m_ARFoundationObjectInput.action.performed += SpawnOrEdit;
            #endif
        }

        void OnDisable()
        {
            #if UNITY_EDITOR && AR_COMPANION
            m_ARFoundationObjectInput.action.performed -= SpawnOrEdit; 
            #elif UNITY_EDITOR
            m_SimulationObjectInput.action.performed -= SpawnOrEdit;
            #else
            m_ARFoundationObjectInput.action.performed -= SpawnOrEdit; 
            #endif
        }

        void Awake()
        {
           m_PlaceObject = GetComponent<PlaceObject>();
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

        private void SpawnOrEdit(InputAction.CallbackContext obj)
        {
            if (!_mAttemptEdit)
            {
                ChessInteractive(obj);
            }
            else
            {
                ChessPlace(obj);   
            }
        }

        private void ChessInteractive(InputAction.CallbackContext obj)
        {
            // Don't spawn the object if the tap was over screen space UI.
            if (IsPointerOverUIObject(obj.ReadValue<Vector2>())) return;
            if (!m_ObjectInstance && !m_Chessboard) return;
            m_Chessboard.ChessInteract(obj.ReadValue<Vector2>());
        }

        private void ChessPlace(InputAction.CallbackContext obj)
        {
            // Don't spawn the object if the tap was over screen space UI.
            if (IsPointerOverUIObject(obj.ReadValue<Vector2>())) return;
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            // Check if raycast value hits on AR Plane
            if (raycastManager.Raycast(obj.ReadValue<Vector2>(), hits, TrackableType.PlaneWithinPolygon))
            {
                foreach (var hit in hits)
                {
                    if (hit.trackable is not ARPlane arPlane)
                        return;
                 
                    if(m_ObjectInstance)
                        m_PlaceObject.Positioning(hit.pose.position, arPlane.normal);
                    else
                    {
                        m_ObjectInstance = m_PlaceObject.ClonePrefab(hit.pose.position, arPlane.normal);
                        m_Chessboard = m_ObjectInstance.GetComponent<Chessboard>();
                    }
                }
            }
        }
        
        // Helper function to check if a pointer is over a UI element
        private bool IsPointerOverUIObject(Vector2 touchPosition)
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(touchPosition.x, touchPosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 0;
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
                Grab("Chess");
                if(m_PlaceObject)
                    m_PlaceObject.ToggleContact(true);
                if(m_Chessboard)
                    m_Chessboard.ToggleContact(false);
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
                Grab(0);
                if(m_PlaceObject)
                    m_PlaceObject.ToggleContact(false);
                if(m_Chessboard)
                    m_Chessboard.ToggleContact(true);
            }
        }
    }
}