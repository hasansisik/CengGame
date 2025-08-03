#if UNITY_EDITOR
using UnityEngine;

namespace Harfpoly.EditorCode
{
    public class EditorSettings : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.targetFrameRate != 60)
            {
                Application.targetFrameRate = 60;
            }
        }
    }
}
#endif
