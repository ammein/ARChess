using System;
using ARChess.Scripts.Project;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ARChess.Scripts.Chess
{
    
    public class PlaceObject : MonoBehaviour
    {
    
        [Header("Place Object")]
        [SerializeField]
        [Tooltip("The prefab to spawn")]
        private GameObject prefab;
        
        private GameObject m_ObjectInstance;
        
        [Header("Project Setting")]
        [SerializeField]
        [Tooltip("Project State Options")]
        private ProjectStateOptions globalProjectStateOptions;

        public GameObject ObjectInstance
        {
            get => m_ObjectInstance;
            set => m_ObjectInstance = value;
        }
        
        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        /// <seealso cref="ClonePrefab"/>
        public event Action<GameObject> ObjectSpawned;

        // ReSharper disable Unity.PerformanceAnalysis
        public GameObject ClonePrefab(Vector3 positionPose, Vector3 spawnNormal)
        {
            
            var facePosition = Camera.main.transform.position;
            var forward = facePosition - positionPose;
            
            // Instantiate object with prefab using same position and rotation
            m_ObjectInstance = Instantiate(prefab);
            
            Positioning(forward, positionPose, spawnNormal);

            ObjectSpawned?.Invoke(m_ObjectInstance);

            return m_ObjectInstance;
        }

        private void Positioning(Vector3 forward, Vector3 positionPose, Vector3 spawnNormal)
        {
            m_ObjectInstance.transform.position = positionPose;
            
            BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
            m_ObjectInstance.transform.localRotation = Quaternion.LookRotation(projectedForward, spawnNormal);
            
            // Resize Chessboard
            m_ObjectInstance.transform.localScale = new Vector3(globalProjectStateOptions.initialChessboardSize, globalProjectStateOptions.initialChessboardSize, globalProjectStateOptions.initialChessboardSize);
        }

        public void Positioning(Vector3 positionPose, Vector3 spawnNormal)
        {
            Positioning(Camera.main.transform.position - positionPose, positionPose, spawnNormal);
        }

        public void ToggleContact(bool toggle)
        {
            if (!m_ObjectInstance) return;
            m_ObjectInstance.transform.Find("Chess Attach").GetComponent<BoxCollider>().providesContacts = toggle;
        }
    }
}
