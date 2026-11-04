using System;
using System.Text;

namespace UIKit
{
    [Flags]
    [Serializable]
    public enum UIKTextCharacters
    {
        None = 0,
        Letters = 1,
        Digits = 2,
        Space = 4,
        Symbols = 8,
        All = Letters | Digits | Space | Symbols
    }

    public static class UIKTextCharactersExtensions
    {
        public static bool Allows(this UIKTextCharacters _characters, char _character)
        {
            if (char.IsControl(_character))
            {
                return false;
            }

            if (char.IsLetter(_character))
            {
                return (_characters & UIKTextCharacters.Letters) != 0;
            }

            if (char.IsDigit(_character))
            {
                return (_characters & UIKTextCharacters.Digits) != 0;
            }

            if (_character == ' ')
            {
                return (_characters & UIKTextCharacters.Space) != 0;
            }

            return (_characters & UIKTextCharacters.Symbols) != 0;
        }

        public static string Filter(this UIKTextCharacters _characters, string _text, int _maxLength)
        {
            if (string.IsNullOrEmpty(_text))
            {
                return string.Empty;
            }

            StringBuilder builder = new();
            foreach (char character in _text)
            {
                if (_maxLength > 0
                    && builder.Length >= _maxLength)
                {
                    break;
                }

                if (_characters.Allows(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }
    }
} // UIKit namespace
