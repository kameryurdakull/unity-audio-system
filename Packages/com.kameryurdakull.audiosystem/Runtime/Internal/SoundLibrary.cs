using System;
using System.Collections.Generic;

namespace AudioSystem
{
    internal sealed class SoundLibrary
    {
        private readonly Guid _identity;
        private readonly Dictionary<int, SoundData> _sounds = new();

        internal SoundLibrary(SoundCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            _identity = catalog.Identity;
            if (_identity == Guid.Empty) throw new ArgumentException("The sound catalog has no identity.", nameof(catalog));
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in catalog.Entries)
            {
                if (entry == null || entry.StableId <= 0 || string.IsNullOrWhiteSpace(entry.Id))
                    throw new ArgumentException("The catalog contains an incomplete entry.", nameof(catalog));
                if (!names.Add(entry.Id) || !_sounds.TryAdd(entry.StableId, entry.Data))
                    throw new ArgumentException($"Duplicate sound ID: {entry.Id}.", nameof(catalog));
                try { AudioValidation.Sound(entry.Data); }
                catch (ArgumentException exception)
                {
                    throw new ArgumentException($"Invalid sound '{entry.Id}': {exception.Message}", nameof(catalog), exception);
                }
            }
        }

        internal SoundData Resolve(SoundKey key)
        {
            if (!key.IsValid) throw new ArgumentException("A valid generated sound ID is required.", nameof(key));
            if (key.CatalogIdentity != _identity)
                throw new ArgumentException("The sound ID belongs to another catalog. Generate IDs from the assigned catalog.", nameof(key));
            if (!_sounds.TryGetValue(key.Value, out var sound))
                throw new KeyNotFoundException($"Sound ID {key.Value} no longer exists. Generate IDs again.");
            return sound;
        }
    }
}
