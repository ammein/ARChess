using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ARChess.Scripts
{
    
    public class PlaceObject : MonoBehaviour
    {
    
        [SerializeField]
        [Tooltip("The prefab to spawn")]
        private GameObject prefab;
        
        private GameObject m_ObjectInstance;

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
        public bool ClonePrefab(Vector3 positionPose, Vector3 spawnNormal)
        {
            
            var facePosition = Camera.main.transform.position;
            var forward = facePosition - positionPose;
            
            if (m_ObjectInstance)
            {
                Positioning(forward, positionPose, spawnNormal);
                return true;
            }
            
            // Instantiate object with prefab using same position and rotation
            m_ObjectInstance = Instantiate(prefab);
            
            Positioning(forward, positionPose, spawnNormal);

            ObjectSpawned?.Invoke(m_ObjectInstance);

            return true;
        }

        private void Positioning(Vector3 forward, Vector3 positionPose, Vector3 spawnNormal)
        {
            m_ObjectInstance.transform.position = positionPose;
            
            BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
            m_ObjectInstance.transform.localRotation = Quaternion.LookRotation(projectedForward, spawnNormal);
        }
    }
}
