using UnityEngine;
using UnityEngine.Video;

namespace ARChess.Scripts.Video
{
    public class OrientationVideoPlayer : MonoBehaviour
    {

        private void Awake()
        {
            if (GetComponent<VideoPlayer>() == null)
            {
                Debug.LogError("VideoPlayer is not attached to a VideoPlayer.");
            }
        }
        
        
        private void Update()
        {
            var video =  GetComponent<VideoPlayer>();
            if (!video) return;
            if (Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight )
            {
                video.aspectRatio = VideoAspectRatio.Stretch;
            }
            else
            {
                video.aspectRatio = VideoAspectRatio.NoScaling;
            }
        }
    }
}
