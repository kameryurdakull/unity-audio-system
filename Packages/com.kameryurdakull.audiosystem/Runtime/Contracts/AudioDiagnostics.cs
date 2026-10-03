namespace AudioSystem
{
    public readonly struct AudioDiagnostics
    {
        public int ActiveEffects { get; }
        public int PausedEffects { get; }
        public int AllocatedEffectSources { get; }
        public int MaximumEffectSources { get; }
        public int ActiveMusicSources { get; }
        public bool IsMusicTransitioning { get; }

        internal AudioDiagnostics(int activeEffects, int pausedEffects, int allocatedEffectSources,
            int maximumEffectSources, int activeMusicSources, bool isMusicTransitioning)
        {
            ActiveEffects = activeEffects;
            PausedEffects = pausedEffects;
            AllocatedEffectSources = allocatedEffectSources;
            MaximumEffectSources = maximumEffectSources;
            ActiveMusicSources = activeMusicSources;
            IsMusicTransitioning = isMusicTransitioning;
        }
    }
}
