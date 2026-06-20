using System;
using ARChess.Scripts.Chess;
using UnityEngine;

namespace ARChess.Scripts.Project
{
    [CreateAssetMenu(fileName = "ProjectStateOptions", menuName = "Scriptable Objects/Project State Options")]
    public class ProjectStateOptions : ScriptableObject
    {
        [Header("Player Settings")]
        public string playerName = "Guest";
        public ChessTeam team = ChessTeam.White;
        
        [Header("Chess Size")]
        [SerializeField]
        [Tooltip("Overall size of the chessboard")]
        [Range(0f, 2f)]
        public float initialChessboardSize = 1f;
        
        [Header("Tutorials")]
        public bool tutorialsEnabled = true;
        public bool tutorialPlayed = false;

        [Header("AR Settings")]
        [Tooltip("For using dynamic lighting from real world source for your scene.")]
        public bool dynamicLighting;

        [Header("Scene Settings")]
        [Tooltip("Main Scene Video Loaded")]
        public bool mainSceneVideoLoaded;

        [Header("Online Settings")] [Tooltip("Ip Address")]
        public string ipAddress = "127.0.0.1";
        [Tooltip("Port for the Ip Address")]
        public ushort port = 8007;
        
        [HideInInspector]
        public bool onlinePlay = false;
        

        // Add a method to reset values if needed
        public void ResetToDefaults()
        {
            playerName = "Guest";
            initialChessboardSize = 1f;
            tutorialsEnabled = true;
            dynamicLighting = false;
            ResetOnline();
        }

        public void OnQuit()
        {
            mainSceneVideoLoaded = false;
            ipAddress = "127.0.0.1";
            port = 8007;
            onlinePlay = false;
            tutorialPlayed = false;
        }

        public void ResetOnline()
        {
            ipAddress = "127.0.0.1";
            port = 8007;
            onlinePlay = false;
        }
    }
}
