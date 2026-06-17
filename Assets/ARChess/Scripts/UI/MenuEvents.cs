using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using ARChess.Scripts.Chess;
using ARChess.Scripts.Loading;
using ARChess.Scripts.Net;
using ARChess.Scripts.Net.Net_Message;
using ARChess.Scripts.Project;
using ARChess.Scripts.Utility;
using TMPro;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;

namespace ARChess.Scripts.UI
{
    public class MenuEvents : MonoBehaviour
    {
        
        public static MenuEvents Instance { get; set; }

        public Server server;
        public Client client;
        
        [Header("Menu References")]
        [SerializeField]
        [Tooltip("For Options UI")]
        [OptionalField]
        private GameObject optionsUI;
        [SerializeField]
        [OptionalField]
        [Tooltip("Online Lobby UI Gameobject")]
        private GameObject onlineLobbyUI;
        [SerializeField]
        [OptionalField]
        [Tooltip("Name Field")]
        private TMP_InputField nameField;
        [SerializeField]
        [OptionalField]
        [Tooltip("Tutorial Checkbox")]
        private Toggle tutorial;
        [SerializeField]
        [OptionalField]
        [Tooltip("Dynamic Lighting Checkbox")]
        private Toggle dynamicLighting;
        [SerializeField]
        [Tooltip("IP Address Field")]
        private TMP_InputField ipField;
        
        [SerializeField]
        [Tooltip("Port Field")]
        private TMP_InputField hostPortField;
        
        [SerializeField]
        [Tooltip("Port Field")]
        private TMP_InputField connectPortField;

        [SerializeField]
        [Tooltip("Project Options")]
        private ProjectStateOptions globalOptions;
        
        [SerializeField]
        [Tooltip("Loading Scene")]
        private LoadingScene loadingScene;
        
        // Multi Logic
        private int playerCount = -1;
        private int currentTeam = -1;

        private string IPPattern =
            @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";

        private void Awake()
        {
            Instance = this;
        }

        public void ToggleOptions(bool enable)
        {
            optionsUI.SetActive(enable);

            if (!enable) return;
            nameField.text = globalOptions.playerName;
            tutorial.isOn = globalOptions.tutorialsEnabled;
            dynamicLighting.isOn = globalOptions.dynamicLighting;
        }

        public void ToggleOnlineLobby(bool enable)
        {
            onlineLobbyUI.SetActive(enable);
            
            // Register Multiplayer Events
            RegisterEvents();
            
            if(!enable) return;
            if (ipField.placeholder != null)
            {
                TMP_Text placeholderTextComponent = ipField.placeholder as TMP_Text;

                if (placeholderTextComponent != null)
                {
                    placeholderTextComponent.text = globalOptions.ipAddress;
                }
            }

            if (hostPortField.placeholder != null)
            {
                TMP_Text placeholderPortTextComponent = hostPortField.placeholder as TMP_Text;

                if (placeholderPortTextComponent != null)
                {
                    placeholderPortTextComponent.text = globalOptions.port.ToString();
                }
            }
            
            if (connectPortField.placeholder != null)
            {
                TMP_Text placeholderPortTextComponent = connectPortField.placeholder as TMP_Text;

                if (placeholderPortTextComponent != null)
                {
                    placeholderPortTextComponent.text = globalOptions.port.ToString();
                }
            }
        }

        public void SetDynamicLighting(bool state)
        {
            globalOptions.dynamicLighting = state;
        }

        public void SetTutorials(bool state)
        {
            globalOptions.tutorialsEnabled = state;
        }

        public void SetName(string playerName)
        {
            globalOptions.playerName = playerName;
        }

        public void SetIpAddress(string ipAddress)
        {
            if (ipField.text.Length > 0)
            {
                if(Regex.IsMatch(ipAddress, IPPattern))
                    globalOptions.ipAddress = ipAddress;
            }
        }

        public void SetTeam(string team)
        {
            globalOptions.team = team.Contains("white") ? ChessTeam.White : ChessTeam.Black;
        }

        public void OnOnlineHostButton()
        {
            ushort myPort = globalOptions.port;
            if (hostPortField.text.Length > 0)
            {
                myPort = ushort.Parse(hostPortField.text);
            }
            server.Init(myPort);
            client.Init(globalOptions.ipAddress, myPort);
            globalOptions.onlinePlay = true;
        }

        public void OnOnlineConnectButton()
        {
            if (ipField.text.Length > 0)
            {
                if(Regex.IsMatch(ipField.text, IPPattern))
                    globalOptions.ipAddress = ipField.text;
            }
            
            ushort myPort = globalOptions.port;
            if (connectPortField.text.Length > 0)
            {
                myPort = ushort.Parse(connectPortField.text);
            }
            
            client.Init(Regex.IsMatch(globalOptions.ipAddress, IPPattern) ? globalOptions.ipAddress : "127.0.0.1", myPort);
            globalOptions.onlinePlay = true;
        }

        public void OnHostBackButton()
        {
            server.Shutdown();
            client.Shutdown();
            Log.LogThis("Server/Client shutdown", this);
            globalOptions.onlinePlay = false;
        }

        public void ResetOptions()
        {
            globalOptions.ResetToDefaults();
            dynamicLighting.isOn = globalOptions.dynamicLighting;
            tutorial.isOn = globalOptions.tutorialsEnabled;
            nameField.text = globalOptions.playerName;
        }
    
        public void QuitGame()
        {
            globalOptions.OnQuit();
            Application.Quit();
        }

        public void OnDestroy()
        {
            globalOptions.OnQuit();
        }
        
        
        #region
        private void RegisterEvents()
        {
            NetUtility.S_WELCOME += OnWelcomeServer;

            NetUtility.C_WELCOME += OnWelcomeClient;

            NetUtility.C_START_GAME += OnStartGameClient;
        }

        private void UnRegisterEvents()
        {
            NetUtility.S_WELCOME -= OnWelcomeServer;
            NetUtility.C_WELCOME -= OnWelcomeClient;
            NetUtility.C_START_GAME -= OnStartGameClient;
        }
        
        //Server
        private void OnWelcomeServer(NetMessage msg, NetworkConnection cnn)
        {
            // Client has connected, assign a team and return the message back to him
            NetWelcome nw = msg as NetWelcome;
            
            // Assign a team
            nw.AssignedTeam = ++playerCount; // When host start a server, it will be "0". Which is a good thing...
            
            // Return back to the client
            Server.Instance.SendToClient(cnn, nw);

            // If full, start the game
            if (playerCount == 1)
            {
                Server.Instance.Broadcast(new NetStartGame());
            }
        }
        
        // Client
        private void OnWelcomeClient(NetMessage msg)
        {
            // Receive the connection message
            NetWelcome nw = msg as NetWelcome;
            
            // Assign the team
            currentTeam = nw.AssignedTeam;
            
            Debug.Log($"My assigned team is {nw.AssignedTeam}");
        }
        
        private void OnStartGameClient(NetMessage msg)
        {
            globalOptions.team = currentTeam == 0 ? ChessTeam.White : ChessTeam.Black;
            globalOptions.onlinePlay = true;

            loadingScene.loadingTextString = "Found your match, loading scene";
            loadingScene.enteringTextString = $"You against {globalOptions.playerName}";
            loadingScene.LoadScene(1);
            if (currentTeam == 1) // Reset counts since we don't need it and properly assigned the team too.
            {
                playerCount = -1;
                currentTeam = -1; 
            }
        }
        #endregion
    }
}
