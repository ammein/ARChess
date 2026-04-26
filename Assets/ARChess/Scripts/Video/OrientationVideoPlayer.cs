using UnityEngine;
using UnityEngine.Video;

namespace ARChess.Scripts.Video
{
    public class OrientationVideoPlayer : MonoBehaviour
    {
        private VideoPlayer _video;

        private void Awake()
        {
            _video = GetComponent<VideoPlayer>();
            if (GetComponent<VideoPlayer>() == null)
            {
                Debug.LogError("VideoPlayer is not attached to a VideoPlayer.");
            }
        }
        
        private void Update()
        {
            if (!_video) return;
            if (Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight )
            {
                _video.aspectRatio = VideoAspectRatio.Stretch;
            }
            else
            {
                _video.aspectRatio = VideoAspectRatio.NoScaling;
            }
        }
    }
}
