using UnityEngine;

namespace Harfpoly.UI
{
    public class MenuManager : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
        }
        
        public void StartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nameof(Scenes.Lobby));
        }

        public void QuitGame()
        {
            Application.Quit();
        }
    }
}
