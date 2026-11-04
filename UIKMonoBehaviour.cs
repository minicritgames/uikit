using UnityEngine;

namespace UIKit
{
    /// <summary>
    /// Adds the OnPreConstruct and OnPreDestroy events. Inherits from MonoBehaviour
    /// </summary>
    public class UIKMonoBehaviour : MonoBehaviour
    {
        protected virtual void Awake()
        {
            OnPreConstruct(false);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            OnPreConstruct(true);
        }
#endif // UNITY_EDITOR
        
        protected virtual void OnDestroy()
        {
            OnPreDestroy();
        }
        

        /// <summary>
        /// Called on Awake and OnValidate
        /// </summary>
        protected virtual void OnPreConstruct(bool _isOnValidate)
        {
        }

        /// <summary>
        /// Called on OnDestroy
        /// </summary>
        protected virtual void OnPreDestroy()
        {
        }


        public UIKCanvas GetCanvas()
        {
            return GetComponentInParent<UIKCanvas>(true);
        }

        public bool IsOnTopScreen()
        {
            UIKCanvas canvas = GetCanvas();
            UIKScreen screen = GetComponentInParent<UIKScreen>(true);
            return !canvas
                || !screen
                || !canvas.topScreen
                || canvas.topScreen == screen;
        }

        public UIKPlayer GetOwningPlayer()
        {
            if (GetCanvas() is UIKCanvas canvas)
            {
                return canvas.GetOwningPlayer();
            }

            return null;
        }
    }
} // UIKit namespace
