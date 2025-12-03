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

        [SerializeField]
        [Tooltip("The prefab size on spawn")]
        private float originSize = 1;

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


            if (m_ObjectInstance)
            {
                Positioning(forward, positionPose, spawnNormal);
                return m_ObjectInstance;
            }
            
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
            // m_ObjectInstance.transform.rotation = Quaternion.LookRotation(projectedForward, spawnNormal);

            m_ObjectInstance.transform.localScale = new Vector3(originSize, originSize, originSize);
        }
    }
}
