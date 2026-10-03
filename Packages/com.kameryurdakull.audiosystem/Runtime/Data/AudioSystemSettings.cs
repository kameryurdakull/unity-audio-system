using UnityEngine;

namespace AudioSystem
{
    [CreateAssetMenu(fileName = nameof(AudioSystemSettings), menuName = AudioConstants.MenuRoot + "Settings")]
    public sealed class AudioSystemSettings : ScriptableObject
    {
        [SerializeField, Min(0)] private int prewarmVoices = 8;
        [SerializeField, Min(1)] private int maxVoices = 32;
        [SerializeField] private VoiceOverflowPolicy overflowPolicy = VoiceOverflowPolicy.StealLowerPriority;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

        public int PrewarmVoices => Mathf.Clamp(prewarmVoices, 0, MaxVoices);
        public int MaxVoices => Mathf.Max(1, maxVoices);
        public VoiceOverflowPolicy OverflowPolicy => overflowPolicy;
        public float MasterVolume => Mathf.Clamp01(masterVolume);

        private void OnValidate()
        {
            maxVoices = Mathf.Max(1, maxVoices);
            prewarmVoices = Mathf.Clamp(prewarmVoices, 0, maxVoices);
        }
    }
}
