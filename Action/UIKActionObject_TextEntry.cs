namespace UIKit
{
    public abstract class UIKActionObject_TextEntry : UIKActionObject
    {
        public override bool CanExecute()
        {
            return base.CanExecute()
                && UIKTextEntryKeyboard.GetOpen(player);
        }

        public override void Execute()
        {
            if (UIKTextEntryKeyboard.GetOpen(player) is UIKTextEntryKeyboard keyboard)
            {
                Execute(keyboard);
            }
        }

        protected abstract void Execute(UIKTextEntryKeyboard _keyboard);
    }

    [UIKActionObject(actionText = "Delete")]
    public class UIKActionObject_TextEntryBackspace : UIKActionObject_TextEntry
    {
        protected override void Execute(UIKTextEntryKeyboard _keyboard)
        {
            _keyboard.Backspace();
        }
    }

    [UIKActionObject(actionText = "Space")]
    public class UIKActionObject_TextEntrySpace : UIKActionObject_TextEntry
    {
        protected override void Execute(UIKTextEntryKeyboard _keyboard)
        {
            _keyboard.Space();
        }
    }

    [UIKActionObject(actionText = "Shift")]
    public class UIKActionObject_TextEntryShift : UIKActionObject_TextEntry
    {
        public override string GetActionText()
        {
            return UIKTextEntryKeyboard.GetOpen(player) is UIKTextEntryKeyboard keyboard
                ? keyboard.GetShiftLabel()
                : base.GetActionText();
        }

        protected override void Execute(UIKTextEntryKeyboard _keyboard)
        {
            _keyboard.CycleShift();
        }
    }

    [UIKActionObject(actionText = "Cursor Left")]
    public class UIKActionObject_TextEntryCaretLeft : UIKActionObject_TextEntry
    {
        protected override void Execute(UIKTextEntryKeyboard _keyboard)
        {
            _keyboard.MoveCaret(-1);
        }
    }

    [UIKActionObject(actionText = "Cursor Right")]
    public class UIKActionObject_TextEntryCaretRight : UIKActionObject_TextEntry
    {
        protected override void Execute(UIKTextEntryKeyboard _keyboard)
        {
            _keyboard.MoveCaret(1);
        }
    }

    [UIKActionObject(actionText = "Done")]
    public class UIKActionObject_TextEntryConfirm : UIKActionObject_TextEntry
    {
        protected override void Execute(UIKTextEntryKeyboard _keyboard)
        {
            _keyboard.Confirm();
        }
    }
} // UIKit namespace
