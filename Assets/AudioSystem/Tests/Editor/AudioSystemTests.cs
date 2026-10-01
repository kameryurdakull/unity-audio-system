using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EventSystem;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VContainer;

namespace AudioSystem.Tests
{
    public sealed class AudioSystemTests
    {
        private const string ClipName = "Audio Test Clip";
        private const string TargetName = "Follow Target";
        private const string MusicRootName = "Music Test Root";
        private const string ClipsField = "clips";
        private const string BusField = "bus";
        private const string LoopField = "loop";
        private const string PriorityField = "priority";
        private const string MaximumInstancesField = "maxInstances";
        private const string InstanceOverflowField = "instanceOverflow";
        private const string CooldownField = "cooldownSeconds";
        private const string PrewarmField = "prewarmVoices";
        private const string MaximumVoicesField = "maxVoices";
        private const string OverflowField = "overflowPolicy";
        private readonly List<UnityEngine.Object> _created = new();
        private SoundService _service;
        private AudioSystemSettings _settings;
        private SoundData _sound;
        private SoundCatalog _catalog;
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            _settings = Create<AudioSystemSettings>();
            Configure(_settings, serialized =>
            {
                serialized.FindProperty(PrewarmField).intValue = 1;
                serialized.FindProperty(MaximumVoicesField).intValue = 2;
                serialized.FindProperty(OverflowField).enumValueIndex = (int)VoiceOverflowPolicy.StealOldest;
            });
            _clip = AudioClip.Create(ClipName, 44100, 1, 44100, false);
            _created.Add(_clip);
            _catalog = Create<SoundCatalog>();
            _sound = CreateSound();
            _service = new SoundService(_settings, _catalog, null);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            foreach (var created in _created)
                if (created != null) UnityEngine.Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void BurstNeverExceedsPoolLimitAndStaleHandlesCannotStopReusedSources()
        {
            var first = _service.Play(KeyFor(_sound));
            var latest = default(SoundHandle);
            for (var index = 0; index < 100; index++) latest = _service.Play(KeyFor(_sound));
            Assert.That(_service.GetDiagnostics().AllocatedEffectSources, Is.EqualTo(2));
            Assert.That(_service.GetDiagnostics().ActiveEffects, Is.EqualTo(2));
            Assert.That(first.IsActive, Is.False);
            Assert.That(first.Stop(), Is.False);
            Assert.That(first.Pause(), Is.False);
            Assert.That(first.SetVolume(0f), Is.False);
            Assert.That(latest.IsActive, Is.True);
        }

        [Test]
        public void PerSoundLimitCanReplaceOldestWithoutGrowingPool()
        {
            Configure(_sound, serialized =>
            {
                serialized.FindProperty(MaximumInstancesField).intValue = 1;
                serialized.FindProperty(InstanceOverflowField).enumValueIndex = (int)SoundInstanceOverflowPolicy.StealOldest;
            });
            var first = _service.Play(KeyFor(_sound));
            var second = _service.Play(KeyFor(_sound));
            Assert.That(first.IsActive, Is.False);
            Assert.That(second.IsActive, Is.True);
            Assert.That(_service.GetDiagnostics().AllocatedEffectSources, Is.EqualTo(1));
        }

        [Test]
        public void CooldownRejectsRepeatedRequestsWithoutConsumingAnotherVoice()
        {
            Configure(_sound, serialized => serialized.FindProperty(CooldownField).floatValue = 10f);
            Assert.That(_service.Play(KeyFor(_sound)).IsActive, Is.True);
            Assert.That(_service.Play(KeyFor(_sound)).IsActive, Is.False);
            Assert.That(_service.GetDiagnostics().ActiveEffects, Is.EqualTo(1));
        }

        [Test]
        public void LowerPriorityPolicyPreservesEqualPriorityAndAllowsMoreImportantSound()
        {
            _service.Dispose();
            Configure(_settings, serialized =>
            {
                serialized.FindProperty(MaximumVoicesField).intValue = 1;
                serialized.FindProperty(OverflowField).enumValueIndex = (int)VoiceOverflowPolicy.StealLowerPriority;
            });
            var important = CreateSound();
            Configure(important, serialized => serialized.FindProperty(PriorityField).intValue = 32);
            _service = new SoundService(_settings, _catalog, null);
            var existing = _service.Play(KeyFor(_sound));
            Assert.That(_service.Play(KeyFor(_sound)).IsActive, Is.False);
            Assert.That(_service.Play(KeyFor(important)).IsActive, Is.True);
            Assert.That(existing.IsActive, Is.False);
        }

