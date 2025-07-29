using UnityEngine;
using UnityEngine.UI;

namespace Harfpoly.UI
{
    public class ButtonAnimation : MonoBehaviour
    {
        [SerializeField] private RectTransform textRectTransform;
        [SerializeField] private float textOffset = 10f;
        
        private Button _button;
        private Vector2 _initialTextPosition;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _initialTextPosition = textRectTransform.anchoredPosition;
        }

        public void OnButtonPressed()
        {
            if (!_button.interactable) return;
            textRectTransform.anchoredPosition = _initialTextPosition + new Vector2(0, textOffset);
        }

        public void OnButtonReleased()
        {
            if (!_button.interactable) return;
            textRectTransform.anchoredPosition = _initialTextPosition;
        }
    }
}
