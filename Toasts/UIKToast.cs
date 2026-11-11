using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UIKit
{
    public class UIKToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float fadeInSeconds = 0.15f;
        [SerializeField] private float fadeOutSeconds = 0.5f;

        public UnityEvent<UIKToast> OnFinished { get; } = new();

        public string message { get; private set; }
        public bool isShowing { get; private set; }
        public bool isFadingOut => isShowing && elapsed > fadeInSeconds + holdSeconds;

        private float holdSeconds;
        private float elapsed;


        protected virtual void Awake()
        {
            if (canvasGroup)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        protected virtual void Update()
        {
            if (!isShowing)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= fadeInSeconds + holdSeconds + fadeOutSeconds)
            {
                Finish();
                return;
            }

            SetAlpha(GetAlpha());
        }

        public void Show(string _message, float _holdSeconds)
        {
            message = _message ?? string.Empty;
            holdSeconds = Mathf.Max(0f, _holdSeconds);
            elapsed = 0f;
            isShowing = true;
            gameObject.SetActive(true);
            if (messageText)
            {
                messageText.text = message;
            }
            SetAlpha(GetAlpha());
        }

        public void Restart(float _holdSeconds)
        {
            if (!isShowing
                || isFadingOut)
            {
                return;
            }

            holdSeconds = Mathf.Max(0f, _holdSeconds);
            elapsed = Mathf.Min(elapsed, fadeInSeconds);
        }

        public void Dismiss()
        {
            if (!isShowing)
            {
                return;
            }

            elapsed = Mathf.Max(elapsed, fadeInSeconds + holdSeconds);
        }

        private float GetAlpha()
        {
            if (elapsed < fadeInSeconds)
            {
                return elapsed / fadeInSeconds;
            }

            float fadeOutElapsed = elapsed - fadeInSeconds - holdSeconds;
            if (fadeOutElapsed <= 0f)
            {
                return 1f;
            }

            return fadeOutSeconds > 0f ? 1f - Mathf.Clamp01(fadeOutElapsed / fadeOutSeconds) : 0f;
        }

        private void SetAlpha(float _alpha)
        {
            if (canvasGroup)
            {
                canvasGroup.alpha = _alpha;
            }
        }

        private void Finish()
        {
            isShowing = false;
            gameObject.SetActive(false);
            OnFinished.Invoke(this);
        }
    }
} // UIKit namespace
