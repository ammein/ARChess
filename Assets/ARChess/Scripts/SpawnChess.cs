using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace ARChess.Scripts
{
    /// <summary>
    /// Behavior with an API for spawning objects from a given set of prefabs.
    /// </summary>
    public class SpawnChess : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The camera that objects will face when spawned. If not set, defaults to the main camera.")]
        private Camera mCameraToFace;

        /// <summary>
        /// The camera that objects will face when spawned. If not set, defaults to the <see cref="Camera.main"/> camera.
        /// </summary>
        public Camera CameraToFace
        {
            get
            {
                EnsureFacingCamera();
                return mCameraToFace;
            }
            set => mCameraToFace = value;
        }

        [FormerlySerializedAs("m_ObjectPrefabs")] [SerializeField] [Tooltip("The list of prefabs available to spawn.")]
        private List<GameObject> mObjectPrefabs = new List<GameObject>();

        /// <summary>
        /// The list of prefabs available to spawn.
        /// </summary>
        public List<GameObject> ObjectPrefabs
        {
            get => mObjectPrefabs;
            set => mObjectPrefabs = value;
        }

        [FormerlySerializedAs("m_SpawnVisualizationPrefab")]
        [SerializeField]
        [Tooltip("Optional prefab to spawn for each spawned object. Use a prefab with the Destroy Self component to make " +
                 "sure the visualization only lives temporarily.")]
        private GameObject mSpawnVisualizationPrefab;

        /// <summary>
        /// Optional prefab to spawn for each spawned object.
        /// </summary>
        /// <remarks>Use a prefab with <see cref="DestroySelf"/> to make sure the visualization only lives temporarily.</remarks>
        public GameObject SpawnVisualizationPrefab
        {
            get => mSpawnVisualizationPrefab;
            set => mSpawnVisualizationPrefab = value;
        }

        [FormerlySerializedAs("m_SpawnOptionIndex")]
        [SerializeField]
        [Tooltip("The index of the prefab to spawn. If outside the range of the list, this behavior will select " +
                 "a random object each time it spawns.")]
        private int mSpawnOptionIndex = -1;

        /// <summary>
        /// The index of the prefab to spawn. If outside the range of <see cref="ObjectPrefabs"/>, this behavior will
        /// select a random object each time it spawns.
        /// </summary>
        /// <seealso cref="IsSpawnOptionRandomized"/>
        public int SpawnOptionIndex
        {
            get => mSpawnOptionIndex;
            set => mSpawnOptionIndex = value;
        }

        /// <summary>
        /// Whether this behavior will select a random object from <see cref="ObjectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="SpawnOptionIndex"/>
        /// <seealso cref="RandomizeSpawnOption"/>
        public bool IsSpawnOptionRandomized => mSpawnOptionIndex < 0 || mSpawnOptionIndex >= mObjectPrefabs.Count;

        [FormerlySerializedAs("m_OnlySpawnInView")]
        [SerializeField]
        [Tooltip("Whether to only spawn an object if the spawn point is within view of the camera.")]
        private bool mOnlySpawnInView = true;

        /// <summary>
        /// Whether to only spawn an object if the spawn point is within view of the <see cref="CameraToFace"/>.
        /// </summary>
        public bool OnlySpawnInView
        {
            get => mOnlySpawnInView;
            set => mOnlySpawnInView = value;
        }

        [FormerlySerializedAs("m_ViewportPeriphery")]
        [SerializeField]
        [Tooltip("The size, in viewport units, of the periphery inside the viewport that will not be considered in view.")]
        private float mViewportPeriphery = 0.15f;

        /// <summary>
        /// The size, in viewport units, of the periphery inside the viewport that will not be considered in view.
        /// </summary>
        public float ViewportPeriphery
        {
            get => mViewportPeriphery;
            set => mViewportPeriphery = value;
        }

        [FormerlySerializedAs("m_ApplyRandomAngleAtSpawn")]
        [SerializeField]
        [Tooltip("When enabled, the object will be rotated about the y-axis when spawned by Spawn Angle Range, " +
                 "in relation to the direction of the spawn point to the camera.")]
        private bool mApplyRandomAngleAtSpawn = true;

        /// <summary>
        /// When enabled, the object will be rotated about the y-axis when spawned by <see cref="SpawnAngleRange"/>
        /// in relation to the direction of the spawn point to the camera.
        /// </summary>
        public bool ApplyRandomAngleAtSpawn
        {
            get => mApplyRandomAngleAtSpawn;
            set => mApplyRandomAngleAtSpawn = value;
        }

        [FormerlySerializedAs("m_SpawnAngleRange")]
        [SerializeField]
        [Tooltip("The range in degrees that the object will randomly be rotated about the y axis when spawned, " +
                 "in relation to the direction of the spawn point to the camera.")]
        private float mSpawnAngleRange = 45f;

        /// <summary>
        /// The range in degrees that the object will randomly be rotated about the y axis when spawned, in relation
        /// to the direction of the spawn point to the camera.
        /// </summary>
        public float SpawnAngleRange
        {
            get => mSpawnAngleRange;
            set => mSpawnAngleRange = value;
        }

        [FormerlySerializedAs("m_SpawnAsChildren")]
        [SerializeField]
        [Tooltip("Whether to spawn each object as a child of this object.")]
        private bool mSpawnAsChildren;

        /// <summary>
        /// Whether to spawn each object as a child of this object.
        /// </summary>
        public bool SpawnAsChildren
        {
            get => mSpawnAsChildren;
            set => mSpawnAsChildren = value;
        }

        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        /// <seealso cref="TrySpawnObject"/>
        public event Action<GameObject> ObjectSpawned;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        void Awake()
        {
            EnsureFacingCamera();
        }

        void EnsureFacingCamera()
        {
            if (mCameraToFace == null)
                mCameraToFace = Camera.main;
        }

        /// <summary>
        /// Sets this behavior to select a random object from <see cref="ObjectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="SpawnOptionIndex"/>
        /// <seealso cref="IsSpawnOptionRandomized"/>
        public void RandomizeSpawnOption()
        {
            mSpawnOptionIndex = -1;
        }

        /// <summary>
        /// For Chess Board Instance
        /// </summary>
        private GameObject InstanceChessBoard;
        
        /// <summary>
        /// For Visualization Plane
        /// </summary>
        private GameObject InstanceVisualizationPrefab;

        /// <summary>
        /// Attempts to spawn an object from <see cref="ObjectPrefabs"/> at the given position. The object will have a
        /// yaw rotation that faces <see cref="CameraToFace"/>, plus or minus a random angle within <see cref="SpawnAngleRange"/>.
        /// </summary>
        /// <param name="spawnPoint">The world space position at which to spawn the object.</param>
        /// <param name="spawnNormal">The world space normal of the spawn surface.</param>
        /// <returns>Returns <see langword="true"/> if the spawner successfully spawned an object. Otherwise returns
        /// <see langword="false"/>, for instance if the spawn point is out of view of the camera.</returns>
        /// <remarks>
        /// The object selected to spawn is based on <see cref="SpawnOptionIndex"/>. If the index is outside
        /// the range of <see cref="ObjectPrefabs"/>, this method will select a random prefab from the list to spawn.
        /// Otherwise, it will spawn the prefab at the index.
        /// </remarks>
        /// <seealso cref="ObjectSpawned"/>
        public bool TrySpawnObject(Vector3 spawnPoint, Vector3 spawnNormal)
        {
            if (mOnlySpawnInView)
            {
                var inViewMin = mViewportPeriphery;
                var inViewMax = 1f - mViewportPeriphery;
                var pointInViewportSpace = CameraToFace.WorldToViewportPoint(spawnPoint);
                if (pointInViewportSpace.z < 0f || pointInViewportSpace.x > inViewMax ||
                    pointInViewportSpace.x < inViewMin ||
                    pointInViewportSpace.y > inViewMax || pointInViewportSpace.y < inViewMin)
                {
                    return false;
                }
            }

            var objectIndex = IsSpawnOptionRandomized ? UnityEngine.Random.Range(0, mObjectPrefabs.Count) : mSpawnOptionIndex;
            if(InstanceChessBoard) Destroy(InstanceChessBoard);
            InstanceChessBoard = Instantiate(mObjectPrefabs[objectIndex]);
            if (mSpawnAsChildren)
                InstanceChessBoard.transform.parent = transform;

            InstanceChessBoard.transform.position = spawnPoint;
            EnsureFacingCamera();

            var facePosition = mCameraToFace.transform.position;
            var forward = facePosition - spawnPoint;
            BurstMathUtility.ProjectOnPlane(forward, spawnNormal, out var projectedForward);
            InstanceChessBoard.transform.rotation = Quaternion.LookRotation(projectedForward, spawnNormal);

            if (mApplyRandomAngleAtSpawn)
            {
                var randomRotation = UnityEngine.Random.Range(-mSpawnAngleRange, mSpawnAngleRange);
                InstanceChessBoard.transform.Rotate(Vector3.up, randomRotation);
            }

            if (mSpawnVisualizationPrefab != null)
            {
                if(InstanceVisualizationPrefab) Destroy(InstanceVisualizationPrefab);
                InstanceVisualizationPrefab = Instantiate(mSpawnVisualizationPrefab);
                var visualizationTrans = InstanceVisualizationPrefab.transform;
                visualizationTrans.position = spawnPoint;
                visualizationTrans.rotation = InstanceChessBoard.transform.rotation;
            }

            ObjectSpawned?.Invoke(InstanceChessBoard);
            return true;
        }
    }
}