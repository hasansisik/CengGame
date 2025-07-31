using System;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

namespace Harfpoly.Gameplay
{
    // Taþ türü (zar sonuçlarýyla eþleþir)
    public enum PieceKind { Square, Triangle, X, Circle }

    public class Piece : MonoBehaviour
    {
        [SerializeField, Min(0)] private float moveDuration = 1f, hopHeight = 1f;
        [SerializeField, Min(0)] private int startingX, startingY;

        [HideInInspector] public int x = 0, y = 0;

        [Header("Piece Kind")]
        [SerializeField] public PieceKind kind = PieceKind.Square;

        [Header("Selection Color")]
        [SerializeField] private Color selectedColor = Color.yellow;

        // Bu taþtaki tüm renderer/mat renklerini tutar (geri döndürebilmek için)
        private struct MatSlot
        {
            public Material mat;
            public string colorProp; // "_BaseColor" (URP) veya "_Color" (Built-in/Standard)
            public Color original;
        }
        private readonly List<MatSlot> _matSlots = new();

        private readonly Vector2[,] _gridPositions = new Vector2[8, 4]
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

            // Bu taþ (ve çocuklarý) üzerindeki tüm Renderer'lardan materyal örneklerini al
            var renderers = GetComponentsInChildren<Renderer>(true);
            _matSlots.Clear();
            foreach (var r in renderers)
            {
                var mats = r.materials; // her birini instancelýyoruz
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    string prop = m.HasProperty("_BaseColor") ? "_BaseColor"
                                : m.HasProperty("_Color") ? "_Color"
                                : null;

                    if (prop == null) continue; // renksiz shader ise atla

                    _matSlots.Add(new MatSlot
                    {
                        mat = m,
                        colorProp = prop,
                        original = m.GetColor(prop)
                    });
                }
            }
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
        public void MoveRightCross() // <-- DÜZELDÝ (boþluk yok)
        {
            if (y > 0 && x < _gridPositions.GetLength(0) - 1)
            {
                y--;
                x++;
                MoveTo(_gridPositions[x, y]);
            }
        }

        public void MoveTo(Vector2 targetGridPosition)
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
            moveSequence.Append(
                transform.DOPath(new[] { mid, targetPosition }, moveDuration, PathType.CatmullRom)
                         .SetEase(Ease.InOutSine)
            );
        }

        // ---- Seçim / Deselect ----
        public void SelectPiece()
        {
            for (int i = 0; i < _matSlots.Count; i++)
            {
                var s = _matSlots[i];
                s.mat.SetColor(s.colorProp, selectedColor);
            }
        }

        public void DeselectPiece()
        {
            for (int i = 0; i < _matSlots.Count; i++)
            {
                var s = _matSlots[i];
                s.mat.SetColor(s.colorProp, s.original);
            }
        }

        public Vector2 GetGridPosition(int i, int j)
        {
            return _gridPositions[i, j];
        }
    }
}
