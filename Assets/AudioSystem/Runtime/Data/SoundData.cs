using UnityEngine;

namespace AudioSystem
{
    [System.Serializable]
    public sealed class SoundData
    {
        internal static SoundData FromClip(AudioClip clip) => new() { clips = new[] { clip } };

        [SerializeField] private AudioClip[] clips = System.Array.Empty<AudioClip>();
        [SerializeField] private AudioBus bus;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;
        [SerializeField] private bool loop;
        [SerializeField, Range(0, AudioConstants.MaximumPriority)] private int priority = 128;
        [SerializeField, Min(1)] private int maxInstances = 4;
        [SerializeField, Min(0f)] private float cooldownSeconds;
        [SerializeField] private SoundInstanceOverflowPolicy instanceOverflow;
        [SerializeField] private SoundSpatialMode spatialMode;
        [SerializeField, Min(0.01f)] private float minDistance = 1f;
        [SerializeField, Min(0.01f)] private float maxDistance = 50f;
        [SerializeField] private SoundDistanceRolloff rolloff;
        [SerializeField, Range(0f, 5f)] private float dopplerLevel;
        [SerializeField] private FollowTargetLostPolicy targetLostPolicy;

        public AudioBus Bus => bus;
        public float Volume => Mathf.Clamp01(volume);
        public Vector2 PitchRange => pitchRange;
        public bool Loop => loop;
        public int Priority => Mathf.Clamp(priority, 0, AudioConstants.MaximumPriority);
        public int MaxInstances => Mathf.Max(1, maxInstances);
        public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds);
        public SoundInstanceOverflowPolicy InstanceOverflow => instanceOverflow;
        public SoundSpatialMode SpatialMode => spatialMode;
        public float MinDistance => Mathf.Max(0.01f, minDistance);
        public float MaxDistance => Mathf.Max(MinDistance, maxDistance);
        public SoundDistanceRolloff Rolloff => rolloff;
        public float DopplerLevel => Mathf.Clamp(dopplerLevel, 0f, 5f);
        public FollowTargetLostPolicy TargetLostPolicy => targetLostPolicy;
        public int ClipCount => clips?.Length ?? 0;

        public bool IsPlayable
        {
            get
            {
                if (ClipCount == 0) return false;
                foreach (var clip in clips)
                    if (clip == null) return false;
                return true;
            }
        }

        internal AudioClip SelectClip(ref int previousIndex)
        {
            var count = clips.Length;
            if (previousIndex >= count) previousIndex = -1;
            var index = count == 1 ? 0 : Random.Range(0, count - (previousIndex >= 0 ? 1 : 0));
            if (count > 1 && previousIndex >= 0 && index >= previousIndex) index++;
            previousIndex = index;
            return clips[index];
        }

        internal float SelectPitch()
        {
            var minimum = Mathf.Clamp(pitchRange.x, AudioConstants.MinimumPitch, AudioConstants.MaximumPitch);
            var maximum = Mathf.Clamp(pitchRange.y, minimum, AudioConstants.MaximumPitch);
            return Random.Range(minimum, maximum);
        }

        internal void Validate()
        {
            pitchRange.x = Mathf.Clamp(pitchRange.x, AudioConstants.MinimumPitch, AudioConstants.MaximumPitch);
            pitchRange.y = Mathf.Clamp(pitchRange.y, pitchRange.x, AudioConstants.MaximumPitch);
            minDistance = Mathf.Max(0.01f, minDistance);
            maxDistance = Mathf.Max(minDistance, maxDistance);
            maxInstances = Mathf.Max(1, maxInstances);
            cooldownSeconds = Mathf.Max(0f, cooldownSeconds);
        }
    }
}