        [Test]
        public void AttachedSoundUsesLocalOffsetTracksMotionAndStopsWhenTargetIsDestroyed()
        {
            var targetObject = new GameObject(TargetName);
            _created.Add(targetObject);
            targetObject.transform.position = new Vector3(5f, 2f, 1f);
            targetObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var offset = new Vector3(0f, 0f, 2f);
            var handle = _service.PlayAttached(KeyFor(_sound), targetObject.transform, offset);
            handle.Pause();
            var source = FindEffectSource();
            Assert.That(source.spatialBlend, Is.EqualTo(1f));
            Assert.That(Vector3.Distance(source.transform.position, targetObject.transform.TransformPoint(offset)), Is.LessThan(0.001f));
            targetObject.transform.position = new Vector3(10f, 3f, 4f);
            _service.Tick();
            Assert.That(Vector3.Distance(source.transform.position, targetObject.transform.TransformPoint(offset)), Is.LessThan(0.001f));
            UnityEngine.Object.DestroyImmediate(targetObject);
            _service.Tick();
            Assert.That(handle.IsActive, Is.False);
        }

        [Test]
        public void PausedVoiceIsRetainedAndCanResume()
        {
            var handle = _service.Play(KeyFor(_sound));
            Assert.That(handle.Pause(), Is.True);
            _service.Tick();
            Assert.That(handle.IsActive, Is.True);
            Assert.That(handle.IsPaused, Is.True);
            Assert.That(handle.Resume(), Is.True);
            Assert.That(handle.IsPaused, Is.False);
        }

        [Test]
        public void BusAndMasterVolumesComposeAndMuteDoesNotEraseTheirSettings()
        {
            var bus = Create<AudioBus>();
            Configure(_sound, serialized => serialized.FindProperty(BusField).objectReferenceValue = bus);
            _service.Play(KeyFor(_sound), new PlaybackOptions(volumeScale: 0.5f));
            var source = FindEffectSource();
            _service.SetBusVolume(bus, 0.5f);
            _service.SetMasterVolume(0.5f);
            Assert.That(source.volume, Is.EqualTo(0.125f).Within(0.001f));
            _service.SetBusMuted(bus, true);
            Assert.That(source.volume, Is.Zero);
            _service.SetBusMuted(bus, false);
            _service.SetMasterMuted(true);
            Assert.That(source.volume, Is.Zero);
            _service.SetMasterMuted(false);
            Assert.That(source.volume, Is.EqualTo(0.125f).Within(0.001f));
        }

        [Test]
        public void CancellationRestoresPreviousMusicAndReleasesIncomingTrack()
        {
            var root = new GameObject(MusicRootName);
            _created.Add(root);
            using var music = new MusicController(root.transform, new AudioGainRouter(1f), new AudioThreadGuard());
            var incoming = CreateSound();
            music.PlayAsync(_sound, 0f, default).GetAwaiter().GetResult();
            using var cancellation = new CancellationTokenSource();
            var transition = music.PlayAsync(incoming, 1f, cancellation.Token);
            DOTween.Goto(music, 0.5f);
            cancellation.Cancel();
            Assert.That(transition.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.Throws<OperationCanceledException>(() => transition.GetAwaiter().GetResult());
            Assert.That(music.ActiveCount, Is.EqualTo(1));
            Assert.That(music.IsTransitioning, Is.False);
            var sources = root.GetComponentsInChildren<AudioSource>();
            Assert.That(sources[0].volume, Is.EqualTo(1f).Within(0.001f));
            Assert.That(sources[1].clip, Is.Null);
        }

        [Test]
        public void CancellationOfStopRestoresTrackAndCompletionReleasesBothSources()
        {
            var root = new GameObject(MusicRootName);
            _created.Add(root);
            using var music = new MusicController(root.transform, new AudioGainRouter(1f), new AudioThreadGuard());
            music.PlayAsync(_sound, 0f, default).GetAwaiter().GetResult();
            using var cancellation = new CancellationTokenSource();
            var canceledStop = music.StopAsync(1f, cancellation.Token);
            DOTween.Goto(music, 0.5f);
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => canceledStop.GetAwaiter().GetResult());
            Assert.That(music.ActiveCount, Is.EqualTo(1));
            var stop = music.StopAsync(1f, default);
            DOTween.Complete(music, true);
            stop.GetAwaiter().GetResult();
            Assert.That(music.ActiveCount, Is.Zero);
        }

