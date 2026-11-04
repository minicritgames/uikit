using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UIKit
{
    [Serializable]
    public enum UIKShiftState
    {
        Off,
        Once,
        Locked
    }

    public class UIKTextEntryKeyboard : UIKMonoBehaviour
    {
        [HideInInspector] public UnityEvent<UIKTextEntryKeyboard> OnClosed = new();

        [SerializeField] protected UIKTextCaretDisplay display;
        [SerializeField] protected UIKElement_2DLayoutGroup_Grid keyGroup;
        [SerializeField] protected GameObject keyPrefab;
        [SerializeField] protected List<string> keyRows = new();
        [SerializeField] protected string spaceLabel = "Space";
        [SerializeField] protected string backspaceLabel = "Delete";
        [SerializeField] protected string shiftLabel = "Shift";
        [SerializeField] protected string capsLabel = "Caps";
        [SerializeField] protected string caretLeftLabel = "<";
        [SerializeField] protected string caretRightLabel = ">";
        [SerializeField] protected string confirmLabel = "Done";

        public UIK2DButtonTextField field { get; private set; }
        public string text { get; private set; } = string.Empty;
        public UIKShiftState shift { get; private set; }

        private int caretIndex;
        private UIKPlayer openPlayer;
        private readonly List<UIK2DButtonKey> keys = new();

        private static readonly Dictionary<UIKPlayer, UIKTextEntryKeyboard> openKeyboardByPlayer = new();


        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);

            if (!_isOnValidate)
            {
                BuildKeys();
            }
        }

        protected override void OnPreDestroy()
        {
            base.OnPreDestroy();

            Unregister();
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            openKeyboardByPlayer.Clear();
        }

        public static UIKTextEntryKeyboard GetOpen(UIKPlayer _player)
        {
            return _player != null
                && openKeyboardByPlayer.TryGetValue(_player, out UIKTextEntryKeyboard keyboard)
                && keyboard
                ? keyboard
                : null;
        }

        public void Open(UIK2DButtonTextField _field)
        {
            field = _field;
            text = _field ? _field.text : string.Empty;
            caretIndex = text.Length;
            shift = UIKShiftState.Off;

            openPlayer = GetOwningPlayer();
            if (openPlayer != null)
            {
                openKeyboardByPlayer[openPlayer] = this;
            }

            RefreshKeys();
            RefreshDisplay();
            NotifyActionObjects();
        }

        public void PressKey(UIK2DButtonKey _key)
        {
            switch (_key.kind)
            {
                case UIKKeyKind.Character:
                    Type(_key.character);
                    break;
                case UIKKeyKind.Space:
                    Space();
                    break;
                case UIKKeyKind.Backspace:
                    Backspace();
                    break;
                case UIKKeyKind.Shift:
                    CycleShift();
                    break;
                case UIKKeyKind.CaretLeft:
                    MoveCaret(-1);
                    break;
                case UIKKeyKind.CaretRight:
                    MoveCaret(1);
                    break;
                case UIKKeyKind.Confirm:
                    Confirm();
                    break;
            }
        }

        public bool Type(char _character)
        {
            char typed = shift == UIKShiftState.Off ? char.ToLowerInvariant(_character) : char.ToUpperInvariant(_character);
            if (!Insert(typed))
            {
                return false;
            }

            if (shift == UIKShiftState.Once)
            {
                SetShift(UIKShiftState.Off);
            }

            return true;
        }

        public bool Space()
        {
            return Insert(' ');
        }

        public void Backspace()
        {
            if (caretIndex <= 0)
            {
                return;
            }

            text = text.Remove(caretIndex - 1, 1);
            caretIndex--;
            RefreshDisplay();
        }

        public void CycleShift()
        {
            SetShift(shift switch
            {
                UIKShiftState.Off => UIKShiftState.Once,
                UIKShiftState.Once => UIKShiftState.Locked,
                _ => UIKShiftState.Off
            });
        }

        public void MoveCaret(int _offset)
        {
            caretIndex = Mathf.Clamp(caretIndex + _offset, 0, text.Length);
            RefreshDisplay();
        }

        public void Confirm()
        {
            if (field)
            {
                field.SetText(text);
            }

            Close();
        }

        public void Cancel()
        {
            Close();
        }

        public string GetShiftLabel()
        {
            return shift == UIKShiftState.Locked ? capsLabel : shiftLabel;
        }

        protected virtual bool Allows(char _character)
        {
            return field
                ? field.characters.Allows(_character)
                : !char.IsControl(_character);
        }

        private bool Insert(char _character)
        {
            if (!Allows(_character)
                || (field && field.maxTextLength > 0 && text.Length >= field.maxTextLength))
            {
                if (openPlayer?.targetUI is UIKTarget target)
                {
                    UIKFeedback.Raise(UIKFeedbackKind.Denied, target);
                }
                return false;
            }

            text = text.Insert(caretIndex, _character.ToString());
            caretIndex++;
            RefreshDisplay();

            return true;
        }

        private void SetShift(UIKShiftState _shift)
        {
            shift = _shift;
            RefreshKeys();
            NotifyActionObjects();
        }

        // Focus goes back to the field once the screen holding the keyboard has closed
        private void Close()
        {
            UIKPlayer player = openPlayer;
            UIK2DButtonTextField closedField = field;
            Unregister();
            NotifyActionObjects(player);

            OnClosed.Invoke(this);

            if (player != null
                && closedField
                && !player.inputDeviceType.UsesCursor())
            {
                player.TryTargetUI(closedField.GetComponent<UIK2DButton>());
            }
        }

        private void Unregister()
        {
            if (openPlayer != null
                && GetOpen(openPlayer) == this)
            {
                openKeyboardByPlayer.Remove(openPlayer);
            }
            openPlayer = null;
        }

        private void NotifyActionObjects(UIKPlayer _player = null)
        {
            _player ??= openPlayer;
            if (_player == null)
            {
                return;
            }

            foreach (Type type in UIKActionObjectReflector.GetAllActionObjectTypes())
            {
                if (type.IsSubclassOf(typeof(UIKActionObject_TextEntry)))
                {
                    UIKActionObject.ReceiveContext(_player, type);
                }
            }
        }

        private void BuildKeys()
        {
            if (!keyGroup
                || !keyPrefab)
            {
                return;
            }

            List<List<(UIKKeyKind kind, char character)>> rows = new();
            int numColumns = 1;
            foreach (string row in keyRows)
            {
                List<(UIKKeyKind kind, char character)> parsedRow = ParseRow(row);
                rows.Add(parsedRow);
                numColumns = Mathf.Max(numColumns, parsedRow.Count);
            }

            keyGroup.SetNumColumns(numColumns);
            foreach (List<(UIKKeyKind kind, char character)> row in rows)
            {
                foreach ((UIKKeyKind kind, char character) in row)
                {
                    AddKey(kind, character);
                }

                for (int i = row.Count; i < numColumns; i++)
                {
                    keyGroup.AddSpacer(new GameObject("Spacer", typeof(RectTransform)));
                }
            }

            RefreshKeys();
        }

        private void AddKey(UIKKeyKind _kind, char _character)
        {
            GameObject keyGO = Instantiate(keyPrefab);
            if (keyGO.GetComponent<UIK2DButton>() is not UIK2DButton button
                || keyGO.GetComponent<UIK2DButtonKey>() is not UIK2DButtonKey key)
            {
                Destroy(keyGO);
                return;
            }

            key.Setup(_kind, _character);
            keyGroup.AddTarget(button);
            keys.Add(key);
        }

        private static List<(UIKKeyKind kind, char character)> ParseRow(string _row)
        {
            List<(UIKKeyKind kind, char character)> parsedRow = new();
            for (int i = 0; i < _row.Length; i++)
            {
                if (_row[i] == '{'
                    && _row.IndexOf('}', i) is int end
                    && end > i)
                {
                    if (ParseSpecialKey(_row.Substring(i + 1, end - i - 1)) is UIKKeyKind kind)
                    {
                        parsedRow.Add((kind, '\0'));
                    }
                    i = end;
                    continue;
                }

                parsedRow.Add((UIKKeyKind.Character, _row[i]));
            }

            return parsedRow;
        }

        private static UIKKeyKind? ParseSpecialKey(string _name)
        {
            switch (_name)
            {
                case "space":
                    return UIKKeyKind.Space;
                case "backspace":
                    return UIKKeyKind.Backspace;
                case "shift":
                    return UIKKeyKind.Shift;
                case "left":
                    return UIKKeyKind.CaretLeft;
                case "right":
                    return UIKKeyKind.CaretRight;
                case "confirm":
                    return UIKKeyKind.Confirm;
                default:
                    Debug.LogError($"Unknown keyboard key {{{_name}}}");
                    return null;
            }
        }

        private void RefreshKeys()
        {
            foreach (UIK2DButtonKey key in keys)
            {
                if (!key)
                {
                    continue;
                }

                key.SetLabel(GetKeyLabel(key));
                if (key.GetComponent<UIK2DButton>() is UIK2DButton button)
                {
                    bool characterRefused = key.kind == UIKKeyKind.Character
                        && !Allows(key.character);
                    bool spaceRefused = key.kind == UIKKeyKind.Space
                        && !Allows(' ');
                    button.SetLocked(characterRefused || spaceRefused);
                }
            }
        }

        private string GetKeyLabel(UIK2DButtonKey _key)
        {
            switch (_key.kind)
            {
                case UIKKeyKind.Character:
                    return (shift == UIKShiftState.Off ? char.ToLowerInvariant(_key.character) : char.ToUpperInvariant(_key.character)).ToString();
                case UIKKeyKind.Space:
                    return spaceLabel;
                case UIKKeyKind.Backspace:
                    return backspaceLabel;
                case UIKKeyKind.Shift:
                    return GetShiftLabel();
                case UIKKeyKind.CaretLeft:
                    return caretLeftLabel;
                case UIKKeyKind.CaretRight:
                    return caretRightLabel;
                default:
                    return confirmLabel;
            }
        }

        private void RefreshDisplay()
        {
            if (!display)
            {
                return;
            }

            display.SetText(text);
            display.SetCaret(true, caretIndex);
        }
    }
} // UIKit namespace
