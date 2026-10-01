using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AudioSystem
{
    public interface ISoundService : ISoundPlayer, IMusicPlayer, IAudioControls
    {
        AudioDiagnostics GetDiagnostics();
    }

    public interface ISoundPlayer
    {
        /// <summary>Plays a catalog entry using its spatial mode. An inactive handle means a playback limit rejected the request.</summary>
        SoundHandle Play(SoundKey sound, PlaybackOptions options = default);

        /// <summary>Plays in 3D at a fixed world position, regardless of the catalog entry's default spatial mode.</summary>
        SoundHandle PlayAt(SoundKey sound, Vector3 position, PlaybackOptions options = default);

        /// <summary>Plays in 3D and follows the target, applying an offset in the target's local space.</summary>
        SoundHandle PlayAttached(SoundKey sound, Transform target, Vector3 localOffset = default,
            PlaybackOptions options = default);

        void StopAllEffects();
    }

    public interface IMusicPlayer
    {
        /// <summary>Cancellation restores the previous track; a new request cancels the previous transition.</summary>
        UniTask PlayMusicAsync(SoundKey sound, float transitionSeconds = 1f,
            CancellationToken cancellationToken = default);
        UniTask StopMusicAsync(float fadeSeconds = 1f, CancellationToken cancellationToken = default);
    }

    public interface IAudioControls
    {
        void SetMasterVolume(float volume);
        void SetMasterMuted(bool muted);
        void SetBusVolume(AudioBus bus, float volume);
        void SetBusMuted(AudioBus bus, bool muted);
    }
}
