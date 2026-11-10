using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    public class UIKScreenMotionPlayer
    {
        private class TrackPlayback
        {
            public UIKScreenMotionProperty property;
            public float from;
            public float to;
            public float duration;
            public float delay;
            public UIKEase ease;
        }

        public UIKScreenPose pose => __pose;
        public bool isPlaying => playbacks.Count > 0;
        public bool isWaiting => isPlaying && elapsed < startDelay;

        private UIKScreenPose __pose = UIKScreenPose.rest;
        private readonly List<TrackPlayback> playbacks = new();
        private float elapsed;
        private float startDelay;


        // A leaving screen drops whatever else it was doing, so it never grows more visible on its way out
        public void PlayTowardAway(UIKScreenMotion _motion)
        {
            playbacks.Clear();
            elapsed = 0f;
            startDelay = 0f;
            foreach (UIKScreenMotionTrack track in _motion.tracks)
            {
                float restValue = UIKScreenPose.GetRestValue(track.property);
                AddPlayback(track, __pose.Get(track.property), track.awayValue, restValue);
            }
        }

        public void PlayTowardRest(UIKScreenMotion _motion, float _startDelay, bool _fromAway)
        {
            playbacks.Clear();
            if (_fromAway)
            {
                __pose = UIKScreenPose.rest;
                foreach (UIKScreenMotionTrack track in _motion.tracks)
                {
                    __pose.Set(track.property, track.awayValue);
                }
            }
            else
            {
                UIKScreenPose currentPose = __pose;
                __pose = UIKScreenPose.rest;
                foreach (UIKScreenMotionTrack track in _motion.tracks)
                {
                    __pose.Set(track.property, currentPose.Get(track.property));
                }
            }

            elapsed = 0f;
            startDelay = _startDelay;
            foreach (UIKScreenMotionTrack track in _motion.tracks)
            {
                float restValue = UIKScreenPose.GetRestValue(track.property);
                AddPlayback(track, __pose.Get(track.property), restValue, track.awayValue);
            }
        }

        public void SetRest()
        {
            playbacks.Clear();
            __pose = UIKScreenPose.rest;
        }

        public void SnapToEnd()
        {
            foreach (TrackPlayback playback in playbacks)
            {
                __pose.Set(playback.property, playback.to);
            }

            playbacks.Clear();
        }

        public void SkipDelay()
        {
            startDelay = Mathf.Min(startDelay, elapsed);
        }

        public bool Tick(float _deltaTime)
        {
            if (!isPlaying)
            {
                return false;
            }

            elapsed += _deltaTime;
            bool allDone = true;
            foreach (TrackPlayback playback in playbacks)
            {
                float trackTime = elapsed - startDelay - playback.delay;
                float t = playback.duration > 0f ? trackTime / playback.duration : (trackTime >= 0f ? 1f : 0f);
                if (t < 1f)
                {
                    allDone = false;
                }

                if (trackTime < 0f)
                {
                    continue;
                }

                __pose.Set(playback.property, Mathf.LerpUnclamped(playback.from, playback.to, UIKEasing.Evaluate(playback.ease, t)));
            }

            if (allDone)
            {
                SnapToEnd();
                return true;
            }

            return false;
        }

        // A track already part of the way there takes the matching part of its duration
        private void AddPlayback(UIKScreenMotionTrack _track, float _from, float _to, float _otherEnd)
        {
            float fullDistance = Mathf.Abs(_to - _otherEnd);
            float distance = Mathf.Abs(_to - _from);
            float duration = fullDistance > 0f ? _track.duration * Mathf.Clamp01(distance / fullDistance) : (distance > 0f ? _track.duration : 0f);
            playbacks.Add(new TrackPlayback
            {
                property = _track.property,
                from = _from,
                to = _to,
                duration = duration,
                delay = _track.delay,
                ease = _track.ease
            });
        }
    }
} // UIKit namespace
