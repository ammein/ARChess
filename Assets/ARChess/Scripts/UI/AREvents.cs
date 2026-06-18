using System;
using ARChess.Scripts.Net;
using ARChess.Scripts.Project;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ARChess.Scripts.UI
{
    public class AREvents : MonoBehaviour
    {
        [FormerlySerializedAs("_projectStateOptions")]
        [SerializeField]
        [Tooltip("Project State Options")]
        private ProjectStateOptions projectStateOptions;
        [SerializeField]
        [Tooltip("Tutorial Checkbox")]
        private Toggle tutorial;
        [SerializeField]
        [Tooltip("Tutorial Checkbox")]
        private Toggle dynamicLighting;

        void Start()
        {
            tutorial.isOn = projectStateOptions.tutorialsEnabled;
            dynamicLighting.isOn = projectStateOptions.dynamicLighting;
        }

        public void ToggleDynamicLighting(bool value)
        {
            projectStateOptions.dynamicLighting = value;
        }

        public void ToggleTutorial(bool value)
        {
            projectStateOptions.tutorialsEnabled = value;
        }

        // When the scene is unloaded, reset all settings
        public void OnDestroy()
        {
            // First, clean up the network while we still know we are online!
            if (projectStateOptions.onlinePlay)
            {
                NetworkManager.ResetOnline();
                
                // Physically close the sockets so they don't run in the background 
                // while the player is sitting in the Main Menu!
                if (Server.Instance != null) Server.Instance.Shutdown();
                if (Client.Instance != null) Client.Instance.Shutdown();
            }
            projectStateOptions.OnQuit();
        }
    }
}
