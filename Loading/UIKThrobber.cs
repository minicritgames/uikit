using UnityEngine;
using UnityEngine.UI;

namespace UIKit
{
    public class UIKThrobber : MonoBehaviour
    {
        [SerializeField] private Graphic[] segments;
        [SerializeField] private float stepsPerSecond = 12f;
        [SerializeField] private int trailLength = 4;
        [SerializeField, Range(0f, 1f)] private float restingAlpha = 0.15f;

        private int leadIndex = -1;


        protected virtual void Awake()
        {
            if (segments == null
                || segments.Length == 0)
            {
                segments = GetComponentsInChildren<Graphic>(true);
            }
        }

        protected virtual void OnEnable()
        {
            leadIndex = -1;
            Refresh();
        }

        protected virtual void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (segments.Length == 0)
            {
                return;
            }

            int lead = (int)(Time.unscaledTime * stepsPerSecond) % segments.Length;
            if (lead == leadIndex)
            {
                return;
            }
            leadIndex = lead;

            for (int i = 0; i < segments.Length; i++)
            {
                if (!segments[i])
                {
                    continue;
                }

                int behind = (lead - i + segments.Length) % segments.Length;
                float trail = trailLength > 0 ? 1f - (float)behind / trailLength : 0f;
                segments[i].canvasRenderer.SetAlpha(behind == 0 ? 1f : Mathf.Max(restingAlpha, trail));
            }
        }
    }
} // UIKit namespace
