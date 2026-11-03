using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    public class UIKElement_SpatialGroup : UIKElement
    {
        [SerializeField] protected float maxAngle = 70f;

        private readonly List<UIKTarget> targets = new();
        private UIKTarget current;


        public void AddTarget(UIKTarget _target)
        {
            if (_target
                && !targets.Contains(_target))
            {
                targets.Add(_target);
            }
        }

        public void RemoveTarget(UIKTarget _target)
        {
            targets.Remove(_target);
            if (current == _target)
            {
                current = null;
            }
        }

        public override UIKTarget GetInnerTarget(UIKInputDirection _direction)
        {
            if (IsAvailable(current))
            {
                return current;
            }

            foreach (UIKTarget target in targets)
            {
                if (IsAvailable(target))
                {
                    return target;
                }
            }

            return null;
        }

        public override UIKTarget GetOuterTarget(UIKInputDirection _direction)
        {
            if (current
                && GetNearestInDirection(current, _direction) is UIKTarget next)
            {
                return next;
            }

            return base.GetOuterTarget(_direction);
        }

        public override void HandleDescendantTargeted(UIKTarget _target, UIKPlayer _player)
        {
            if (targets.Contains(_target))
            {
                current = _target;
            }
        }

        protected UIKTarget GetNearestInDirection(UIKTarget _from, UIKInputDirection _direction)
        {
            Vector2 forward = _direction switch
            {
                UIKInputDirection.Up => Vector2.up,
                UIKInputDirection.Down => Vector2.down,
                UIKInputDirection.Left => Vector2.left,
                _ => Vector2.right,
            };
            float minDot = Mathf.Cos(maxAngle * Mathf.Deg2Rad);
            Vector2 origin = transform.InverseTransformPoint(_from.transform.position);

            UIKTarget nearest = null;
            float nearestScore = float.PositiveInfinity;
            foreach (UIKTarget target in targets)
            {
                if (target == _from
                    || !IsAvailable(target))
                {
                    continue;
                }

                Vector2 offset = (Vector2)transform.InverseTransformPoint(target.transform.position) - origin;
                float distance = offset.magnitude;
                float dot = distance > 0f ? Vector2.Dot(offset / distance, forward) : 0f;
                if (dot < minDot)
                {
                    continue;
                }

                float score = distance * (2f - dot);
                if (score < nearestScore)
                {
                    nearestScore = score;
                    nearest = target;
                }
            }

            return nearest;
        }

        private static bool IsAvailable(UIKTarget _target)
        {
            return _target
                && _target.isActiveAndEnabled
                && _target.interactable;
        }
    }
} // UIKit namespace
