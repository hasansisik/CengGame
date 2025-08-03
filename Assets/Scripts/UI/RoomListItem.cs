using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Harfpoly.UI
{
    public class RoomListItem : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI roomNameText;
        public TextMeshProUGUI playerCountText;
        public Button joinButton;

        private string roomName;

        public void Setup(string roomName, int currentPlayers, int maxPlayers, System.Action<string> onJoinClicked)
        {
            this.roomName = roomName;
            
            if (roomNameText != null)
            {
                roomNameText.text = roomName;
            }
            
            if (playerCountText != null)
            {
                playerCountText.text = $"{currentPlayers}/{maxPlayers}";
            }
            
            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(() => onJoinClicked?.Invoke(roomName));
                
                // Oda doluysa butonu deaktif et
                joinButton.interactable = currentPlayers < maxPlayers;
            }
        }
    }
}