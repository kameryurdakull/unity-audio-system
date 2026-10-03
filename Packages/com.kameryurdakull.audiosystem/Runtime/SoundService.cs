using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AudioSystem
{
    public sealed class SoundService : ISoundService, IDisposable
    {
        private readonly AudioThreadGuard _thread = new();
        private readonly AudioSystemSettings _settings;
        private readonly SoundLibrary _library;
        private readonly GameObject _root;
        private readonly AudioGainRouter _gains;
        private readonly SoundVoicePool _effects;
        private readonly MusicController _music;
        private bool _disposed;

        internal SoundService(AudioSystemSettings settings, SoundCatalog catalog, Transform parent)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _library = new SoundLibrary(catalog);
            _settings = settings;
            _root = new GameObject(AudioConstants.RuntimeRootName);
            _root.transform.SetParent(parent, false);
            _gains = new AudioGainRouter(settings.MasterVolume);
            _effects = new SoundVoicePool(settings, _root.transform, _gains, _thread);
            _music = new MusicController(_root.transform, _gains, _thread);
        }

        public SoundHandle Play(SoundKey id, PlaybackOptions options = default)
        {
            EnsureAvailable();
            var sound = _library.Resolve(id);
            return _effects.Play(sound, options, Vector3.zero, null, Vector3.zero,
                sound.SpatialMode == SoundSpatialMode.ThreeDimensional);
        }

        public SoundHandle PlayAt(SoundKey id, Vector3 position, PlaybackOptions options = default)
        {
            EnsureAvailable();
            var sound = _library.Resolve(id);
            AudioValidation.Position(position, nameof(position));
            return _effects.Play(sound, options, position, null, Vector3.zero, true);
        }

        public SoundHandle PlayAttached(SoundKey id, Transform target, Vector3 localOffset = default,
            PlaybackOptions options = default)
        {
            EnsureAvailable();
            var sound = _library.Resolve(id);
            if (target == null) throw new ArgumentNullException(nameof(target));
            AudioValidation.Position(localOffset, nameof(localOffset));
            return _effects.Play(sound, options, target.position, target, localOffset, true);
        }

        public UniTask PlayMusicAsync(SoundKey id, float transitionSeconds = 1f,
            CancellationToken cancellationToken = default)
        {
            EnsureAvailable();
            var sound = _library.Resolve(id);
            AudioValidation.Finite(transitionSeconds, nameof(transitionSeconds));
            return _music.PlayAsync(sound, Mathf.Max(0f, transitionSeconds), cancellationToken);
        }

        public UniTask StopMusicAsync(float fadeSeconds = 1f, CancellationToken cancellationToken = default)
        {
            EnsureAvailable();
            AudioValidation.Finite(fadeSeconds, nameof(fadeSeconds));
            return _music.StopAsync(Mathf.Max(0f, fadeSeconds), cancellationToken);
        }

        public void StopAllEffects()
        {
            EnsureAvailable();
            _effects.StopAll();
        }

        public void SetMasterVolume(float volume)
        {
            EnsureAvailable();
            AudioValidation.Volume(volume, nameof(volume));
            _gains.MasterVolume = volume;
            ApplyVolumes();
        }

        public void SetMasterMuted(bool muted)
        {
            EnsureAvailable();
            _gains.MasterMuted = muted;
            ApplyVolumes();
        }

        public void SetBusVolume(AudioBus bus, float volume)
        {
            EnsureAvailable();
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            AudioValidation.Volume(volume, nameof(volume));
            _gains.SetVolume(bus, volume);
            ApplyVolumes();
        }

        public void SetBusMuted(AudioBus bus, bool muted)
        {
            EnsureAvailable();
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _gains.SetMuted(bus, muted);
            ApplyVolumes();
        }

        public AudioDiagnostics GetDiagnostics()
        {
            EnsureAvailable();
            _effects.CountActive(out var active, out var paused);
            return new AudioDiagnostics(active, paused, _effects.AllocatedCount, _settings.MaxVoices,
                _music.ActiveCount, _music.IsTransitioning);
        }

        internal void Tick()
        {
            if (_disposed) return;
            _effects.Tick();
            _music.Tick();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _thread.Check();
            _disposed = true;
            _music.Dispose();
            _effects.Dispose();
            if (_root == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(_root);
            else UnityEngine.Object.DestroyImmediate(_root);
        }

        private void ApplyVolumes()
        {
            _effects.ApplyVolumes();
            _music.ApplyVolumes();
        }

        private void EnsureAvailable()
        {
            _thread.Check();
            if (_disposed) throw new ObjectDisposedException(nameof(SoundService));
        }
    }
}
