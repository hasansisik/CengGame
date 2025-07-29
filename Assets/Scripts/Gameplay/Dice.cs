using System;
using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Harfpoly.Gameplay
{
    public class Dice : MonoBehaviour
    {
        [SerializeField] private Rigidbody diceRb;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float force = 10f;
        [SerializeField] private float torqueAmount = 5f;
        
        private IEnumerator _checkDiceResultCoroutine;
        
        [Button("Throw Dice")]
        public void ThrowDice()
        {
            if (_checkDiceResultCoroutine != null)
            {
                StopCoroutine(_checkDiceResultCoroutine);
            }
            
            _checkDiceResultCoroutine = CheckDiceResult();
            StartCoroutine(_checkDiceResultCoroutine);
        }

        private IEnumerator CheckDiceResult()
        {
            diceRb.isKinematic = false;
            diceRb.transform.SetPositionAndRotation(firePoint.position, Random.rotation);
            diceRb.linearVelocity = Vector3.zero;
            diceRb.angularVelocity = Vector3.zero;
            diceRb.AddForce(firePoint.forward * force, ForceMode.Impulse);
            diceRb.AddTorque(Random.insideUnitSphere * torqueAmount, ForceMode.Impulse);
            yield return new WaitForSeconds(1f);
            var timer = 0f;
            while (timer < 10f)
            {
                if (diceRb.IsSleeping() || (diceRb.angularVelocity.magnitude < 0.01f && diceRb.linearVelocity.magnitude < 0.01f))
                {
                    Debug.Log($"Sonuc: {GetTopFace()}");
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }
            
            diceRb.linearVelocity = Vector3.zero;
            diceRb.angularVelocity = Vector3.zero;
            diceRb.isKinematic = true;
            Debug.Log($"Sonuc: {GetTopFace()} (T)");
        }
        
        private string GetTopFace()
        {
            var dice = diceRb.transform;
            var faceNormals = new[]
            {
                dice.up,
                -dice.up,
                dice.forward,
                -dice.forward,
                dice.right,
                -dice.right
            };

            var bestFace = 0;
            var maxDot = -1f;

            for (int i = 0; i < faceNormals.Length; i++)
            {
                var dot = Vector3.Dot(faceNormals[i], Vector3.up);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestFace = i;
                }
            }

            return ((DiceFaces)(bestFace + 1)).ToString();
        }

        private void OnDisable()
        {
            if (_checkDiceResultCoroutine != null)
            {
                StopCoroutine(_checkDiceResultCoroutine);
            }
        }
    }

    [Serializable]
    public enum DiceFaces
    {
        Daire = 1,
        Kare = 2,
        X = 3,
        Ucgen = 4,
        G = 5,
        P = 6,
    }
}
