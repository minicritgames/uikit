using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    public class UIKScreenMotionPlayer
    {
        private class TrackPlayback
        {
            public UIKScreenMotionBody body;
            public UIKScreenMotionProperty property;
            public float from;
            public float to;
            public float duration;
            public float delay;
            public UIKEase ease;
        }

        private struct StartValue
        {
            public UIKScreenMotionBody body;
            public UIKScreenMotionProperty property;
            public float value;
        }

        public bool isPlaying => playbacks.Count > 0;
        public bool isWaiting => isPlaying && elapsed < startDelay;
        public IReadOnlyList<UIKScreenMotionBody> posedBodies => __posedBodies;

        private readonly UIKScreenMotionBodySource source;
        private readonly List<TrackPlayback> playbacks = new();
        private readonly List<UIKScreenMotionBody> __posedBodies = new();
        private float elapsed;
        private float startDelay;


        public UIKScreenMotionPlayer(UIKScreenMotionBodySource _source)
        {
            source = _source;
        }


        // A closing screen drops whatever else it was doing, so it never grows more visible on its way out;
        // a covered one finishes it, so it isn't left half-posed underneath
        public void PlayTowardAway(UIKScreenMotion _motion, bool _closing)
        {
            ForgetDeadBodies();
            if (!_closing)
            {
                SnapToEnd();
            }

            playbacks.Clear();
            elapsed = 0f;
            startDelay = 0f;
            foreach (UIKScreenMotionTrack track in _motion.tracks)
            {
                IReadOnlyList<UIKScreenMotionBody> bodies = source.GetMotionBodies(track.group);
                float restValue = UIKScreenPose.GetRestValue(track.property);
                for (int i = 0; i < bodies.Count; i++)
                {
                    AddPlayback(bodies[i], track, i, bodies[i].pose.Get(track.property), track.awayValue, restValue);
                }
            }
        }

        // Anything the motion doesn't move starts at rest
        public void PlayTowardRest(UIKScreenMotion _motion, float _startDelay, bool _fromAway)
        {
            ForgetDeadBodies();
            playbacks.Clear();

            List<StartValue> startValues = new();
            foreach (UIKScreenMotionTrack track in _motion.tracks)
            {
                foreach (UIKScreenMotionBody body in source.GetMotionBodies(track.group))
                {
                    startValues.Add(new StartValue
                    {
                        body = body,
                        property = track.property,
                        value = _fromAway ? track.awayValue : body.pose.Get(track.property)
                    });
                }
            }

            foreach (UIKScreenMotionBody body in source.GetAllMotionBodies())
            {
                body.pose = UIKScreenPose.rest;
                TrackPosed(body);
            }

            foreach (StartValue startValue in startValues)
            {
                startValue.body.pose.Set(startValue.property, startValue.value);
            }

            elapsed = 0f;
            startDelay = _startDelay;
            foreach (UIKScreenMotionTrack track in _motion.tracks)
            {
                IReadOnlyList<UIKScreenMotionBody> bodies = source.GetMotionBodies(track.group);
                float restValue = UIKScreenPose.GetRestValue(track.property);
                for (int i = 0; i < bodies.Count; i++)
                {
                    AddPlayback(bodies[i], track, i, bodies[i].pose.Get(track.property), restValue, track.awayValue);
                }
            }
        }

        public void SetRest()
        {
            playbacks.Clear();
            foreach (UIKScreenMotionBody body in __posedBodies)
            {
                body.pose = UIKScreenPose.rest;
            }
        }

        public void SnapToEnd()
        {
            foreach (TrackPlayback playback in playbacks)
            {
                playback.body.pose.Set(playback.property, playback.to);
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

                playback.body.pose.Set(playback.property, Mathf.LerpUnclamped(playback.from, playback.to, UIKEasing.Evaluate(playback.ease, t)));
            }

            if (allDone)
            {
                SnapToEnd();
                return true;
            }

            return false;
        }

        private void ForgetDeadBodies()
        {
            __posedBodies.RemoveAll(IsDead);
        }

        private static bool IsDead(UIKScreenMotionBody _body)
        {
            return !_body.IsAlive();
        }

        private void TrackPosed(UIKScreenMotionBody _body)
        {
            if (!__posedBodies.Contains(_body))
            {
                __posedBodies.Add(_body);
            }
        }

        // A track already part of the way there takes the matching part of its duration
        private void AddPlayback(UIKScreenMotionBody _body, UIKScreenMotionTrack _track, int _index, float _from, float _to, float _otherEnd)
        {
            TrackPosed(_body);

            float fullDistance = Mathf.Abs(_to - _otherEnd);
            float distance = Mathf.Abs(_to - _from);
            float duration = fullDistance > 0f ? _track.duration * Mathf.Clamp01(distance / fullDistance) : (distance > 0f ? _track.duration : 0f);
            playbacks.Add(new TrackPlayback
            {
                body = _body,
                property = _track.property,
                from = _from,
                to = _to,
                duration = duration,
                delay = _track.delay + _index * _track.stagger,
                ease = _track.ease
            });
        }
    }
} // UIKit namespace
