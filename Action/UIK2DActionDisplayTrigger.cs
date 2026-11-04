using UnityEngine;

namespace UIKit
{
    public class UIK2DActionDisplayTrigger : UIKMonoBehaviour, UIKActionTrigger
    {
        [SerializeField] protected UIK2DActionDisplay actionDisplay;
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected UIKActionObjectReference actionObject;
        [SerializeField] protected UIKInputAction inputAction;

        private bool canExecute;
        

        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);
            
            if (!_isOnValidate)
            {
                if (GetComponentInParent<UIKScreen>(true) is UIKScreen screen)
                {
                    screen.RegisterActionTrigger(this);
                }

                GetCanvas()?.OnTopScreenChanged.AddListener(Canvas_OnTopScreenChanged);
                
                if (GetOwningPlayer() is UIKPlayer player)
                {
                    player.OnInputActionMapChanged.AddListener(Player_OnInputActionMapChanged);
                    player.OnInputDeviceTypeChanged.AddListener(Player_OnInputDeviceTypeChanged);

                    if (actionObject != null
                        && actionObject.GetActionObject(player) is UIKActionObject actionObjectInstance)
                    {
                        actionObjectInstance.OnCanExecuteChanged.AddListener(ActionObject_OnCanExecuteChanged);
                        actionObjectInstance.OnActionTextChanged.AddListener(ActionObject_OnActionTextChanged);
                        ActionObject_OnCanExecuteChanged(actionObjectInstance.CanExecute());
                    }
                }
                
                RefreshActionDisplay();
            }
        }

        protected override void OnPreDestroy()
        {
            base.OnPreDestroy();
            
            if (GetComponentInParent<UIKScreen>(true) is UIKScreen screen)
            {
                screen.UnregisterActionTrigger(this);
            }

            GetCanvas()?.OnTopScreenChanged.RemoveListener(Canvas_OnTopScreenChanged);
        }
        
        
        public UIKActionObject GetTriggerActionObject()
        {
            if (actionObject != null
                && GetOwningPlayer() is UIKPlayer player)
            {
                return actionObject.GetActionObject(player);
            }

            return null;
        }

        public UIKInputAction GetTriggerInputAction()
        {
            return inputAction;
        }
        
        protected virtual void UpdateCanInteract(bool _canExecute)
        {
            canExecute = _canExecute;
            RefreshCanInteract();
        }

        private void RefreshCanInteract()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = canExecute && IsOnTopScreen() ? 1f : 0.5f;
            }
        }

        private void Canvas_OnTopScreenChanged(UIKScreen _topScreen)
        {
            RefreshCanInteract();
        }

        private void Player_OnInputActionMapChanged(string _actionMap)
        {
            RefreshActionDisplay();
        }

        private void Player_OnInputDeviceTypeChanged(UIKInputDevice _inputDeviceType)
        {
            RefreshActionDisplay();
        }

        private void RefreshActionDisplay()
        {
            if (actionDisplay != null)
            {
                actionDisplay.SetInputAction(inputAction, false);

                if (actionObject != null
                    && GetOwningPlayer() is UIKPlayer player)
                {
                    actionDisplay.SetActionText(actionObject.GetActionText(player), false);
                }
                
                actionDisplay.RefreshDisplay();
            }
        }

        private void ActionObject_OnCanExecuteChanged(bool _canExecute)
        {
            UpdateCanInteract(_canExecute);
        }

        private void ActionObject_OnActionTextChanged(string _actionText)
        {
            RefreshActionDisplay();
        }
    }
}
