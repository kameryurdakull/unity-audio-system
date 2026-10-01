using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    [Serializable]
    public sealed class SoundCatalogEntry
    {
        [SerializeField, HideInInspector] private int stableId;
        [SerializeField] private string id;
        [SerializeField] private SoundData data = new();

        public int StableId => stableId;
        public string Id => id;
        public SoundData Data => data;

        internal SoundCatalogEntry(string id, SoundData data)
        {
            this.id = id;
            this.data = data;
        }

        internal void SetStableId(int value) => stableId = value;
    }

    [CreateAssetMenu(fileName = nameof(SoundCatalog), menuName = AudioConstants.MenuRoot + "Sound Catalog")]
    public sealed class SoundCatalog : ScriptableObject
    {
        [SerializeField, HideInInspector] private string identity;
        [SerializeField, HideInInspector] private int nextStableId = 1;
        [SerializeField] private List<SoundCatalogEntry> entries = new();
        [SerializeField] private string generatedNamespace = "Game.Audio";
        [SerializeField] private string enumName = "SoundId";

        public Guid Identity => Guid.TryParse(identity, out var value) ? value : Guid.Empty;
        public IReadOnlyList<SoundCatalogEntry> Entries => entries;
        public string GeneratedNamespace => generatedNamespace;
        public string EnumName => enumName;

        internal SoundCatalogEntry Add(string id, SoundData data)
        {
            var entry = new SoundCatalogEntry(id, data);
            entries.Add(entry);
            Normalize();
            return entry;
        }

        internal void BindIdentity(Guid assetIdentity) => identity = assetIdentity.ToString("N");

        internal void Normalize()
        {
            if (Identity == Guid.Empty) identity = Guid.NewGuid().ToString("N");
            entries ??= new List<SoundCatalogEntry>();
            nextStableId = Mathf.Max(1, nextStableId);
            foreach (var entry in entries)
                if (entry != null && entry.StableId >= nextStableId)
                    nextStableId = checked(entry.StableId + 1);

            var used = new HashSet<int>();
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                if (entry.StableId <= 0 || !used.Add(entry.StableId))
                {
                    entry.SetStableId(nextStableId);
                    used.Add(nextStableId);
                    nextStableId = checked(nextStableId + 1);
                }
                entry.Data?.Validate();
            }
        }

        private void OnValidate() => Normalize();
        private void OnEnable()
        {
            if (Identity == Guid.Empty) identity = Guid.NewGuid().ToString("N");
        }
    }
}
