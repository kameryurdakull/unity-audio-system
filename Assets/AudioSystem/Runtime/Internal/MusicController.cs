using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace AudioSystem
{
    internal sealed class MusicController : IDisposable
    {
        private sealed class MusicVoice
        {
            internal AudioSource Source;
            internal SoundData Sound;
            internal float FadeGain = 1f;
        }

        private sealed class Transition
        {
            internal readonly UniTaskCompletionSource Completion = new();
            internal MusicVoice Previous;
            internal MusicVoice Next;
            internal Sequence Tween;
            internal CancellationTokenRegistration Registration;
        }

        private readonly MusicVoice[] _voices = new MusicVoice[AudioConstants.MusicVoiceCount];
        private readonly AudioGainRouter _gains;
        private readonly AudioThreadGuard _thread;
        private MusicVoice _current;
        private Transition _transition;
        private ulong _requestVersion;
        private bool _disposed;

        internal bool IsTransitioning => _transition != null;
        internal int ActiveCount
        {
            get
            {
                var count = 0;
                foreach (var voice in _voices)
                    if (voice.Sound != null) count++;
                return count;
            }
        }

        internal MusicController(Transform root, AudioGainRouter gains, AudioThreadGuard thread)
        {
            _gains = gains;
            _thread = thread;
            for (var index = 0; index < _voices.Length; index++)
                _voices[index] = new MusicVoice
                {
                    Source = AudioSourceFactory.Create(root, AudioConstants.MusicSourceName)
                };
        }

        internal UniTask PlayAsync(SoundData sound, float duration, CancellationToken cancellationToken)
        {
            _thread.Check();
            if (_disposed) throw new ObjectDisposedException(nameof(MusicController));
            cancellationToken.ThrowIfCancellationRequested();
            var request = ++_requestVersion;
            SupersedeTransition();
            // Completing an old task can synchronously invoke a caller that starts a newer request.
            if (request != _requestVersion) return UniTask.FromCanceled();
            if (_current != null && _current.Sound == sound && _current.Source.isPlaying)
                return UniTask.CompletedTask;

            var next = _voices[0] == _current ? _voices[1] : _voices[0];
            var previousIndex = -1;
            next.Sound = sound;
            next.FadeGain = duration <= 0f ? 1f : 0f;
            AudioSourceFactory.Configure(next.Source, sound, sound.SelectClip(ref previousIndex),
                1f, sound.Loop, false);
            ApplyVolume(next);
            next.Source.Play();

            if (duration <= 0f)
            {
                StopVoice(_current);
                _current = next;
                return UniTask.CompletedTask;
            }

            return StartTransition(_current, next, duration, cancellationToken);
        }

        internal UniTask StopAsync(float duration, CancellationToken cancellationToken)
        {
            _thread.Check();
            if (_disposed) throw new ObjectDisposedException(nameof(MusicController));
            cancellationToken.ThrowIfCancellationRequested();
            var request = ++_requestVersion;
            SupersedeTransition();
            if (request != _requestVersion) return UniTask.FromCanceled();
            if (_current == null) return UniTask.CompletedTask;
            if (duration <= 0f)
            {
                StopVoice(_current);
                _current = null;
                return UniTask.CompletedTask;
            }

            return StartTransition(_current, null, duration, cancellationToken);
        }

        internal void Tick()
        {
            if (_transition != null || _current == null || AudioListener.pause) return;
            if (_current.Source.isPlaying || AudioSourceFactory.IsLoading(_current.Source)) return;
            StopVoice(_current);
            _current = null;
        }

        internal void ApplyVolumes()
        {
            foreach (var voice in _voices)
                if (voice.Sound != null) ApplyVolume(voice);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _thread.Check();
            _disposed = true;
            _requestVersion++;
            var transition = _transition;
            _transition = null;
            if (transition != null)
            {
                transition.Registration.Dispose();
                transition.Tween.Kill();
            }

            foreach (var voice in _voices) StopVoice(voice);
            _current = null;
            transition?.Completion.TrySetCanceled();
        }

        private UniTask StartTransition(MusicVoice previous, MusicVoice next, float duration,
            CancellationToken cancellationToken)
        {
            var transition = new Transition { Previous = previous, Next = next };
            _transition = transition;
            var sequence = DOTween.Sequence().SetTarget(this).SetUpdate(true).SetAutoKill(true);
            transition.Tween = sequence;
            if (previous != null) sequence.Join(CreateFade(previous, 0f, duration));
            if (next != null) sequence.Join(CreateFade(next, 1f, duration));
            sequence.OnComplete(() => CompleteTransition(transition));
            sequence.OnKill(() => CancelTransition(transition, default));
            sequence.Play();

            if (cancellationToken.CanBeCanceled)
            {
                var registration = cancellationToken.Register(() =>
                {
                    if (_thread.IsCurrent) CancelTransition(transition, cancellationToken);
                    else UniTask.Post(() => CancelTransition(transition, cancellationToken));
                });
                if (_transition == transition) transition.Registration = registration;
                else registration.Dispose();
            }

            return transition.Completion.Task;
        }

        private Tween CreateFade(MusicVoice voice, float target, float duration) =>
            DOTween.To(() => voice.FadeGain, value =>
            {
                voice.FadeGain = value;
                ApplyVolume(voice);
            }, target, duration).SetEase(Ease.Linear);

        private void CompleteTransition(Transition transition)
        {
            if (_transition != transition) return;
            _transition = null;
            transition.Registration.Dispose();
            StopVoice(transition.Previous);
            _current = transition.Next;
            transition.Completion.TrySetResult();
        }

        private void SupersedeTransition()
        {
            var transition = _transition;
            if (transition == null) return;
            _transition = null;
            transition.Registration.Dispose();
            transition.Tween.Kill();
            if (transition.Next != null)
            {
                StopVoice(transition.Previous);
                _current = transition.Next;
            }
            else _current = transition.Previous;
            RestoreVolume(_current);
            transition.Completion.TrySetCanceled();
        }

        private void CancelTransition(Transition transition, CancellationToken cancellationToken)
        {
            if (_transition != transition) return;
            _transition = null;
            transition.Registration.Dispose();
            transition.Tween.Kill();
            StopVoice(transition.Next);
            _current = transition.Previous;
            RestoreVolume(_current);
            transition.Completion.TrySetCanceled(cancellationToken);
        }

        private void RestoreVolume(MusicVoice voice)
        {
            if (voice == null) return;
            voice.FadeGain = 1f;
            ApplyVolume(voice);
        }

        private void ApplyVolume(MusicVoice voice) =>
            voice.Source.volume = voice.Sound.Volume * voice.FadeGain * _gains.GetGain(voice.Sound.Bus);

        private static void StopVoice(MusicVoice voice)
        {
            if (voice == null) return;
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Source.outputAudioMixerGroup = null;
            voice.Sound = null;
            voice.FadeGain = 1f;
        }
    }
}
