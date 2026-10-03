using UnityEngine;

namespace AudioSystem
{
    internal static class AudioSourceFactory
    {
        internal static bool IsLoading(AudioSource source)
        {
            var clip = source.clip;
            return clip != null && (clip.loadState == AudioDataLoadState.Loading
                || clip.loadState == AudioDataLoadState.Unloaded);
        }

        internal static AudioSource Create(Transform parent, string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(parent, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = false;
            return source;
        }

        internal static void Configure(AudioSource source, SoundData sound, AudioClip clip,
            float pitch, bool loop, bool spatial)
        {
            source.Stop();
            source.clip = clip;
            source.loop = loop;
            source.pitch = Mathf.Clamp(pitch, AudioConstants.MinimumPitch, AudioConstants.MaximumPitch);
            source.priority = sound.Priority;
            source.outputAudioMixerGroup = sound.Bus != null ? sound.Bus.Output : null;
            source.spatialBlend = spatial ? 1f : 0f;
            source.minDistance = sound.MinDistance;
            source.maxDistance = sound.MaxDistance;
            source.rolloffMode = sound.Rolloff == SoundDistanceRolloff.Linear
                ? AudioRolloffMode.Linear : AudioRolloffMode.Logarithmic;
            source.dopplerLevel = spatial ? sound.DopplerLevel : 0f;
            source.mute = false;
        }
    }
}
