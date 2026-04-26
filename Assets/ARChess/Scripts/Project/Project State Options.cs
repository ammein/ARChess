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
        public float initialChessboardSize = 0.06f;
        
        [Header("Tutorials")]
        public bool tutorialsEnabled = true;
        public bool tutorialPlayed = false;

        [Header("AR Settings")]
        [Tooltip("For using dynamic lighting from real world source for your scene.")]
        public bool dynamicLighting;

        [Header("Scene Settings")]
        [Tooltip("Main Scene Video Loaded")]
        public bool mainSceneVideoLoaded;

        // Add a method to reset values if needed
        public void ResetToDefaults()
        {
            playerName = "Guest";
            team = ChessTeam.White;
            initialChessboardSize = 0.06f;
            tutorialsEnabled = true;
            dynamicLighting = false;
            tutorialPlayed = false;
        }
    }
}
