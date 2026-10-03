using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AudioSystem.Editor
{
    internal static class EditorAudioPreview
    {
        private const string AudioUtilTypeName = "UnityEditor.AudioUtil";
        private const string PlayMethodName = "PlayPreviewClip";
        private const string StopMethodName = "StopAllPreviewClips";
        private static readonly Type AudioUtilType = typeof(EditorUtility).Assembly.GetType(AudioUtilTypeName);
        private static readonly MethodInfo PlayMethod = AudioUtilType?.GetMethod(PlayMethodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        private static readonly MethodInfo StopMethod = AudioUtilType?.GetMethod(StopMethodName,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        // Unity's native clip preview API is internal; keep the version-dependent access in one editor adapter.
        internal static bool TryPlay(AudioClip clip)
        {
            if (PlayMethod == null || StopMethod == null || clip == null) return false;
            try
            {
                StopMethod.Invoke(null, null);
                PlayMethod.Invoke(null, new object[] { clip, 0, false });
                return true;
            }
            catch (TargetInvocationException) { return false; }
        }

        internal static void Stop()
        {
            if (StopMethod == null) return;
            try { StopMethod.Invoke(null, null); }
            catch (TargetInvocationException) { }
        }
    }
}
