using UnityEngine;
using UnityEngine.Video;

namespace ARChess.Scripts.Video
{
    
    public class OrientationVideoPlayer : MonoBehaviour
    {
        private VideoPlayer _video;
        private VideoAspectRatio _videoAspectRatio;

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
            if (_videoAspectRatio is not VideoAspectRatio.Stretch &&
                (Screen.orientation is ScreenOrientation.LandscapeLeft ||
                 Screen.orientation is ScreenOrientation.LandscapeRight) )
            {
                _video.aspectRatio = VideoAspectRatio.Stretch;
                _videoAspectRatio = VideoAspectRatio.Stretch;
            }
            else if(_videoAspectRatio is not VideoAspectRatio.NoScaling)
            {
                _video.aspectRatio = VideoAspectRatio.NoScaling;
                _videoAspectRatio = VideoAspectRatio.NoScaling;
            }
        }
    }
}
