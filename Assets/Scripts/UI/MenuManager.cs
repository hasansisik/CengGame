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
            UnityEngine.SceneManagement.SceneManager.LoadScene(nameof(Scenes.CengStart));
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        // Navigation methods for the new scene flow
        public void GoToCengMenu()
        {
            LoadSceneIfExists(nameof(Scenes.CengMenu));
        }

        private void LoadSceneIfExists(string sceneName)
        {
            try
            {
                // Try loading by name first
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Scene '{sceneName}' could not be loaded. Make sure it's added to Build Settings. Error: {e.Message}");
                
                // Alternative: Try loading by path
                try
                {
                    string scenePath = $"Assets/Scenes/{sceneName}.unity";
                    Debug.Log($"Trying to load scene by path: {scenePath}");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(scenePath);
                }
                catch (System.Exception pathError)
                {
                    Debug.LogError($"Could not load scene by path either: {pathError.Message}");
                }
            }
        }

        public void GoToCengLobi()
        {
            LoadSceneIfExists(nameof(Scenes.CengLobi));
        }

        public void GoToCeng()
        {
            LoadSceneIfExists(nameof(Scenes.Ceng));
        }

        public void GoToCengStart()
        {
            LoadSceneIfExists(nameof(Scenes.CengStart));
        }

        // Method to handle multiplayer button click (goes to lobby)
        public void StartMultiplayer()
        {
            GoToCengLobi();
        }

        // Method to handle create game button click (goes to game scene)
        public void CreateGame()
        {
            GoToCeng();
        }
    }
}
