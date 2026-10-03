using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AudioSystem.Editor
{
    internal static class SoundIdGenerator
    {
        internal const string GeneratedFolder = "Assets/AudioSystemGenerated";
        private const string AssemblyFile = "AudioSystem.Generated.asmdef";
        private const string OwnerPrefix = "// AudioSystem generated catalog: ";
        private const string DefaultAssemblyName = "AudioSystem.Generated";
        private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
            "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
            "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
            "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
            "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly",
            "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
            "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "virtual", "void", "volatile", "while", "None", "value__",
            "__arglist", "__makeref", "__reftype", "__refvalue"
        };
        private static readonly HashSet<string> GeneratedTypeNames = new(StringComparer.Ordinal)
        {
            "Guid", "CancellationToken", "SoundKey", "SoundHandle", "PlaybackOptions", "ISoundPlayer",
            "IMusicPlayer", "IEventBus", "UniTask", "Vector3", "System", "AudioSystem", "Cysharp", "EventSystem", "UnityEngine"
        };

        internal static string Generate(SoundCatalog catalog)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Generate sound IDs in Edit Mode.");
            var assetPath = AssetDatabase.GetAssetPath(catalog);
            if (string.IsNullOrEmpty(assetPath)) throw new InvalidOperationException("Save the catalog asset first.");
            var identity = Guid.Parse(AssetDatabase.AssetPathToGUID(assetPath));
            Validate(catalog);
            var path = GetOutputPath(catalog);
            var owner = OwnerPrefix + identity.ToString("N");
            if (File.Exists(path) && !ReadNormalized(path).StartsWith(owner + "\n", StringComparison.Ordinal))
                throw new InvalidOperationException($"{path} belongs to another catalog or contains handwritten code. Choose a different enum name.");
            var folder = GetOutputFolder(catalog);
            var assemblyPath = folder + "/" + AssemblyFile;
            var assemblySource = BuildAssemblySource(catalog);
            if (File.Exists(assemblyPath) && ReadNormalized(assemblyPath) != assemblySource)
                throw new InvalidOperationException($"{assemblyPath} has custom content. Restore the generated assembly before generating IDs.");

            Undo.RecordObject(catalog, "Generate Sound IDs");
            catalog.BindIdentity(identity);
            EditorUtility.SetDirty(catalog);
            var source = BuildSource(catalog);
            Directory.CreateDirectory(folder);
            WriteIfChanged(assemblyPath, assemblySource);
            WriteIfChanged(path, source);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return path;
        }

        internal static string GetOutputPath(SoundCatalog catalog) => GetOutputFolder(catalog) + "/" + catalog.EnumName + ".cs";

        internal static string GetOutputFolder(SoundCatalog catalog) =>
            ResolveOutputFolder(catalog.GeneratedFolder, AssetDatabase.GetAssetPath(catalog));

        internal static string ResolveOutputFolder(string folder, string catalogPath)
        {
            if (string.IsNullOrWhiteSpace(folder) || Path.IsPathRooted(folder))
                throw new ArgumentException("The generated folder must be inside Assets.");
            var normalized = folder.Replace('\\', '/');
            if (normalized.StartsWith("Packages/", StringComparison.Ordinal))
                throw new ArgumentException("Generated files belong to Assets, not to installed packages.");
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var relative = normalized.StartsWith("Assets/", StringComparison.Ordinal)
                ? normalized : !string.IsNullOrEmpty(catalogPath)
                    ? Path.Combine(Path.GetDirectoryName(catalogPath), normalized)
                    : throw new ArgumentException("Save the catalog before choosing a relative output folder.");
            var full = Path.GetFullPath(Path.Combine(projectRoot, relative));
            var assetsPrefix = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Generated code can only be written to a subfolder of Assets.");
            return "Assets/" + full.Substring(assetsPrefix.Length).Replace('\\', '/');
        }

        internal static string GetAssemblyName(SoundCatalog catalog) => GetOutputFolder(catalog) == GeneratedFolder
            ? DefaultAssemblyName : DefaultAssemblyName + "." + catalog.GeneratedNamespace + "." + catalog.EnumName;

        internal static string BuildAssemblySource(SoundCatalog catalog) =>
            "{\n  \"name\": \"" + GetAssemblyName(catalog) + "\",\n  \"references\": [\"AudioSystem.Runtime\", \"UniTask\", \"EventSystem\"],\n  \"autoReferenced\": true\n}\n";

        internal static bool IsCurrent(SoundCatalog catalog)
        {
            try
            {
                var path = GetOutputPath(catalog);
                var assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(catalog));
                return Guid.TryParse(assetGuid, out var identity) && catalog.Identity == identity &&
                    File.Exists(path) && ReadNormalized(path) == BuildSource(catalog);
            }
            catch (ArgumentException) { return false; }
        }

        internal static void Validate(SoundCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (!IsIdentifier(catalog.EnumName) || GeneratedTypeNames.Contains(catalog.EnumName))
                throw new ArgumentException("The enum name must be a valid C# identifier without conflicting with audio API types.");
            if (string.IsNullOrEmpty(catalog.GeneratedNamespace) ||
                catalog.GeneratedNamespace.Split('.').Any(part => !IsIdentifier(part)))
                throw new ArgumentException("The generated namespace must contain valid C# identifiers.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            var numbers = new HashSet<int>();
            foreach (var entry in catalog.Entries)
            {
                if (entry == null || !IsIdentifier(entry.Id) || entry.Id == catalog.EnumName)
                    throw new ArgumentException($"Invalid sound ID '{entry?.Id}'. Use a C# identifier such as UiClick; None is reserved.");
                if (!names.Add(entry.Id)) throw new ArgumentException($"Duplicate sound ID '{entry.Id}'.");
                if (entry.StableId <= 0 || !numbers.Add(entry.StableId)) throw new ArgumentException($"Invalid numeric ID for '{entry.Id}'.");
                if (entry.Data == null || !entry.Data.IsPlayable) throw new ArgumentException($"Assign valid clips to '{entry.Id}'.");
                AudioValidation.Sound(entry.Data);
            }
        }

        internal static string BuildSource(SoundCatalog catalog)
        {
            Validate(catalog);
            var source = new StringBuilder();
            source.AppendLine(OwnerPrefix + catalog.Identity.ToString("N"));
            source.AppendLine("// Generated by the SoundCatalog inspector. Edit the catalog instead of this file.");
            source.AppendLine("using System;");
            source.AppendLine("using System.Threading;");
            source.AppendLine("using AudioSystem;");
            source.AppendLine("using Cysharp.Threading.Tasks;");
            source.AppendLine("using EventSystem;");
            source.AppendLine("using UnityEngine;");
            source.AppendLine();
            source.AppendLine("namespace " + catalog.GeneratedNamespace);
            source.AppendLine("{");
            source.AppendLine("    public enum " + catalog.EnumName);
            source.AppendLine("    {");
            source.AppendLine("        None = 0,");
            foreach (var entry in catalog.Entries.OrderBy(entry => entry.StableId))
                source.AppendLine($"        {entry.Id} = {entry.StableId},");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    public static class " + catalog.EnumName + "Extensions");
            source.AppendLine("    {");
            source.AppendLine($"        private static readonly Guid CatalogIdentity = new Guid(\"{catalog.Identity:N}\");");
            source.AppendLine($"        public static SoundKey ToSoundKey(this {catalog.EnumName} id) => new SoundKey(CatalogIdentity, (int)id);");
            source.AppendLine($"        public static SoundHandle Play(this ISoundPlayer player, {catalog.EnumName} id, PlaybackOptions options = default) => player.Play(id.ToSoundKey(), options);");
            source.AppendLine($"        public static SoundHandle PlayAt(this ISoundPlayer player, {catalog.EnumName} id, Vector3 position, PlaybackOptions options = default) => player.PlayAt(id.ToSoundKey(), position, options);");
            source.AppendLine($"        public static SoundHandle PlayAttached(this ISoundPlayer player, {catalog.EnumName} id, Transform target, Vector3 localOffset = default, PlaybackOptions options = default) => player.PlayAttached(id.ToSoundKey(), target, localOffset, options);");
            source.AppendLine($"        public static UniTask PlayMusicAsync(this IMusicPlayer player, {catalog.EnumName} id, float transitionSeconds = 1f, CancellationToken cancellationToken = default) => player.PlayMusicAsync(id.ToSoundKey(), transitionSeconds, cancellationToken);");
            source.AppendLine($"        public static void PublishSound(this IEventBus events, {catalog.EnumName} id, PlaybackOptions options = default) => events.Publish(new PlaySoundRequested(id.ToSoundKey(), options));");
            source.AppendLine($"        public static void PublishSoundAt(this IEventBus events, {catalog.EnumName} id, Vector3 position, PlaybackOptions options = default) => events.Publish(PlaySoundRequested.At(id.ToSoundKey(), position, options));");
            source.AppendLine($"        public static void PublishSoundAttached(this IEventBus events, {catalog.EnumName} id, Transform target, Vector3 localOffset = default, PlaybackOptions options = default) => events.Publish(PlaySoundRequested.Attached(id.ToSoundKey(), target, localOffset, options));");
            source.AppendLine($"        public static void PublishMusic(this IEventBus events, {catalog.EnumName} id, float transitionSeconds = 1f) => events.Publish(new PlayMusicRequested(id.ToSoundKey(), transitionSeconds));");
            source.AppendLine("    }");
            source.AppendLine("}");
            return source.ToString().Replace("\r\n", "\n");
        }

        internal static bool IsIdentifier(string value)
        {
            if (string.IsNullOrEmpty(value) || Keywords.Contains(value)) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                var letter = character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';
                if (!letter && (index == 0 || character is < '0' or > '9')) return false;
            }
            return true;
        }

        private static void WriteIfChanged(string path, string content)
        {
            if (!File.Exists(path) || ReadNormalized(path) != content)
                File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        private static string ReadNormalized(string path) => File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
