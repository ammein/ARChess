using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

// This script is to fix loading error that XR Simulation cannot find camera.
// https://discussions.unity.com/t/the-objects-of-type-unityengine-camera-and-unityengine-xr-simulation-simulatedtrackedimage-have-been-destroyed/1615601/3
namespace ARChess.Scripts.Loading
{
    public class SceneUtility : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        void OnSceneUnloaded(Scene current)
        {
            if (current == SceneManager.GetActiveScene())
            {
                LoaderUtility.Deinitialize();
                LoaderUtility.Initialize();
            }
        }

        void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
    }
}