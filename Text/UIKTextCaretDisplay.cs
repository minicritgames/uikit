using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.UI;

namespace UIKit
{
    public class UIKTextCaretDisplay : MonoBehaviour
    {
        [SerializeField] protected TMP_Text text;
        [SerializeField] protected Graphic caret;
        [SerializeField] protected float blinkInterval = 0.5f;
        [SerializeField] protected bool followLines;
        [SerializeField] protected Color selectionColor = new(0.25f, 0.5f, 1f, 0.5f);
        [SerializeField] protected bool scrollToCaret;

        private bool caretShown;
        private int caretIndex;
        private int selectionStart;
        private int selectionEnd;
        private float blinkStartTime;
        private Rect placedTextRect;
        private float scroll;
        private Vector2 baseTextPosition;
        private readonly List<Image> selectionImages = new();


        protected virtual void Awake()
        {
            // Typed text is shown as is: a typed tag would hide characters and shift every index after it
            if (text)
            {
                text.richText = false;
                baseTextPosition = text.rectTransform.anchoredPosition;
            }
        }

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
                Refresh();
            }

            caret.enabled = selectionStart == selectionEnd
                && Mathf.FloorToInt((Time.unscaledTime - blinkStartTime) / blinkInterval) % 2 == 0;
        }


        public void SetText(string _text)
        {
            if (text)
            {
                text.SetText(_text);
            }

            Refresh();
        }

        public void SetCaret(bool _shown, int _index)
        {
            caretShown = _shown;
            caretIndex = _index;
            blinkStartTime = Time.unscaledTime;

            Refresh();
        }

        public void SetSelection(int _start, int _end)
        {
            selectionStart = Mathf.Min(_start, _end);
            selectionEnd = Mathf.Max(_start, _end);

            Refresh();
        }

        public int GetIndexAt(Vector2 _screenPosition)
        {
            if (!text)
            {
                return 0;
            }

            text.ForceMeshUpdate();
            if (text.textInfo.characterCount == 0)
            {
                return 0;
            }

            Canvas canvas = text.canvas ? text.canvas.rootCanvas : null;
            Camera camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            int index = TMP_TextUtilities.GetCursorIndexFromPosition(text, _screenPosition, camera, out CaretPosition position);
            if (index < 0)
            {
                return 0;
            }

            return position == CaretPosition.Right ? index + 1 : index;
        }

        private void Refresh()
        {
            if (!text)
            {
                return;
            }

            placedTextRect = text.rectTransform.rect;
            text.ForceMeshUpdate();
            RefreshScroll();
            RefreshCaret();
            RefreshSelection();
        }

        private void RefreshCaret()
        {
            if (!caret)
            {
                return;
            }

            caret.enabled = caretShown
                && selectionStart == selectionEnd;
            if (!caretShown)
            {
                return;
            }

            GetCaretPlacement(out float caretX, out float lineCenterY, out _);
            Vector3 caretWorldPosition = text.rectTransform.TransformPoint(new Vector3(caretX, followLines ? lineCenterY : 0f, 0f));
            Vector3 caretPosition = caret.rectTransform.localPosition;
            Vector3 caretParentPosition = caret.rectTransform.parent.InverseTransformPoint(caretWorldPosition);
            caretPosition.x = caretParentPosition.x;
            if (followLines)
            {
                caretPosition.y = caretParentPosition.y;
            }
            caret.rectTransform.localPosition = caretPosition;
        }

        private void RefreshScroll()
        {
            if (!scrollToCaret)
            {
                return;
            }

            TMP_TextInfo textInfo = text.textInfo;
            Rect rect = text.rectTransform.rect;
            float windowTop = rect.yMax - text.margin.y;
            float windowBottom = rect.yMin + text.margin.w;
            float newScroll = 0f;
            if (caretShown
                && textInfo.lineCount > 0)
            {
                float contentTop = textInfo.lineInfo[0].ascender;
                float contentBottom = textInfo.lineInfo[textInfo.lineCount - 1].descender;
                if (contentTop - contentBottom > windowTop - windowBottom)
                {
                    GetCaretPlacement(out _, out _, out TMP_LineInfo caretLine);
                    newScroll = Mathf.Clamp(scroll, windowBottom - caretLine.descender, windowTop - caretLine.ascender);
                    newScroll = Mathf.Clamp(newScroll, windowTop - contentTop, windowBottom - contentBottom);
                }
            }

            scroll = newScroll;
            text.rectTransform.anchoredPosition = baseTextPosition + new Vector2(0f, scroll);
        }

        private void RefreshSelection()
        {
            int imageCount = 0;
            TMP_TextInfo textInfo = text.textInfo;
            int end = Mathf.Min(selectionEnd, textInfo.characterCount);
            if (caretShown
                && selectionStart < end)
            {
                for (int lineNumber = 0; lineNumber < textInfo.lineCount; lineNumber++)
                {
                    TMP_LineInfo line = textInfo.lineInfo[lineNumber];
                    int first = Mathf.Max(selectionStart, line.firstCharacterIndex);
                    int last = Mathf.Min(end - 1, line.lastCharacterIndex);
                    if (first > last)
                    {
                        continue;
                    }

                    float xMin = textInfo.characterInfo[first].origin;
                    float xMax = textInfo.characterInfo[last].xAdvance;
                    PlaceSelectionImage(imageCount, new Rect(xMin, line.descender, xMax - xMin, line.ascender - line.descender));
                    imageCount++;
                }
            }

            for (int i = imageCount; i < selectionImages.Count; i++)
            {
                if (selectionImages[i])
                {
                    selectionImages[i].enabled = false;
                }
            }
        }

        private void PlaceSelectionImage(int _index, Rect _rect)
        {
            while (selectionImages.Count <= _index)
            {
                GameObject imageObject = new("Selection", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                imageObject.GetComponent<LayoutElement>().ignoreLayout = true;
                imageObject.transform.SetParent(text.rectTransform.parent, false);
                Image newImage = imageObject.GetComponent<Image>();
                newImage.raycastTarget = false;
                selectionImages.Add(newImage);
            }

            Image image = selectionImages[_index];
            image.color = selectionColor;
            image.enabled = true;

            RectTransform imageTransform = image.rectTransform;
            int textSiblingIndex = text.rectTransform.GetSiblingIndex();
            if (imageTransform.GetSiblingIndex() > textSiblingIndex)
            {
                imageTransform.SetSiblingIndex(textSiblingIndex);
            }

            Transform parent = imageTransform.parent;
            Vector3 min = parent.InverseTransformPoint(text.rectTransform.TransformPoint(_rect.min));
            Vector3 max = parent.InverseTransformPoint(text.rectTransform.TransformPoint(_rect.max));
            imageTransform.anchorMin = new Vector2(0.5f, 0.5f);
            imageTransform.anchorMax = new Vector2(0.5f, 0.5f);
            imageTransform.pivot = new Vector2(0.5f, 0.5f);
            imageTransform.sizeDelta = new Vector2(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
            imageTransform.localPosition = (min + max) * 0.5f;
        }

        private void GetCaretPlacement(out float _x, out float _lineCenterY, out TMP_LineInfo _line)
        {
            TMP_TextInfo textInfo = text.textInfo;
            int count = Mathf.Min(caretIndex, textInfo.characterCount);
            if (textInfo.characterCount == 0
                || textInfo.lineCount == 0)
            {
                _x = GetEmptyCaretX();
                _lineCenterY = GetEmptyLineCenterY();
                _line = default;
                _line.ascender = _lineCenterY;
                _line.descender = _lineCenterY;
                return;
            }

            if (count < textInfo.characterCount
                && (count == 0 || textInfo.characterInfo[count].lineNumber != textInfo.characterInfo[count - 1].lineNumber))
            {
                TMP_CharacterInfo character = textInfo.characterInfo[count];
                _x = character.origin;
                _line = textInfo.lineInfo[character.lineNumber];
            }
            else
            {
                TMP_CharacterInfo previous = textInfo.characterInfo[count - 1];
                _x = previous.xAdvance;
                _line = textInfo.lineInfo[previous.lineNumber];
            }

            _lineCenterY = (_line.ascender + _line.descender) * 0.5f;
        }

        private float GetEmptyCaretX()
        {
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

        // Where the first line would sit, from the font's line metrics, since empty text has no lines to read
        private float GetEmptyLineCenterY()
        {
            Rect rect = text.rectTransform.rect;
            float top = rect.yMax - text.margin.y;
            float bottom = rect.yMin + text.margin.w;
            if (!text.font)
            {
                return (top + bottom) * 0.5f;
            }

            FaceInfo faceInfo = text.font.faceInfo;
            float scale = text.fontSize / faceInfo.pointSize * faceInfo.scale;
            float lineHeight = (faceInfo.ascentLine - faceInfo.descentLine) * scale;
            switch (text.verticalAlignment)
            {
                case VerticalAlignmentOptions.Top:
                    return top - lineHeight * 0.5f;
                case VerticalAlignmentOptions.Bottom:
                    return bottom + lineHeight * 0.5f;
                default:
                    return (top + bottom) * 0.5f;
            }
        }
    }
} // UIKit namespace
