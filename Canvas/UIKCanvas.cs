using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UIKit
{
    [Serializable]
    public struct UIKScreenInputTypeActionMap
    {
        public UIKScreenInputType screenInputType;
        public UIKActionMap actionMap;
    }

    [Serializable]
    public struct UIKInputDeviceInputIconDatabase
    {
        public UIKInputDevice inputDevice;
        public UIKInputIconDatabase inputIconDatabase;
    }
    
    [RequireComponent(typeof(Canvas))]
    public class UIKCanvas : MonoBehaviour, UIKInputActionHandler
    {
        [HideInInspector] public UnityEvent<UIKScreen> OnTopScreenChanged = new();

        [SerializeField] protected UIKInputAction leftClickInputAction;
        [SerializeField] protected UIKInputAction uiSubmitInputAction;
        [SerializeField] protected UIKInputAction uiMoveInputAction;
        [SerializeField] protected float movePressThreshold = 0.5f;
        [SerializeField] protected float moveReleaseThreshold = 0.3f;
        [SerializeField] protected float moveRepeatDelay = 0.4f;
        [SerializeField] protected float moveRepeatInterval = 0.1f;
        [SerializeField] public List<UIKScreenInputTypeActionMap> screenInputTypeActionMaps = new();
        [SerializeField] public List<UIKInputDeviceInputIconDatabase> inputDeviceInputIconDatabases = new();
        
        protected Transform screenStackPanelTransform;
        protected Dictionary<int, UIKScreenStack> screenStackByLayer = new();

        public UIKScreen topScreen { get; private set; }
        public UIK2DButtonTextField editingTextField { get; private set; }
        protected virtual string textEntryScreenName => null;
        private List<InputControl> consumedInputControlsThisFrame = new();
        private UIKInputDirection? heldMoveDirection;
        private float nextMoveRepeatTime;
        
        
        protected virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
            
            // Create the screen stack panel
            GameObject screenStackPanelGO = new("ScreenStackPanel", typeof(RectTransform));
            screenStackPanelGO.transform.SetParent(transform);
            screenStackPanelGO.transform.localScale = Vector3.one;
            screenStackPanelGO.transform.localPosition = Vector3.zero;
            
            RectTransform screenStackPanelRectTransform = screenStackPanelGO.GetComponent<RectTransform>();
            screenStackPanelRectTransform.anchorMin = Vector2.zero;
            screenStackPanelRectTransform.anchorMax = Vector2.one;
            screenStackPanelRectTransform.offsetMin = Vector2.zero;
            screenStackPanelRectTransform.offsetMax = Vector2.zero;
            screenStackPanelRectTransform.sizeDelta = Vector2.zero;
            
            screenStackPanelTransform = screenStackPanelGO.transform;
        }

        protected virtual void Update()
        {
            if (GetOwningPlayer() is UIKPlayer player
                && !player.targetUI
                && !player.inputDeviceType.UsesCursor()
                && GetFirstValidTarget(player) is UIKTarget target)
            {
                player.TryTargetUI(target);
            }

            UpdateHeldMove();

            consumedInputControlsThisFrame.Clear();
        }


        private void ScreenStack_OnStackChanged()
        {
            RefreshTopScreen();
        }
        
        public string GetActionMapForScreenInputType(UIKScreenInputType _inputType)
        {
            return screenInputTypeActionMaps.FirstOrDefault(o => o.screenInputType == _inputType).actionMap.name;
        }
        
        private Coroutine CoCheckTopScreenChanged;
        public virtual void RefreshTopScreen()
        {
            if (CoCheckTopScreenChanged != null)
            {
                StopCoroutine(CoCheckTopScreenChanged);
            }
            CoCheckTopScreenChanged = StartCoroutine(DoCheckTopScreenChanged());
        }

        private IEnumerator DoCheckTopScreenChanged()
        {
            // Wait a frame for deleted screens and child index changes to propagate
            yield return WaitForFrameEnd();
            
            UIKScreen lastTopScreen = topScreen;
            UIKScreen newTopScreen = GetTopScreen();
            topScreen = newTopScreen;
            if (lastTopScreen != topScreen)
            {
                HandleTopScreenChanged();
                
                if (topScreen
                    && GetOwningPlayer() is UIKPlayer player)
                {
                    // Determine if we should lock/unlock the cursor on this screen
                    if (player.inputDeviceType.UsesCursor())
                    {
                        if (topScreen.inputType == UIKScreenInputType.Game)
                        {
                            Cursor.lockState = CursorLockMode.Locked;
                            Cursor.visible = false;
                        }
                        else
                        {
                            Cursor.lockState = CursorLockMode.None;
                            Cursor.visible = true;
                        }
                    }
                    
                    if (GetActionMapForScreenInputType(topScreen.inputType) is string actionMapName
                        && player.playerInput.currentActionMap.name != actionMapName)
                    {
                        // Wait a frame for press input to be finish
                        // Switching our current action map will re-broadcast any inputs from this frame
                        yield return WaitForFrameEnd();
                    
                        player.SetInputActionMap(actionMapName);
                    }
                }  
            }
        }

        // End of frame never comes in batch mode, so headless runs wait for the next frame instead
        private static object WaitForFrameEnd()
        {
            return Application.isBatchMode ? null : new WaitForEndOfFrame();
        }

        protected virtual void HandleTopScreenChanged()
        {
            if (GetOwningPlayer() is UIKPlayer player
                && !player.inputDeviceType.UsesCursor())
            {
                if (player.targetUI)
                {
                    bool targetIsInTopScreen = false;
                    if (topScreen)
                    {
                        foreach (UIKTarget target in topScreen.GetComponentsInChildren<UIKTarget>())
                        {
                            if (player.targetUI == target)
                            {
                                targetIsInTopScreen = true;
                                break;
                            }
                        }
                    }

                    if (!targetIsInTopScreen)
                    {
                        player.TryUntargetUI(player.targetUI);
                    }
                }
            }
            
            OnTopScreenChanged?.Invoke(topScreen);
        }

        public virtual Camera GetWorldCamera()
        {
            return GetComponent<Canvas>()?.worldCamera;
        }

        public virtual Camera GetUICamera()
        {
            return GetComponent<Canvas>()?.worldCamera;
        }

        protected virtual UIKScreen GetTopScreen()
        {
            foreach (UIKScreenStack screenStack in GetScreenStacksOrdered())
            {
                foreach (UIKWidget screenWidget in screenStack.GetWidgetsOrdered())
                {
                    if (screenWidget.active
                        && screenWidget is UIKScreen screen)
                    {
                        return screen;
                    }
                }
            }

            return null;
        }

        public virtual UIKTarget GetFirstValidTarget(UIKPlayer _player)
        {
            if (topScreen
                && topScreen.firstTarget is UIKElement element
                && element
                && element.GetInnerTarget(UIKInputDirection.Down) is UIKTarget target
                && target.CanPlayerTarget(_player))
            {
                return target;
            }

            return null;
        }
        
        public virtual UIKPlayer GetOwningPlayer()
        {
            if (UIKPlayerManager.instance != null)
            {
                if (UIKPlayerManager.instance.players.FirstOrDefault(p => p.canvas == this) is UIKPlayer player)
                {
                    return player;
                }
            }

            return null;
        }
        
        public UIKScreen PushScreen(string _name)
        {
            if (UIKScreen.GetScreenPrefab(_name) is GameObject screenPrefab
                && screenPrefab.GetComponent<UIKScreen>() is UIKScreen screen
                && screen.GetType().GetCustomAttribute<UIKScreenAttribute>() is UIKScreenAttribute screenAttribute
                && GetScreenStack(screenAttribute.layer) is UIKScreenStack screenStack)
            {
                UIKScreen screenInstance = screenStack.PushToStack(screenAttribute.name, screenPrefab) as UIKScreen;
                OnPostScreenPushed(screenInstance);
                
                return screenInstance;
            }

            return null;
        }

        public void PopScreen(string _name)
        {
            foreach (UIKScreenStack screenStack in GetScreenStacksOrdered())
            {
                foreach (UIKWidget screenWidget in screenStack.GetWidgetsOrdered())
                {
                    if (screenWidget.widgetName == _name
                        && screenWidget is UIKScreen screenInstance)
                    {
                        OnPreScreenPopped(screenInstance);
                        screenStack.PopFromStack(screenInstance);
                        OnPostScreenPopped();
                        
                        return;
                    }
                }
            }
        }

        public void PopScreen(UIKScreen _screen)
        {
            if (_screen
                && _screen.GetType().GetCustomAttribute<UIKScreenAttribute>() is UIKScreenAttribute screenAttribute
                && GetScreenStack(screenAttribute.layer) is UIKScreenStack screenStack
                && screenStack.GetWidgetsOrdered().Contains(_screen))
            {
                OnPreScreenPopped(_screen);
                screenStack.PopFromStack(_screen);
                OnPostScreenPopped();
            }
        }

        public void PopAllScreens()
        {
            foreach (UIKScreenStack screenStack in GetScreenStacksOrdered().ToArray())
            {
                foreach (UIKScreen screenInstance in screenStack.GetWidgetsOrdered().OfType<UIKScreen>().ToArray())
                {
                    OnPreScreenPopped(screenInstance);
                    screenStack.PopFromStack(screenInstance);
                    OnPostScreenPopped();
                }
            }
        }

        public virtual bool OpenTextEntry(UIK2DButtonTextField _field)
        {
            if (string.IsNullOrEmpty(textEntryScreenName)
                || PushScreen(textEntryScreenName) is not UIKScreen screen
                || screen.GetComponentInChildren<UIKTextEntryKeyboard>(true) is not UIKTextEntryKeyboard keyboard)
            {
                return false;
            }

            keyboard.Open(_field);
            return true;
        }

        public void SetEditingTextField(UIK2DButtonTextField _field)
        {
            if (editingTextField
                && editingTextField != _field)
            {
                editingTextField.EndEditing(true);
            }

            editingTextField = _field;
        }

        public void ClearEditingTextField(UIK2DButtonTextField _field)
        {
            if (editingTextField == _field)
            {
                editingTextField = null;
            }
        }

        protected virtual void OnPostScreenPushed(UIKScreen _screen)
        {
        }

        protected virtual void OnPreScreenPopped(UIKScreen _screen)
        {
        }

        protected virtual void OnPostScreenPopped()
        {
        }

        public UIKScreenStack GetScreenStack(int _layer)
        {
            if (!screenStackByLayer.ContainsKey(_layer))
            {
                GameObject screenStackGO = new(_layer.ToString(), typeof(RectTransform), typeof(UIKScreenStack));
                screenStackGO.transform.SetParent(screenStackPanelTransform);
                screenStackGO.transform.localPosition = Vector3.zero;
                screenStackGO.transform.localRotation = Quaternion.identity;
                screenStackGO.transform.localScale = Vector3.one;
                
                RectTransform screenStackRectTransform = screenStackGO.GetComponent<RectTransform>();
                screenStackRectTransform.anchorMin = Vector2.zero;
                screenStackRectTransform.anchorMax = Vector2.one;
                screenStackRectTransform.offsetMin = Vector2.zero;
                screenStackRectTransform.offsetMax = Vector2.zero;
                screenStackRectTransform.sizeDelta = Vector2.zero;

                if (screenStackGO.GetComponent<UIKScreenStack>() is UIKScreenStack screenStack)
                {
                    screenStackByLayer.Add(_layer, screenStack);
                    
                    screenStack.OnStackChanged.AddListener(ScreenStack_OnStackChanged);
                }
                
                UpdateScreenStacks();
            }
            
            return screenStackByLayer[_layer] ? screenStackByLayer[_layer] : null;
        }
        
        private void UpdateScreenStacks()
        {
            foreach (UIKScreenStack screenStack in GetScreenStacksOrdered())
            {
                screenStack.transform.SetAsFirstSibling();
            }
        }

        protected void ConsumeInputControl(InputAction.CallbackContext _context)
        {
            // Hack to get around some internal Unity code where they assume the
            // validity of controls inside an InputAction.CallbackContext. It's
            // possibly null in this case because sometimes game code will leave
            // a player due to an input
            try
            {
                if (_context.control == null)
                {
                    return;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }
            
            consumedInputControlsThisFrame.Add(_context.control);
        }

        private IEnumerable<UIKScreenStack> GetScreenStacksOrdered()
        {
            return screenStackByLayer.OrderByDescending(p => p.Key).Select(p => p.Value).Where(s => s);
        }
        
        public virtual bool HandleInputAction(InputAction.CallbackContext _context)
        {
            if (consumedInputControlsThisFrame.Contains(_context.control))
            {
                return true;
            }

            if (editingTextField
                && HandleTextFieldEditingInput(_context))
            {
                return true;
            }

            // Consume UI inputs before broadcasting them
            if ((leftClickInputAction == _context.action || uiSubmitInputAction == _context.action)
                && _context.action.WasPressedThisFrame()
                && _context.action.triggered)
            {
                if (GetOwningPlayer().TrySubmitUI(GetOwningPlayer().GetTargetUI()))
                {
                    ConsumeInputControl(_context);
                    
                    return true;
                }
            }
            else if (uiMoveInputAction == _context.action)
            {
                if (HandleMoveInput(_context.ReadValue<Vector2>()))
                {
                    ConsumeInputControl(_context);
                    
                    return true;
                }
            }
            
            // If any of the screens consume the input action, then we return false
            foreach (UIKScreenStack screenStack in GetScreenStacksOrdered().ToArray())
            {
                foreach (UIKScreen screen in screenStack.GetWidgetsOrdered().Cast<UIKScreen>().ToArray())
                {
                    if (screen.HandleInputAction(_context))
                    {
                        ConsumeInputControl(_context);
                        
                        return true;
                    }
                }
            }

            return false;
        }

        // Typing owns the keyboard, so no key reaches an action; a click elsewhere commits the edit before it lands
        protected virtual bool HandleTextFieldEditingInput(InputAction.CallbackContext _context)
        {
            InputDevice device;
            try
            {
                device = _context.control?.device;
            }
            catch (IndexOutOfRangeException)
            {
                device = null;
            }

            if (device is Keyboard)
            {
                return true;
            }

            if (leftClickInputAction == _context.action
                && _context.action.WasPressedThisFrame()
                && !editingTextField.IsPointerOver())
            {
                editingTextField.EndEditing(true);
            }

            return false;
        }

        protected virtual bool HandleMoveInput(Vector2 _value)
        {
            if (heldMoveDirection.HasValue)
            {
                if (_value.magnitude < moveReleaseThreshold)
                {
                    heldMoveDirection = null;
                    return false;
                }

                if (_value.GetInputDirection() == heldMoveDirection.Value)
                {
                    return true;
                }
            }
            else if (_value.magnitude < movePressThreshold)
            {
                return false;
            }

            heldMoveDirection = _value.GetInputDirection();
            nextMoveRepeatTime = Time.unscaledTime + moveRepeatDelay;

            if (GetOwningPlayer() is not UIKPlayer player)
            {
                return false;
            }

            if (player.TryNavigateUIByDirection(heldMoveDirection.Value))
            {
                return true;
            }

            if (player.targetUI)
            {
                UIKFeedback.Raise(UIKFeedbackKind.NavigationBump, player.targetUI);
            }

            return false;
        }

        protected virtual void UpdateHeldMove()
        {
            if (!heldMoveDirection.HasValue)
            {
                return;
            }

            if (GetOwningPlayer() is not UIKPlayer player
                || player.playerInput?.actions?.FindAction($"{uiMoveInputAction.actionMap}/{uiMoveInputAction.action}") is not InputAction moveAction
                || !moveAction.enabled
                || moveAction.ReadValue<Vector2>().magnitude < moveReleaseThreshold)
            {
                heldMoveDirection = null;
                return;
            }

            if (Time.unscaledTime >= nextMoveRepeatTime)
            {
                nextMoveRepeatTime = Time.unscaledTime + moveRepeatInterval;
                player.TryNavigateUIByDirection(heldMoveDirection.Value);
            }
        }

        public Sprite GetInputActionIcon(InputAction _inputAction)
        {
            if (GetOwningPlayer() is UIKPlayer player
                && _inputAction != null
                && _inputAction.bindings.Count > 0)
            {
                foreach (InputBinding inputBinding in _inputAction.bindings)
                {
                    string path = inputBinding.effectivePath;
                    string deviceFromPath = path.Split(">")[0].TrimStart('<');
                    UIKInputDevice bindingInputDevice = UIKInputExtensions.GetInputDeviceFromString(deviceFromPath);
                    
                    if (player.inputDeviceType.IsFlagSet(bindingInputDevice))
                    {
                        return GetInputBindingIcon(path);
                    }
                }
            }

            return null;
        }

        public Sprite GetInputBindingIcon(string _bindingPath)
        {
            if (string.IsNullOrEmpty(_bindingPath))
            {
                return null;
            }

            UIKInputDevice bindingInputDevice = UIKInputExtensions.GetInputDeviceFromString(_bindingPath.Split(">")[0].TrimStart('<'));
            foreach (UIKInputDeviceInputIconDatabase inputIconDatabase in inputDeviceInputIconDatabases)
            {
                if (inputIconDatabase.inputDevice.IsFlagSet(bindingInputDevice))
                {
                    return inputIconDatabase.inputIconDatabase.GetIcon(_bindingPath);
                }
            }

            return null;
        }

        public string GetInputActionName(InputAction _inputAction)
        {
            if (_inputAction != null)
            {
                string bindingDisplayString = _inputAction.GetBindingDisplayString();
                bindingDisplayString = bindingDisplayString.Split("|")[0];
                bindingDisplayString = bindingDisplayString.TrimEnd();
                return bindingDisplayString;
            }

            return string.Empty;
        }
    }
} // UIKit namespace
