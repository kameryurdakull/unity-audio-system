using UnityEditor;
using UnityEngine;

namespace AudioSystem.Editor
{
    [CustomEditor(typeof(SoundDefinition))]
    internal sealed class SoundDefinitionEditor : UnityEditor.Editor
    {
        private const string PreviewLabel = "Preview Clip Variation";
        private const string StopLabel = "Stop Preview";
        private const string MissingClips = "Assign at least one AudioClip and remove missing clip references.";
        private const string PreviewUnavailable = "Native audio preview is unavailable. Select the AudioClip to use its inspector preview.";
        private const string PreviewDescription = "Preview plays the raw clip. Use Play Mode to hear volume, pitch, mixer routing and 3D attenuation.";
        private const string SpatialDescription = "PlayAt and PlayAttached force 3D playback. Play uses the spatial mode configured here.";
        private const string PriorityDescription = "Lower priority values are more important. Steal Lower Priority never interrupts a sound of equal or higher importance.";
        private int _previewIndex = -1;
        private bool _previewUnavailable;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Legacy definition. Select this asset, open your SoundCatalog and use Import Selected Clips / SoundDefinitions to copy its settings into the catalog.", MessageType.Info);
            DrawDefaultInspector();
            var sound = (SoundDefinition)target;
            if (!sound.IsPlayable) EditorGUILayout.HelpBox(MissingClips, MessageType.Warning);
            EditorGUILayout.HelpBox(SpatialDescription, MessageType.Info);
            EditorGUILayout.HelpBox(PriorityDescription, MessageType.Info);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!sound.IsPlayable || Application.isPlaying))
            {
                if (GUILayout.Button(PreviewLabel))
                {
                    if (_previewIndex >= sound.ClipCount) _previewIndex = -1;
                    _previewUnavailable = !EditorAudioPreview.TryPlay(sound.SelectClip(ref _previewIndex));
                }
            }
            if (GUILayout.Button(StopLabel)) EditorAudioPreview.Stop();
            EditorGUILayout.HelpBox(_previewUnavailable ? PreviewUnavailable : PreviewDescription,
                _previewUnavailable ? MessageType.Warning : MessageType.Info);
        }

        private void OnDisable() => EditorAudioPreview.Stop();
    }
}
