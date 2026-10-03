using System;
using EventSystem;
using VContainer.Unity;

namespace AudioSystem
{
    internal sealed class AudioSystemEntryPoint : IInitializable, ITickable, IDisposable
    {
        private readonly SoundService _service;
        private readonly AudioEventBridge _bridge;

        public AudioSystemEntryPoint(SoundService service, IEventBus events)
        {
            _service = service;
            _bridge = new AudioEventBridge(events, service);
        }

        public void Initialize() => _bridge.Subscribe();
        public void Tick() => _service.Tick();
        public void Dispose() => _bridge.Dispose();
    }
}
