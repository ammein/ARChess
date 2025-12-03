using UnityEngine;
using UnityEngine.SceneManagement;

namespace ARChess.Scripts
{
    public class MainMenu : MonoBehaviour
    {
        public void StartGame()
        {
            SceneManager.LoadScene("ARScene");
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
