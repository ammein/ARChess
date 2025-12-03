using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class OrientationVideoPlayer : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        VideoPlayer video =  GetComponent<VideoPlayer>();
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
