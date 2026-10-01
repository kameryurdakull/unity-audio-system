using System;
using EventSystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AudioSystem
{
    [DisallowMultipleComponent]
    public sealed class AudioSystemScope : LifetimeScope
    {
        private const string MissingSettings = "Assign AudioSystemSettings before starting the audio scope.";
        [SerializeField] private AudioSystemSettings settings;
        [SerializeField] private SoundCatalog catalog;

        protected override void Configure(IContainerBuilder builder)
        {
            if (settings == null) throw new InvalidOperationException(MissingSettings);
            if (!builder.Exists(typeof(IEventBus), includeInterfaceTypes: true, findParentScopes: true))
                builder.RegisterEventBus();
            builder.RegisterSoundSystem(settings, catalog, transform);
        }
    }
}
