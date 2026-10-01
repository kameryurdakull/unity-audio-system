using System;
using System.Threading;
using UnityEngine;

namespace AudioSystem
{
    internal static class AudioValidation
    {
        internal static void Finite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName, AudioConstants.InvalidNumber);
        }

        internal static void Volume(float value, string parameterName)
        {
            Finite(value, parameterName);
            if (value < 0f || value > 1f) throw new ArgumentOutOfRangeException(parameterName);
        }

        internal static void Position(Vector3 value, string parameterName)
        {
            Finite(value.x, parameterName);
            Finite(value.y, parameterName);
            Finite(value.z, parameterName);
        }

        internal static void Sound(SoundData sound)
        {
            if (sound == null) throw new ArgumentNullException(nameof(sound));
            if (!sound.IsPlayable) throw new ArgumentException(AudioConstants.InvalidSound, nameof(sound));
            Volume(sound.Volume, nameof(sound.Volume));
            Finite(sound.PitchRange.x, nameof(sound.PitchRange));
            Finite(sound.PitchRange.y, nameof(sound.PitchRange));
            Finite(sound.CooldownSeconds, nameof(sound.CooldownSeconds));
            Finite(sound.MinDistance, nameof(sound.MinDistance));
            Finite(sound.MaxDistance, nameof(sound.MaxDistance));
            Finite(sound.DopplerLevel, nameof(sound.DopplerLevel));
        }
    }

    internal sealed class AudioThreadGuard
    {
        private readonly int _threadId = Thread.CurrentThread.ManagedThreadId;
        internal bool IsCurrent => Thread.CurrentThread.ManagedThreadId == _threadId;

        internal void Check()
        {
            if (!IsCurrent) throw new InvalidOperationException(AudioConstants.MainThreadRequired);
        }
    }
}
