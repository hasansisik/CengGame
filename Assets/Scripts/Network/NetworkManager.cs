using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Harfpoly.Network
{
    public class NetworkManager : MonoBehaviourPun, IConnectionCallbacks
    {
        [Header("Settings")]
        public string gameVersion = "1.0";
        
        private static NetworkManager instance;
        public static NetworkManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<NetworkManager>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                // PhotonNetwork ayarları
                PhotonNetwork.AutomaticallySyncScene = true;
                PhotonNetwork.GameVersion = gameVersion;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ConnectToPhoton();
        }

        public void ConnectToPhoton()
        {
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("Connecting to Photon...");
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        public void CreateRoom(string roomName)
        {
            if (PhotonNetwork.IsConnected)
            {
                RoomOptions roomOptions = new RoomOptions()
                {
                    MaxPlayers = 2, // Dama için 2 oyuncu
                    IsVisible = true,
                    IsOpen = true
                };

                PhotonNetwork.CreateRoom(roomName, roomOptions);
                Debug.Log($"Creating room: {roomName}");
            }
            else
            {
                Debug.LogError("Not connected to Photon!");
            }
        }

        public void JoinRoom(string roomName)
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.JoinRoom(roomName);
                Debug.Log($"Joining room: {roomName}");
            }
            else
            {
                Debug.LogError("Not connected to Photon!");
            }
        }

        public void LeaveRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }

        // IConnectionCallbacks
        public void OnConnected()
        {
            Debug.Log("Connected to Photon");
        }

        public void OnConnectedToMaster()
        {
            Debug.Log("Connected to Photon Master Server");
            PhotonNetwork.JoinLobby();
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogError($"Disconnected from Photon: {cause}");
        }

        public void OnRegionListReceived(RegionHandler regionHandler)
        {
            Debug.Log("Region list received");
        }

        public void OnCustomAuthenticationResponse(System.Collections.Generic.Dictionary<string, object> data)
        {
            Debug.Log("Custom authentication response received");
        }

        public void OnCustomAuthenticationFailed(string debugMessage)
        {
            Debug.LogError($"Custom authentication failed: {debugMessage}");
        }
    }
}