using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AudioSystem.Editor
{
    internal sealed class AudioSetupWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Audio System/Setup";
        private const string Title = "Audio Setup";
        private const string SettingsField = "settings";
        private const string CatalogField = "catalog";
        private const string CreateScopeLabel = "Create Audio Manager In Scene";
        private AudioSystemSettings _settings;
        private SoundCatalog _catalog;

        [MenuItem(MenuPath)]
        private static void Open() => GetWindow<AudioSetupWindow>(Title);

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Assign one catalog and settings to the scene manager. Consumer scripts use generated enum IDs through VContainer or EventBus.", MessageType.Info);
            _settings = (AudioSystemSettings)EditorGUILayout.ObjectField("Settings", _settings, typeof(AudioSystemSettings), false);
            _catalog = (SoundCatalog)EditorGUILayout.ObjectField("Sound Catalog", _catalog, typeof(SoundCatalog), false);
            if (GUILayout.Button("Create Settings Asset")) _settings = CreateAsset(_settings);
            if (GUILayout.Button("Create Sound Catalog")) _catalog = CreateAsset(_catalog);
            using (new EditorGUI.DisabledScope(_settings == null || _catalog == null || Application.isPlaying))
                if (GUILayout.Button(CreateScopeLabel)) CreateScope();
        }

        private static T CreateAsset<T>(T fallback) where T : ScriptableObject
        {
            var path = EditorUtility.SaveFilePanelInProject("Create " + typeof(T).Name, typeof(T).Name, "asset", string.Empty);
            if (string.IsNullOrEmpty(path)) return fallback;
            var asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            return asset;
        }

        private void CreateScope()
        {
            var root = new GameObject("Audio Manager");
            Undo.RegisterCreatedObjectUndo(root, CreateScopeLabel);
            var scope = Undo.AddComponent<AudioSystemScope>(root);
            var serialized = new SerializedObject(scope);
            serialized.FindProperty(SettingsField).objectReferenceValue = _settings;
            serialized.FindProperty(CatalogField).objectReferenceValue = _catalog;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
        }
    }
}
