using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UIKit
{
    public class UIK2DButtonSlider : UIK2DButtonPart, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] public UnityEvent<float> OnValueChanged = new();

        [SerializeField] protected RectTransform track;
        [SerializeField] protected RectTransform fill;
        [SerializeField] protected RectTransform handle;
        [SerializeField] protected TMP_Text valueText;
        [SerializeField] protected string valueFormat = "0.0";
        [SerializeField] protected float minValue;
        [SerializeField] protected float maxValue = 1f;
        [SerializeField] protected float step = 0.1f;
        [SerializeField] protected float defaultValue;

        public float value { get; private set; }

        private readonly List<string> valueLabels = new();


        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);

            if (!_isOnValidate
                || !Application.isPlaying)
            {
                value = Quantize(defaultValue);
            }

            if (_isOnValidate
                || TMP_Settings.instance == null)
            {
                return;
            }

            RefreshVisuals();
        }


        public void OnPointerDown(PointerEventData _eventData)
        {
            SetValueFromPointer(_eventData);
        }

        public void OnDrag(PointerEventData _eventData)
        {
            SetValueFromPointer(_eventData);
        }

        public void OnPointerUp(PointerEventData _eventData)
        {
            UIKFeedback.Raise(UIKFeedbackKind.SliderRelease, uik2DButton);
        }

        public override bool HandleButtonNavigation(UIKPlayer _player, UIKInputDirection _direction)
        {
            switch (_direction)
            {
                case UIKInputDirection.Left:
                    if (!uik2DButton.locked)
                    {
                        StepValue(value - step);
                    }
                    return true;
                case UIKInputDirection.Right:
                    if (!uik2DButton.locked)
                    {
                        StepValue(value + step);
                    }
                    return true;
            }

            return false;
        }

        public virtual void SetRange(float _minValue, float _maxValue, float _step)
        {
            minValue = _minValue;
            maxValue = Mathf.Max(_minValue, _maxValue);
            step = _step;
            value = Quantize(value);
            RefreshVisuals();
        }

        public void SetValueFormat(string _valueFormat)
        {
            valueFormat = _valueFormat;
            RefreshVisuals();
        }

        public void SetValueLabels(List<string> _labels)
        {
            valueLabels.Clear();
            if (_labels != null)
            {
                valueLabels.AddRange(_labels);
            }
            RefreshVisuals();
        }

        public virtual void SetValue(float _value, bool _notify = true)
        {
            _value = Quantize(_value);
            if (Mathf.Approximately(value, _value))
            {
                return;
            }

            value = _value;
            RefreshVisuals();

            if (_notify)
            {
                OnValueChanged.Invoke(value);
            }
        }

        protected void StepValue(float _value)
        {
            float previousValue = value;
            SetValue(_value);
            if (!Mathf.Approximately(value, previousValue))
            {
                UIKFeedback.Raise(UIKFeedbackKind.SliderStep, uik2DButton);
            }
        }

        public float GetNormalizedValue()
        {
            return maxValue > minValue ? Mathf.InverseLerp(minValue, maxValue, value) : 0f;
        }

        protected float Quantize(float _value)
        {
            _value = Mathf.Clamp(_value, minValue, maxValue);
            if (step > 0f)
            {
                int stepDecimals = (decimal.GetBits((decimal)step)[3] >> 16) & 0xFF;
                double stepped = System.Math.Round(minValue + System.Math.Round((_value - minValue) / step) * step, stepDecimals);
                _value = Mathf.Clamp((float)stepped, minValue, maxValue);
            }

            return _value;
        }

        protected void SetValueFromPointer(PointerEventData _eventData)
        {
            if (!track
                || uik2DButton.locked
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(track, _eventData.position, _eventData.pressEventCamera, out Vector2 localPoint))
            {
                return;
            }

            float normalized = Mathf.InverseLerp(track.rect.xMin, track.rect.xMax, localPoint.x);
            StepValue(Mathf.Lerp(minValue, maxValue, normalized));
        }

        protected virtual void RefreshVisuals()
        {
            float normalized = GetNormalizedValue();

            if (fill)
            {
                fill.anchorMax = new Vector2(normalized, fill.anchorMax.y);
            }

            if (handle)
            {
                handle.anchorMin = new Vector2(normalized, handle.anchorMin.y);
                handle.anchorMax = new Vector2(normalized, handle.anchorMax.y);
            }

            if (valueText)
            {
                int labelIndex = Mathf.RoundToInt(value - minValue);
                valueText.SetText(labelIndex >= 0 && labelIndex < valueLabels.Count ? valueLabels[labelIndex] : value.ToString(valueFormat));
            }
        }
    }
} // UIKit namespace
