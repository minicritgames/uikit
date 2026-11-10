using UnityEngine;

namespace UIKit
{
    [RequireComponent(typeof(UIK2DButtonTooltip))]
    public class UIK2DButtonTag : UIK2DButtonPart
    {
        [SerializeField] protected GameObject icon;

        public bool tagVisible { get; private set; }

        // Looked up on use, since code may set the tag before this object's first Awake
        protected UIK2DButtonTooltip tooltip => GetComponent<UIK2DButtonTooltip>();


        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);

            // Edit-time validation can't toggle GameObjects, so the authored icon state is the starting state
            if (!_isOnValidate)
            {
                SetVisible(icon && icon.activeSelf);
            }
        }


        public void SetVisible(bool _visible)
        {
            tagVisible = _visible;

            if (icon)
            {
                icon.SetActive(tagVisible);
            }

            tooltip?.SetTooltipEnabled(tagVisible);
        }

        public void SetVisible(bool _visible, string _tooltipText)
        {
            tooltip?.SetText(_tooltipText);
            SetVisible(_visible);
        }
    }
} // UIKit namespace
