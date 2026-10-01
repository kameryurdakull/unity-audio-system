namespace AudioSystem
{
    /// <summary>A generation-checked reference to one playback, safe after its pooled source is reused.</summary>
    public readonly struct SoundHandle
    {
        private readonly SoundVoicePool _owner;
        private readonly int _slot;
        private readonly ulong _generation;

        internal SoundHandle(SoundVoicePool owner, int slot, ulong generation)
        {
            _owner = owner;
            _slot = slot;
            _generation = generation;
        }

        public bool IsActive => _owner != null && _owner.IsActive(_slot, _generation);
        public bool IsPaused => IsActive && _owner.IsPaused(_slot, _generation);
        public bool Stop() => _owner != null && _owner.Stop(_slot, _generation);
        public bool Pause() => _owner != null && _owner.SetPaused(_slot, _generation, true);
        public bool Resume() => _owner != null && _owner.SetPaused(_slot, _generation, false);
        public bool SetVolume(float volumeScale) => _owner != null && _owner.SetVolume(_slot, _generation, volumeScale);
    }
}
