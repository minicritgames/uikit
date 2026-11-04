using UnityEngine;
using UnityEngine.UI;

namespace UIKit
{
    [RequireComponent(typeof(ScrollRect))]
    public class UIKElement_ScrollView : UIKElement
    {
        [SerializeField] protected UIKElement content;
        [SerializeField] protected float scrollIntoViewPadding = 4f;
        [SerializeField] protected bool centerTargets;
        [SerializeField] protected GameObject startOverflowIndicator;
        [SerializeField] protected GameObject endOverflowIndicator;

        protected ScrollRect scrollRect;


        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);

            scrollRect = GetComponent<ScrollRect>();

            if (!_isOnValidate)
            {
                scrollRect.onValueChanged.AddListener(ScrollRect_OnValueChanged);
            }
        }

        protected virtual void OnEnable()
        {
            RefreshOverflowIndicators();
        }


        public override UIKTarget GetInnerTarget(UIKInputDirection _direction)
        {
            return content?.GetInnerTarget(_direction);
        }

        public override void HandleDescendantTargeted(UIKTarget _target, UIKPlayer _player)
        {
            base.HandleDescendantTargeted(_target, _player);

            if (_player != null
                && !_player.inputDeviceType.UsesCursor())
            {
                ScrollIntoView((RectTransform)_target.transform);
            }
        }

        public virtual void ScrollIntoView(RectTransform _target)
        {
            if (!scrollRect
                || !scrollRect.content
                || !scrollRect.viewport)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

            Rect view = scrollRect.viewport.rect;
            Bounds targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollRect.viewport, _target);
            Vector2 offset = new(
                scrollRect.horizontal ? GetOffset(targetBounds.min.x, targetBounds.max.x, view.xMin, view.xMax) : 0f,
                scrollRect.vertical ? GetOffset(targetBounds.min.y, targetBounds.max.y, view.yMin, view.yMax) : 0f);

            if (offset != Vector2.zero)
            {
                scrollRect.StopMovement();
                scrollRect.content.anchoredPosition -= offset;
                scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(scrollRect.horizontalNormalizedPosition);
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            }

            RefreshOverflowIndicators();
        }

        protected virtual float GetOffset(float _targetMin, float _targetMax, float _viewMin, float _viewMax)
        {
            if (centerTargets)
            {
                return (_targetMin + _targetMax) * 0.5f - (_viewMin + _viewMax) * 0.5f;
            }
            if (_targetMax + scrollIntoViewPadding > _viewMax)
            {
                return _targetMax + scrollIntoViewPadding - _viewMax;
            }
            if (_targetMin - scrollIntoViewPadding < _viewMin)
            {
                return _targetMin - scrollIntoViewPadding - _viewMin;
            }

            return 0f;
        }

        protected virtual void RefreshOverflowIndicators()
        {
            if (!scrollRect
                || !scrollRect.content
                || !scrollRect.viewport)
            {
                return;
            }

            bool horizontal = scrollRect.horizontal;
            float overflow = horizontal
                ? scrollRect.content.rect.width - scrollRect.viewport.rect.width
                : scrollRect.content.rect.height - scrollRect.viewport.rect.height;
            float position = horizontal ? scrollRect.horizontalNormalizedPosition : 1f - scrollRect.verticalNormalizedPosition;
            bool overflows = overflow > 0.5f;

            if (startOverflowIndicator)
            {
                startOverflowIndicator.SetActive(overflows && position > 0.001f);
            }
            if (endOverflowIndicator)
            {
                endOverflowIndicator.SetActive(overflows && position < 0.999f);
            }
        }

        private void ScrollRect_OnValueChanged(Vector2 _position)
        {
            RefreshOverflowIndicators();
        }
    }
} // UIKit namespace
