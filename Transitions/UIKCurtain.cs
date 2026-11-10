using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    [RequireComponent(typeof(RectTransform))]
    public class UIKCurtain : MonoBehaviour, UIKScreenMotionBodySource
    {
        private struct CoveredCallback
        {
            public object holder;
            public Action action;
            public bool releaseAfter;
        }

        [SerializeField] protected CanvasGroup canvasGroup;
        // The incoming motion covers and the outgoing motion reveals
        [SerializeField] protected UIKScreenTransition transition;
        [SerializeField] protected bool blocksInput = true;

        public bool isCovered { get; private set; }
        public bool isHeld => holders.Count > 0;

        private UIKScreenMotionPlayer motionPlayer => __motionPlayer ??= new UIKScreenMotionPlayer(this);
        private UIKScreenMotionBody rootBody => __rootBody ??= new UIKScreenMotionBody((RectTransform)transform, canvasGroup);

        private UIKScreenMotionPlayer __motionPlayer;
        private UIKScreenMotionBody __rootBody;
        private readonly HashSet<object> holders = new();
        private readonly List<CoveredCallback> coveredCallbacks = new();
        private bool revealing;


        protected virtual void Awake()
        {
            if (!isHeld
                && !revealing)
            {
                gameObject.SetActive(false);
            }
        }

        protected virtual void Update()
        {
            bool finished = motionPlayer.Tick(UIKScreenTransitions.GetDeltaTime());
            ApplyPose();
            if (!finished)
            {
                return;
            }

            if (revealing)
            {
                FinishReveal();
            }
            else if (isHeld)
            {
                FinishCover();
            }
        }

        // Turned off by anything else while held, it no longer covers, so the next hold covers again
        protected virtual void OnDisable()
        {
            isCovered = false;
            revealing = false;
            UIKInputBlockers.Remove(this);
            UIKScreenTransitions.ReleaseMotions(this);
        }


        public void Hold(object _holder, Action _onCovered = null, bool _coverInstantly = false)
        {
            if (_holder == null)
            {
                return;
            }

            holders.Add(_holder);
            StartCover(_coverInstantly);
            AddCoveredCallback(_holder, _onCovered, false);
        }

        public void Release(object _holder)
        {
            if (!holders.Remove(_holder))
            {
                return;
            }

            coveredCallbacks.RemoveAll(c => c.holder == _holder);
            if (holders.Count == 0)
            {
                StartReveal();
            }
        }

        public void Cover(Action _whileCovered)
        {
            if (_whileCovered == null)
            {
                return;
            }

            // Delegates to the same method compare equal, so each cover holds by its own token
            object holder = new();
            holders.Add(holder);
            StartCover(false);
            AddCoveredCallback(holder, _whileCovered, true);
        }

        public IReadOnlyList<UIKScreenMotionBody> GetMotionBodies(string _group)
        {
            return UIKScreenMotionElement.CollectBodies(this, rootBody, _group);
        }

        public IReadOnlyList<UIKScreenMotionBody> GetAllMotionBodies()
        {
            return UIKScreenMotionElement.CollectAllBodies(this, rootBody);
        }

        private void AddCoveredCallback(object _holder, Action _action, bool _releaseAfter)
        {
            if (_action == null)
            {
                return;
            }

            coveredCallbacks.Add(new CoveredCallback
            {
                holder = _holder,
                action = _action,
                releaseAfter = _releaseAfter
            });
            if (isCovered)
            {
                RunCoveredCallbacks();
            }
        }

        private void StartCover(bool _instantly)
        {
            if (isCovered
                || (gameObject.activeSelf && !revealing && motionPlayer.isPlaying))
            {
                return;
            }

            bool fromAway = !gameObject.activeSelf;
            gameObject.SetActive(true);
            revealing = false;
            if (blocksInput)
            {
                UIKInputBlockers.Add(this);
            }

            if (_instantly
                || UIKScreenTransitions.instant
                || !transition
                || !transition.incoming.HasTracks())
            {
                motionPlayer.SetRest();
                ApplyPose();
                FinishCover();
                return;
            }

            motionPlayer.PlayTowardRest(transition.incoming, 0f, fromAway);
            ApplyPose();
        }

        private void FinishCover()
        {
            isCovered = true;
            UIKScreenTransitions.HoldMotions(this);
            RunCoveredCallbacks();
        }

        private void RunCoveredCallbacks()
        {
            while (isCovered
                && coveredCallbacks.Count > 0)
            {
                CoveredCallback callback = coveredCallbacks[0];
                coveredCallbacks.RemoveAt(0);
                // A throwing callback must still let go, or the screen stays covered with input blocked
                try
                {
                    callback.action.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                finally
                {
                    if (callback.releaseAfter)
                    {
                        Release(callback.holder);
                    }
                }
            }
        }

        private void StartReveal()
        {
            isCovered = false;
            revealing = true;
            coveredCallbacks.Clear();
            UIKScreenTransitions.ReleaseMotions(this);
            UIKInputBlockers.Remove(this);

            if (!gameObject.activeSelf)
            {
                revealing = false;
                return;
            }

            if (UIKScreenTransitions.instant
                || !transition
                || !transition.outgoing.HasTracks())
            {
                FinishReveal();
                return;
            }

            motionPlayer.PlayTowardAway(transition.outgoing, true);
            ApplyPose();
        }

        private void FinishReveal()
        {
            revealing = false;
            motionPlayer.SetRest();
            gameObject.SetActive(false);
        }

        private void ApplyPose()
        {
            rootBody.Apply(false);
            IReadOnlyList<UIKScreenMotionBody> posedBodies = motionPlayer.posedBodies;
            for (int i = 0; i < posedBodies.Count; i++)
            {
                if (posedBodies[i] != rootBody)
                {
                    posedBodies[i].Apply(false);
                }
            }
        }
    }
} // UIKit namespace
