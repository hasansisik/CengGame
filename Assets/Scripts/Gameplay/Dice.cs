using System;
using System.Collections;
using System.Text;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Harfpoly.Gameplay
{
    /// <summary>
    /// Zar yüzleri:
    /// 1 = Daire, 2 = Kare, 3 = X, 4 = Ucgen, 5 = G (Gülen), 6 = P (Pass)
    /// </summary>
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

    public class Dice : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] private Rigidbody diceRb;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float force = 10f;
        [SerializeField] private float torqueAmount = 5f;

        [Header("Face Map (Up, Down, Forward, Back, Right, Left)")]
        [Tooltip("Zar modelindeki yüzleri SIRAYLA: Up, Down, Forward, Back, Right, Left normallerine göre eþle.")]
        [SerializeField]
        public DiceFaces[] faceMap = new DiceFaces[6]
        {
            DiceFaces.Daire,  // Up
            DiceFaces.Kare,   // Down (-Up)
            DiceFaces.X,      // Forward
            DiceFaces.Ucgen,  // Back (-Forward)
            DiceFaces.G,      // Right
            DiceFaces.P       // Left (-Right)
        };

        [Header("Mode")]
        [SerializeField] private bool usePhysics = true;     // false => RNG (tam eþit olasýlýk)

        [Header("Stats (optional)")]
        [SerializeField] private bool collectStats = false;
        private readonly int[] counts = new int[6];
        private int totalRolls = 0;

        /// <summary>Zar sonuçlandýðýnda yayýnlanýr.</summary>
        public event Action<DiceFaces> OnRolled;

        private IEnumerator _checkDiceResultCoroutine;
        private bool _isRolling;
        public bool IsRolling => _isRolling;

        // ------------- PUBLIC API -------------

        [Button("Throw Dice")]
        public void ThrowDice()
        {
            if (_isRolling) return;

            // RNG modu: tamamen eþit olasýlýk (1/6)
            if (!usePhysics)
            {
                var face = (DiceFaces)UnityEngine.Random.Range(1, 7); // 1..6
                EmitResult(face, false);
                return;
            }

            if (_checkDiceResultCoroutine != null)
                StopCoroutine(_checkDiceResultCoroutine);

            _checkDiceResultCoroutine = CheckDiceResult();
            StartCoroutine(_checkDiceResultCoroutine);
        }

        [Button("Print Stats")]
        public void PrintStats()
        {
            if (!collectStats)
            {
                Debug.Log("[Dice] Stats kapalý. Inspector'da 'collectStats' iþaretle.");
                return;
            }
            if (totalRolls == 0) { Debug.Log("[Dice] Henüz atýþ yok."); return; }

            string[] names = { "Daire", "Kare", "X", "Ucgen", "G", "P" };
            var sb = new StringBuilder();
            sb.AppendLine($"[Dice] Toplam {totalRolls} atýþ:");
            for (int i = 0; i < 6; i++)
            {
                float pct = 100f * counts[i] / Mathf.Max(1, totalRolls);
                sb.AppendLine($" - {names[i]}: {counts[i]} ({pct:0.0}%)");
            }
            Debug.Log(sb.ToString());
        }

        [Button("Reset Stats")]
        public void ResetStats()
        {
            for (int i = 0; i < 6; i++) counts[i] = 0;
            totalRolls = 0;
            Debug.Log("[Dice] Ýstatistikler sýfýrlandý.");
        }

        [Button("Debug: Print Current Top")]
        public void DebugPrintCurrentTop()
        {
            if (diceRb == null)
            {
                Debug.LogWarning("[Dice] Rigidbody yok.");
                return;
            }
            int idx = GetTopIndex();
            var face = GetTopFace();
            Debug.Log($"[Dice Debug] TopIndex={idx} (0=Up,1=Down,2=Forward,3=Back,4=Right,5=Left) -> {face}");
        }

        // ------------- INTERNAL -------------

        private IEnumerator CheckDiceResult()
        {
            _isRolling = true;

            if (diceRb == null || firePoint == null)
            {
                Debug.LogError("[Dice] Rigidbody veya FirePoint atanmamýþ!");
                _isRolling = false;
                yield break;
            }

            diceRb.isKinematic = false;
            diceRb.transform.SetPositionAndRotation(firePoint.position, Random.rotation);

            // Sende linearVelocity kullanýlýyordu; onu koruyorum.
            diceRb.linearVelocity = Vector3.zero;
            diceRb.angularVelocity = Vector3.zero;

            diceRb.AddForce(firePoint.forward * force, ForceMode.Impulse);
            diceRb.AddTorque(Random.insideUnitSphere * torqueAmount, ForceMode.Impulse);

            // minimum bekleme (zýplama bitsin)
            yield return new WaitForSeconds(1f);

            float timer = 0f;
            while (timer < 10f)
            {
                bool almostStopped = diceRb.angularVelocity.sqrMagnitude < 0.0001f
                                  && diceRb.linearVelocity.sqrMagnitude < 0.0001f;

                if (diceRb.IsSleeping() || almostStopped)
                {
                    var face = GetTopFace();
                    EmitResult(face, false);
                    _isRolling = false;
                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            // timeout: zorla durdur ve oku
            diceRb.linearVelocity = Vector3.zero;
            diceRb.angularVelocity = Vector3.zero;
            diceRb.isKinematic = true;

            var forced = GetTopFace();
            EmitResult(forced, true);

            _isRolling = false;
        }

        private void EmitResult(DiceFaces face, bool viaTimeout)
        {
            if (collectStats)
            {
                counts[(int)face - 1]++;
                totalRolls++;
            }

            Debug.Log(viaTimeout ? $"[Dice] Sonuç (timeout): {face}" : $"[Dice] Sonuç: {face}");
            OnRolled?.Invoke(face);
        }

        private int GetTopIndex()
        {
            var t = diceRb.transform;
            Vector3[] normals =
            {
                t.up,
                -t.up,
                t.forward,
                -t.forward,
                t.right,
                -t.right
            };

            int best = 0;
            float maxDot = -1f;
            for (int i = 0; i < normals.Length; i++)
            {
                float dot = Vector3.Dot(normals[i], Vector3.up);
                if (dot > maxDot) { maxDot = dot; best = i; }
            }
            return best; // 0=Up,1=Down,2=Forward,3=Back,4=Right,5=Left
        }

        private DiceFaces GetTopFace()
        {
            int idx = GetTopIndex();
            if (faceMap != null && faceMap.Length == 6)
                return faceMap[idx];

            // Emniyet: map yoksa sabit sýra
            return (DiceFaces)(idx + 1);
        }

        private void OnDisable()
        {
            if (_checkDiceResultCoroutine != null)
                StopCoroutine(_checkDiceResultCoroutine);

            _isRolling = false;
        }
    }
}
