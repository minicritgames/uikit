using TMPro;
using UnityEngine;

namespace UIKit
{
    public class UIKLoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private float fadeOutSeconds = 0.25f;
        [SerializeField] private bool blocksInput = true;

        public bool isShowing { get; private set; }

        private float fadeOutElapsed;


        protected virtual void Awake()
        {
            if (!isShowing)
            {
                gameObject.SetActive(false);
            }
        }

        protected virtual void Update()
        {
            if (isShowing
                || !canvasGroup)
            {
                return;
            }

            fadeOutElapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = fadeOutSeconds > 0f ? 1f - Mathf.Clamp01(fadeOutElapsed / fadeOutSeconds) : 0f;
            if (canvasGroup.alpha <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        protected virtual void OnDisable()
        {
            UIKInputBlockers.Remove(this);
        }

        public void Show(string _status = null)
        {
            isShowing = true;
            gameObject.SetActive(true);
            if (canvasGroup)
            {
                canvasGroup.alpha = 1f;
            }
            SetStatus(_status);

            if (blocksInput)
            {
                UIKInputBlockers.Add(this);
            }
        }

        public void SetStatus(string _status)
        {
            if (statusText)
            {
                statusText.text = _status ?? string.Empty;
            }
        }

        public void Hide()
        {
            if (!isShowing)
            {
                return;
            }

            isShowing = false;
            fadeOutElapsed = 0f;
            UIKInputBlockers.Remove(this);
            if (!canvasGroup
                || fadeOutSeconds <= 0f)
            {
                gameObject.SetActive(false);
            }
        }
    }
} // UIKit namespace
