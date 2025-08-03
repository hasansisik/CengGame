using UnityEngine;
using UnityEngine.UI;

namespace Harfpoly.UI
{
    public class UIScaler : MonoBehaviour
    {
        [SerializeField] private float minimumRatio = 1.5f;

        private void OnEnable()
        {
            var isWide = Screen.width / (float)Screen.height < minimumRatio;
            GetComponent<CanvasScaler>().matchWidthOrHeight = isWide ? 0 : 1;
        }
    }
}
