using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UIKit
{
    public class UIKTextCaretDisplay : MonoBehaviour
    {
        [SerializeField] protected TMP_Text text;
        [SerializeField] protected Graphic caret;
        [SerializeField] protected float blinkInterval = 0.5f;

        private bool caretShown;
        private int caretIndex;
        private float blinkStartTime;
        private Rect placedTextRect;


        protected virtual void Update()
        {
            if (!caret
                || !caretShown)
            {
                return;
            }

            // Layout can resize the text after the caret was placed, which moves its characters
            if (text
                && text.rectTransform.rect != placedTextRect)
            {
                RefreshCaret();
            }

            caret.enabled = Mathf.FloorToInt((Time.unscaledTime - blinkStartTime) / blinkInterval) % 2 == 0;
        }


        public void SetText(string _text)
        {
            if (text)
            {
                text.SetText(_text);
            }

            RefreshCaret();
        }

        public void SetCaret(bool _shown, int _index)
        {
            caretShown = _shown;
            caretIndex = _index;
            blinkStartTime = Time.unscaledTime;

            RefreshCaret();
        }

        private void RefreshCaret()
        {
            if (!caret)
            {
                return;
            }

            caret.enabled = caretShown;
            if (!caretShown
                || !text)
            {
                return;
            }

            placedTextRect = text.rectTransform.rect;
            text.ForceMeshUpdate();
            Vector3 caretWorldPosition = text.rectTransform.TransformPoint(new Vector3(GetCaretX(), 0f, 0f));
            Vector3 caretPosition = caret.rectTransform.localPosition;
            caretPosition.x = caret.rectTransform.parent.InverseTransformPoint(caretWorldPosition).x;
            caret.rectTransform.localPosition = caretPosition;
        }

        // In the text's local space
        private float GetCaretX()
        {
            TMP_TextInfo textInfo = text.textInfo;
            int count = Mathf.Min(caretIndex, textInfo.characterCount);
            if (count > 0)
            {
                return textInfo.characterInfo[count - 1].xAdvance;
            }

            if (textInfo.characterCount > 0)
            {
                return textInfo.characterInfo[0].origin;
            }

            Rect rect = text.rectTransform.rect;
            switch (text.horizontalAlignment)
            {
                case HorizontalAlignmentOptions.Left:
                    return rect.xMin + text.margin.x;
                case HorizontalAlignmentOptions.Right:
                    return rect.xMax - text.margin.z;
                default:
                    return rect.center.x;
            }
        }
    }
} // UIKit namespace
