using System.Collections.Generic;

namespace AudioSystem
{
    internal sealed class AudioGainRouter
    {
        private struct BusState
        {
            internal float Volume;
            internal bool Muted;
        }

        private readonly Dictionary<AudioBus, BusState> _states = new();
        internal float MasterVolume { get; set; }
        internal bool MasterMuted { get; set; }

        internal AudioGainRouter(float masterVolume) => MasterVolume = masterVolume;

        internal float GetGain(AudioBus bus)
        {
            if (MasterMuted) return 0f;
            if (bus == null) return MasterVolume;
            var state = GetState(bus);
            return state.Muted ? 0f : MasterVolume * state.Volume;
        }

        internal void SetVolume(AudioBus bus, float volume)
        {
            var state = GetState(bus);
            state.Volume = volume;
            _states[bus] = state;
        }

        internal void SetMuted(AudioBus bus, bool muted)
        {
            var state = GetState(bus);
            state.Muted = muted;
            _states[bus] = state;
        }

        private BusState GetState(AudioBus bus)
        {
            if (_states.TryGetValue(bus, out var state)) return state;
            state = new BusState { Volume = bus.DefaultVolume };
            _states.Add(bus, state);
            return state;
        }
    }
}
