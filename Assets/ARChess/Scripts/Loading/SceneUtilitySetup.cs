using UnityEngine;

// This script is to fix loading error that XR Simulation cannot find camera.
// https://discussions.unity.com/t/the-objects-of-type-unityengine-camera-and-unityengine-xr-simulation-simulatedtrackedimage-have-been-destroyed/1615601/3
namespace ARChess.Scripts.Loading
{
    public static class SceneUtilitySetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Setup()
        {
            var gameObject = new GameObject("SceneUtility");
            gameObject.AddComponent<SceneUtility>();
        }
    }
}