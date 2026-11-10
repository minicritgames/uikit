using UnityEngine;
using UnityEngine.Events;

namespace UIKit
{
    public class UIKScreenStack : UIKWidgetStack
    {
        [HideInInspector] public UnityEvent<UIKScreen> OnScreenOpened = new();
        [HideInInspector] public UnityEvent<UIKScreen> OnScreenClosed = new();


        public override UIKWidget PushToStack(string _widgetName, GameObject _widgetPrefab)
        {
            UIKWidget widget = base.PushToStack(_widgetName, _widgetPrefab);
            if (widget is UIKScreen screen)
            {
                OnScreenOpened.Invoke(screen);
            }

            return widget;
        }

        protected override bool IsLeaving(UIKWidget _widget)
        {
            return _widget is UIKScreen screen
                && screen.isClosing;
        }

        protected override void RemoveWidget(UIKWidget _widget)
        {
            if (_widget is UIKScreen screen)
            {
                screen.MarkClosing();
                OnScreenClosed.Invoke(screen);
                return;
            }

            base.RemoveWidget(_widget);
        }

        protected override void UpdateWidgetsInStack()
        {
            base.UpdateWidgetsInStack();

            if (widgetsInStack.Count > 0)
            {
                UIKScreenLowerScreenVisibility visibility = UIKScreenLowerScreenVisibility.ShowAll;

                // Top screen should always be visible
                if (widgetsInStack[^1] is UIKScreen topScreen)
                {
                    if (topScreen.GetCanvasGroup() is CanvasGroup canvasGroup)
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    }

                    topScreen.SetStackHidden(false);
                    visibility = topScreen.lowerScreenVisibility;
                }

                for (int i = 0; i < widgetsInStack.Count - 1; i++)
                {
                    if (widgetsInStack[i] is UIKScreen screen)
                    {
                        if (screen.GetCanvasGroup() is CanvasGroup canvasGroup)
                        {
                            canvasGroup.interactable = false;
                            canvasGroup.blocksRaycasts = false;
                        }

                        screen.SetStackHidden(visibility == UIKScreenLowerScreenVisibility.HideAll);
                    }
                }
            }
        }

        protected override UIKWidget TryInstantiateWidget(GameObject _widgetPrefab, Transform _parentTransform)
        {
            UIKWidget widget = base.TryInstantiateWidget(_widgetPrefab, _parentTransform);
            if (widget is not UIKScreen)
            {
                Debug.LogError($"Can't instantiate a widget into {nameof(UIKScreenStack)} that isn't of type {nameof(UIKScreen)}");
                return null;
            }

            return widget;
        }
    }
} // UIKit namespace
