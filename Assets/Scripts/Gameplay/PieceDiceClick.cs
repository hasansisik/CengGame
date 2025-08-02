using UnityEngine;

public class PieceDiceClick : MonoBehaviour
{
    public BoardManager boardManager; // Inspector'dan atanacak

    private void OnMouseDown()
    {
        if (boardManager != null)
        {
            boardManager.SendMessage("TryRollDice");
        }
        else
        {
            Debug.LogWarning("[PieceDiceClick] BoardManager atanmadý!");
        }
    }
}
