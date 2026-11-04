using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    public static class UIKInputBlockers
    {
        private static readonly HashSet<object> blockers = new();

        public static bool isBlocked => blockers.Count > 0;


        public static void Add(object _blocker)
        {
            if (_blocker != null)
            {
                blockers.Add(_blocker);
            }
        }

        public static void Remove(object _blocker)
        {
            if (_blocker != null)
            {
                blockers.Remove(_blocker);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            blockers.Clear();
        }
    }
} // UIKit namespace