        [Test]
        public void RapidMusicRequestsStayAtTwoSourcesAndSupersedePreviousTasks()
        {
            var root = new GameObject(MusicRootName);
            _created.Add(root);
            using var music = new MusicController(root.transform, new AudioGainRouter(1f), new AudioThreadGuard());
            var other = CreateSound();
            var previous = music.PlayAsync(_sound, 1f, default);
            for (var index = 0; index < 50; index++)
            {
                var next = music.PlayAsync(index % 2 == 0 ? other : _sound, 1f, default);
                Assert.Throws<OperationCanceledException>(() => previous.GetAwaiter().GetResult());
                previous = next;
            }
            Assert.That(root.GetComponentsInChildren<AudioSource>().Length, Is.EqualTo(2));
            DOTween.Complete(music, true);
            previous.GetAwaiter().GetResult();
            Assert.That(music.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void NewRequestFromCancellationContinuationIsNotOverwrittenByOlderCaller()
        {
            var root = new GameObject(MusicRootName);
            _created.Add(root);
            using var music = new MusicController(root.transform, new AudioGainRouter(1f), new AudioThreadGuard());
            var other = CreateSound();
            var original = music.PlayAsync(_sound, 1f, default);
            var continuationRequest = default(UniTask);
            var awaiter = original.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                try { awaiter.GetResult(); }
                catch (OperationCanceledException) { }
                continuationRequest = music.PlayAsync(other, 1f, default);
            });
            var supersededCaller = music.PlayAsync(_sound, 1f, default);
            Assert.Throws<OperationCanceledException>(() => supersededCaller.GetAwaiter().GetResult());
            Assert.That(continuationRequest.Status, Is.EqualTo(UniTaskStatus.Pending));
            DOTween.Complete(music, true);
            continuationRequest.GetAwaiter().GetResult();
            Assert.That(music.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposingDuringMusicTransitionCancelsTaskAndClearsSources()
        {
            var root = new GameObject(MusicRootName);
            _created.Add(root);
            var music = new MusicController(root.transform, new AudioGainRouter(1f), new AudioThreadGuard());
            var transition = music.PlayAsync(_sound, 1f, default);
            music.Dispose();
            Assert.Throws<OperationCanceledException>(() => transition.GetAwaiter().GetResult());
            foreach (var source in root.GetComponentsInChildren<AudioSource>()) Assert.That(source.clip, Is.Null);
            Assert.That(music.ActiveCount, Is.Zero);
        }

        [Test]
        public void EventBridgeSubscribesOnceAndUnsubscribesOnDisposal()
        {
            var events = new EventBus();
            using var bridge = new AudioEventBridge(events, _service);
            bridge.Subscribe();
            bridge.Subscribe();
            events.Publish(new PlaySoundRequested(KeyFor(_sound)));
            Assert.That(_service.GetDiagnostics().ActiveEffects, Is.EqualTo(1));
            bridge.Dispose();
            events.Publish(new PlaySoundRequested(KeyFor(_sound)));
            Assert.That(_service.GetDiagnostics().ActiveEffects, Is.EqualTo(1));
        }

        [Test]
        public void VContainerRegistersServiceAndOwnsItsLifetime()
        {
            var builder = new ContainerBuilder();
            builder.RegisterEventBus();
            builder.RegisterSoundSystem(_settings, _catalog);
            var container = builder.Build();
            var service = container.Resolve<ISoundService>();
            Assert.That(container.Resolve<ISoundPlayer>(), Is.SameAs(service));
            Assert.That(container.Resolve<IMusicPlayer>(), Is.SameAs(service));
            Assert.That(container.Resolve<IAudioControls>(), Is.SameAs(service));
            Assert.That(service.Play(KeyFor(_sound)).IsActive, Is.True);
            container.Dispose();
            Assert.Throws<ObjectDisposedException>(() => service.Play(KeyFor(_sound)));
        }

        [Test]
        public void RegistrationRequiresEventBusBeforeCreatingRuntimeResources()
        {
            var builder = new ContainerBuilder();
            Assert.Throws<InvalidOperationException>(() => builder.RegisterSoundSystem(_settings, _catalog));
        }

        [Test]
        public void DefaultOptionsHaveUnitGainAndInvalidValuesAreRejectedBeforePlayback()
        {
            Assert.That(default(PlaybackOptions).VolumeScale, Is.EqualTo(1f));
            Assert.That(default(PlaybackOptions).PitchScale, Is.EqualTo(1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PlaybackOptions(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.SetMasterVolume(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => _service.PlayAt(KeyFor(_sound), new Vector3(float.NaN, 0f, 0f)));
            Assert.That(_service.GetDiagnostics().ActiveEffects, Is.Zero);
        }

        private AudioSource FindEffectSource()
        {
            foreach (var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (source.gameObject.name == AudioConstants.EffectSourceName && source.clip != null) return source;
            throw new InvalidOperationException(AudioConstants.EffectSourceName);
        }

        private SoundData CreateSound()
        {
            var sound = Create<SoundDefinition>();
            Configure(sound, serialized =>
            {
                var clips = serialized.FindProperty(ClipsField);
                clips.arraySize = 1;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = _clip;
                serialized.FindProperty(LoopField).boolValue = true;
                serialized.FindProperty(MaximumInstancesField).intValue = 128;
            });
            var data = sound.ExportData();
            _catalog.Add("TestSound" + _catalog.Entries.Count, data);
            return data;
        }

        private SoundKey KeyFor(SoundData sound)
        {
            foreach (var entry in _catalog.Entries)
                if (ReferenceEquals(entry.Data, sound)) return new SoundKey(_catalog.Identity, entry.StableId);
            throw new InvalidOperationException();
        }

        private static void Configure(SoundData target, Action<SerializedObject> apply)
        {
            var temporary = ScriptableObject.CreateInstance<SoundDefinition>();
            try
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(target), temporary);
                Configure(temporary, apply);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(temporary), target);
            }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }

        private T Create<T>() where T : ScriptableObject
        {
            var created = ScriptableObject.CreateInstance<T>();
            _created.Add(created);
            return created;
        }

        private static void Configure(UnityEngine.Object target, Action<SerializedObject> apply)
        {
            var serialized = new SerializedObject(target);
            apply(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
