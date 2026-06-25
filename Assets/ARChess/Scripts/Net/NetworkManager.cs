using System;
using ARChess.Scripts.Net.Net_Message;
using ARChess.Scripts.Project;
using Unity.Networking.Transport;
using UnityEngine;

namespace ARChess.Scripts.Net
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        // The events the UI and Chessboard listen to
        public static event Action onStartGameClient;
        public static event Action<NetMakeMove> onMakeMoveClientEvent;
        public static event Action<NetPlaceBoard> onPlaceBoardClientEvent;
        public static event Action<NetRematch> onRematchClientEvent;
        public static event Action connectionClientDropped;

        public ProjectStateOptions globalOptions;

        // Lobby tracking variables
        private int playerCount = -1;
        [HideInInspector]
        public int currentTeam = -1;
        private bool _eventsRegistered = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void Init()
        {
            if (Instance != null)
            {
                Instance.RegisterEvents();
                Instance.playerCount = -1;
                Instance.currentTeam = -1;
            }
        }

        public static void ResetOnline()
        {
            if (Instance)
            {
                Instance.playerCount = -1;
                Instance.currentTeam = -1;

                if (Instance.globalOptions)
                {
                    Instance.globalOptions.ResetOnline();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                UnRegisterEvents();
            }
        }

        #region Network Event Registrations

        private void RegisterEvents()
        {
            // Prevent double-subscribing if Init() is called twice!
            if (_eventsRegistered) return; 

            NetUtility.S_WELCOME += OnWelcomeServer;
            NetUtility.C_WELCOME += OnWelcomeClient;
            NetUtility.C_START_GAME += OnStartGameClientInternal;
            
            // The Make Move events
            NetUtility.S_MAKE_MOVE += OnMakeMoveServer;
            NetUtility.C_MAKE_MOVE += OnMakeMoveClient;
            
            // The Place Board events
            NetUtility.S_PLACE_BOARD += OnPlaceBoardServer;
            NetUtility.C_PLACE_BOARD += OnPlaceBoardClient;
            
            // The Rematch events
            NetUtility.S_REMATCH += OnRematchServer;
            NetUtility.C_REMATCH += OnRematchClient;

            if (Server.Instance != null) Server.Instance.connectionDropped += OnConnectionDropped;
            if (Client.Instance != null) Client.Instance.connectionDropped += OnConnectionDropped;

            _eventsRegistered = true;
        }

        private void UnRegisterEvents()
        {
            NetUtility.S_WELCOME -= OnWelcomeServer;
            NetUtility.C_WELCOME -= OnWelcomeClient;
            NetUtility.C_START_GAME -= OnStartGameClientInternal;
            
            NetUtility.S_MAKE_MOVE -= OnMakeMoveServer;
            NetUtility.C_MAKE_MOVE -= OnMakeMoveClient;
            
            NetUtility.S_PLACE_BOARD -= OnPlaceBoardServer;
            NetUtility.C_PLACE_BOARD -= OnPlaceBoardClient;
            
            NetUtility.S_REMATCH -= OnRematchServer;
            NetUtility.C_REMATCH -= OnRematchClient;
            
            if (Server.Instance != null) Server.Instance.connectionDropped -= OnConnectionDropped;
            if (Client.Instance != null) Client.Instance.connectionDropped -= OnConnectionDropped;

            _eventsRegistered = false;
        }
        
        // Connection Dropped
        private void OnConnectionDropped()
        {
            ResetOnline();
            connectionClientDropped?.Invoke();
        }

        // Server
        private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
        {
            NetWelcome nw = msg as NetWelcome;
            nw.AssignedTeam = ++playerCount; 
            Server.Instance.SendToClient(cnn, nw);

            if (playerCount == 1)
            {
                Server.Instance.Broadcast(new NetStartGame());
            }
        }
        
        private void OnRematchServer(NetMessage msg, NetworkConnection cnn)
        {
            Server.Instance.Broadcast(msg);
        }
        
        // Client
        private void OnWelcomeClient(NetMessage msg)
        {
            NetWelcome nw = msg as NetWelcome;
            currentTeam = nw.AssignedTeam;
            Debug.Log($"My assigned team is {nw.AssignedTeam}");
        }
        
        private void OnStartGameClientInternal(NetMessage msg)
        {
            onStartGameClient?.Invoke();
        }

        // Server: Broadcast the move
        private void OnMakeMoveServer(NetMessage msg, NetworkConnection cnn)
        {
            NetMakeMove mm = msg as NetMakeMove;
            Server.Instance.Broadcast(msg);
        }
        
        // Client: Pass the move to the Chessboard
        private void OnMakeMoveClient(NetMessage msg)
        {
            NetMakeMove mm = msg as NetMakeMove;
            
            onMakeMoveClientEvent?.Invoke(mm);
        }
        
        // Server: Receives board placement and broadcasts it
        private void OnPlaceBoardServer(NetMessage msg, NetworkConnection cnn)
        {
            Server.Instance.Broadcast(msg);
        }
        
        // Client: Receives placement from server and tells PlaceObject.cs
        private void OnPlaceBoardClient(NetMessage msg)
        {
            NetPlaceBoard npb = msg as NetPlaceBoard;
            
            onPlaceBoardClientEvent?.Invoke(npb);
        }
        
        private void OnRematchClient(NetMessage msg)
        {
            // Receive the connection message
            NetRematch rm = msg as NetRematch;
            
            // Send to event
            onRematchClientEvent?.Invoke(rm);
        }

        #endregion
    }
}