using UnityEngine;

namespace ARChess.Scripts.Project
{
    [CreateAssetMenu(fileName = "ProjectStateOptions", menuName = "Scriptable Objects/Project State Options")]
    public class ProjectStateOptions : ScriptableObject
    {
        [Header("Player Settings")]
        public string playerName = "Guest";
        
        [Header("Chess Size")]
        [SerializeField]
        [Tooltip("Overall size of the chessboard")]
        [Range(0f, 1f)]
        public float initialChessboardSize = 0.06f;
        
        [Header("Tutorials")]
        public bool tutorialsEnabled = true;

        // Add a method to reset values if needed
        public void ResetToDefaults()
        {
            playerName = "Guest";
            initialChessboardSize = 0.06f;
            tutorialsEnabled = true;
        }
    }
}
