using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
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
            globalOptions.team = ChessTeam.White;
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
            globalOptions.team = ChessTeam.Black;
        }

        public void OnHostBackButton()
        {
            server.Shutdown();
            client.Shutdown();
            Log.LogThis("Server/Client shutdown", this);
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
    }
}
