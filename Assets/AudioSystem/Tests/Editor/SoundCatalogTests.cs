using System;
using System.Collections.Generic;
using AudioSystem.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AudioSystem.Tests
{
    public sealed class SoundCatalogTests
    {
        private const string ClipName = "Catalog Test Clip";
        private const string EntriesField = "entries";
        private const string IdField = "id";
        private const string UiClickId = "UiClick";
        private const string EngineLoopId = "EngineLoop";
        private const string NewSoundId = "NewSound";
        private SoundCatalog _catalog;
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            _catalog = ScriptableObject.CreateInstance<SoundCatalog>();
            _clip = AudioClip.Create(ClipName, 44100, 1, 44100, false);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_catalog);
            UnityEngine.Object.DestroyImmediate(_clip);
        }

        [Test]
        public void ReorderingDoesNotChangeKeysOrGeneratedSource()
        {
            var first = _catalog.Add(UiClickId, SoundData.FromClip(_clip));
            var secondId = _catalog.Add(EngineLoopId, SoundData.FromClip(_clip)).StableId;
            var source = SoundIdGenerator.BuildSource(_catalog);
            var firstKey = new SoundKey(_catalog.Identity, first.StableId);
            var serialized = new SerializedObject(_catalog);
            serialized.FindProperty(EntriesField).MoveArrayElement(0, 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(_catalog.Entries[1].Id, Is.EqualTo(UiClickId));
            Assert.That(new SoundKey(_catalog.Identity, _catalog.Entries[1].StableId), Is.EqualTo(firstKey));
            Assert.That(_catalog.Entries[0].StableId, Is.EqualTo(secondId));
            Assert.That(SoundIdGenerator.BuildSource(_catalog), Is.EqualTo(source));
        }

        [Test]
        public void DeletingLastEntryAndReloadingNeverReusesItsNumericId()
        {
            _catalog.Add(UiClickId, SoundData.FromClip(_clip));
            var removed = _catalog.Add(EngineLoopId, SoundData.FromClip(_clip)).StableId;
            var serialized = new SerializedObject(_catalog);
            serialized.FindProperty(EntriesField).DeleteArrayElementAtIndex(1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var restored = ScriptableObject.CreateInstance<SoundCatalog>();
            try
            {
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(_catalog), restored);
                var replacement = restored.Add(NewSoundId, SoundData.FromClip(_clip));
                Assert.That(replacement.StableId, Is.GreaterThan(removed));
            }
            finally { UnityEngine.Object.DestroyImmediate(restored); }
        }

        [Test]
        public void DuplicatingSerializedRowAllocatesNewNumericIdAndRequiresUniqueName()
        {
            var original = _catalog.Add(UiClickId, SoundData.FromClip(_clip));
            var serialized = new SerializedObject(_catalog);
            serialized.FindProperty(EntriesField).InsertArrayElementAtIndex(0);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _catalog.Normalize();
            Assert.That(_catalog.Entries[0].StableId, Is.EqualTo(original.StableId));
            Assert.That(_catalog.Entries[1].StableId, Is.Not.EqualTo(original.StableId));
            Assert.Throws<ArgumentException>(() => SoundIdGenerator.BuildSource(_catalog));
        }

        [TestCase("bad id")]
        [TestCase("3DHit")]
        [TestCase("class")]
        [TestCase("None")]
        [TestCase("SoundId")]
        [TestCase("value__")]
        [TestCase("")]
        public void InvalidNamesCannotGenerateCompilableLookingButBrokenEnums(string id)
        {
            _catalog.Add(id, SoundData.FromClip(_clip));
            Assert.Throws<ArgumentException>(() => SoundIdGenerator.BuildSource(_catalog));
        }

        [Test]
        public void InvalidCatalogIsRejectedBeforeAllocatingAudioSources()
        {
            _catalog.Add(UiClickId, new SoundData());
            var settings = ScriptableObject.CreateInstance<AudioSystemSettings>();
            var before = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length;
            try
            {
                Assert.Throws<ArgumentException>(() => new SoundService(settings, _catalog, null));
                Assert.That(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length, Is.EqualTo(before));
                Assert.Throws<ArgumentException>(() => SoundIdGenerator.BuildSource(_catalog));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [Test]
        public void UnknownAndForeignKeysAreRejectedWithoutPlayingAnotherSound()
        {
            var entry = _catalog.Add(UiClickId, SoundData.FromClip(_clip));
            var library = new SoundLibrary(_catalog);
            Assert.Throws<ArgumentException>(() => library.Resolve(default));
            Assert.Throws<ArgumentException>(() => library.Resolve(new SoundKey(Guid.NewGuid(), entry.StableId)));
            Assert.Throws<KeyNotFoundException>(() => library.Resolve(new SoundKey(_catalog.Identity, entry.StableId + 1)));
            Assert.That(library.Resolve(new SoundKey(_catalog.Identity, entry.StableId)), Is.SameAs(entry.Data));
        }

        [Test]
        public void DuplicateNamesAreRejectedByGeneratorAndRuntime()
        {
            _catalog.Add(UiClickId, SoundData.FromClip(_clip));
            _catalog.Add(UiClickId, SoundData.FromClip(_clip));
            Assert.Throws<ArgumentException>(() => SoundIdGenerator.BuildSource(_catalog));
            Assert.Throws<ArgumentException>(() => new SoundLibrary(_catalog));
        }

        [Test]
        public void PlaybackSettingChangesDoNotChangeGeneratedCode()
        {
            var entry = _catalog.Add(UiClickId, SoundData.FromClip(_clip));
            var source = SoundIdGenerator.BuildSource(_catalog);
            JsonUtility.FromJsonOverwrite("{\"volume\":0.25}", entry.Data);
            Assert.That(entry.Data.Volume, Is.EqualTo(0.25f));
            Assert.That(SoundIdGenerator.BuildSource(_catalog), Is.EqualTo(source));
        }

        [TestCase("SoundKey")]
        [TestCase("Guid")]
        [TestCase("Invalid Name")]
        public void EnumTypeNamesCannotConflictWithGeneratedApiTypes(string name)
        {
            var serialized = new SerializedObject(_catalog);
            serialized.FindProperty("enumName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.Throws<ArgumentException>(() => SoundIdGenerator.BuildSource(_catalog));
        }

        [Test]
        public void RenamingPreservesNumericIdAndUpdatesGeneratedName()
        {
            var entry = _catalog.Add(UiClickId, SoundData.FromClip(_clip));
            var number = entry.StableId;
            var serialized = new SerializedObject(_catalog);
            serialized.FindProperty(EntriesField).GetArrayElementAtIndex(0).FindPropertyRelative(IdField).stringValue = NewSoundId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(_catalog.Entries[0].StableId, Is.EqualTo(number));
            Assert.That(SoundIdGenerator.BuildSource(_catalog), Does.Contain(NewSoundId + " = " + number));
        }

    }
}
