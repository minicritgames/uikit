using TMPro;
using UnityEngine;

namespace UIKit
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIKTooltip : MonoBehaviour
    {
        [SerializeField] protected TMP_Text text;

        public RectTransform rectTransform => (RectTransform)transform;


        protected virtual void Awake()
        {
            // The tooltip often overlaps its own button, and catching the pointer would unhover it
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }


        public virtual void SetText(string _text)
        {
            if (text)
            {
                text.SetText(_text);
            }
        }
    }
} // UIKit namespace
