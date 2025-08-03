using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;
using Harfpoly.UI;

namespace Harfpoly.Network.Lobby
{
    public class LobbyManager : MonoBehaviourPun, ILobbyCallbacks, IMatchmakingCallbacks
    {
        [Header("UI Elements")]
        public TMP_InputField roomNameInput;
        public Transform roomListParent;
        public GameObject roomListItemPrefab;
        public Button createRoomButton;
        public Button joinRandomRoomButton;
        public TextMeshProUGUI statusText;

        [Header("Room Settings")]
        public int maxPlayersPerRoom = 2;

        private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

        private void Awake()
        {
            // Photon callbacks'leri kaydet
            PhotonNetwork.AddCallbackTarget(this);
        }

        private void Start()
        {
            // UI elemanları varsa event listener'ları ekle
            if (createRoomButton != null)
                createRoomButton.onClick.AddListener(CreateRoom);
            
            if (joinRandomRoomButton != null)
                joinRandomRoomButton.onClick.AddListener(JoinRandomRoom);

            // NetworkManager'ın başlatılmasını bekle
            if (NetworkManager.Instance == null)
            {
                // NetworkManager yoksa oluştur
                GameObject networkManagerGO = new GameObject("NetworkManager");
                networkManagerGO.AddComponent<NetworkManager>();
            }

            UpdateStatusText("Connected to lobby");
        }

        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        // Method to handle create game button click
        public void CreateGame()
        {
            string roomName = GenerateRoomName();
            CreateRoomWithName(roomName);
        }

        public void CreateRoom()
        {
            string roomName = roomNameInput != null ? roomNameInput.text : GenerateRoomName();
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = GenerateRoomName();
            }
            CreateRoomWithName(roomName);
        }

        private void CreateRoomWithName(string roomName)
        {
            if (PhotonNetwork.IsConnected)
            {
                RoomOptions roomOptions = new RoomOptions()
                {
                    MaxPlayers = maxPlayersPerRoom,
                    IsVisible = true,
                    IsOpen = true
                };

                roomOptions.CustomRoomProperties = new Hashtable()
                {
                    { "GameType", "Ceng" },
                    { "CreatedAt", System.DateTime.Now.ToString() }
                };

                PhotonNetwork.CreateRoom(roomName, roomOptions);
                UpdateStatusText($"Creating room: {roomName}");
            }
            else
            {
                UpdateStatusText("Not connected to Photon!");
            }
        }

        public void JoinRandomRoom()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.JoinRandomRoom();
                UpdateStatusText("Joining random room...");
            }
            else
            {
                UpdateStatusText("Not connected to Photon!");
            }
        }

        public void JoinSpecificRoom(string roomName)
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.JoinRoom(roomName);
                UpdateStatusText($"Joining room: {roomName}");
            }
            else
            {
                UpdateStatusText("Not connected to Photon!");
            }
        }

        // Method to go back to menu
        public void GoBackToMenu()
        {
            LoadSceneIfExists(nameof(Harfpoly.Scenes.CengMenu));
        }

        // Method to join game (existing method for compatibility)
        public void JoinGame()
        {
            JoinRandomRoom();
        }

        private string GenerateRoomName()
        {
            return "Room_" + UnityEngine.Random.Range(1000, 9999);
        }

        private void UpdateStatusText(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
            Debug.Log(status);
        }

        private void UpdateRoomList()
        {
            if (roomListParent == null || roomListItemPrefab == null) return;

            // Mevcut room listesini temizle
            foreach (Transform child in roomListParent)
            {
                Destroy(child.gameObject);
            }

            // Yeni room listesini oluştur
            foreach (var roomInfo in cachedRoomList.Values)
            {
                if (roomInfo.IsOpen && roomInfo.PlayerCount < roomInfo.MaxPlayers)
                {
                    GameObject roomItem = Instantiate(roomListItemPrefab, roomListParent);
                    
                    // RoomListItem script'ini kullan
                    var roomListItemScript = roomItem.GetComponent<RoomListItem>();
                    if (roomListItemScript != null)
                    {
                        roomListItemScript.Setup(
                            roomInfo.Name, 
                            roomInfo.PlayerCount, 
                            roomInfo.MaxPlayers, 
                            JoinSpecificRoom
                        );
                    }
                    else
                    {
                        // Fallback: Eğer RoomListItem script'i yoksa manuel setup
                        var roomButton = roomItem.GetComponent<Button>();
                        var roomTexts = roomItem.GetComponentsInChildren<TextMeshProUGUI>();
                        
                        // İlk text → Oda ismi
                        if (roomTexts.Length > 0 && roomTexts[0] != null)
                        {
                            roomTexts[0].text = roomInfo.Name;
                        }
                        
                        // İkinci text → Oyuncu sayısı
                        if (roomTexts.Length > 1 && roomTexts[1] != null)
                        {
                            roomTexts[1].text = $"{roomInfo.PlayerCount}/{roomInfo.MaxPlayers}";
                        }
                        
                        if (roomButton != null)
                        {
                            string roomName = roomInfo.Name;
                            roomButton.onClick.RemoveAllListeners();
                            roomButton.onClick.AddListener(() => JoinSpecificRoom(roomName));
                            
                            // Oda doluysa butonu deaktif et
                            roomButton.interactable = roomInfo.PlayerCount < roomInfo.MaxPlayers;
                        }
                    }
                }
            }
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

        // ILobbyCallbacks
        public void OnJoinedLobby()
        {
            UpdateStatusText("Joined lobby successfully");
        }

        public void OnLeftLobby()
        {
            UpdateStatusText("Left lobby");
        }

        public void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            foreach (RoomInfo roomInfo in roomList)
            {
                if (roomInfo.RemovedFromList)
                {
                    cachedRoomList.Remove(roomInfo.Name);
                }
                else
                {
                    cachedRoomList[roomInfo.Name] = roomInfo;
                }
            }
            UpdateRoomList();
        }

        public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
        {
            // Lobby istatistikleri güncellendiğinde
        }

        // IMatchmakingCallbacks
        public void OnFriendListUpdate(List<FriendInfo> friendList)
        {
            // Arkadaş listesi güncellendiğinde
        }

        public void OnCreatedRoom()
        {
            UpdateStatusText($"Room created successfully: {PhotonNetwork.CurrentRoom.Name}");
        }

        public void OnCreateRoomFailed(short returnCode, string message)
        {
            UpdateStatusText($"Failed to create room: {message}");
        }

        public void OnJoinedRoom()
        {
            UpdateStatusText($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
            
            // Oyun sahnesine geç
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(nameof(Harfpoly.Scenes.Ceng));
            }
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            UpdateStatusText($"Failed to join room: {message}");
        }

        public void OnJoinRandomFailed(short returnCode, string message)
        {
            UpdateStatusText($"No random room available. Creating new room...");
            CreateGame(); // Rastgele oda bulunamadıysa yeni oda oluştur
        }

        public void OnLeftRoom()
        {
            UpdateStatusText("Left room");
        }
    }
}