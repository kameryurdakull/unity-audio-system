using System;

namespace AudioSystem
{
    /// <summary>Identifies a catalog entry without coupling the package to a project's generated enum.</summary>
    public readonly struct SoundKey : IEquatable<SoundKey>
    {
        public Guid CatalogIdentity { get; }
        public int Value { get; }
        public bool IsValid => CatalogIdentity != Guid.Empty && Value > 0;

        public SoundKey(Guid catalogIdentity, int value)
        {
            CatalogIdentity = catalogIdentity;
            Value = value;
        }

        public bool Equals(SoundKey other) => CatalogIdentity == other.CatalogIdentity && Value == other.Value;
        public override bool Equals(object obj) => obj is SoundKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(CatalogIdentity, Value);
        public static bool operator ==(SoundKey left, SoundKey right) => left.Equals(right);
        public static bool operator !=(SoundKey left, SoundKey right) => !left.Equals(right);
    }
}
