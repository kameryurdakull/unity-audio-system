using AudioSystem.Editor;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AudioSystem.Demo.Editor
{
    public static class DemoSceneBuilder
    {
        private const string MenuPath = "Tools/Audio System/Create Playback Lab";
        public const string ScenePath = "Assets/AudioSystemDemo/Scenes/AudioPlaybackLab.unity";
        private const string DataFolder = "Assets/AudioSystemDemo/Data";
        private const string ClipsFolder = "Assets/AudioSystemDemo/Clips";
        private const string CatalogName = "SoundCatalog";
        private const string CatalogField = "catalog";
        private const string UiClickId = "UiClick";
        private const string EngineLoopId = "EngineLoop";
        private const string MenuMusicId = "MenuMusic";
        private const string GameplayMusicId = "GameplayMusic";
        private const string SettingsName = "DemoSettings";
        private const string EffectsBusName = "EffectsBus";
        private const string MusicBusName = "MusicBus";
        private const string EffectName = "DemoEffect";
        private const string LoopName = "DemoFollowingLoop";
        private const string MusicAName = "DemoMusicA";
        private const string MusicBName = "DemoMusicB";
        private const string CameraName = "Main Camera";
        private const string CameraTag = "MainCamera";
        private const string LightName = "Directional Light";
        private const string FloorName = "Floor";
        private const string EmitterName = "Moving 3D Emitter";
        private const string RootName = "Audio Playback Lab";
        private const string WaveExtension = ".wav";
        private const string AssetExtension = ".asset";
        private const string RiffChunk = "RIFF";
        private const string WaveChunk = "WAVE";
        private const string FormatChunk = "fmt ";
        private const string DataChunk = "data";
        private const int SampleRate = 22050;
        private const int BytesPerSample = 2;
        private const float ToneAmplitude = 0.18f;

        private static class Fields
        {
            internal const string Settings = "settings";
            internal const string Prewarm = "prewarmVoices";
            internal const string MaximumVoices = "maxVoices";
            internal const string Overflow = "overflowPolicy";
            internal const string Clips = "clips";
            internal const string Bus = "bus";
            internal const string Volume = "volume";
            internal const string Loop = "loop";
            internal const string MaximumInstances = "maxInstances";
            internal const string SpatialMode = "spatialMode";
            internal const string EffectsBus = "effectsBus";
            internal const string MusicBus = "musicBus";
            internal const string Emitter = "emitter";
        }

        [MenuItem(MenuPath)]
        public static void CreateDemo()
        {
            var catalog = EnsureCatalog();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                return;
            }

            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(ClipsFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            AssetDatabase.Refresh();
            var settings = CreateAsset<AudioSystemSettings>(SettingsName);
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty(Fields.Prewarm).intValue = 4;
            serializedSettings.FindProperty(Fields.MaximumVoices).intValue = 16;
            serializedSettings.FindProperty(Fields.Overflow).enumValueIndex = (int)VoiceOverflowPolicy.StealOldest;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            var effectsBus = CreateAsset<AudioBus>(EffectsBusName);
            var musicBus = CreateAsset<AudioBus>(MusicBusName);
            var previousScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var cameraObject = new GameObject(CameraName);
                cameraObject.tag = CameraTag;
                var camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                camera.transform.position = new Vector3(0f, 5f, -10f);
                camera.transform.LookAt(new Vector3(0f, 1f, 6f));
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.08f, 0.12f);
                var lightObject = new GameObject(LightName);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = FloorName;
                floor.transform.position = new Vector3(0f, 0f, 6f);
                floor.transform.localScale = new Vector3(2f, 1f, 2f);
                var emitter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                emitter.name = EmitterName;
                emitter.transform.position = new Vector3(-6f, 1f, 6f);
                var root = new GameObject(RootName);
                var scope = root.AddComponent<AudioDemoScope>();
                var scopeSerialized = new SerializedObject(scope);
                scopeSerialized.FindProperty(Fields.Settings).objectReferenceValue = settings;
                scopeSerialized.FindProperty(CatalogField).objectReferenceValue = catalog;
                scopeSerialized.ApplyModifiedPropertiesWithoutUndo();
                var controller = root.AddComponent<AudioDemoController>();
                var serialized = new SerializedObject(controller);
                serialized.FindProperty(Fields.EffectsBus).objectReferenceValue = effectsBus;
                serialized.FindProperty(Fields.MusicBus).objectReferenceValue = musicBus;
                serialized.FindProperty(Fields.Emitter).objectReferenceValue = emitter.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
            AssetDatabase.SaveAssets();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        public static SoundCatalog EnsureCatalog()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(ClipsFolder);
            AssetDatabase.Refresh();
            var catalog = CreateAsset<SoundCatalog>(CatalogName);
            if (catalog.Entries.Count == 0)
            {
                var effects = CreateAsset<AudioBus>(EffectsBusName);
                var music = CreateAsset<AudioBus>(MusicBusName);
                catalog.Add(UiClickId, CreateSound(EffectName, effects, new[] { 880f, 1320f, 1046.5f }, 0.45f, false, false));
                catalog.Add(EngineLoopId, CreateSound(LoopName, effects, new[] { 146.83f }, 2f, true, true));
                catalog.Add(MenuMusicId, CreateSound(MusicAName, music, new[] { 220f, 261.63f, 329.63f, 261.63f }, 8f, true, false));
                catalog.Add(GameplayMusicId, CreateSound(MusicBName, music, new[] { 196f, 246.94f, 293.66f, 246.94f }, 8f, true, false));
                EditorUtility.SetDirty(catalog);
            }
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static T CreateAsset<T>(string assetName) where T : ScriptableObject
        {
            var path = DataFolder + "/" + assetName + AssetExtension;
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static SoundData CreateSound(string soundName, AudioBus bus, float[] frequencies,
            float duration, bool loop, bool spatial)
        {
            var path = ClipsFolder + "/" + soundName + WaveExtension;
            if (!File.Exists(path)) WriteTone(path, frequencies, duration);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var sampleSettings = importer.defaultSampleSettings;
            sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = sampleSettings;
            importer.SaveAndReimport();
            var sound = ScriptableObject.CreateInstance<SoundDefinition>();
            var serialized = new SerializedObject(sound);
            var clips = serialized.FindProperty(Fields.Clips);
            clips.arraySize = 1;
            clips.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            serialized.FindProperty(Fields.Bus).objectReferenceValue = bus;
            serialized.FindProperty(Fields.Volume).floatValue = loop ? 0.35f : 0.65f;
            serialized.FindProperty(Fields.Loop).boolValue = loop;
            serialized.FindProperty(Fields.MaximumInstances).intValue = loop ? 1 : 32;
            serialized.FindProperty(Fields.SpatialMode).enumValueIndex = spatial
                ? (int)SoundSpatialMode.ThreeDimensional : (int)SoundSpatialMode.TwoDimensional;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var data = sound.ExportData();
            Object.DestroyImmediate(sound);
            return data;
        }

        private static void WriteTone(string path, float[] frequencies, float duration)
        {
            var sampleCount = Mathf.RoundToInt(SampleRate * duration);
            var dataLength = sampleCount * BytesPerSample;
            using var writer = new BinaryWriter(File.Create(path));
            writer.Write(Encoding.ASCII.GetBytes(RiffChunk));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes(WaveChunk));
            writer.Write(Encoding.ASCII.GetBytes(FormatChunk));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * BytesPerSample);
            writer.Write((short)BytesPerSample);
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes(DataChunk));
            writer.Write(dataLength);
            var noteLength = duration / frequencies.Length;
            for (var index = 0; index < sampleCount; index++)
            {
                var time = (float)index / SampleRate;
                var note = Mathf.Min((int)(time / noteLength), frequencies.Length - 1);
                var noteTime = time - note * noteLength;
                var envelope = Mathf.Min(1f, noteTime / 0.02f, (noteLength - noteTime) / 0.06f);
                var sample = Mathf.Sin(2f * Mathf.PI * frequencies[note] * noteTime) * envelope * ToneAmplitude;
                writer.Write((short)(sample * short.MaxValue));
            }
        }
    }
}
