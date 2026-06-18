using System;
using System.Collections;
using System.Collections.Generic;
using ARChess.Scripts.Net;
using ARChess.Scripts.Net.Net_Message;
using ARChess.Scripts.Project;
using ARChess.Scripts.Utility;
using TMPro;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Rendering;
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
        [SerializeField]
        [Tooltip("Placeboard UI Container")]
        private GameObject placeboardTextContainer;
        [SerializeField]
        [Tooltip("Overlay Placeboard")]
        private GameObject overlayPlaceboard;
        [SerializeField]
        [Tooltip("Placeboard Text")]
        private TMP_Text placeBoardText;
        
        private ChessTeam startingTeam;
        private GameObject m_ObjectInstance;
        private GameObject _probeGameObject;
        private ReflectionProbe _probeComponent;
        private Chessboard _chessboard;
        private List<int> _teamPlaceFirst = new List<int>();
        private bool startOnlinePlay;
        
        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        public event Action<GameObject> objectSpawned;
        
        private bool _invoked;

        private void Awake()
        {
            RegisterEvents();
        }

        private void Update()
        {
            if (objectSpawned != null && m_ObjectInstance && !_invoked)
            {
                objectSpawned.Invoke(m_ObjectInstance);   
                _invoked = true;
            }
            
            // Place Board Online Play
            if(globalProjectStateOptions.onlinePlay)
                OnlineBoardPlacement();

            // End Game
            if (!m_ObjectInstance || !_invoked) return;
            if (!_chessboard)
            {
                m_ObjectInstance.TryGetComponent(out _chessboard);
            }
            
            // Check if the chess is ended
            switch (_chessboard.EndGame)
            {
                case true:
                    if(yourTurn.activeInHierarchy)
                        yourTurn.SetActive(false);
                    
                    if (!endGame.activeInHierarchy)
                    {
                        endGame.SetActive(true);
                        playerText.text = _chessboard.playerWins;
                        teamText.text = _chessboard.teamWins;
                    }
                    break;
                case false:
                    if(endGame.activeInHierarchy)
                        endGame.SetActive(false);
                    
                    // Reset your turn gameObject
                    if(!yourTurn.activeInHierarchy && _chessboard.MyTurn)
                        yourTurn.SetActive(true);
                    break;
            }
        }

        private void OnDestroy()
        {
            UnregisterEvents();
            startOnlinePlay = false;
        }

        private void OnlineBoardPlacement()
        {
            if (!startOnlinePlay)
            {
                // Determine who the opponent is based on your local starting team
                int opponentTeamId = (startingTeam == ChessTeam.White) ? 1 : 0;
                
                bool iHavePlaced = (m_ObjectInstance);
                // Check if the list specifically contains the opponent's team ID
                bool opponentHasPlaced = _teamPlaceFirst.Contains(opponentTeamId);

                // --- THE SPECIFIC CONDITION ---
                // Trigger ONLY if the opponent has placed their board, but you haven't yet
                if (opponentHasPlaced && !iHavePlaced)
                {
                    if (!placeboardTextContainer.activeInHierarchy)
                    {
                        placeboardTextContainer.SetActive(true);
                        overlayPlaceboard.SetActive(true);
                        
                        var color = overlayPlaceboard.GetComponent<UnityEngine.UI.Image>().color;
                        color.a = 0.0f; // Clear background so they can still scan the AR environment
                        overlayPlaceboard.GetComponent<UnityEngine.UI.Image>().color = color;
                        
                        ChildrenLayerMask.All(placeboardTextContainer, "Ignore Raycast");
                        overlayPlaceboard.layer = LayerMask.NameToLayer("Ignore Raycast");
                        placeBoardText.text = "Your opponent is waiting for you to place your board to start the game";   
                    }
                }
                // Scenario B: You placed yours, but the opponent hasn't (Show dim overlay & wait text)
                else if (iHavePlaced && !opponentHasPlaced)
                {
                    if (!placeboardTextContainer.activeInHierarchy || overlayPlaceboard.layer == LayerMask.NameToLayer("Ignore Raycast"))
                    {
                        placeboardTextContainer.SetActive(true);
                        overlayPlaceboard.SetActive(true);
                        
                        var color = overlayPlaceboard.GetComponent<UnityEngine.UI.Image>().color;
                        color.a = 0.5f; // Dim the screen since they can't do anything until opponent joins
                        overlayPlaceboard.GetComponent<UnityEngine.UI.Image>().color = color;   
                        
                        ChildrenLayerMask.All(placeboardTextContainer, "UI");
                        overlayPlaceboard.layer = LayerMask.NameToLayer("UI");
                        placeBoardText.text = "Waiting for the opponent to place their board";   
                    }
                }
                // Scenario C: You haven't placed, and opponent hasn't placed (Do absolutely nothing / Keep UI hidden)
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                else if (!iHavePlaced && !opponentHasPlaced)
                {
                    if (placeboardTextContainer.activeInHierarchy) placeboardTextContainer.SetActive(false);
                    if (overlayPlaceboard.activeInHierarchy) overlayPlaceboard.SetActive(false);
                }
            } 
            else // startOnlinePlay is true (Both have placed)
            {
                // Clean up and turn off all placement overlays completely
                if (placeboardTextContainer.activeInHierarchy) placeboardTextContainer.SetActive(false);
                if (overlayPlaceboard.activeInHierarchy) overlayPlaceboard.SetActive(false);
                placeBoardText.text = "";
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
            
            startOnlinePlay = false;
            
            Destroy(_probeGameObject);
        }
        
        private IEnumerator TrySendBoardPlacement(ChessTeam team)
        {
            bool sentSuccessfully = false;
            
            // Loop runs until the message successfully enters the network pipeline
            while (!sentSuccessfully)
            {
                // Only attempt if the client actually exists
                if (Client.Instance)
                {
                    // Because SendToServer now returns a bool, this will become true 
                    // the moment the network is ready and accepts the message.
                    NetPlaceBoard npb = new NetPlaceBoard();

                    npb.Team = (int)team;
                    
                    sentSuccessfully = Client.Instance.SendToServer(npb);
                }
                
                // If it failed (network not ready yet), wait 0.5 seconds before trying again
                if (!sentSuccessfully)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
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
            
            // Send to server to acknowledge NetPlaceBoard
            // --- THE NEW FIX ---
            // Start the reliable retry loop instead of a one-off send
            if (globalProjectStateOptions.onlinePlay)
            {
                StartCoroutine(TrySendBoardPlacement(startingTeam));
            }
            // -------------------

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

        #region Network Event Listeners

        public void RegisterEvents()
        {
            // Listen to the NetworkManager instead of raw NetUtility
            NetworkManager.onPlaceBoardClientEvent += OnNetworkPlaceBoard;
        }

        public void UnregisterEvents()
        {
            NetworkManager.onPlaceBoardClientEvent -= OnNetworkPlaceBoard;
        }
        
        // Triggered by the Action in NetworkManager
        private void OnNetworkPlaceBoard(NetPlaceBoard npb)
        {
            // Prevent adding duplicate teams if the network sends it twice
            if (!_teamPlaceFirst.Contains(npb.Team))
            {
                _teamPlaceFirst.Add(npb.Team);
            }

            if (_teamPlaceFirst.Count == 2)
            {
                startOnlinePlay = true;
            }
        }

        #endregion
    }
}
