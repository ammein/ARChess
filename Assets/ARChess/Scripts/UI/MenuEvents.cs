using System.Runtime.Serialization;
using ARChess.Scripts.Chess;
using ARChess.Scripts.Net;
using ARChess.Scripts.Project;
using ARChess.Scripts.Utility;
using TMPro;
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
        [OptionalField]
        [Tooltip("IP Address Field")]
        private TMP_InputField ipField;

        [SerializeField]
        [Tooltip("Project Options")]
        private ProjectStateOptions globalOptions;

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
            
            if(!enable) return;
            ipField.text = globalOptions.ipAddress;
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

        public void SetTeam(string team)
        {
            globalOptions.team = team.Contains("white") ? ChessTeam.White : ChessTeam.Black;
        }

        public void OnOnlineHostButton()
        {
            server.Init(8007);
            client.Init(globalOptions.ipAddress, 8007);
        }

        public void OnOnlineConnectButton()
        {
            client.Init(ipField.text, 8007);
        }

        public void OnHostBackButton()
        {
            server.Shutdown();
            client.Shutdown();
            Log.LogThis("Server/Client shutdown", this);
        }

        public void SetIPAddress(string ipAddress)
        {
            globalOptions.ipAddress = ipAddress;
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
            // Reset Main Scene Video Loaded
            globalOptions.mainSceneVideoLoaded = false;
            Application.Quit();
        }
    }
}
