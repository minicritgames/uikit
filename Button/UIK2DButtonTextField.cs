using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UIKit
{
    public class UIK2DButtonTextField : UIK2DButtonPart
    {
        [SerializeField] public UnityEvent<string> OnTextChanged = new();

        [SerializeField] protected UIKTextCaretDisplay display;
        [SerializeField] protected UIKTextCharacters allowedCharacters = UIKTextCharacters.All;
        [SerializeField] protected int maxLength;
        [SerializeField] protected float keyRepeatDelay = 0.4f;
        [SerializeField] protected float keyRepeatInterval = 0.05f;

        public string text { get; private set; } = string.Empty;
        public UIKTextCharacters characters => allowedCharacters;
        public int maxTextLength => maxLength;
        public bool editing => editingPlayer != null;

        private UIKPlayer editingPlayer;
        private readonly List<Keyboard> editingKeyboards = new();
        private string editText = string.Empty;
        private int caretIndex;
        private int editStartFrame;
        private Key? repeatKey;
        private float nextRepeatTime;

        private static readonly Key[] repeatableKeys =
        {
            Key.Backspace,
            Key.Delete,
            Key.LeftArrow,
            Key.RightArrow
        };


        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);

            if (_isOnValidate
                || TMP_Settings.instance == null)
            {
                return;
            }

            RefreshDisplay();
        }

        protected override void OnPreDestroy()
        {
            base.OnPreDestroy();

            EndEditing(false);
        }

        protected virtual void OnDisable()
        {
            EndEditing(true);
        }

        protected virtual void Update()
        {
            if (!editing
                || Time.frameCount == editStartFrame
                || editingKeyboards.Count == 0)
            {
                return;
            }

            Keyboard keyboard = editingKeyboards[0];
            if (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                EndEditing(true);
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                EndEditing(false);
                return;
            }

            if (keyboard.homeKey.wasPressedThisFrame)
            {
                SetCaret(0);
            }
            else if (keyboard.endKey.wasPressedThisFrame)
            {
                SetCaret(editText.Length);
            }

            UpdateRepeatableKeys(keyboard);
        }


        protected override void Button_OnClickHandled(UIKEventData _eventData)
        {
            base.Button_OnClickHandled(_eventData);

            if (editing
                || _eventData.pressingPlayer is not UIKPlayer player)
            {
                return;
            }

            if (player.inputDeviceType.UsesCursor())
            {
                BeginEditing(player);
            }
            else
            {
                player.canvas?.OpenTextEntry(this);
            }
        }

        protected override void Button_OnLockedChanged(bool _locked)
        {
            base.Button_OnLockedChanged(_locked);

            if (_locked)
            {
                EndEditing(true);
            }
        }

        public void SetText(string _text, bool _notify = true)
        {
            string filteredText = allowedCharacters.Filter(_text, maxLength);
            if (filteredText == text)
            {
                return;
            }

            text = filteredText;
            RefreshDisplay();

            if (_notify)
            {
                OnTextChanged.Invoke(text);
            }
        }

        public bool IsPointerOver()
        {
            return uik2DButton
                && uik2DButton.hovered;
        }

        public void BeginEditing(UIKPlayer _player)
        {
            if (editing
                || _player == null)
            {
                return;
            }

            editingPlayer = _player;
            editText = text;
            caretIndex = editText.Length;
            editStartFrame = Time.frameCount;
            repeatKey = null;

            foreach (InputDevice device in _player.GetInputDevices())
            {
                if (device is Keyboard keyboard)
                {
                    editingKeyboards.Add(keyboard);
                    keyboard.onTextInput += Keyboard_OnTextInput;
                }
            }

            _player.canvas?.SetEditingTextField(this);
            RefreshDisplay();
        }

        public void EndEditing(bool _commit)
        {
            if (!editing)
            {
                return;
            }

            foreach (Keyboard keyboard in editingKeyboards)
            {
                keyboard.onTextInput -= Keyboard_OnTextInput;
            }
            editingKeyboards.Clear();

            UIKPlayer player = editingPlayer;
            editingPlayer = null;
            player.canvas?.ClearEditingTextField(this);

            if (_commit)
            {
                SetText(editText);
            }
            RefreshDisplay();
        }

        private void Keyboard_OnTextInput(char _character)
        {
            if (!allowedCharacters.Allows(_character)
                || (maxLength > 0 && editText.Length >= maxLength))
            {
                return;
            }

            editText = editText.Insert(caretIndex, _character.ToString());
            caretIndex++;
            RefreshDisplay();
        }

        private void UpdateRepeatableKeys(Keyboard _keyboard)
        {
            if (repeatKey is Key heldKey
                && !_keyboard[heldKey].isPressed)
            {
                repeatKey = null;
            }

            foreach (Key key in repeatableKeys)
            {
                if (_keyboard[key].wasPressedThisFrame)
                {
                    repeatKey = key;
                    nextRepeatTime = Time.unscaledTime + keyRepeatDelay;
                    ApplyKey(key);
                    return;
                }
            }

            if (repeatKey is Key repeatingKey
                && Time.unscaledTime >= nextRepeatTime)
            {
                nextRepeatTime = Time.unscaledTime + keyRepeatInterval;
                ApplyKey(repeatingKey);
            }
        }

        private void ApplyKey(Key _key)
        {
            switch (_key)
            {
                case Key.Backspace:
                    if (caretIndex > 0)
                    {
                        editText = editText.Remove(caretIndex - 1, 1);
                        caretIndex--;
                    }
                    break;
                case Key.Delete:
                    if (caretIndex < editText.Length)
                    {
                        editText = editText.Remove(caretIndex, 1);
                    }
                    break;
                case Key.LeftArrow:
                    caretIndex = Mathf.Max(caretIndex - 1, 0);
                    break;
                case Key.RightArrow:
                    caretIndex = Mathf.Min(caretIndex + 1, editText.Length);
                    break;
            }

            RefreshDisplay();
        }

        private void SetCaret(int _index)
        {
            caretIndex = Mathf.Clamp(_index, 0, editText.Length);
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (!display)
            {
                return;
            }

            display.SetText(editing ? editText : text);
            display.SetCaret(editing, caretIndex);
        }
    }
} // UIKit namespace
