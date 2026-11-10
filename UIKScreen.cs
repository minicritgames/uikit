using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UIKit
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UIKScreenAttribute : Attribute
    {
        public string name;
        public int layer;
    }
    
    [Serializable]
    public enum UIKScreenInputType
    {
        None,
        UI,
        Game
    }

    [Serializable]
    public enum UIKScreenLowerScreenVisibility
    {
        ShowAll,
        HideAll
    }
    
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIKScreen : UIKWidget, UIKInputActionHandler
    {
        [SerializeField] public UIKScreenInputType inputType;
        [SerializeField] public UIKScreenLowerScreenVisibility lowerScreenVisibility;
        [SerializeField] public UIKInputAction backInputAction;
        [SerializeField] public UIKElement firstTarget;
        [SerializeField] public List<string> transitionCategories = new();

        protected Dictionary<UIKInputAction, List<UIKButton>> buttonByAction = new();
        protected Dictionary<UIKInputAction, List<UIKActionTrigger>> triggerByAction = new();

        public bool isClosing { get; private set; }
        public bool isMoving => motionPlayer.isPlaying;
        public bool isMotionWaiting => motionPlayer.isWaiting;
        public bool isVisible => GetCanvasGroup() is CanvasGroup canvasGroup
            && canvasGroup.alpha > 0f;

        private UIKScreenMotionPlayer motionPlayer => __motionPlayer ??= new UIKScreenMotionPlayer(this);
        private UIKScreenMotionBody rootBody => __rootBody ??= new UIKScreenMotionBody((RectTransform)transform, GetCanvasGroup());

        private UIKScreenMotionPlayer __motionPlayer;
        private UIKScreenMotionBody __rootBody;
        private bool stackHidden;
        private bool awaitingTransition;


        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);

            if (firstTarget == this)
            {
                Debug.LogError("First target cannot be self (infinite recursion)");
                firstTarget = null;
            }

            if (_isOnValidate
                && (navigation.up || navigation.down || navigation.left || navigation.right))
            {
                Debug.LogError("Screens do not use the navigation property");
                navigation = new UIKNavigation();
            }
        }
        
        
        public override UIKTarget GetInnerTarget(UIKInputDirection _direction)
        {
            return firstTarget?.GetInnerTarget(_direction);
        }

        /// <returns>True if the back action was handled, false if not</returns>
        public virtual bool HandleBackAction()
        {
            CloseScreen();
            
            return true;
        }
        
        public virtual void CloseScreen()
        {
            if (GetCanvas())
            {
                GetCanvas().PopScreen(this);
            }
        }

        public virtual void RegisterButton(UIKButton _button)
        {
            if (_button?.GetClickAction() is UIKInputAction inputAction
                && inputAction.IsValid())
            {
                if (buttonByAction.TryGetValue(inputAction, out List<UIKButton> boundButtons))
                {
                    boundButtons.Add(_button);
                }
                else
                {
                    buttonByAction.Add(inputAction, new List<UIKButton> { _button });
                }
            }
        }

        public virtual void UnregisterButton(UIKButton _button)
        {
            if (_button?.GetClickAction() is UIKInputAction inputAction
                && inputAction.IsValid())
            {
                if (buttonByAction.TryGetValue(inputAction, out List<UIKButton> boundButtons))
                {
                    boundButtons.Remove(_button);
                }
            }
        }

        public virtual void RegisterActionTrigger(UIKActionTrigger _actionTrigger)
        {
            if (_actionTrigger?.GetTriggerInputAction() is UIKInputAction inputAction
                && inputAction.IsValid())
            {
                if (triggerByAction.TryGetValue(inputAction, out List<UIKActionTrigger> boundTriggers))
                {
                    boundTriggers.Add(_actionTrigger);
                }
                else
                {
                    triggerByAction.Add(inputAction, new List<UIKActionTrigger> { _actionTrigger });
                }
            }
        }

        public virtual void UnregisterActionTrigger(UIKActionTrigger _actionTrigger)
        {
            if (_actionTrigger?.GetTriggerInputAction() is UIKInputAction inputAction
                && inputAction.IsValid())
            {
                if (triggerByAction.TryGetValue(inputAction, out List<UIKActionTrigger> boundTriggers))
                {
                    boundTriggers.Remove(_actionTrigger);
                }
            }
        }

        public CanvasGroup GetCanvasGroup()
        {
            return GetComponent<CanvasGroup>();
        }

        public void MarkClosing()
        {
            isClosing = true;
            Deactivate();
            if (GetCanvasGroup() is CanvasGroup canvasGroup)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void SetStackHidden(bool _hidden)
        {
            stackHidden = _hidden;
            ApplyPose();
        }

        // Kept invisible from the push until the canvas picks its transition, so it never shows a frame early
        public void SetAwaitingTransition()
        {
            awaitingTransition = true;
            ApplyPose();
        }

        public void PlayOutgoingMotion(UIKScreenMotion _motion)
        {
            awaitingTransition = false;
            motionPlayer.PlayTowardAway(_motion, isClosing);
            ApplyPose();
        }

        public void PlayIncomingMotion(UIKScreenMotion _motion, float _startDelay, bool _fromAway)
        {
            awaitingTransition = false;
            motionPlayer.PlayTowardRest(_motion, _startDelay, _fromAway);
            ApplyPose();
        }

        public void SetRestPose()
        {
            awaitingTransition = false;
            motionPlayer.SetRest();
            ApplyPose();
        }

        public void SnapMotion()
        {
            motionPlayer.SnapToEnd();
            ApplyPose();
        }

        public void SkipMotionDelay()
        {
            motionPlayer.SkipDelay();
        }

        public bool TickMotion(float _deltaTime)
        {
            bool finished = motionPlayer.Tick(_deltaTime);
            ApplyPose();
            return finished;
        }

        // Offsets are in parent sizes, so a resize moves a screen that's away to stay away
        protected virtual void OnRectTransformDimensionsChange()
        {
            ApplyPose();
        }

        public void ApplyPose()
        {
            bool hidden = awaitingTransition
                || (stackHidden && !motionPlayer.isPlaying);
            rootBody.Apply(hidden);
            IReadOnlyList<UIKScreenMotionBody> posedBodies = motionPlayer.posedBodies;
            for (int i = 0; i < posedBodies.Count; i++)
            {
                if (posedBodies[i] != rootBody)
                {
                    posedBodies[i].Apply(false);
                }
            }
        }

        public IReadOnlyList<UIKScreenMotionBody> GetMotionBodies(string _group)
        {
            List<UIKScreenMotionBody> bodies = new();
            if (string.IsNullOrEmpty(_group))
            {
                bodies.Add(rootBody);
                return bodies;
            }

            foreach (UIKScreenMotionElement element in GetOwnMotionElements())
            {
                if (element.group == _group
                    && element.isActiveAndEnabled)
                {
                    bodies.Add(element.body);
                }
            }

            return bodies;
        }

        public IReadOnlyList<UIKScreenMotionBody> GetAllMotionBodies()
        {
            List<UIKScreenMotionBody> bodies = new()
            {
                rootBody
            };
            foreach (UIKScreenMotionElement element in GetOwnMotionElements())
            {
                bodies.Add(element.body);
            }

            return bodies;
        }

        // Elements inside a nested screen belong to it, and one on this screen's own transform would fight the screen's pose
        private List<UIKScreenMotionElement> GetOwnMotionElements()
        {
            List<UIKScreenMotionElement> elements = new();
            foreach (UIKScreenMotionElement element in GetComponentsInChildren<UIKScreenMotionElement>(true))
            {
                if (element.transform != transform
                    && element.GetComponentInParent<UIKScreen>(true) == this)
                {
                    elements.Add(element);
                }
            }

            return elements;
        }
        
        public bool HandleInputAction(InputAction.CallbackContext _context)
        {
            // Only try to consume input actions on the screen if we're active
            if (active)
            {
                // If child screen logic wants to capture input first, go for it
                if (HandleScreenInputAction(_context))
                {
                    return true;
                }
            }
        
            return false;
        }

        protected virtual bool HandleScreenInputAction(InputAction.CallbackContext _context)
        {
            // If this was a button press
            if (_context.action.WasPressedThisFrame()
                && _context.action.triggered)
            {
                if (HandleScreenButtonPress(_context))
                {
                    return true;
                }

                if (HandleScreenActionTrigger(_context))
                {
                    return true;
                }
            }
            
            return false;
        }

        protected virtual bool HandleScreenButtonPress(InputAction.CallbackContext _context)
        {
            // If this is a back action, and we'd like to handle it
            if (backInputAction == _context.action)
            {
                if (HandleBackAction())
                {
                    UIKFeedback.Raise(UIKFeedbackKind.Back, this);
                    return true;
                }
            }
                    
            // If any of our registered buttons wants to consume this input action, handle it with a click event
            if (buttonByAction.TryGetValue((UIKInputAction)_context.action, out List<UIKButton> buttons))
            {
                foreach (UIKButton button in buttons)
                {
                    if (button is UIKTarget selectable
                        && GetOwningPlayer().TryTargetUI(selectable)
                        && GetOwningPlayer().TrySubmitUI(selectable))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual bool HandleScreenActionTrigger(InputAction.CallbackContext _context)
        { 
            // If any of our registered action triggers wants to consume this input action, handle it by executing it
            if (triggerByAction.TryGetValue((UIKInputAction)_context.action, out List<UIKActionTrigger> actionTriggers))
            {
                foreach (UIKActionTrigger actionTrigger in actionTriggers)
                {
                    if (actionTrigger.GetTriggerActionObject() is UIKActionObject actionObject
                        && actionObject.TryExecute())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        
        
        public static GameObject GetScreenPrefab(string _name)
        {
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>($"Screen/{_name}");
            handle.WaitForCompletion(); // Not typically good practice, but the screen system is not build for async right now
            GameObject screenPrefab = handle.Result;
            
            if (screenPrefab
                && screenPrefab.GetComponent<UIKScreen>() is UIKScreen screen
                && screen.GetType().GetCustomAttribute(typeof(UIKScreenAttribute)) is UIKScreenAttribute screenAttribute
                && screenAttribute.name == _name)
            {
                return screenPrefab;
            }

            return null;
        }
    }
} // UIKit namespace
