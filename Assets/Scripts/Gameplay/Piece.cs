using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace Harfpoly.Gameplay
{
    public class Piece : MonoBehaviour
    {
        [SerializeField, Min(0)] private float moveDuration = 1f, hopHeight = 1f;
        [SerializeField, Min(0)] private int startingX, startingY;
        
        [HideInInspector] public int x = 0, y = 0;
        
        private readonly Vector2[,] _gridPositions = new Vector2[8, 4] // TODO: GameManager'a alınacak. Ayrıca sahneden referans ile yapılabilir.
        {
            { new(4.05f, -9.41f), new(1.31f, -9.41f), new(-1.33f, -9.41f), new(-3.95f, -9.41f) },
            { new(4.05f, -6.89f), new(1.31f, -6.89f), new(-1.33f, -6.89f), new(-3.95f, -6.89f) },
            { new(4.05f, -4.17f), new(1.31f, -4.17f), new(-1.33f, -4.17f), new(-3.95f, -4.17f) },
            { new(4.05f, -1.41f), new(1.31f, -1.41f), new(-1.33f, -1.41f), new(-3.95f, -1.41f) },
            { new(4.05f, 1.65f), new(1.31f, 1.65f), new(-1.33f, 1.65f), new(-3.95f, 1.65f) },
            { new(4.05f, 4.18f), new(1.31f, 4.18f), new(-1.33f, 4.18f), new(-3.95f, 4.18f) },
            { new(4.05f, 6.89f), new(1.31f, 6.89f), new(-1.33f, 6.89f), new(-3.95f, 6.89f) },
            { new(4.05f, 9.46f), new(1.31f, 9.46f), new(-1.33f, 9.46f), new(-3.95f, 9.46f) }
        };

        private void Awake()
        {
            x = startingX;
            y = startingY;
        }

        [Button("Move One Front")]
        public void MoveOneFront()
        {
            if (x < _gridPositions.GetLength(0) - 1)
            {
                x++;
                MoveTo(_gridPositions[x, y]);
            }
        }
        
        [Button("Move Two Front")]
        public void MoveTwoFront()
        {
            if (x < _gridPositions.GetLength(0) - 2)
            {
                x += 2;
                MoveTo(_gridPositions[x, y]);
            }
        }
        
        [Button("Move Left Cross")]
        public void MoveLeftCross()
        {
            if (y < _gridPositions.GetLength(1) - 1 && x < _gridPositions.GetLength(0) - 1)
            {
                y++;
                x++;
                MoveTo(_gridPositions[x, y]);
            }
        }
        
        [Button("Move Right Cross")]
        public void MoveRightCross()
        {
            if (y > 0 && x < _gridPositions.GetLength(0) - 1)
            {
                y--;
                x++;
                MoveTo(_gridPositions[x, y]);
            }
        }

        private void MoveTo(Vector2 targetGridPosition)
        {
            var targetPosition = new Vector3(
                targetGridPosition.x,
                transform.position.y,
                targetGridPosition.y
            );
            
            var mid = new Vector3(
                (transform.position.x + targetPosition.x) / 2f,
                Mathf.Max(transform.position.y, targetPosition.y) + hopHeight,
                (transform.position.z + targetPosition.z) / 2f
            );

            var moveSequence = DOTween.Sequence();
            moveSequence.Append(transform.DOPath(new[] { mid, targetPosition }, moveDuration, PathType.CatmullRom).SetEase(Ease.InOutSine));
        }
    }
}
