using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EnhanceTouch = UnityEngine.InputSystem.EnhancedTouch;

namespace ARChess.Scripts
{
    [RequireComponent(typeof(ARRaycastManager), typeof(ARPlaneManager))]
    public class PlaceObject : MonoBehaviour
    {
    
        [SerializeField]
        private GameObject prefab;
    
        private ARRaycastManager _arRaycastManager;
        private ARPlaneManager _arPlaneManager;
        private List<ARRaycastHit> hits = new List<ARRaycastHit>();

        private void Awake()
        {
            _arRaycastManager = GetComponent<ARRaycastManager>();
            _arPlaneManager = GetComponent<ARPlaneManager>();
        }

        private void OnEnable()
        {
            EnhanceTouch.TouchSimulation.Enable();
            EnhanceTouch.EnhancedTouchSupport.Enable();
            EnhanceTouch.Touch.onFingerDown -= FingerDown;
        }

        private void OnDisable()
        {
            EnhanceTouch.TouchSimulation.Disable();
            EnhanceTouch.EnhancedTouchSupport.Disable();
        }

        private void FingerDown(EnhanceTouch.Finger finger)
        {
            // If finger is down, we don't want to call this function
            if (finger.index != 0) return;

            // Hoit on simple Plane with polygon from current finger touch screen position
            if (_arRaycastManager.Raycast(finger.currentTouch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                foreach (ARRaycastHit arRaycastHit in hits)
                {
                    // Assign pose to a variable
                    Pose pose = arRaycastHit.pose;
                    // Instantiate object with prefab using same position and rotation
                    GameObject obj = Instantiate(prefab, pose.position, pose.rotation);
                }
            }
        }
    }
}
