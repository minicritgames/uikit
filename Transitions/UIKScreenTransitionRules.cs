using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    [Serializable]
    public enum UIKScreenMatchKind
    {
        Any,
        Screen,
        Category
    }

    [Serializable]
    public enum UIKScreenTransitionDirection
    {
        Forward,
        Back,
        Both
    }

    [Serializable]
    public class UIKScreenMatcher
    {
        public UIKScreenMatchKind kind;
        public string name;


        public bool Matches(UIKScreen _screen)
        {
            switch (kind)
            {
                case UIKScreenMatchKind.Screen:
                    return _screen
                        && _screen.widgetName == name;
                case UIKScreenMatchKind.Category:
                    return _screen
                        && _screen.transitionCategories != null
                        && _screen.transitionCategories.Contains(name);
                default:
                    return true;
            }
        }

        public int GetSpecificity()
        {
            switch (kind)
            {
                case UIKScreenMatchKind.Screen:
                    return 150;
                case UIKScreenMatchKind.Category:
                    return 50;
                default:
                    return 0;
            }
        }
    }

    // Written as the forward change from one screen to another; going back undoes it
    [Serializable]
    public class UIKScreenTransitionRule
    {
        public UIKScreenMatcher from = new();
        public UIKScreenMatcher to = new();
        public UIKScreenTransitionDirection direction = UIKScreenTransitionDirection.Both;
        public bool reverseOnBack = true;
        public UIKScreenTransition transition;
        public bool overridePriority;
        public int priority;


        public bool Matches(UIKScreen _from, UIKScreen _to, bool _back)
        {
            if (_back)
            {
                return direction != UIKScreenTransitionDirection.Forward
                    && from.Matches(_to)
                    && to.Matches(_from);
            }

            return direction != UIKScreenTransitionDirection.Back
                && from.Matches(_from)
                && to.Matches(_to);
        }

        public int GetPriority()
        {
            return overridePriority ? priority : from.GetSpecificity() + to.GetSpecificity();
        }
    }

    [CreateAssetMenu(menuName = "UIKit/Screen Transition Rules", fileName = "ScreenTransitionRules")]
    public class UIKScreenTransitionRules : ScriptableObject
    {
        public List<UIKScreenTransitionRule> rules = new();

        [NonSerialized] private readonly HashSet<string> warnedTies = new();


        public UIKScreenTransition Resolve(UIKScreen _from, UIKScreen _to, bool _back, out bool _reversed)
        {
            UIKScreenTransitionRule bestRule = null;
            int bestPriority = int.MinValue;
            bool tied = false;
            foreach (UIKScreenTransitionRule rule in rules)
            {
                if (rule == null
                    || !rule.transition
                    || !rule.Matches(_from, _to, _back))
                {
                    continue;
                }

                int rulePriority = rule.GetPriority();
                if (rulePriority > bestPriority)
                {
                    bestRule = rule;
                    bestPriority = rulePriority;
                    tied = false;
                }
                else if (rulePriority == bestPriority)
                {
                    tied = true;
                }
            }

            if (tied
                && warnedTies.Add($"{GetScreenName(_from)}>{GetScreenName(_to)}>{_back}"))
            {
                Debug.LogWarning($"Screen transition rules in {name} tie at priority {bestPriority} for {GetScreenName(_from)} to {GetScreenName(_to)}; using the first");
            }

            _reversed = bestRule != null
                && _back
                && bestRule.reverseOnBack;
            return bestRule?.transition;
        }

        private static string GetScreenName(UIKScreen _screen)
        {
            return _screen ? _screen.widgetName : "nothing";
        }
    }
} // UIKit namespace
