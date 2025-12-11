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
        public RawImageOpacityControl control;
        public TextMeshProUGUI loadingText;
        
        [Header("Controls")]
        [Range(0f, 1f)]
        public float animationSpeed = 0.5f; // Time between dot changes
        public string loadingTextString = "Loading";
        
        private int _dotCount;
        private Coroutine _ellipsisCoroutine;

        public void LoadScene(int id)
        {
            loadingText.text = loadingTextString;
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
                    loadingText.text = loadingTextString; // Remove dots
                }

                // Wait for the specified animation speed
                yield return new WaitForSeconds(animationSpeed);
            }
        }

        private IEnumerator LoadSceneAsync(int id)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(id);
            
            loadingScreen.SetActive(true);
            control.opacity = 1.0f;

            while (operation is { isDone: false })
            {
                var progress = Mathf.Clamp01(operation.progress / .9f);
                loadingBarFill.fillAmount = progress;
                yield return null;
            }

            if (operation is { isDone: true })
            {
                StopCoroutine(_ellipsisCoroutine);
            }
        }
    }
}
