using System;

namespace AudioSystem
{
    public readonly struct PlaybackOptions
    {
        private readonly bool _configured;
        private readonly float _volumeScale;
        private readonly float _pitchScale;

        public float VolumeScale => _configured ? _volumeScale : 1f;
        public float PitchScale => _configured ? _pitchScale : 1f;
        public bool? LoopOverride { get; }

        public PlaybackOptions(float volumeScale = 1f, float pitchScale = 1f, bool? loopOverride = null)
        {
            AudioValidation.Finite(volumeScale, nameof(volumeScale));
            AudioValidation.Finite(pitchScale, nameof(pitchScale));
            if (volumeScale < 0f || volumeScale > 1f) throw new ArgumentOutOfRangeException(nameof(volumeScale));
            if (pitchScale <= 0f) throw new ArgumentOutOfRangeException(nameof(pitchScale));
            _configured = true;
            _volumeScale = volumeScale;
            _pitchScale = pitchScale;
            LoopOverride = loopOverride;
        }
    }
}
