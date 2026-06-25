using System.Collections;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using ARChess.Scripts.Chess;
using ARChess.Scripts.Loading;
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
        [Tooltip("Connection Lobby UI Gameobject")]
        private GameObject connectionLobbyUI;
        [SerializeField]
        [Tooltip("Connection Text")]
        private TextMeshProUGUI connectionText;
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
        
        // Logic
        private Coroutine _ellipsesAnimation;
        private const string IPPattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
        private bool onlineConnection = false;

        private void Start()
        {
            // Register Multiplayer Events
            NetworkManager.Init();
            NetworkManager.onStartGameClient += OnStartGameClient;
            NetworkManager.connectionClientDropped += OnConnectionDropped;
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
            // THE RESET
            if (Server.Instance != null) Server.Instance.Shutdown();
            if (Client.Instance != null) Client.Instance.Shutdown();

            ushort myPort = globalOptions.port;
            if (hostPortField.text.Length > 0)
            {
                myPort = ushort.Parse(hostPortField.text);
            }
            
            globalOptions.team = ChessTeam.White;
            // Re-fetch instances dynamically rather than via inspector fields
            Server.Instance.Init(myPort);
            Client.Instance.Init(globalOptions.ipAddress, myPort);
            onlineConnection = true;
            connectionText.text = "Finding player";
            _ellipsesAnimation = StartCoroutine(AnimateConnectionTextEllipses(0.5f));
        }

        public void OnOnlineConnectButton()
        {
            // THE RESET
            if (Client.Instance != null) Client.Instance.Shutdown();

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
            
            globalOptions.team = ChessTeam.Black;
            Client.Instance.Init(Regex.IsMatch(globalOptions.ipAddress, IPPattern) ? globalOptions.ipAddress : "127.0.0.1", myPort);
            onlineConnection = true;
            connectionText.text = "Waiting for connection to host";
            _ellipsesAnimation = StartCoroutine(AnimateConnectionTextEllipses(0.5f));
        }

        public void OnHostBackButton()
        {
            if(_ellipsesAnimation != null) StopCoroutine(_ellipsesAnimation);
            connectionText.text = "Disconnecting from server";
            // Use the Singletons instead of local references 
            if (Server.Instance != null) Server.Instance.Shutdown();
            if (Client.Instance != null) Client.Instance.Shutdown();
            
            NetworkManager.ResetOnline();
            
            _ellipsesAnimation = StartCoroutine(AnimateConnectionTextEllipses(0.5f));
            StartCoroutine(CloseOnline());
        }
        
        private IEnumerator AnimateConnectionTextEllipses(float animationDotSpeed)
        {
            int dotCount = 0;
            string initialText = connectionText.text;
            StringBuilder dots =  new StringBuilder();
            while (onlineConnection)
            {
                // Add dots up to 3
                dots.Append('.', dotCount);
                connectionText.text += dots.ToString();

                // Increment dot count, reset after 3
                dotCount++;
                
                // If we reach 3 dots, reset to 0 and remove the dots
                if (dotCount > 3)
                {
                    dots.Clear();
                    dotCount = 0; // Reset to 0
                    connectionText.text = initialText; // Remove dots
                }

                // Wait for the specified animation speed
                yield return new WaitForSeconds(animationDotSpeed);
            }
        }

        private IEnumerator CloseOnline()
        {
            while (onlineConnection)
            {
                if(onlineConnection) yield return new WaitForSeconds(0.2f);
                
                if(_ellipsesAnimation != null)
                    StopCoroutine(_ellipsesAnimation);
                Log.LogThis("Server/Client shutdown", this);
                globalOptions.ResetOnline();
                connectionLobbyUI.SetActive(false);
                onlineLobbyUI.SetActive(true);
                onlineConnection = false;
                yield break;

            }
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
            StopAllCoroutines();
            NetworkManager.onStartGameClient -= OnStartGameClient;
            NetworkManager.connectionClientDropped -= OnConnectionDropped;
        }
        
        private void OnConnectionDropped()
        {
            if(!Server.Instance || !Client.Instance) onlineConnection = false;
        }

        private void OnStartGameClient()
        {
            globalOptions.onlinePlay = true;
            loadingScene.loadingTextString = "Found your match, loading scene";
            loadingScene.enteringTextString = $"Prepare yourself \"{globalOptions.playerName}\"";
            if(_ellipsesAnimation != null)
                StopCoroutine(_ellipsesAnimation);
            connectionText.text = "Found player!";
            loadingScene.LoadScene(1);
        }
    }
}
