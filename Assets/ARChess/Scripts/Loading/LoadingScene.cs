using System.Collections;
using ARChess.Scripts.Image;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARChess.Scripts.Loading
{
    public class LoadingScene : MonoBehaviour
    {
        [Header("Elements")]
        public GameObject loadingScreen;
        public UnityEngine.UI.Image loadingBarFill;
        public RawImageOpacityControl backgroundOpacityControl;
        public TextMeshProUGUI loadingText;
        
        [Header("Controls")]
        [SerializeField]
        [Tooltip("Delay in seconds before loading scene")]
        private float beforeLoadDelay = 1.5f;
        [SerializeField]
        [Tooltip("Delay in seconds after loading scene")]
        private float afterLoadDelay = 1.5f;
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Animation speed between ellipsis text changes")]
        private float animationDotSpeed = 0.5f; // Time between dot changes
        public string loadingTextString = "Loading";
        public string enteringTextString = "Starting";
        
        private int _dotCount;
        private Coroutine _ellipsisCoroutine;
        private string _textLoadingState;

        public void LoadScene(int id)
        {
            loadingScreen.SetActive(true);
            backgroundOpacityControl.opacity = 1.0f;
            _textLoadingState = loadingTextString;
            StartCoroutine(LoadSceneAsync(id));
            _ellipsisCoroutine = StartCoroutine(AnimateEllipsis());
        }

        private IEnumerator AnimateEllipsis()
        {
            while (true)
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
                    loadingText.text = _textLoadingState; // Remove dots
                }

                // Wait for the specified animation speed
                yield return new WaitForSeconds(animationDotSpeed);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private IEnumerator LoadSceneAsync(int id)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(id);
            if (operation != null)
            {
                // https://docs.unity3d.com/ScriptReference/AsyncOperation-allowSceneActivation.html
                operation.allowSceneActivation = false;

                while (operation is { isDone: false })
                {
                    loadingText.text = loadingTextString;
                    var progress = Mathf.Clamp01(operation.progress / .9f);
                    loadingBarFill.fillAmount = progress;

                    if (loadingBarFill.fillAmount >= .9f)
                    {
                        yield return new WaitForSeconds(beforeLoadDelay);
                        _textLoadingState = enteringTextString;
                        yield return new WaitForSeconds(afterLoadDelay);
                        operation.allowSceneActivation = true;
                        backgroundOpacityControl.opacity = 0.0f;
                        StopCoroutine(_ellipsisCoroutine);
                    }
                    yield return null;
                }
            }
            
            // https://discussions.unity.com/t/solved-fully-reset-current-scene/727611/13
            // Unload the current scene
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene().name);

            // Remove unused assets for current scene
            yield return Resources.UnloadUnusedAssets();
        }
    }
}
