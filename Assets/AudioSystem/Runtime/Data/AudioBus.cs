using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    [CreateAssetMenu(fileName = nameof(AudioBus), menuName = AudioConstants.MenuRoot + "Audio Bus")]
    public sealed class AudioBus : ScriptableObject
    {
        [SerializeField] private AudioMixerGroup output;
        [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;

        public AudioMixerGroup Output => output;
        public float DefaultVolume => Mathf.Clamp01(defaultVolume);
    }
}
