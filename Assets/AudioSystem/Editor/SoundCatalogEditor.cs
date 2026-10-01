using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AudioSystem.Editor
{
    [CustomEditor(typeof(SoundCatalog))]
    internal sealed class SoundCatalogEditor : UnityEditor.Editor
    {
        private const string EntriesField = "entries";
        private const string NamespaceField = "generatedNamespace";
        private const string EnumField = "enumName";
        private const string NewId = "NewSound";
        private const string GenerateLabel = "Generate Sound IDs";
        private const string ImportLabel = "Import Selected Clips / SoundDefinitions";
        private const string PreviewLabel = "Preview Selected Sound";
        private const string StopLabel = "Stop Preview";
        private ReorderableList _list;
        private string _message;
        private bool _failed;
        private int _previewIndex = -1;
        private SoundCatalog Catalog => (SoundCatalog)target;

        private void OnEnable()
        {
            _list = new ReorderableList(serializedObject, serializedObject.FindProperty(EntriesField), true, true, true, true);
            _list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Sounds — ID and playback settings");
            _list.elementHeightCallback = index => EditorGUI.GetPropertyHeight(_list.serializedProperty.GetArrayElementAtIndex(index), true) + 6f;
            _list.drawElementCallback = (rect, index, active, focused) =>
            {
                rect.y += 2f;
                rect.height -= 4f;
                var entry = _list.serializedProperty.GetArrayElementAtIndex(index);
                var id = Catalog.Entries.Count > index ? Catalog.Entries[index]?.Id : null;
                EditorGUI.PropertyField(rect, entry, new GUIContent(string.IsNullOrEmpty(id) ? NewId : id), true);
            };
            _list.onAddCallback = list =>
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(Catalog, "Add Sound");
                Catalog.Add(UniqueId(Catalog, NewId), new SoundData());
                EditorUtility.SetDirty(Catalog);
                serializedObject.Update();
                list.index = Catalog.Entries.Count - 1;
                list.serializedProperty.GetArrayElementAtIndex(list.index).isExpanded = true;
            };
            _list.onSelectCallback = list => _previewIndex = -1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(NamespaceField));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(EnumField));
                _list.DoLayoutList();
                serializedObject.ApplyModifiedProperties();
                if (!SoundIdGenerator.IsCurrent(Catalog))
                    EditorGUILayout.HelpBox("Add your clips, enter IDs such as UiClick or EngineLoop, then generate IDs. Names and numeric IDs are validated before generation.", MessageType.Info);
                if (GUILayout.Button(GenerateLabel)) Run(() => SoundIdGenerator.Generate(Catalog));
                if (GUILayout.Button(ImportLabel)) Run(() => ImportSelected(Catalog));
                DrawImportArea();
            }
            var selected = _list.index >= 0 && _list.index < Catalog.Entries.Count ? Catalog.Entries[_list.index]?.Data : null;
            using (new EditorGUI.DisabledScope(selected == null || !selected.IsPlayable || Application.isPlaying))
                if (GUILayout.Button(PreviewLabel))
                    Run(() => EditorAudioPreview.TryPlay(selected.SelectClip(ref _previewIndex))
                        ? "Raw clip preview. Test routing, pitch and 3D attenuation in Play Mode."
                        : "Audio preview is unavailable; use the AudioClip inspector preview.");
            if (GUILayout.Button(StopLabel)) EditorAudioPreview.Stop();
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _failed ? MessageType.Error : MessageType.Info);
        }

        internal static string ImportSelected(SoundCatalog catalog) => ImportSources(catalog, Selection.objects);

        private static string ImportSources(SoundCatalog catalog, IEnumerable<UnityEngine.Object> sources)
        {
            Undo.RecordObject(catalog, ImportLabel);
            var added = 0;
            var skipped = 0;
            foreach (var selected in sources)
            {
                var data = selected is AudioClip clip ? SoundData.FromClip(clip) :
                    selected is SoundDefinition legacy ? legacy.ExportData() : null;
                if (data == null) continue;
                if (!data.IsPlayable) { skipped++; continue; }
                var name = new string(selected.name.Select(character =>
                    character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_' ? character : '_').ToArray());
                if (!SoundIdGenerator.IsIdentifier(name)) name = NewId + "_" + name;
                catalog.Add(UniqueId(catalog, name), data);
                added++;
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return $"Imported {added} sounds; skipped {skipped} definitions without valid clips. Generate IDs after importing.";
        }

        private void DrawImportArea()
        {
            var area = GUILayoutUtility.GetRect(0f, 44f, GUILayout.ExpandWidth(true));
            GUI.Box(area, "Drop AudioClips or legacy SoundDefinitions here");
            var current = Event.current;
            if (!area.Contains(current.mousePosition) ||
                current.type is not (EventType.DragUpdated or EventType.DragPerform)) return;
            if (!DragAndDrop.objectReferences.Any(source => source is AudioClip or SoundDefinition)) return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                Run(() => ImportSources(Catalog, DragAndDrop.objectReferences));
            }
            current.Use();
        }

        private static string UniqueId(SoundCatalog catalog, string name)
        {
            var existing = catalog.Entries.Select(entry => entry?.Id).ToHashSet(StringComparer.Ordinal);
            if (!existing.Contains(name)) return name;
            for (var suffix = 1; suffix <= existing.Count + 1; suffix++)
            {
                var candidate = name + suffix;
                if (!existing.Contains(candidate)) return candidate;
            }
            throw new InvalidOperationException("Unable to allocate a unique sound name.");
        }

        private void Run(Func<string> action)
        {
            try { _message = action(); _failed = false; }
            catch (Exception exception) { _message = exception.Message; _failed = true; }
            serializedObject.Update();
        }

        private void OnDisable() => EditorAudioPreview.Stop();
    }
}
