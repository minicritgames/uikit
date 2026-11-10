using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    public static class UIKScreenTransitions
    {
        // A hitch (a load step, a teardown) would otherwise skip most of a motion in one frame
        private const float maxDeltaTime = 1f / 20f;

        public static float timeScale = 1f;
        public static bool instant;

        public static bool motionsHeld => motionHolders.Count > 0;

        private static readonly HashSet<object> motionHolders = new();


        public static float GetDeltaTime()
        {
            return Mathf.Min(Time.unscaledDeltaTime, maxDeltaTime) * timeScale;
        }

        public static void HoldMotions(object _holder)
        {
            if (_holder != null)
            {
                motionHolders.Add(_holder);
            }
        }

        public static void ReleaseMotions(object _holder)
        {
            if (_holder != null)
            {
                motionHolders.Remove(_holder);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            motionHolders.Clear();
        }
    }
} // UIKit namespace
