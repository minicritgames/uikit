using UnityEngine;

namespace UIKit
{
    public class UIKScreenMotionBody
    {
        // Whatever layout or other code writes between frames becomes the base the pose is applied on top of;
        // read-backs differ from written values by float error, hence the tolerances
        private const float alphaTolerance = 0.001f;
        private const float positionTolerance = 0.01f;
        private const float scaleTolerance = 0.0001f;
        private const float rotationTolerance = 0.01f;

        public UIKScreenPose pose = UIKScreenPose.rest;

        private readonly RectTransform rectTransform;
        private readonly CanvasGroup canvasGroup;
        private bool written;
        private float baseAlpha;
        private Vector2 basePosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private float writtenAlpha;
        private Vector2 writtenPosition;
        private Vector3 writtenScale;
        private Quaternion writtenRotation;


        public UIKScreenMotionBody(RectTransform _rectTransform, CanvasGroup _canvasGroup)
        {
            rectTransform = _rectTransform;
            canvasGroup = _canvasGroup;
        }


        public bool IsAlive()
        {
            return rectTransform;
        }

        public void Apply(bool _hidden)
        {
            if (!rectTransform)
            {
                return;
            }

            if (canvasGroup)
            {
                if (!written
                    || Mathf.Abs(canvasGroup.alpha - writtenAlpha) > alphaTolerance)
                {
                    baseAlpha = canvasGroup.alpha;
                }

                writtenAlpha = _hidden ? 0f : baseAlpha * pose.alpha;
                canvasGroup.alpha = writtenAlpha;
            }

            if (!written
                || Vector2.Distance(rectTransform.anchoredPosition, writtenPosition) > positionTolerance)
            {
                basePosition = rectTransform.anchoredPosition;
            }

            if (!written
                || Vector3.Distance(rectTransform.localScale, writtenScale) > scaleTolerance)
            {
                baseScale = rectTransform.localScale;
            }

            if (!written
                || Quaternion.Angle(rectTransform.localRotation, writtenRotation) > rotationTolerance)
            {
                baseRotation = rectTransform.localRotation;
            }

            Vector2 parentSize = rectTransform.parent is RectTransform parentRectTransform ? parentRectTransform.rect.size : Vector2.zero;
            writtenPosition = basePosition + new Vector2(pose.offsetX * parentSize.x, pose.offsetY * parentSize.y);
            writtenScale = baseScale * pose.scale;
            writtenRotation = baseRotation * Quaternion.Euler(0f, 0f, pose.rotation);
            rectTransform.anchoredPosition = writtenPosition;
            rectTransform.localScale = writtenScale;
            rectTransform.localRotation = writtenRotation;
            written = true;
        }
    }
} // UIKit namespace
