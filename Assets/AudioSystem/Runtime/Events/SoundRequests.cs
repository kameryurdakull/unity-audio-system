using UnityEngine;

namespace AudioSystem
{
    public enum SoundPlacement
    {
        CatalogDefault,
        WorldPosition,
        FollowTarget
    }

    public readonly struct PlaySoundRequested
    {
        public SoundKey Sound { get; }
        public PlaybackOptions Options { get; }
        public SoundPlacement Placement { get; }
        public Vector3 PositionOrOffset { get; }
        public Transform Target { get; }

        public PlaySoundRequested(SoundKey sound, PlaybackOptions options = default)
            : this(sound, options, SoundPlacement.CatalogDefault, default, null) { }

        private PlaySoundRequested(SoundKey sound, PlaybackOptions options, SoundPlacement placement,
            Vector3 positionOrOffset, Transform target)
        {
            Sound = sound;
            Options = options;
            Placement = placement;
            PositionOrOffset = positionOrOffset;
            Target = target;
        }

        public static PlaySoundRequested At(SoundKey sound, Vector3 position, PlaybackOptions options = default) =>
            new(sound, options, SoundPlacement.WorldPosition, position, null);

        public static PlaySoundRequested Attached(SoundKey sound, Transform target,
            Vector3 localOffset = default, PlaybackOptions options = default) =>
            new(sound, options, SoundPlacement.FollowTarget, localOffset, target);
    }

    public readonly struct PlayMusicRequested
    {
        public SoundKey Sound { get; }
        public float TransitionSeconds { get; }
        public PlayMusicRequested(SoundKey sound, float transitionSeconds = 1f)
        {
            Sound = sound;
            TransitionSeconds = transitionSeconds;
        }
    }

    public readonly struct StopMusicRequested
    {
        public float FadeSeconds { get; }
        public StopMusicRequested(float fadeSeconds = 1f) => FadeSeconds = fadeSeconds;
    }

    public readonly struct StopAllEffectsRequested { }
}
