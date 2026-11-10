using System;
using UnityEngine;

namespace UIKit
{
    [Serializable]
    public enum UIKEase
    {
        Linear,
        QuadIn,
        QuadOut,
        QuadInOut,
        CubicIn,
        CubicOut,
        CubicInOut,
        ExpoIn,
        ExpoOut,
        ExpoInOut,
        BackIn,
        BackOut
    }

    public static class UIKEasing
    {
        private const float backOvershoot = 1.70158f;


        public static float Evaluate(UIKEase _ease, float _t)
        {
            float t = Mathf.Clamp01(_t);
            switch (_ease)
            {
                case UIKEase.QuadIn:
                    return t * t;
                case UIKEase.QuadOut:
                    return 1f - (1f - t) * (1f - t);
                case UIKEase.QuadInOut:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                case UIKEase.CubicIn:
                    return t * t * t;
                case UIKEase.CubicOut:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case UIKEase.CubicInOut:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
                case UIKEase.ExpoIn:
                    return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
                case UIKEase.ExpoOut:
                    return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
                case UIKEase.ExpoInOut:
                    if (t <= 0f
                        || t >= 1f)
                    {
                        return t;
                    }
                    return t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
                case UIKEase.BackIn:
                    return (backOvershoot + 1f) * t * t * t - backOvershoot * t * t;
                case UIKEase.BackOut:
                    return 1f + (backOvershoot + 1f) * Mathf.Pow(t - 1f, 3f) + backOvershoot * Mathf.Pow(t - 1f, 2f);
                default:
                    return t;
            }
        }
    }
} // UIKit namespace
