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
        [SerializeField] protected float doubleClickTime = 0.3f;

        public string text { get; private set; } = string.Empty;
        public UIKTextCharacters characters => allowedCharacters;
        public int maxTextLength => maxLength;
        public bool editing => editingPlayer != null;

        private UIKPlayer editingPlayer;
        private readonly List<Keyboard> editingKeyboards = new();
        private Mouse editingMouse;
        private string editText = string.Empty;
        private int caretIndex;
        private int selectionAnchor;
        private int editStartFrame;
        private Key? repeatKey;
        private float nextRepeatTime;
        private bool dragging;
        private float lastClickTime = float.NegativeInfinity;
        private int lastClickIndex = -1;

        private static readonly Key[] repeatableKeys =
        {
            Key.Backspace,
            Key.Delete,
            Key.LeftArrow,
            Key.RightArrow
        };

        private int selectionStart => Mathf.Min(caretIndex, selectionAnchor);
        private int selectionEnd => Mathf.Max(caretIndex, selectionAnchor);
        private bool hasSelection => caretIndex != selectionAnchor;


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
                || Time.frameCount == editStartFrame)
            {
                return;
            }

            UpdateMouse();

            if (editingKeyboards.Count == 0)
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

            if (keyboard.ctrlKey.isPressed
                && UpdateShortcuts(keyboard))
            {
                return;
            }

            if (keyboard.homeKey.wasPressedThisFrame)
            {
                MoveCaretTo(0, keyboard.shiftKey.isPressed);
            }
            else if (keyboard.endKey.wasPressedThisFrame)
            {
                MoveCaretTo(editText.Length, keyboard.shiftKey.isPressed);
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
            selectionAnchor = caretIndex;
            editStartFrame = Time.frameCount;
            repeatKey = null;
            dragging = false;
            lastClickIndex = -1;

            foreach (InputDevice device in _player.GetInputDevices())
            {
                if (device is Keyboard keyboard)
                {
                    editingKeyboards.Add(keyboard);
                    keyboard.onTextInput += Keyboard_OnTextInput;
                }
                else if (device is Mouse mouse)
                {
                    editingMouse = mouse;
                }
            }

            _player.canvas?.SetEditingTextField(this);
            RefreshDisplay();

            if (editingMouse != null
                && editingMouse.leftButton.isPressed
                && display)
            {
                caretIndex = GetIndexUnderMouse();
                selectionAnchor = caretIndex;
                dragging = true;
                lastClickTime = Time.unscaledTime;
                lastClickIndex = caretIndex;
                RefreshDisplay();
            }
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
            editingMouse = null;
            dragging = false;

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
            if (!allowedCharacters.Allows(_character))
            {
                return;
            }

            InsertText(_character.ToString());
        }

        private void UpdateMouse()
        {
            if (editingMouse == null
                || !display)
            {
                return;
            }

            if (editingMouse.leftButton.wasPressedThisFrame
                && IsPointerOver())
            {
                int index = GetIndexUnderMouse();
                bool doubleClick = index == lastClickIndex
                    && Time.unscaledTime - lastClickTime <= doubleClickTime;
                lastClickTime = doubleClick ? float.NegativeInfinity : Time.unscaledTime;
                lastClickIndex = index;

                if (doubleClick)
                {
                    selectionAnchor = FindWordStart(index);
                    caretIndex = FindWordEnd(index);
                    dragging = false;
                }
                else
                {
                    caretIndex = index;
                    if (editingKeyboards.Count == 0
                        || !editingKeyboards[0].shiftKey.isPressed)
                    {
                        selectionAnchor = index;
                    }
                    dragging = true;
                }

                RefreshDisplay();
                return;
            }

            if (!dragging)
            {
                return;
            }

            if (!editingMouse.leftButton.isPressed)
            {
                dragging = false;
                return;
            }

            int dragIndex = GetIndexUnderMouse();
            if (dragIndex != caretIndex)
            {
                caretIndex = dragIndex;
                RefreshDisplay();
            }
        }

        private int GetIndexUnderMouse()
        {
            return Mathf.Clamp(display.GetIndexAt(editingMouse.position.ReadValue()), 0, editText.Length);
        }

        private bool UpdateShortcuts(Keyboard _keyboard)
        {
            if (_keyboard.aKey.wasPressedThisFrame)
            {
                selectionAnchor = 0;
                caretIndex = editText.Length;
                RefreshDisplay();
                return true;
            }

            if (_keyboard.cKey.wasPressedThisFrame)
            {
                CopySelection();
                return true;
            }

            if (_keyboard.xKey.wasPressedThisFrame)
            {
                if (CopySelection())
                {
                    DeleteSelection();
                    RefreshDisplay();
                }
                return true;
            }

            if (_keyboard.vKey.wasPressedThisFrame)
            {
                string pasted = (GUIUtility.systemCopyBuffer ?? string.Empty)
                    .Replace("\r\n", " ")
                    .Replace('\n', ' ')
                    .Replace('\r', ' ')
                    .Replace('\t', ' ');
                InsertText(allowedCharacters.Filter(pasted, 0));
                return true;
            }

            return false;
        }

        private bool CopySelection()
        {
            if (!hasSelection)
            {
                return false;
            }

            GUIUtility.systemCopyBuffer = editText.Substring(selectionStart, selectionEnd - selectionStart);
            return true;
        }

        private void InsertText(string _text)
        {
            DeleteSelection();

            if (maxLength > 0)
            {
                _text = _text.Substring(0, Mathf.Clamp(maxLength - editText.Length, 0, _text.Length));
            }

            editText = editText.Insert(caretIndex, _text);
            caretIndex += _text.Length;
            selectionAnchor = caretIndex;
            RefreshDisplay();
        }

        private bool DeleteSelection()
        {
            if (!hasSelection)
            {
                return false;
            }

            int start = selectionStart;
            editText = editText.Remove(start, selectionEnd - start);
            caretIndex = start;
            selectionAnchor = start;
            return true;
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
                    ApplyKey(_keyboard, key);
                    return;
                }
            }

            if (repeatKey is Key repeatingKey
                && Time.unscaledTime >= nextRepeatTime)
            {
                nextRepeatTime = Time.unscaledTime + keyRepeatInterval;
                ApplyKey(_keyboard, repeatingKey);
            }
        }

        private void ApplyKey(Keyboard _keyboard, Key _key)
        {
            bool shift = _keyboard.shiftKey.isPressed;
            bool byWord = _keyboard.ctrlKey.isPressed;

            switch (_key)
            {
                case Key.Backspace:
                    if (!DeleteSelection()
                        && caretIndex > 0)
                    {
                        int start = byWord || shift ? FindPreviousWordStart(caretIndex) : caretIndex - 1;
                        editText = editText.Remove(start, caretIndex - start);
                        caretIndex = start;
                        selectionAnchor = start;
                    }
                    RefreshDisplay();
                    break;
                case Key.Delete:
                    if (!DeleteSelection()
                        && caretIndex < editText.Length)
                    {
                        int end = byWord ? FindNextWordStart(caretIndex) : caretIndex + 1;
                        editText = editText.Remove(caretIndex, end - caretIndex);
                    }
                    RefreshDisplay();
                    break;
                case Key.LeftArrow:
                    if (hasSelection
                        && !shift
                        && !byWord)
                    {
                        MoveCaretTo(selectionStart, false);
                    }
                    else
                    {
                        MoveCaretTo(byWord ? FindPreviousWordStart(caretIndex) : caretIndex - 1, shift);
                    }
                    break;
                case Key.RightArrow:
                    if (hasSelection
                        && !shift
                        && !byWord)
                    {
                        MoveCaretTo(selectionEnd, false);
                    }
                    else
                    {
                        MoveCaretTo(byWord ? FindNextWordStart(caretIndex) : caretIndex + 1, shift);
                    }
                    break;
            }
        }

        private void MoveCaretTo(int _index, bool _extendSelection)
        {
            caretIndex = Mathf.Clamp(_index, 0, editText.Length);
            if (!_extendSelection)
            {
                selectionAnchor = caretIndex;
            }

            RefreshDisplay();
        }

        // Letters and digits make words, a run of other symbols is a word of its own, and spaces only separate them
        private static int GetCharacterClass(char _character)
        {
            if (char.IsWhiteSpace(_character))
            {
                return 0;
            }

            return char.IsLetterOrDigit(_character) ? 1 : 2;
        }

        private int FindPreviousWordStart(int _index)
        {
            int index = _index;
            while (index > 0
                && GetCharacterClass(editText[index - 1]) == 0)
            {
                index--;
            }

            if (index == 0)
            {
                return 0;
            }

            int wordClass = GetCharacterClass(editText[index - 1]);
            while (index > 0
                && GetCharacterClass(editText[index - 1]) == wordClass)
            {
                index--;
            }

            return index;
        }

        private int FindNextWordStart(int _index)
        {
            int index = _index;
            if (index < editText.Length)
            {
                int wordClass = GetCharacterClass(editText[index]);
                while (wordClass != 0
                    && index < editText.Length
                    && GetCharacterClass(editText[index]) == wordClass)
                {
                    index++;
                }
            }

            while (index < editText.Length
                && GetCharacterClass(editText[index]) == 0)
            {
                index++;
            }

            return index;
        }

        private int FindWordStart(int _index)
        {
            int wordClass = GetWordClassAt(_index);
            int index = _index;
            while (index > 0
                && GetCharacterClass(editText[index - 1]) == wordClass)
            {
                index--;
            }

            return index;
        }

        private int FindWordEnd(int _index)
        {
            int wordClass = GetWordClassAt(_index);
            int index = _index;
            while (index < editText.Length
                && GetCharacterClass(editText[index]) == wordClass)
            {
                index++;
            }

            return index;
        }

        // A click lands between characters; the word is the one after it unless the click is at the end of the text
        private int GetWordClassAt(int _index)
        {
            if (_index < editText.Length)
            {
                return GetCharacterClass(editText[_index]);
            }

            return _index > 0 ? GetCharacterClass(editText[_index - 1]) : 0;
        }

        private void RefreshDisplay()
        {
            if (!display)
            {
                return;
            }

            display.SetText(editing ? editText : text);
            display.SetCaret(editing, caretIndex);
            display.SetSelection(editing ? selectionStart : 0, editing ? selectionEnd : 0);
        }
    }
} // UIKit namespace
