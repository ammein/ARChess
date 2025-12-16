using System.Collections;
using ARChess.Scripts.Image;
using ARChess.Scripts.Project;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace ARChess.Scripts.Loading
{
    public class MainMenuLoading : MonoBehaviour
    {
        [Header("Elements")]
        public GameObject loadingScene;
        public GameObject mainScene;
        public VideoPlayer videoPlayer;
        public GameObject loadingBar;
        public UnityEngine.UI.Image loadingBarFill;
        public TextMeshProUGUI loadingText;
        
        [Header("Animation Controls")]
        public float startValue;
        public float endValue = 1f;
        public float duration = 3f;
        [Range(0f, 1f)]
        public float animationSpeed = 0.5f; // Time between dot changes
        [SerializeField]
        [Tooltip("Loading Text String")]
        private string loadingTextString;
        
        [Header("Settings")]
        [SerializeField]
        private ProjectStateOptions _projectStateOptions;
        
        private float _currentValue;
        private int _dotCount;
        private Coroutine _ellipsisCoroutine;

        public void Awake()
        {
            loadingScene.SetActive(true);
            mainScene.SetActive(false);
        }

        public void Start()
        {
            if (videoPlayer != null && !videoPlayer.isPrepared && !_projectStateOptions.mainSceneVideoLoaded)
            {
                videoPlayer.Play();
                loadingBar.GetComponent<RawImageOpacityControl>().opacity = 1f;
                StartCoroutine(CheckLoad(startValue, endValue));
                StartCoroutine(AnimateEllipsis());
            } else if (_projectStateOptions.mainSceneVideoLoaded && loadingScene.activeSelf && !mainScene.activeSelf)
            {
                loadingScene.SetActive(false);
                mainScene.SetActive(true);
            }
        }
        
        private IEnumerator AnimateEllipsis()
        {
            loadingText.text = loadingTextString;
            while (!Mathf.Approximately(loadingBarFill.fillAmount, endValue))
            {
                // Add dots up to 3
                string dots = new string('.', _dotCount);
                loadingText.text += dots;

                // Increment dot count, reset after 3
                _dotCount++;
                
                // If we reach 3 dots, reset to 0 and remove the dots
                if (_dotCount > 3)
                {
                    _dotCount = 0; // Reset to 0
                    loadingText.text = loadingTextString; // Remove dots
                }

                // Wait for the specified animation speed
                yield return new WaitForSeconds(animationSpeed);
            }
        }

        IEnumerator CheckLoad(float from, float to)
        {
            videoPlayer.Pause();
            float elapsedTime = 0f;

            while (!videoPlayer.isPrepared)
            {
                // Increment the elapsed time using Time.deltaTime for frame-rate independence
                elapsedTime += Time.deltaTime;
                
                _currentValue = Mathf.Lerp(from, to, elapsedTime / duration);
                
                // Apply the Lerp function
                var animateTime = 0f;

                while (animateTime < duration)
                {
                    // Convert t into 0 to 1 value
                    var t = animateTime / duration;
                
                    loadingBarFill.fillAmount = t;
                
                    animateTime += Time.deltaTime;

                    yield return null;
                }
            
                // Ensure the value reaches the exact endValue at the end of the duration
                _currentValue = to;

                if (Mathf.Approximately(_currentValue, endValue) && videoPlayer.isPrepared)
                {
                    loadingBarFill.fillAmount = _currentValue;
                    loadingScene.SetActive(false);
                    mainScene.SetActive(true);
                    videoPlayer.Play();
                    _projectStateOptions.mainSceneVideoLoaded = true;
                }

                // Yield control back to Unity, so the Coroutine can resume in the next frame
                yield return null;
            }
        }
    }
}
