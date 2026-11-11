using UnityEngine;

namespace UIKit
{
    [CreateAssetMenu(menuName = "UIKit/Screen Transition", fileName = "ScreenTransition")]
    public class UIKScreenTransition : ScriptableObject
    {
        public UIKScreenMotion outgoing = new();
        public UIKScreenMotion incoming = new();
        // Seconds from the start of the outgoing motion; a sequence waits out the outgoing motion plus a pause
        public float incomingDelay;

        public static UIKScreenTransition instant
        {
            get
            {
                if (!__instant)
                {
                    __instant = CreateInstance<UIKScreenTransition>();
                    __instant.name = "Instant";
                    __instant.hideFlags = HideFlags.DontSave;
                }

                return __instant;
            }
        }

        private static UIKScreenTransition __instant;
    }
} // UIKit namespace
