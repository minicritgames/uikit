using UnityEngine;
using UnityEngine.UI;

namespace UIKit
{
    public class UIK2DButtonTooltip : UIK2DButtonPart
    {
        [SerializeField] protected UIKTooltip tooltipPrefab;
        [SerializeField] protected string tooltipText;
        [SerializeField] protected RectTransform anchor;
        [SerializeField] protected Vector2 anchorPoint = new(1f, 1f);
        [SerializeField] protected Vector2 offset;

        public bool tooltipEnabled { get; private set; } = true;

        protected UIKTooltip tooltip;


        protected virtual void OnEnable()
        {
            RefreshTooltip();
        }

        protected virtual void OnDisable()
        {
            HideTooltip();
        }

        protected virtual void LateUpdate()
        {
            if (tooltip
                && tooltip.gameObject.activeSelf)
            {
                PlaceTooltip();
            }
        }

        protected override void OnPreDestroy()
        {
            base.OnPreDestroy();

            if (tooltip)
            {
                Destroy(tooltip.gameObject);
            }
        }


        public void SetText(string _text)
        {
            tooltipText = _text;
            RefreshTooltip();
        }

        public string GetText()
        {
            return tooltipText;
        }

        public void SetTooltipEnabled(bool _enabled)
        {
            tooltipEnabled = _enabled;
            RefreshTooltip();
        }

        public void SetTooltipPrefab(UIKTooltip _tooltipPrefab)
        {
            if (tooltip)
            {
                Destroy(tooltip.gameObject);
                tooltip = null;
            }

            tooltipPrefab = _tooltipPrefab;
            RefreshTooltip();
        }

        public void SetAnchor(RectTransform _anchor, Vector2 _anchorPoint)
        {
            anchor = _anchor;
            anchorPoint = _anchorPoint;
        }

        protected override void Button_OnTargeted(UIKPlayer _player)
        {
            base.Button_OnTargeted(_player);

            RefreshTooltip();
        }

        protected override void Button_OnUntargeted(UIKPlayer _player)
        {
            base.Button_OnUntargeted(_player);

            RefreshTooltip();
        }

        protected void RefreshTooltip()
        {
            if (isActiveAndEnabled
                && tooltipEnabled
                && tooltipPrefab
                && !string.IsNullOrEmpty(tooltipText)
                && uik2DButton
                && uik2DButton.targetedByPlayers.Count > 0)
            {
                ShowTooltip();
            }
            else
            {
                HideTooltip();
            }
        }

        protected virtual void ShowTooltip()
        {
            if (!(GetComponentInParent<Canvas>() is Canvas canvas))
            {
                return;
            }

            // Parented to the root canvas so it draws above sibling buttons and escapes scroll view masks
            Transform root = canvas.rootCanvas.transform;
            if (!tooltip)
            {
                tooltip = Instantiate(tooltipPrefab, root, false);
            }
            else if (tooltip.transform.parent != root)
            {
                tooltip.transform.SetParent(root, false);
            }

            tooltip.SetText(tooltipText);
            tooltip.gameObject.SetActive(true);
            tooltip.transform.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip.rectTransform);
            PlaceTooltip();
        }

        protected virtual void HideTooltip()
        {
            if (tooltip)
            {
                tooltip.gameObject.SetActive(false);
            }
        }

        protected void PlaceTooltip()
        {
            RectTransform canvasRect = (RectTransform)tooltip.transform.parent;
            RectTransform anchorRect = anchor ? anchor : (RectTransform)transform;

            Vector3 anchorWorld = anchorRect.TransformPoint(Rect.NormalizedToPoint(anchorRect.rect, anchorPoint));
            Vector2 position = (Vector2)canvasRect.InverseTransformPoint(anchorWorld) + offset;

            Rect tooltipRect = tooltip.rectTransform.rect;
            Rect bounds = canvasRect.rect;
            position.x = Mathf.Clamp(position.x, bounds.xMin - tooltipRect.xMin, bounds.xMax - tooltipRect.xMax);
            position.y = Mathf.Clamp(position.y, bounds.yMin - tooltipRect.yMin, bounds.yMax - tooltipRect.yMax);

            tooltip.rectTransform.localPosition = position;
        }
    }
} // UIKit namespace
