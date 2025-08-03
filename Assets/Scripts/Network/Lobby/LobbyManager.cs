using UnityEngine;

namespace Harfpoly.Network.Lobby
{
    public class LobbyManager : MonoBehaviour
    {
        private void Awake()
        {
            
        }

        // Method to handle create game button click (goes to game scene)
        public void CreateGame()
        {
            LoadSceneIfExists(nameof(Harfpoly.Scenes.Ceng));
        }

        // Method to go back to menu
        public void GoBackToMenu()
        {
            LoadSceneIfExists(nameof(Harfpoly.Scenes.CengMenu));
        }

        // Method to join game
        public void JoinGame()
        {
            LoadSceneIfExists(nameof(Harfpoly.Scenes.Ceng));
        }

        private void LoadSceneIfExists(string sceneName)
        {
            try
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Scene '{sceneName}' could not be loaded. Make sure it's added to Build Settings. Error: {e.Message}");
            }
        }
    }
}
