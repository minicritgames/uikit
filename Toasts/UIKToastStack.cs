using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UIKit
{
    public class UIKToastStack : MonoBehaviour
    {
        [SerializeField] private UIKToast toastPrefab;
        [SerializeField] private RectTransform container;
        [SerializeField] private int maxVisible = 3;
        [SerializeField] private int maxQueued = 8;
        [SerializeField] private float defaultHoldSeconds = 4f;

        public UnityEvent<string> OnToastShown { get; } = new();

        public int visibleCount => visible.Count;
        public int queuedCount => queued.Count;

        private struct QueuedToast
        {
            public string message;
            public float holdSeconds;
        }

        private readonly List<UIKToast> visible = new();
        private readonly List<UIKToast> idle = new();
        private readonly Queue<QueuedToast> queued = new();


        protected virtual void OnDisable()
        {
            for (int i = visible.Count - 1; i >= 0; i--)
            {
                visible[i].OnFinished.RemoveListener(Toast_OnFinished);
                visible[i].gameObject.SetActive(false);
                idle.Add(visible[i]);
            }
            visible.Clear();
            queued.Clear();
        }

        public void Push(string _message)
        {
            Push(_message, defaultHoldSeconds);
        }

        public void Push(string _message, float _holdSeconds)
        {
            if (string.IsNullOrEmpty(_message)
                || !toastPrefab
                || !isActiveAndEnabled)
            {
                return;
            }

            foreach (UIKToast toast in visible)
            {
                if (toast.message == _message
                    && !toast.isFadingOut)
                {
                    toast.Restart(_holdSeconds);
                    return;
                }
            }

            foreach (QueuedToast queuedToast in queued)
            {
                if (queuedToast.message == _message)
                {
                    return;
                }
            }

            if (visible.Count < Mathf.Max(1, maxVisible))
            {
                ShowToast(_message, _holdSeconds);
                return;
            }

            if (queued.Count >= maxQueued)
            {
                return;
            }

            queued.Enqueue(new QueuedToast { message = _message, holdSeconds = _holdSeconds });
            visible[0].Dismiss();
        }

        public void Clear()
        {
            queued.Clear();
            foreach (UIKToast toast in visible)
            {
                toast.Dismiss();
            }
        }

        private void ShowToast(string _message, float _holdSeconds)
        {
            UIKToast toast = GetToast();
            toast.transform.SetAsLastSibling();
            toast.OnFinished.AddListener(Toast_OnFinished);
            visible.Add(toast);
            toast.Show(_message, _holdSeconds);
            OnToastShown.Invoke(_message);
        }

        private UIKToast GetToast()
        {
            if (idle.Count > 0)
            {
                UIKToast reused = idle[idle.Count - 1];
                idle.RemoveAt(idle.Count - 1);
                return reused;
            }

            return Instantiate(toastPrefab, container ? container : (RectTransform)transform, false);
        }

        private void Toast_OnFinished(UIKToast _toast)
        {
            _toast.OnFinished.RemoveListener(Toast_OnFinished);
            visible.Remove(_toast);
            idle.Add(_toast);

            if (queued.Count > 0
                && isActiveAndEnabled)
            {
                QueuedToast next = queued.Dequeue();
                ShowToast(next.message, next.holdSeconds);
                if (queued.Count > 0)
                {
                    visible[0].Dismiss();
                }
            }
        }
    }
} // UIKit namespace
