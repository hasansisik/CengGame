using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace Harfpoly.Game
{
    public class MultiplayerGameManager : MonoBehaviourPun, IPunObservable, IInRoomCallbacks
    {
        [Header("UI Elements")]
        public TextMeshProUGUI currentPlayerText;
        public TextMeshProUGUI gameStatusText;
        public TextMeshProUGUI playerListText;
        public GameObject waitingPanel;

        [Header("Game Settings")]
        public int maxPlayers = 2;

        // Turn-based system
        private int currentPlayerIndex = 0;
        private bool isGameStarted = false;
        private Player[] playersArray;

        private void Start()
        {
            PhotonNetwork.AddCallbackTarget(this);
            
            if (PhotonNetwork.InRoom)
            {
                UpdatePlayerList();
                
                // Oyun başlatma kontrolü
                if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
                {
                    StartGame();
                }
                else
                {
                    ShowWaitingPanel("Waiting for another player...");
                }
            }
            else
            {
                ShowWaitingPanel("Not connected to room!");
            }
        }

        private void OnDestroy()
        {
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        private void UpdatePlayerList()
        {
            if (playerListText != null)
            {
                string playerList = "Players:\n";
                int index = 0;
                foreach (var player in PhotonNetwork.PlayerList)
                {
                    string marker = (index == currentPlayerIndex) ? "→ " : "  ";
                    playerList += $"{marker}{player.NickName} (Player {index + 1})\n";
                    index++;
                }
                playerListText.text = playerList;
            }
        }

        private void ShowWaitingPanel(string message)
        {
            if (waitingPanel != null)
            {
                waitingPanel.SetActive(true);
            }
            
            if (gameStatusText != null)
            {
                gameStatusText.text = message;
            }
        }

        private void HideWaitingPanel()
        {
            if (waitingPanel != null)
            {
                waitingPanel.SetActive(false);
            }
        }

        private void StartGame()
        {
            if (PhotonNetwork.IsMasterClient && !isGameStarted)
            {
                photonView.RPC("StartGameRPC", RpcTarget.All);
            }
        }

        [PunRPC]
        private void StartGameRPC()
        {
            isGameStarted = true;
            playersArray = PhotonNetwork.PlayerList;
            currentPlayerIndex = 0;
            
            HideWaitingPanel();
            UpdateCurrentPlayerDisplay();
            
            Debug.Log("Game started!");
        }

        private void UpdateCurrentPlayerDisplay()
        {
            if (currentPlayerText != null && playersArray != null && playersArray.Length > currentPlayerIndex)
            {
                Player currentPlayer = playersArray[currentPlayerIndex];
                currentPlayerText.text = $"Current Turn: {currentPlayer.NickName}";
                
                // Oyuncu sırasını kontrol et
                if (IsMyTurn())
                {
                    if (gameStatusText != null)
                    {
                        gameStatusText.text = "Your turn! Make your move.";
                    }
                }
                else
                {
                    if (gameStatusText != null)
                    {
                        gameStatusText.text = "Waiting for opponent's move...";
                    }
                }
            }
            
            UpdatePlayerList();
        }

        public bool IsMyTurn()
        {
            if (!isGameStarted || playersArray == null || playersArray.Length <= currentPlayerIndex)
                return false;
                
            return playersArray[currentPlayerIndex] == PhotonNetwork.LocalPlayer;
        }

        public void NextTurn()
        {
            if (PhotonNetwork.IsMasterClient && isGameStarted)
            {
                photonView.RPC("NextTurnRPC", RpcTarget.All);
            }
        }

        [PunRPC]
        private void NextTurnRPC()
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % playersArray.Length;
            UpdateCurrentPlayerDisplay();
            
            Debug.Log($"Turn changed to: {playersArray[currentPlayerIndex].NickName}");
        }

        // Hamle yapma fonksiyonu - sadece sırası olan oyuncu kullanabilir
        public bool TryMakeMove(Vector2 position)
        {
            if (!IsMyTurn())
            {
                Debug.Log("It's not your turn!");
                return false;
            }

            // Hamleyi diğer oyunculara gönder
            photonView.RPC("MakeMoveRPC", RpcTarget.Others, position.x, position.y, PhotonNetwork.LocalPlayer.ActorNumber);
            
            // Kendi hamlemizi işle
            ProcessMove(position, PhotonNetwork.LocalPlayer.ActorNumber);
            
            // Sırayı değiştir
            NextTurn();
            
            return true;
        }

        [PunRPC]
        private void MakeMoveRPC(float x, float y, int playerActorNumber)
        {
            Vector2 position = new Vector2(x, y);
            ProcessMove(position, playerActorNumber);
        }

        private void ProcessMove(Vector2 position, int playerActorNumber)
        {
            // Bu fonksiyonu oyuna özel hamle işleme mantığı ile değiştirin
            Debug.Log($"Player {playerActorNumber} made move at {position}");
            
            // Örnek: Dama hamle işleme
            // CheckersGame.Instance.ProcessMove(position, playerActorNumber);
        }

        public void LeaveGame()
        {
            PhotonNetwork.LeaveRoom();
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Veri gönder
                stream.SendNext(currentPlayerIndex);
                stream.SendNext(isGameStarted);
            }
            else
            {
                // Veri al
                currentPlayerIndex = (int)stream.ReceiveNext();
                isGameStarted = (bool)stream.ReceiveNext();
                UpdateCurrentPlayerDisplay();
            }
        }

        // IInRoomCallbacks
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"Player {newPlayer.NickName} joined the room");
            UpdatePlayerList();
            
            if (PhotonNetwork.CurrentRoom.PlayerCount >= 2 && !isGameStarted)
            {
                StartGame();
            }
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"Player {otherPlayer.NickName} left the room");
            
            if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
            {
                ShowWaitingPanel("Opponent left. Waiting for new player...");
                isGameStarted = false;
            }
            
            UpdatePlayerList();
        }

        public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            // Oda özellikleri güncellendiğinde
        }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Oyuncu özellikleri güncellendiğinde
        }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"Master client switched to: {newMasterClient.NickName}");
        }
    }
}