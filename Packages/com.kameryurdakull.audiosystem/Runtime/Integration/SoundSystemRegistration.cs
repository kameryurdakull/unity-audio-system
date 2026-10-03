using System;
using EventSystem;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AudioSystem
{
    public static class SoundSystemRegistration
    {
        private const string MissingEventBus = "Register EventSystem.IEventBus before calling RegisterSoundSystem.";

        /// <summary>Register the mandatory EventSystem.IEventBus in this scope or an ancestor before calling this method.</summary>
        public static void RegisterSoundSystem(this IContainerBuilder builder, AudioSystemSettings settings, SoundCatalog catalog,
            Transform parent = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!builder.Exists(typeof(IEventBus), includeInterfaceTypes: true, findParentScopes: true))
                throw new InvalidOperationException(MissingEventBus);
            builder.Register(_ => new SoundService(settings, catalog, parent), Lifetime.Singleton)
                .AsSelf().As<ISoundService>().As<ISoundPlayer>().As<IMusicPlayer>().As<IAudioControls>();
            builder.RegisterEntryPoint<AudioSystemEntryPoint>();
        }
    }
}
