using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    internal sealed class SoundVoicePool : IDisposable
    {
        private sealed class Voice
        {
            internal AudioSource Source;
            internal SoundData Sound;
            internal Transform Target;
            internal Vector3 Offset;
            internal bool Following;
            internal bool Active;
            internal bool Paused;
            internal ulong Generation;
            internal float VolumeScale;
        }

        private sealed class SoundState
        {
            internal float LastPlayTime = float.NegativeInfinity;
            internal int LastClipIndex = -1;
        }

        private readonly List<Voice> _voices;
        private readonly Dictionary<SoundData, SoundState> _soundStates = new();
        private readonly AudioSystemSettings _settings;
        private readonly Transform _root;
        private readonly AudioGainRouter _gains;
        private readonly AudioThreadGuard _thread;
        private ulong _generation;
        private bool _disposed;

        internal int AllocatedCount => _voices.Count;

        internal SoundVoicePool(AudioSystemSettings settings, Transform root, AudioGainRouter gains,
            AudioThreadGuard thread)
        {
            _settings = settings;
            _root = root;
            _gains = gains;
            _thread = thread;
            _voices = new List<Voice>(settings.MaxVoices);
            for (var index = 0; index < settings.PrewarmVoices; index++) AddVoice();
        }

        internal SoundHandle Play(SoundData sound, PlaybackOptions options, Vector3 position,
            Transform target, Vector3 offset, bool spatial)
        {
            if (!_soundStates.TryGetValue(sound, out var state))
            {
                state = new SoundState();
                _soundStates.Add(sound, state);
            }

            var now = Time.unscaledTime;
            if (now - state.LastPlayTime < sound.CooldownSeconds) return default;

            var matchingCount = 0;
            for (var index = 0; index < _voices.Count; index++)
                if (_voices[index].Active && _voices[index].Sound == sound) matchingCount++;

            var slot = matchingCount >= sound.MaxInstances
                ? sound.InstanceOverflow == SoundInstanceOverflowPolicy.StealOldest
                    ? FindVictim(VoiceOverflowPolicy.StealOldest, sound, sound.Priority) : -1
                : FindAvailable(sound.Priority);
            if (slot < 0) return default;

            var voice = _voices[slot];
            Release(voice);
            voice.Sound = sound;
            voice.Target = target;
            voice.Offset = offset;
            voice.Following = target != null;
            voice.Active = true;
            voice.Paused = false;
            voice.Generation = ++_generation;
            voice.VolumeScale = options.VolumeScale;
            var clip = sound.SelectClip(ref state.LastClipIndex);
            AudioSourceFactory.Configure(voice.Source, sound, clip,
                sound.SelectPitch() * options.PitchScale, options.LoopOverride ?? sound.Loop, spatial);
            voice.Source.transform.position = target != null ? target.TransformPoint(offset) : position;
            ApplyVolume(voice);
            voice.Source.Play();
            state.LastPlayTime = now;
            return new SoundHandle(this, slot, voice.Generation);
        }

        internal void Tick()
        {
            foreach (var voice in _voices)
            {
                if (!voice.Active) continue;
                if (voice.Following)
                {
                    if (voice.Target != null)
                        voice.Source.transform.position = voice.Target.TransformPoint(voice.Offset);
                    else if (voice.Sound.TargetLostPolicy == FollowTargetLostPolicy.Stop)
                    {
                        Release(voice);
                        continue;
                    }
                    else voice.Following = false;
                }

                if (!voice.Paused && !AudioListener.pause && !voice.Source.isPlaying
                    && !AudioSourceFactory.IsLoading(voice.Source)) Release(voice);
            }
        }

        internal bool IsActive(int slot, ulong generation)
        {
            _thread.Check();
            return !_disposed && slot >= 0 && slot < _voices.Count && _voices[slot].Active
                && _voices[slot].Generation == generation;
        }

        internal bool IsPaused(int slot, ulong generation) => IsActive(slot, generation) && _voices[slot].Paused;

        internal bool Stop(int slot, ulong generation)
        {
            if (!IsActive(slot, generation)) return false;
            Release(_voices[slot]);
            return true;
        }

        internal bool SetPaused(int slot, ulong generation, bool paused)
        {
            if (!IsActive(slot, generation)) return false;
            var voice = _voices[slot];
            if (voice.Paused == paused) return true;
            voice.Paused = paused;
            if (paused) voice.Source.Pause();
            else voice.Source.UnPause();
            return true;
        }

        internal bool SetVolume(int slot, ulong generation, float volumeScale)
        {
            if (!IsActive(slot, generation)) return false;
            AudioValidation.Volume(volumeScale, nameof(volumeScale));
            var voice = _voices[slot];
            voice.VolumeScale = volumeScale;
            ApplyVolume(voice);
            return true;
        }

        internal void ApplyVolumes()
        {
            foreach (var voice in _voices)
                if (voice.Active) ApplyVolume(voice);
        }

        internal void CountActive(out int active, out int paused)
        {
            active = 0;
            paused = 0;
            foreach (var voice in _voices)
            {
                if (!voice.Active) continue;
                active++;
                if (voice.Paused) paused++;
            }
        }

        internal void StopAll()
        {
            foreach (var voice in _voices) Release(voice);
        }

        public void Dispose()
        {
            if (_disposed) return;
            StopAll();
            _soundStates.Clear();
            _disposed = true;
        }

        private int FindAvailable(int priority)
        {
            for (var index = 0; index < _voices.Count; index++)
                if (!_voices[index].Active) return index;
            if (_voices.Count < _settings.MaxVoices) return AddVoice();
            return FindVictim(_settings.OverflowPolicy, null, priority);
        }

        private int FindVictim(VoiceOverflowPolicy policy, SoundData matching, int priority)
        {
            if (policy == VoiceOverflowPolicy.RejectNew) return -1;
            var candidate = -1;
            for (var index = 0; index < _voices.Count; index++)
            {
                var voice = _voices[index];
                if (!voice.Active || (matching != null && voice.Sound != matching)) continue;
                if (policy == VoiceOverflowPolicy.StealLowerPriority && voice.Sound.Priority <= priority) continue;
                if (candidate < 0 || (policy == VoiceOverflowPolicy.StealLowerPriority
                    && voice.Sound.Priority > _voices[candidate].Sound.Priority)
                    || (voice.Sound.Priority == _voices[candidate].Sound.Priority
                        && voice.Generation < _voices[candidate].Generation)
                    || (policy == VoiceOverflowPolicy.StealOldest
                        && voice.Generation < _voices[candidate].Generation)) candidate = index;
            }

            return candidate;
        }

        private int AddVoice()
        {
            _voices.Add(new Voice { Source = AudioSourceFactory.Create(_root, AudioConstants.EffectSourceName) });
            return _voices.Count - 1;
        }

        private void ApplyVolume(Voice voice) =>
            voice.Source.volume = voice.Sound.Volume * voice.VolumeScale * _gains.GetGain(voice.Sound.Bus);

        private static void Release(Voice voice)
        {
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Source.outputAudioMixerGroup = null;
            voice.Active = false;
            voice.Paused = false;
            voice.Following = false;
            voice.Target = null;
            voice.Sound = null;
        }
    }
}
