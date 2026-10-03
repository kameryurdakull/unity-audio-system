using EventSystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AudioSystem.PlaybackLab
{
    public sealed class AudioDemoScope : LifetimeScope
    {
        [SerializeField] private AudioSystemSettings settings;
        [SerializeField] private SoundCatalog catalog;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEventBus();
            builder.RegisterSoundSystem(settings, catalog, transform);
            builder.RegisterComponentInHierarchy<AudioDemoController>();
        }
    }
}
