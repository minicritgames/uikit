using System;
using System.Collections.Generic;

namespace UIKit
{
    [Serializable]
    public enum UIKScreenMotionProperty
    {
        Alpha,
        OffsetX,
        OffsetY,
        Scale,
        Rotation
    }

    [Serializable]
    public class UIKScreenMotionTrack
    {
        public UIKScreenMotionProperty property;
        // Offsets are fractions of the parent's size, rotation is in degrees
        public float awayValue;
        public float duration = 0.2f;
        public float delay;
        public UIKEase ease = UIKEase.CubicOut;
        // Empty moves the screen itself; otherwise every motion element of that group, each one stagger seconds after the last
        public string group;
        public float stagger;
    }

    [Serializable]
    public class UIKScreenMotion
    {
        public List<UIKScreenMotionTrack> tracks = new();


        public bool HasTracks()
        {
            return tracks != null
                && tracks.Count > 0;
        }
    }

    public struct UIKScreenPose
    {
        public float alpha;
        public float offsetX;
        public float offsetY;
        public float scale;
        public float rotation;

        public static UIKScreenPose rest => new()
        {
            alpha = 1f,
            offsetX = 0f,
            offsetY = 0f,
            scale = 1f,
            rotation = 0f
        };


        public float Get(UIKScreenMotionProperty _property)
        {
            switch (_property)
            {
                case UIKScreenMotionProperty.Alpha:
                    return alpha;
                case UIKScreenMotionProperty.OffsetX:
                    return offsetX;
                case UIKScreenMotionProperty.OffsetY:
                    return offsetY;
                case UIKScreenMotionProperty.Scale:
                    return scale;
                case UIKScreenMotionProperty.Rotation:
                    return rotation;
                default:
                    return 0f;
            }
        }

        public void Set(UIKScreenMotionProperty _property, float _value)
        {
            switch (_property)
            {
                case UIKScreenMotionProperty.Alpha:
                    alpha = _value;
                    break;
                case UIKScreenMotionProperty.OffsetX:
                    offsetX = _value;
                    break;
                case UIKScreenMotionProperty.OffsetY:
                    offsetY = _value;
                    break;
                case UIKScreenMotionProperty.Scale:
                    scale = _value;
                    break;
                case UIKScreenMotionProperty.Rotation:
                    rotation = _value;
                    break;
            }
        }

        public static float GetRestValue(UIKScreenMotionProperty _property)
        {
            return rest.Get(_property);
        }
    }
} // UIKit namespace
