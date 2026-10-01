using AudioSystem;
using System;
using EventSystem;
using Game.Audio;
using NUnit.Framework;
using UnityEditor;
using VContainer;

namespace AudioSystem.Demo.Tests
{
    public sealed class GeneratedSoundIdTests
    {
        private const string CatalogPath = "Assets/AudioSystemDemo/Data/SoundCatalog.asset";
        private const string SettingsPath = "Assets/AudioSystemDemo/Data/DemoSettings.asset";
        [Test]
        public void GeneratedEnumResolvesDemoCatalogAndEventBusPublishesItsKey()
        {
            var demo = AssetDatabase.LoadAssetAtPath<SoundCatalog>(CatalogPath);
            var settings = AssetDatabase.LoadAssetAtPath<AudioSystemSettings>(SettingsPath);
            var builder = new ContainerBuilder();
            builder.RegisterEventBus();
            builder.RegisterSoundSystem(settings, demo);
            using var container = builder.Build();
            var player = container.Resolve<ISoundPlayer>();
            Assert.That(player.Play(SoundId.UiClick).IsActive, Is.True);
            Assert.Throws<ArgumentException>(() => player.Play(SoundId.None));
            Assert.That(SoundId.UiClick.ToSoundKey().CatalogIdentity, Is.EqualTo(demo.Identity));
            var events = new EventSystem.EventBus();
            var received = default(SoundKey);
            events.Subscribe<PlaySoundRequested>(request => received = request.Sound);
            events.PublishSound(SoundId.EngineLoop);
            Assert.That(received, Is.EqualTo(SoundId.EngineLoop.ToSoundKey()));
        }
    }
}
