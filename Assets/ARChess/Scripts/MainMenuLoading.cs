using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ARChess.Scripts
{
    public class MainMenuLoading : MonoBehaviour
    {
        [Header("Elements")]
        public GameObject loadingScene;
        public GameObject mainScene;
        public VideoPlayer videoPlayer;
        public GameObject loadingBar;
        public Image loadingBarFill;
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
            if (videoPlayer != null && !videoPlayer.isPrepared)
            {
                videoPlayer.Play();
                loadingBar.GetComponent<RawImageOpacityControl>().opacity = 1f;
                StartCoroutine(CheckLoad(startValue, endValue, duration));
                StartCoroutine(AnimateEllipsis(loadingText));
            }
        }
        
        private IEnumerator AnimateEllipsis(TextMeshProUGUI text)
        {
            text.text = loadingTextString;
            while (true)
            {
                // Add dots up to 3
                string dots = new string('.', _dotCount);
                text.text += dots;

                // Increment dot count, reset after 3
                _dotCount++;
                
                // If we reach 3 dots, reset to 0 and remove the dots
                if (_dotCount > 3)
                {
                    _dotCount = 0; // Reset to 0
                    text.text = loadingTextString; // Remove dots
                }

                // Wait for the specified animation speed
                yield return new WaitForSeconds(animationSpeed);
            }
        }

        IEnumerator CheckLoad(float from, float to, float timeDuration)
        {
            videoPlayer.Pause();
            float elapsedTime = 0f;

            while (elapsedTime < timeDuration)
            {
                // Calculate the 't' value, which represents the progress from 0 to 1
                float t = elapsedTime / timeDuration;

                // Apply the Lerp function
                _currentValue = Mathf.Lerp(from, to, t);

                // Increment the elapsed time using Time.deltaTime for frame-rate independence
                elapsedTime += Time.deltaTime;

                // Yield control back to Unity, so the Coroutine can resume in the next frame
                yield return null;
            }
            
            // Ensure the value reaches the exact endValue at the end of the duration
            _currentValue = to;
            
            loadingBarFill.fillAmount = _currentValue;

            if (Mathf.Approximately(_currentValue, endValue) && videoPlayer.isPrepared)
            {
                loadingScene.SetActive(false);
                mainScene.SetActive(true);
                videoPlayer.Play();
            }
        }
    }
}
