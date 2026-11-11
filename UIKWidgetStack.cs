using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace UIKit
{
    public class UIKWidgetStack : MonoBehaviour
    {
        [HideInInspector] public UnityEvent OnStackChanged = new();

        [SerializeField] protected bool manageActivation = true;
        
        protected List<UIKWidget> widgetsInStack = new();
        

        public virtual UIKWidget PushToStack(string _widgetName, GameObject _widgetPrefab)
        {
            if (TryInstantiateWidget(_widgetPrefab, transform) is UIKWidget widget)
            {
                widget.Setup(_widgetName, this);
                UpdateWidgetsInStack();
                
                return widget;
            }
            
            return null;
        }
        
        public virtual bool PopFromStack(UIKWidget _widget)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<UIKWidget>() is UIKWidget widget
                    && widget == _widget
                    && !IsLeaving(widget))
                {
                    RemoveWidget(widget);
                    UpdateWidgetsInStack();

                    return true;
                }
            }
            
            return false;
        }

        public virtual bool PopStack()
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<UIKWidget>() is UIKWidget widget
                    && !IsLeaving(widget))
                {
                    RemoveWidget(widget);
                    UpdateWidgetsInStack();

                    return true;
                }
            }
            
            return false;
        }

        public virtual void ClearStack()
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<UIKWidget>() is UIKWidget widget
                    && !IsLeaving(widget))
                {
                    RemoveWidget(widget);
                }
            }
            
            UpdateWidgetsInStack();
        }

        public List<UIKWidget> GetWidgets()
        {
            return widgetsInStack;
        }

        public int GetNumWidgets()
        {
            return widgetsInStack.Count;
        }

        public List<UIKWidget> GetWidgetsOrdered()
        {
            List<UIKWidget> reversedWidgets = new(widgetsInStack);
            reversedWidgets.Reverse();
            return reversedWidgets;
        }
        
        public UIKWidget GetWidgetByName(string _widgetName)
        {
            return widgetsInStack.FirstOrDefault(w => w.widgetName == _widgetName);
        }

        protected virtual void UpdateWidgetsInStack()
        {
            // Re-determine the widgets in the stack by checking our children
            widgetsInStack.Clear();
            foreach (Transform child in transform)
            {
                if (!child
                    || !child.gameObject
                    || child.gameObject.IsPendingDestroy())
                {
                    continue;
                }

                if (child.GetComponent<UIKWidget>() is UIKWidget widget
                    && !IsLeaving(widget))
                {
                    widgetsInStack.Add(widget);
                }
            }

            if (manageActivation)
            {
                // Deactivate widgets that we're not tracking that are pending destruction
                foreach (Transform child in transform)
                {
                    if (child?.gameObject
                        && child.gameObject.IsPendingDestroy()
                        && child.GetComponent<UIKWidget>() is UIKWidget widget)
                    {
                        widget.Deactivate();
                    }
                }

                // Deactivate all widgets in the stack and activate the topmost one
                for (int i = 0; i < widgetsInStack.Count; i++)
                {
                    widgetsInStack[i].SetActive(i == widgetsInStack.Count - 1);
                }
            }
            
            OnStackChanged.Invoke();
        }

        protected virtual bool IsLeaving(UIKWidget _widget)
        {
            return false;
        }

        protected virtual void RemoveWidget(UIKWidget _widget)
        {
            _widget.gameObject.SafeDestroy();
        }

        protected virtual UIKWidget TryInstantiateWidget(GameObject _widgetPrefab, Transform _parentTransform)
        {
            if (!_widgetPrefab)
            {
                Debug.LogError("Failed to instantiate widget prefab because it was null.");
                return null;
            }
            
            if (Instantiate(_widgetPrefab, _parentTransform) is GameObject widgetGO)
            {
                return widgetGO.GetComponent<UIKWidget>();
            }

            Debug.LogError($"Failed to instantiate widget prefab `{_widgetPrefab.name}` because it could not be found in resources.");
            return null;
        }
    }
} // UIKit namespace
