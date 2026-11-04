using UnityEngine;

namespace UIKit
{
    public class UIK2DActionDisplayButton : UIK2DButton, UIKActionTrigger
    {
        [SerializeField] protected UIK2DActionDisplay actionDisplay;
        [SerializeField] protected CanvasGroup canvasGroup;
        // Off: the click input runs the action object without targeting the button, so only a cursor ever focuses it
        [SerializeField] protected bool clickInputTargetsButton = true;

        private bool canExecute;
        private bool canClick;

        protected override bool registersAsScreenButton => clickInputTargetsButton;


        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);
            
            if (!_isOnValidate)
            {
                if (!clickInputTargetsButton
                    && GetComponentInParent<UIKScreen>(true) is UIKScreen screen)
                {
                    screen.RegisterActionTrigger(this);
                }

                GetCanvas()?.OnTopScreenChanged.AddListener(Canvas_OnTopScreenChanged);

                if (GetOwningPlayer() is UIKPlayer player)
                {
                    player.OnInputActionMapChanged.AddListener(Player_OnInputActionMapChanged);
                    player.OnInputDeviceTypeChanged.AddListener(Player_OnInputDeviceTypeChanged);
                    UpdateCanClick(player.inputDeviceType.UsesCursor());

                    if (clickActionObject != null
                        && clickActionObject.GetActionObject(player) is UIKActionObject actionObjectInstance)
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

            if (!clickInputTargetsButton
                && GetComponentInParent<UIKScreen>(true) is UIKScreen screen)
            {
                screen.UnregisterActionTrigger(this);
            }

            GetCanvas()?.OnTopScreenChanged.RemoveListener(Canvas_OnTopScreenChanged);
        }


        public UIKActionObject GetTriggerActionObject()
        {
            if (clickActionObject != null
                && GetOwningPlayer() is UIKPlayer player)
            {
                return clickActionObject.GetActionObject(player);
            }

            return null;
        }

        public UIKInputAction GetTriggerInputAction()
        {
            return clickInputAction;
        }

        protected virtual void UpdateCanInteract(bool _canExecute)
        {
            canExecute = _canExecute;

            RefreshClickable();
        }

        protected virtual void UpdateCanClick(bool _canClick)
        {
            canClick = _canClick;
            
            style.SetGraphicVisible(canClick);

            RefreshClickable();
        }

        private void RefreshClickable()
        {
            bool active = canExecute
                && IsOnTopScreen();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 1f : 0.5f;
                canvasGroup.interactable = active && canClick;
                canvasGroup.blocksRaycasts = active && canClick;
            }

            if (!clickInputTargetsButton)
            {
                SetInteractable(active && canClick);
            }
        }

        private void Canvas_OnTopScreenChanged(UIKScreen _topScreen)
        {
            RefreshClickable();
        }

        private void Player_OnInputActionMapChanged(string _actionMap)
        {
            RefreshActionDisplay();
        }

        private void Player_OnInputDeviceTypeChanged(UIKInputDevice _inputDeviceType)
        {
            RefreshActionDisplay();
            UpdateCanClick(_inputDeviceType.UsesCursor());
        }

        private void RefreshActionDisplay()
        {
            if (actionDisplay != null)
            {
                actionDisplay.SetInputAction(clickInputAction, false);

                if (clickActionObject != null
                    && GetOwningPlayer() is UIKPlayer player)
                {
                    actionDisplay.SetActionText(clickActionObject.GetActionText(player), false);
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
} // UIKit namespace
