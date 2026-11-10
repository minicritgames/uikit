using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIKScreenMotionElement : MonoBehaviour
    {
        [SerializeField] public string group;

        public UIKScreenMotionBody body
        {
            get
            {
                __body ??= new UIKScreenMotionBody((RectTransform)transform, GetComponent<CanvasGroup>());
                return __body;
            }
        }

        private UIKScreenMotionBody __body;


        // An empty group is the source's own root
        public static List<UIKScreenMotionBody> CollectBodies(Component _source, UIKScreenMotionBody _rootBody, string _group)
        {
            List<UIKScreenMotionBody> bodies = new();
            if (string.IsNullOrEmpty(_group))
            {
                bodies.Add(_rootBody);
                return bodies;
            }

            foreach (UIKScreenMotionElement element in GetOwnElements(_source))
            {
                if (element.group == _group
                    && element.isActiveAndEnabled)
                {
                    bodies.Add(element.body);
                }
            }

            return bodies;
        }

        public static List<UIKScreenMotionBody> CollectAllBodies(Component _source, UIKScreenMotionBody _rootBody)
        {
            List<UIKScreenMotionBody> bodies = new()
            {
                _rootBody
            };
            foreach (UIKScreenMotionElement element in GetOwnElements(_source))
            {
                bodies.Add(element.body);
            }

            return bodies;
        }

        // Elements inside a nested source belong to it, and one on the source's own transform would fight the root's pose
        private static List<UIKScreenMotionElement> GetOwnElements(Component _source)
        {
            List<UIKScreenMotionElement> elements = new();
            foreach (UIKScreenMotionElement element in _source.GetComponentsInChildren<UIKScreenMotionElement>(true))
            {
                if (element.transform != _source.transform
                    && element.GetComponentInParent<UIKScreenMotionBodySource>(true) as Component == _source)
                {
                    elements.Add(element);
                }
            }

            return elements;
        }
    }
} // UIKit namespace
