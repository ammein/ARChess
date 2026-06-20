using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ARChess.Scripts.Net;
using ARChess.Scripts.Net.Net_Message;
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
        [SerializeField]
        [Tooltip("Rematch Button")]
        private GameObject rematchButton;
        [SerializeField]
        [Tooltip("Text for Online Status")]
        private GameObject onlineStatus;
        [SerializeField]
        [Tooltip("Placeboard UI Container")]
        private GameObject placeboardTextContainer;
        [SerializeField]
        [Tooltip("Overlay Placeboard")]
        private GameObject overlayScreen;
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
        private bool[] _playerRematch = new bool[2];
        private bool _hasNotifiedQuit;
        
        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        public event Action<GameObject> objectSpawned;
        
        private bool _invoked;

        private void Awake()
        {
            RegisterEvents();
            
            // Tell the script what team we are immediately so the 
            // Update loop math doesn't get confused before we place the board!
            if (globalProjectStateOptions != null)
            {
                startingTeam = globalProjectStateOptions.team;
            }
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
            
            // If Online Play and not started yet, don't run end game yet.
            if (globalProjectStateOptions.onlinePlay && !startOnlinePlay) return;
            
            // Check if the chess is ended
            switch (_chessboard.EndGame)
            {
                case true:
                    if(!overlayScreen.activeInHierarchy)
                        overlayScreen.SetActive(true);
                    
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
                    if(overlayScreen.activeInHierarchy)
                        overlayScreen.SetActive(false);
                    
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
                
                // If I physically have the board locally, I have placed it! 
                // Do NOT wait for the network to echo my own packet back to me.
                bool iHavePlaced = m_ObjectInstance != null;
                
                // Check if the list specifically contains the opponent's team ID
                bool opponentHasPlaced = _teamPlaceFirst.Contains(opponentTeamId);
                
                // Trigger ONLY if the opponent has placed their board, but you haven't yet
                if (opponentHasPlaced && !iHavePlaced)
                {
                    placeboardTextContainer.SetActive(true);
                    overlayScreen.SetActive(true);
                        
                    var color = overlayScreen.GetComponent<UnityEngine.UI.Image>().color;
                    color.a = 0.0f; // Clear background so they can still scan the AR environment
                    overlayScreen.GetComponent<UnityEngine.UI.Image>().color = color;
                        
                    // Tell the UI system to stop absorbing clicks!
                    overlayScreen.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                    placeboardTextContainer.GetComponentInChildren<UnityEngine.UI.Image>().raycastTarget = false;
                    placeBoardText.raycastTarget = false;
                    
                    placeBoardText.text = "Your opponent is waiting for you to place your board to start the game";   
                }
                // Scenario B: You placed yours, but the opponent hasn't (Show dim overlay & wait text)
                else if (iHavePlaced && !opponentHasPlaced)
                {
                    placeboardTextContainer.SetActive(true);
                    overlayScreen.SetActive(true);
                        
                    var color = overlayScreen.GetComponent<UnityEngine.UI.Image>().color;
                    color.a = 0.8f; // Dim the screen since they can't do anything until opponent joins
                    overlayScreen.GetComponent<UnityEngine.UI.Image>().color = color;   
                        
                    // Turn the UI blocker back on so they can't touch the board!
                    overlayScreen.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
                    ChildrenLayerMask.Chess(m_ObjectInstance, "Chess", "Ignore Raycast");
                    ChildrenLayerMask.Chess(m_ObjectInstance, "Tile", "Ignore Tile");
                    ChildrenLayerMask.Chess(m_ObjectInstance, "Visual Tile", "Ignore Visual Tile");
                    placeboardTextContainer.GetComponentInChildren<UnityEngine.UI.Image>().raycastTarget = true;
                    placeBoardText.raycastTarget = true;
                    placeBoardText.text = "Waiting for the opponent to place their board";  
                }
                // Scenario C: You haven't placed, and opponent hasn't placed (Do absolutely nothing / Keep UI hidden)
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                else if (!iHavePlaced && !opponentHasPlaced)
                {
                    if (placeboardTextContainer.activeInHierarchy) placeboardTextContainer.SetActive(false);
                    if (overlayScreen.activeInHierarchy) overlayScreen.SetActive(false);
                }
            } 
            else // startOnlinePlay is true (Both have placed)
            {
                ChildrenLayerMask.Chess(m_ObjectInstance, "Ignore Raycast", "Chess");
                ChildrenLayerMask.Chess(m_ObjectInstance, "Ignore Tile", "Tile");
                ChildrenLayerMask.Chess(m_ObjectInstance, "Ignore Visual Tile", "Visual Tile");
                // Clean up and turn off all placement overlays completely
                if (placeboardTextContainer.activeInHierarchy) placeboardTextContainer.SetActive(false);
                if (overlayScreen.activeInHierarchy) overlayScreen.SetActive(false);
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
                    if(startOnlinePlay)
                        chessboard.OnRematch();
                    else
                        chessboard.GameReset();
        }

        public void ExitGame()
        {
            Disconnected();
        }

        public void EndGame()
        {
            if (startOnlinePlay)
            {
                StartCoroutine(TryEndGame());
                OnEndGame();
            }
            else
            {
                OnEndGame();
            }
        }
        
        private IEnumerator TryEndGame()
        {
            bool sentSuccessfully = false;
            
            // Loop runs until the message successfully enters the network pipeline
            while (!sentSuccessfully)
            {
                if (Client.Instance)
                {
                    NetRematch rm = new NetRematch();
                    rm.teamId = startingTeam == ChessTeam.White ? 0 : 1;
                    rm.wantRematch = 0; // 0 = Quit
                    sentSuccessfully = Client.Instance.SendToServer(rm);
                }
                
                if (!sentSuccessfully)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            // --- THE RACE CONDITION FIX ---
            // Wait a brief moment to guarantee the Server broadcasts the packet 
            // to the opponent BEFORE we pull the plug and destroy the Server.
            yield return new WaitForSeconds(0.2f);
            // ------------------------------
            
            if (Server.Instance) Server.Instance.Shutdown();
            if (Client.Instance) Client.Instance.Shutdown();
        }

        private void OnEndGame()
        {
            if (m_ObjectInstance && m_ObjectInstance.TryGetComponent(out Chessboard chessboard))
            {
                chessboard.EndGame = false;
                chessboard.yourTurnUI.gameObject.SetActive(false);
            }
            
            endGame.SetActive(false);
            
            if(globalProjectStateOptions.onlinePlay)
                Disconnected();
            
            if(_teamPlaceFirst.Count > 0)
                _teamPlaceFirst.Clear();
            
            Destroy(_probeGameObject);
        }

        private IEnumerator OpponentSuddenQuit()
        {
            placeboardTextContainer.SetActive(true);
            overlayScreen.SetActive(false);
            placeboardTextContainer.layer = LayerMask.NameToLayer("Ignore Raycast");
            placeBoardText.text = "Opponent has left the game";
            
            yield return new WaitForSeconds(5f);
            
            placeboardTextContainer.SetActive(false);
            placeboardTextContainer.layer = LayerMask.NameToLayer("UI");
            placeBoardText.text = "";
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

                    npb.Team = team == ChessTeam.White ? 0 : 1;
                    
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
            // Override the global options with the official Network Assigned Team (0=White, 1=Black)
            // This guarantees players never accidentally pick the same color!
            if (globalProjectStateOptions.onlinePlay && NetworkManager.Instance != null && NetworkManager.Instance.currentTeam != -1)
            {
                globalProjectStateOptions.team = NetworkManager.Instance.currentTeam == 0 ? ChessTeam.White : ChessTeam.Black;
            }
            
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

        private void Disconnected()
        {
            if (_hasNotifiedQuit) return;
            if(startOnlinePlay)
                startOnlinePlay = false;
            StartCoroutine(OpponentSuddenQuit());
            
            // Shutdown server & client
            if (Server.Instance) Server.Instance.Shutdown();
            if (Client.Instance) Client.Instance.Shutdown();
            
            _hasNotifiedQuit = true;
        }

        #region Network Event Listeners

        public void RegisterEvents()
        {
            NetworkManager.onPlaceBoardClientEvent += OnNetworkPlaceBoard;
            NetworkManager.onRematchClientEvent += OnNetworkRematch;
            NetworkManager.onStartGameClient += OnStartGame;
            NetworkManager.connectionClientDropped += OnConnectionDropped;
        }

        public void UnregisterEvents()
        {
            NetworkManager.onPlaceBoardClientEvent -= OnNetworkPlaceBoard;
            NetworkManager.onRematchClientEvent -= OnNetworkRematch;
            NetworkManager.onStartGameClient -= OnStartGame;
            NetworkManager.connectionClientDropped -= OnConnectionDropped;
        }
        
        // Happens when client/server suddenly disconnected...
        private void OnConnectionDropped()
        {
            Disconnected();
        }
        
        private void OnStartGame()
        {
            // The room just filled up with both players!
            // If I already placed my board before they joined, tell them!
            if (m_ObjectInstance && globalProjectStateOptions.onlinePlay)
            {
                StartCoroutine(TrySendBoardPlacement(startingTeam));
            }
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
                bool init = _chessboard.OnlineStartPlay();
                if(init)
                    startOnlinePlay = true;
            }
        }
        
        private void OnNetworkRematch(NetRematch rm)
        {
            int enemyTeamId = startingTeam != ChessTeam.White ? 0 : 1;
            int myTeamId = startingTeam == ChessTeam.White ? 0 : 1;

            // --- THE QUIT/CANCEL CONDITION ---
            // TryEndGame() sends wantRematch = 0. This means the opponent left the room.
            if (rm.wantRematch == 0)
            {
                if(!rematchButton.GetComponent<Button>().interactable)
                    rematchButton.GetComponent<Button>().interactable = true;

                // Turn off online status
                if(globalProjectStateOptions.onlinePlay)
                    globalProjectStateOptions.onlinePlay = false;
                
                if(startOnlinePlay)
                    startOnlinePlay = false;
                
                var cancelColor = rematchButton.GetComponentInChildren<TextMeshProUGUI>().color;
                cancelColor.a = 1f;
                rematchButton.GetComponentInChildren<TextMeshProUGUI>().color = cancelColor;
                
                // Inform the remaining player
                onlineStatus.gameObject.SetActive(true);
                onlineStatus.GetComponent<TextMeshProUGUI>().text = "Opponent has left the match.";
                
                // Reset local rematch logic
                _playerRematch[0] = false;
                _playerRematch[1] = false;
                return;
            }
            // ---------------------------------
            
            // Otherwise, they sent wantRematch = 1 (They want to play again!)
            _playerRematch[rm.teamId] = true;

            if (!_playerRematch[myTeamId] && _playerRematch[enemyTeamId])
            {
                if(!rematchButton.GetComponent<Button>().interactable)
                    rematchButton.GetComponent<Button>().interactable = true;
                
                onlineStatus.gameObject.SetActive(true);
                onlineStatus.GetComponent<TextMeshProUGUI>().text = "Your opponent wants to rematch";   
            } 
            else if (_playerRematch[myTeamId] && !_playerRematch[enemyTeamId])
            {
                if(rematchButton.GetComponent<Button>().interactable)
                    rematchButton.GetComponent<Button>().interactable = false;
                
                onlineStatus.gameObject.SetActive(true);
                var waitColor = rematchButton.GetComponentInChildren<TextMeshProUGUI>().color;
                waitColor.a = 0.3f;
                rematchButton.GetComponentInChildren<TextMeshProUGUI>().color = waitColor;
                onlineStatus.GetComponent<TextMeshProUGUI>().text = "Waiting for your opponent to rematch";   
            }

            // If BOTH players have hit the rematch button
            if (_playerRematch[0] && _playerRematch[1])
            {
                // Reset Visuals
                var finalColor = rematchButton.GetComponentInChildren<TextMeshProUGUI>().color;
                finalColor.a = 1f;
                rematchButton.GetComponentInChildren<TextMeshProUGUI>().color = finalColor;
                rematchButton.GetComponent<Button>().interactable = true;
                onlineStatus.GetComponent<TextMeshProUGUI>().text = "";
                onlineStatus.gameObject.SetActive(false);
                
                // Reset local logic array for the next game!
                _playerRematch[0] = false;
                _playerRematch[1] = false;

                if (m_ObjectInstance.TryGetComponent(out Chessboard chessboard))
                {
                    chessboard.GameReset();
                }
            }
        }

        #endregion
    }
}
