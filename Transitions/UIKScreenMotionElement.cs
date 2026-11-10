using UnityEngine;

namespace UIKit
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIKScreenMotionElement : MonoBehaviour
    {
        [SerializeField] public string group;

        public UIKScreenMotionBody body
        {
            get
            {
                __body ??= new UIKScreenMotionBody((RectTransform)transform, GetComponent<CanvasGroup>());
                return __body;
            }
        }

        private UIKScreenMotionBody __body;
    }
} // UIKit namespace
