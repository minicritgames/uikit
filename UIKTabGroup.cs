using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UIKit
{
    [Serializable]
    public struct UIKTab
    {
        public UIK2DButton button;
        public UIKWidget page;
    }

    public class UIKTabGroup : UIKElement
    {
        [HideInInspector] public UnityEvent<int> OnCurrentTabChanged = new();

        [SerializeField] protected UIKElement_2DLayoutGroup tabButtonGroup;
        [SerializeField] protected UIKWidgetSwitcher pageSwitcher;
        [SerializeField] protected UIKInputDirection pagesDirectionFromTabs = UIKInputDirection.Down;
        [SerializeField] protected bool tabSwitchingWraps = true;
        [SerializeField] protected UIKElement_ScrollView tabScrollView;
        [SerializeField] protected List<UIKTab> tabs = new();

        public int currentTabIndex { get; private set; }


        protected override void OnPreConstruct(bool _isOnValidate)
        {
            base.OnPreConstruct(_isOnValidate);

            if (_isOnValidate)
            {
                return;
            }

            if (tabButtonGroup
                && pageSwitcher)
            {
                tabButtonGroup.navigation.Set(pagesDirectionFromTabs, pageSwitcher);
            }

            foreach (UIKTab tab in tabs)
            {
                BindTab(tab);
            }
            RefreshTabButtons();
        }


        public void AddTab(UIK2DButton _button, UIKWidget _page)
        {
            if (!_button
                || !_page
                || !tabButtonGroup
                || !pageSwitcher)
            {
                return;
            }

            tabButtonGroup.AddTarget(_button);
            _page.transform.SetParent(pageSwitcher.transform, false);

            UIKTab tab = new() { button = _button, page = _page };
            tabs.Add(tab);
            BindTab(tab);

            pageSwitcher.Refresh();
            if (tabs.Count == 1)
            {
                currentTabIndex = 0;
                pageSwitcher.SetCurrentWidget(_page);
            }
            RefreshTabButtons();
        }

        public void ClearTabs()
        {
            foreach (UIKTab tab in tabs)
            {
                tabButtonGroup?.RemoveTarget(tab.button);
                tab.page?.gameObject.SafeDestroy();
            }

            tabs.Clear();
            currentTabIndex = 0;
            pageSwitcher?.Refresh();
        }

        public int GetNumTabs()
        {
            return tabs.Count;
        }

        public int GetTabIndex(UIKWidget _page)
        {
            return tabs.FindIndex(t => t.page == _page);
        }

        public bool IsTabVisible(int _index)
        {
            return _index >= 0
                && _index < tabs.Count
                && tabs[_index].button
                && tabs[_index].button.gameObject.activeSelf;
        }

        public void SetTabVisible(int _index, bool _visible)
        {
            if (_index < 0
                || _index >= tabs.Count
                || !tabs[_index].button
                || IsTabVisible(_index) == _visible)
            {
                return;
            }

            tabButtonGroup.SetTargetVisible(tabs[_index].button, _visible);

            if (!_visible
                && _index == currentTabIndex)
            {
                SelectTab(FindTabRelative(1));
            }
        }

        public bool IsTabSelectable(int _index)
        {
            return IsTabVisible(_index)
                && !tabs[_index].button.locked;
        }

        public void SetTabLocked(int _index, bool _locked)
        {
            if (_index < 0
                || _index >= tabs.Count
                || !tabs[_index].button)
            {
                return;
            }

            tabs[_index].button.SetLocked(_locked);

            if (_locked
                && _index == currentTabIndex)
            {
                SelectTab(FindTabRelative(1));
            }
        }

        public void SelectTab(int _index)
        {
            SelectTab(_index, true);
        }

        public void SelectTab(int _index, bool _feedback)
        {
            if (_index < 0
                || _index >= tabs.Count
                || !IsTabSelectable(_index))
            {
                return;
            }

            if (_index == currentTabIndex)
            {
                RefreshTabButtons();
                return;
            }

            // Captured before the switch, because hiding the old page untargets anything inside it
            UIKPlayer player = GetOwningPlayer();
            UIKTarget previousTarget = player?.targetUI;
            bool targetWasOnTabButton = previousTarget
                && tabs.Exists(t => t.button == previousTarget);
            bool targetWasOnPage = previousTarget
                && tabs[currentTabIndex].page
                && previousTarget.transform.IsChildOf(tabs[currentTabIndex].page.transform);

            currentTabIndex = _index;
            pageSwitcher.SetCurrentWidget(tabs[_index].page);
            RefreshTabButtons();

            if (player != null
                && !player.inputDeviceType.UsesCursor())
            {
                if (targetWasOnTabButton)
                {
                    player.TryTargetUI(tabs[_index].button);
                }
                else if (targetWasOnPage
                    && tabs[_index].page.GetInnerTarget(pagesDirectionFromTabs) is UIKTarget pageTarget)
                {
                    player.TryTargetUI(pageTarget);
                }
            }

            if (_feedback)
            {
                UIKFeedback.Raise(UIKFeedbackKind.TabChanged, this);
            }
            OnCurrentTabChanged.Invoke(currentTabIndex);
        }

        public bool CanSelectTabRelative(int _offset)
        {
            return FindTabRelative(_offset) >= 0;
        }

        public void SelectTabRelative(int _offset)
        {
            SelectTab(FindTabRelative(_offset));
        }

        // Hidden and locked tabs are stepped over; -1 when there is no other selectable tab that far away
        protected int FindTabRelative(int _offset)
        {
            int step = Math.Sign(_offset);
            int remaining = Math.Abs(_offset);
            int index = currentTabIndex;
            for (int i = 0; i < tabs.Count * remaining && remaining > 0; i++)
            {
                index += step;
                if (tabSwitchingWraps)
                {
                    index = ((index % tabs.Count) + tabs.Count) % tabs.Count;
                }
                else if (index < 0
                    || index >= tabs.Count)
                {
                    return -1;
                }

                if (IsTabSelectable(index))
                {
                    remaining--;
                }
            }

            return remaining == 0 && index != currentTabIndex ? index : -1;
        }

        public override UIKTarget GetInnerTarget(UIKInputDirection _direction)
        {
            return pageSwitcher?.GetInnerTarget(_direction) ?? tabButtonGroup?.GetInnerTarget(_direction);
        }

        protected virtual void BindTab(UIKTab _tab)
        {
            _tab.button?.OnClickHandled.AddListener(_ => SelectTab(tabs.FindIndex(t => t.button == _tab.button)));
        }

        protected virtual void RefreshTabButtons()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].button?.GetComponent<UIK2DButtonCheckbox>()?.SetChecked(i == currentTabIndex, false);
            }

            if (pageSwitcher
                && currentTabIndex < tabs.Count)
            {
                pageSwitcher.navigation.Set(GetOppositeDirection(pagesDirectionFromTabs), tabs[currentTabIndex].button);
            }

            if (tabScrollView
                && currentTabIndex < tabs.Count
                && tabs[currentTabIndex].button)
            {
                tabScrollView.ScrollIntoView((RectTransform)tabs[currentTabIndex].button.transform);
            }
        }

        private static UIKInputDirection GetOppositeDirection(UIKInputDirection _direction)
        {
            switch (_direction)
            {
                case UIKInputDirection.Up:
                    return UIKInputDirection.Down;
                case UIKInputDirection.Down:
                    return UIKInputDirection.Up;
                case UIKInputDirection.Left:
                    return UIKInputDirection.Right;
                default:
                    return UIKInputDirection.Left;
            }
        }
    }
} // UIKit namespace
