using System;
using Cysharp.Threading.Tasks;
using EventSystem;
using UnityEngine;

namespace AudioSystem
{
    internal sealed class AudioEventBridge : IDisposable
    {
        private readonly IEventBus _events;
        private readonly ISoundService _sound;
        private bool _subscribed;

        internal AudioEventBridge(IEventBus events, ISoundService sound)
        {
            _events = events;
            _sound = sound;
        }

        internal void Subscribe()
        {
            if (_subscribed) return;
            _events.Subscribe<PlaySoundRequested>(OnPlaySound);
            _events.Subscribe<PlayMusicRequested>(OnPlayMusic);
            _events.Subscribe<StopMusicRequested>(OnStopMusic);
            _events.Subscribe<StopAllEffectsRequested>(OnStopAllEffects);
            _subscribed = true;
        }

        public void Dispose()
        {
            if (!_subscribed) return;
            _events.Unsubscribe<PlaySoundRequested>(OnPlaySound);
            _events.Unsubscribe<PlayMusicRequested>(OnPlayMusic);
            _events.Unsubscribe<StopMusicRequested>(OnStopMusic);
            _events.Unsubscribe<StopAllEffectsRequested>(OnStopAllEffects);
            _subscribed = false;
        }

        private void OnPlaySound(PlaySoundRequested request)
        {
            switch (request.Placement)
            {
                case SoundPlacement.WorldPosition:
                    _sound.PlayAt(request.Sound, request.PositionOrOffset, request.Options);
                    break;
                case SoundPlacement.FollowTarget:
                    _sound.PlayAttached(request.Sound, request.Target, request.PositionOrOffset, request.Options);
                    break;
                case SoundPlacement.CatalogDefault:
                    _sound.Play(request.Sound, request.Options);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
        }

        private void OnPlayMusic(PlayMusicRequested request) =>
            _sound.PlayMusicAsync(request.Sound, request.TransitionSeconds).Forget(LogError);

        private void OnStopMusic(StopMusicRequested request) =>
            _sound.StopMusicAsync(request.FadeSeconds).Forget(LogError);

        private void OnStopAllEffects(StopAllEffectsRequested request) => _sound.StopAllEffects();

        private static void LogError(Exception exception)
        {
            if (exception is not OperationCanceledException) Debug.LogException(exception);
        }
    }
}
