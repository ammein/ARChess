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
            EnhanceTouch.Touch.onFingerDown += FingerDown;
        }

        private void OnDisable()
        {
            EnhanceTouch.TouchSimulation.Disable();
            EnhanceTouch.EnhancedTouchSupport.Disable();
            EnhanceTouch.Touch.onFingerDown -= FingerDown;
        }

        private void FingerDown(EnhanceTouch.Finger finger)
        {
            // If finger is down, we don't want to call this function
            if (finger.index != 0) return;

            if (_arRaycastManager.Raycast(finger.currentTouch.screenPosition, hits,
                    TrackableType.PlaneWithinPolygon))
            {
                ClonePrefab();   
            }
        }

        private void ClonePrefab()
        {
            // Hoit on simple Plane with polygon from current finger touch screen position
            foreach (ARRaycastHit hit in hits)
            {
                // Assign pose to a variable
                Pose pose = hit.sessionRelativePose;
                
                // Instantiate object with prefab using same position and rotation
                GameObject obj = Instantiate(prefab, pose.position, pose.rotation);

                if (_arPlaneManager.GetPlane(hit.trackableId).alignment == PlaneAlignment.HorizontalUp)
                {
                    // Rotate obj towards our camera so that it would be more realistic
                    Vector3 position = obj.transform.position;
                    // Rotate only on x & y axis. Therefore, set Y to 0 so that it will be 0 when targetRotation quaternion multiplies
                    position.y = 0f;
                    Vector3 cameraPosition = Camera.main.transform.position;
                    cameraPosition.y = 0f;
                    Vector3 direction = cameraPosition - position;
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    obj.transform.rotation = targetRotation;
                }
            }
        }
    }
}
