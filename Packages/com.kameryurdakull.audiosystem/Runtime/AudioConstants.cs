namespace AudioSystem
{
    internal static class AudioConstants
    {
        internal const string MenuRoot = "Audio System/";
        internal const string RuntimeRootName = "Audio System Runtime";
        internal const string EffectSourceName = "Effect Voice";
        internal const string MusicSourceName = "Music Voice";
        internal const float MinimumPitch = 0.01f;
        internal const float MaximumPitch = 3f;
        internal const int MaximumPriority = 256;
        internal const int MusicVoiceCount = 2;
        internal const string MainThreadRequired = "Audio System must be accessed from Unity's main thread.";
        internal const string InvalidSound = "The sound definition must contain at least one clip and no missing clips.";
        internal const string InvalidNumber = "The value must be finite.";
    }

    public enum VoiceOverflowPolicy
    {
        RejectNew,
        StealOldest,
        StealLowerPriority
    }

    public enum SoundSpatialMode
    {
        TwoDimensional,
        ThreeDimensional
    }

    public enum SoundInstanceOverflowPolicy
    {
        RejectNew,
        StealOldest
    }

    public enum SoundDistanceRolloff
    {
        Logarithmic,
        Linear
    }

    public enum FollowTargetLostPolicy
    {
        Stop,
        KeepLastPosition
    }
}
