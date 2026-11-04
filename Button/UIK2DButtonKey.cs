using System;

namespace UIKit
{
    [Serializable]
    public enum UIKKeyKind
    {
        Character,
        Space,
        Backspace,
        Shift,
        CaretLeft,
        CaretRight,
        Confirm
    }

    public class UIK2DButtonKey : UIK2DButtonPart
    {
        public UIKKeyKind kind { get; private set; }
        public char character { get; private set; }


        protected override void Button_OnClickHandled(UIKEventData _eventData)
        {
            base.Button_OnClickHandled(_eventData);

            GetComponentInParent<UIKTextEntryKeyboard>()?.PressKey(this);
        }

        public void Setup(UIKKeyKind _kind, char _character)
        {
            kind = _kind;
            character = _character;
        }

        public void SetLabel(string _label)
        {
            GetComponent<UIK2DButtonLabel>()?.SetText(_label);
        }
    }
} // UIKit namespace
