using UnityEngine;
using Photon.Pun;

namespace Harfpoly.Game
{
    public class NetworkedGamePiece : MonoBehaviourPun, IPunObservable
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        
        private Vector3 networkPosition;
        private bool isMoving = false;
        private MultiplayerGameManager gameManager;

        private void Start()
        {
            gameManager = FindObjectOfType<MultiplayerGameManager>();
            networkPosition = transform.position;
        }

        private void Update()
        {
            // Pozisyon senkronizasyonu
            if (!photonView.IsMine)
            {
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * moveSpeed);
            }
        }

        private void OnMouseDown()
        {
            // Sadece kendi sıran geldiğinde hareket edebilirsin
            if (gameManager != null && gameManager.IsMyTurn() && photonView.IsMine)
            {
                Debug.Log("Piece selected - it's your turn!");
                // Buraya seçim mantığını ekleyebilirsiniz
            }
            else if (gameManager != null && !gameManager.IsMyTurn())
            {
                Debug.Log("It's not your turn!");
            }
            else if (!photonView.IsMine)
            {
                Debug.Log("This piece doesn't belong to you!");
            }
        }

        public void MoveTo(Vector3 targetPosition)
        {
            if (gameManager != null && gameManager.IsMyTurn() && photonView.IsMine)
            {
                // Hamleyi RPC ile diğer oyunculara gönder
                photonView.RPC("RPC_MovePiece", RpcTarget.Others, targetPosition.x, targetPosition.y, targetPosition.z);
                
                // Kendi pozisyonunu güncelle
                transform.position = targetPosition;
                networkPosition = targetPosition;
                
                // Sırayı değiştir
                gameManager.NextTurn();
            }
        }

        [PunRPC]
        private void RPC_MovePiece(float x, float y, float z)
        {
            Vector3 newPosition = new Vector3(x, y, z);
            networkPosition = newPosition;
            
            if (!photonView.IsMine)
            {
                transform.position = newPosition;
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Pozisyon verilerini gönder
                stream.SendNext(transform.position);
                stream.SendNext(isMoving);
            }
            else
            {
                // Pozisyon verilerini al
                networkPosition = (Vector3)stream.ReceiveNext();
                isMoving = (bool)stream.ReceiveNext();
            }
        }
    }
}