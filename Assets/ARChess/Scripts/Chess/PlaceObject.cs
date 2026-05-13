using System;
using System.Linq;
using ARChess.Scripts.Project;
using ARChess.Scripts.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ARChess.Scripts.Chess
{
    
    public class PlaceObject : MonoBehaviour
    {
    
        [Header("Place Object")]
        [SerializeField]
        [Tooltip("The prefab to spawn")]
        private GameObject prefab;
        
        [Header("Project Setting")]
        [SerializeField]
        [Tooltip("Project State Options")]
        private ProjectStateOptions globalProjectStateOptions;
        
        [Header("UI Settings")]
        [SerializeField]
        [Tooltip("The gameobject to enable matchmaking")]
        private GameObject yourTurn;
        [SerializeField]
        [Tooltip("The gameobject to change player text")]
        private TextMeshProUGUI playerText;
        [SerializeField]
        [Tooltip("The gameobject to change team text")]
        private TextMeshProUGUI teamText;
        [SerializeField]
        [Tooltip("End Game Game Object")]
        private GameObject endGame;
        
        private ChessTeam startingTeam;
        private GameObject m_ObjectInstance;
        private GameObject _probeGameObject;
        private ReflectionProbe _probeComponent;
        
        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        public event Action<GameObject> objectSpawned;
        
        private bool _invoked;

        private void Update()
        {
            if (objectSpawned != null && m_ObjectInstance && !_invoked)
            {
                objectSpawned.Invoke(m_ObjectInstance);   
                _invoked = true;
            }

            // End Game
            if (!m_ObjectInstance || !_invoked) return;
            if (!m_ObjectInstance.TryGetComponent(out Chessboard chessboard)) return;
            
            switch (chessboard.EndGame)
            {
                case true:
                    endGame.SetActive(true);
                    playerText.text = chessboard.playerWins;
                    teamText.text = chessboard.teamWins;
                    break;
                case false:
                    endGame.SetActive(false);
                    break;
            }
        }
        
        private void InitializeProbe(Vector3 position)
        {
            // 1. Create a new GameObject
            _probeGameObject = new GameObject("Runtime Reflection Probe");
            _probeGameObject.transform.position = position;

            // 2. Add the ReflectionProbe component
            _probeComponent = _probeGameObject.AddComponent<ReflectionProbe>();

            // 3. Configure for Runtime
            _probeComponent.mode = ReflectionProbeMode.Realtime;
            _probeComponent.refreshMode = ReflectionProbeRefreshMode.ViaScripting; // Or .EveryFrame
            
            // Time Slicing Mode into Individual Faces
            // Behavior: Renders 1 cubemap face per frame (takes 6 frames total).
            // Best For: Balance between performance and update speed.
            // Risk: Minor visual desynchronization across faces during fast camera movements.
            _probeComponent.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        
            // Define probe resolution
            _probeComponent.resolution = 64; 

            // 4. Update the probe
            _probeComponent.RenderProbe();
        }

        public void ResetGame()
        {
            if(endGame.activeInHierarchy)
                if(m_ObjectInstance.TryGetComponent(out Chessboard chessboard))
                    chessboard.OnResetButton();
        }

        public void EndGame()
        {
            if (m_ObjectInstance && m_ObjectInstance.TryGetComponent(out Chessboard chessboard))
            {
                chessboard.EndGame = false;
                chessboard.yourTurnUI.gameObject.SetActive(false);
            }
            
            endGame.SetActive(false);
            
            Destroy(_probeGameObject);

            Resources.UnloadUnusedAssets();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public GameObject ClonePrefab(Vector3 positionPose, Vector3 spawnNormal)
        {
            startingTeam = globalProjectStateOptions.team;
            if (prefab.TryGetComponent(out Chessboard chessboard))
            {
                chessboard.startingTeam = startingTeam;
                if(yourTurn != null)
                    chessboard.yourTurnUI = yourTurn;
            }
            
            var facePosition = Camera.main!.transform.position;
            var forward = -(facePosition - positionPose);  // Have to negate forward position to let the chess piece player towards camera
            
            InitializeProbe(positionPose);
            
            // Instantiate object with prefab using same position and rotation
            m_ObjectInstance = Instantiate(prefab);
            
            Positioning(forward, positionPose, spawnNormal);

            return m_ObjectInstance;
        }

        private void Positioning(Vector3 forward, Vector3 positionPose, Vector3 spawnNormal)
        {
            m_ObjectInstance.transform.position = positionPose;
            
            BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
            m_ObjectInstance.transform.localRotation = Quaternion.LookRotation(projectedForward, spawnNormal);
            
            // Resize Chessboard
            m_ObjectInstance.transform.localScale = new Vector3(globalProjectStateOptions.initialChessboardSize, globalProjectStateOptions.initialChessboardSize, globalProjectStateOptions.initialChessboardSize);
            
            // Move probe
            if (!_probeGameObject || !_probeComponent) return;
            _probeGameObject.transform.position = positionPose;
            _probeComponent.RenderProbe();
        }

        public void Positioning(Vector3 positionPose, Vector3 spawnNormal)
        {
            // Have to negate forward position to let the chess piece player towards camera
            Positioning(-(Camera.main.transform.position - positionPose), positionPose, spawnNormal);
        }

        public void ToggleContact(bool toggle)
        {
            if (!m_ObjectInstance) return;
            m_ObjectInstance.transform.Find("Chess Attach").GetComponent<BoxCollider>().providesContacts = toggle;
        }
    }
}
