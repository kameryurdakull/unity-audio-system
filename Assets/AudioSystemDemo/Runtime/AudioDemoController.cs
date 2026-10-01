using Game.Audio;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EventSystem;
using UnityEngine;
using VContainer;

namespace AudioSystem.Demo
{
    public sealed class AudioDemoController : MonoBehaviour
    {
        private const string Title = "AUDIO SYSTEM | PLAYBACK LAB";
        private const string Description = "Synthetic test tones | The camera is the listener | Moving sphere is a 3D emitter";
        private const string TwoDLabel = "Play 2D";
        private const string EventLabel = "Play through EventBus";
        private const string LeftLabel = "3D left";
        private const string RightLabel = "3D right";
        private const string FollowLabel = "Start following loop";
        private const string PauseLabel = "Pause loop";
        private const string ResumeLabel = "Resume loop";
        private const string StopLoopLabel = "Stop loop";
        private const string BurstLabel = "Burst 48 voices";
        private const string StopEffectsLabel = "Stop all effects";
        private const string MusicALabel = "Music A";
        private const string MusicBLabel = "Music B";
        private const string CancelLabel = "Cancel transition";
        private const string StopMusicLabel = "Fade out music";
        private const string MasterLabel = "Master";
        private const string EffectsLabel = "Effects bus";
        private const string MusicLabel = "Music bus";
        private const string MuteLabel = "Mute master";
        private const string ReadyStatus = "Ready";
        private const string TransitionStatus = "Transition started";
        private const string CompleteStatus = "Transition completed";
        private const string CanceledStatus = "Transition canceled; previous track restored";
        private const float TransitionDuration = 2f;
        private const int BurstCount = 48;

        [SerializeField] private AudioBus effectsBus;
        [SerializeField] private AudioBus musicBus;
        [SerializeField] private Transform emitter;

        private ISoundService _sound;
        private IEventBus _events;
        private SoundHandle _following;
        private Tween _motion;
        private CancellationTokenSource _musicCancellation;
        private float _master = 1f;
        private float _effectsVolume = 1f;
        private float _musicVolume = 1f;
        private bool _muted;
        private string _status = ReadyStatus;
        private int _musicVersion;

        [Inject]
        public void Construct(ISoundService sound, IEventBus events)
        {
            _sound = sound;
            _events = events;
        }

        private void Start()
        {
            _motion = DOTween.To(() => emitter.position, position => emitter.position = position,
                new Vector3(6f, 1f, 6f), 4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDisable()
        {
            _musicVersion++;
            _motion?.Kill();
            _following.Stop();
            _musicCancellation?.Cancel();
            _musicCancellation?.Dispose();
            _musicCancellation = null;
        }

        private void OnGUI()
        {
            if (_sound == null) return;
            GUILayout.BeginArea(new Rect(20f, 20f, 480f, 640f), GUI.skin.box);
            GUILayout.Label(Title);
            GUILayout.Label(Description);
            var diagnostics = _sound.GetDiagnostics();
            GUILayout.Label($"Effects: {diagnostics.ActiveEffects} active / {diagnostics.PausedEffects} paused | Pool: {diagnostics.AllocatedEffectSources}/{diagnostics.MaximumEffectSources}");
            GUILayout.Label($"Music: {diagnostics.ActiveMusicSources}/2 | Transition: {diagnostics.IsMusicTransitioning}");
            GUILayout.Space(8f);

            if (GUILayout.Button(TwoDLabel)) _sound.Play(SoundId.UiClick);
            if (GUILayout.Button(EventLabel)) _events.PublishSound(SoundId.UiClick);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(LeftLabel)) _sound.PlayAt(SoundId.UiClick, new Vector3(-6f, 1f, 6f));
            if (GUILayout.Button(RightLabel)) _sound.PlayAt(SoundId.UiClick, new Vector3(6f, 1f, 6f));
            GUILayout.EndHorizontal();
            if (GUILayout.Button(FollowLabel))
            {
                _following.Stop();
                _following = _sound.PlayAttached(SoundId.EngineLoop, emitter);
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(PauseLabel)) _following.Pause();
            if (GUILayout.Button(ResumeLabel)) _following.Resume();
            if (GUILayout.Button(StopLoopLabel)) _following.Stop();
            GUILayout.EndHorizontal();
            if (GUILayout.Button(BurstLabel))
                for (var index = 0; index < BurstCount; index++) _sound.Play(SoundId.UiClick);
            if (GUILayout.Button(StopEffectsLabel)) _events.Publish(new StopAllEffectsRequested());

            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(MusicALabel)) StartMusic(SoundId.MenuMusic);
            if (GUILayout.Button(MusicBLabel)) StartMusic(SoundId.GameplayMusic);
            GUILayout.EndHorizontal();
            if (GUILayout.Button(CancelLabel)) _musicCancellation?.Cancel();
            if (GUILayout.Button(StopMusicLabel)) _sound.StopMusicAsync(TransitionDuration).Forget(LogError);
            GUILayout.Label(_status);

            DrawVolume(MasterLabel, ref _master, _sound.SetMasterVolume);
            DrawVolume(EffectsLabel, ref _effectsVolume, value => _sound.SetBusVolume(effectsBus, value));
            DrawVolume(MusicLabel, ref _musicVolume, value => _sound.SetBusVolume(musicBus, value));
            var muted = GUILayout.Toggle(_muted, MuteLabel);
            if (muted != _muted)
            {
                _muted = muted;
                _sound.SetMasterMuted(muted);
            }
            GUILayout.EndArea();
        }

        private void StartMusic(SoundId sound)
        {
            _musicCancellation?.Dispose();
            _musicCancellation = new CancellationTokenSource();
            PlayMusicAsync(sound, _musicCancellation.Token).Forget(LogError);
        }

        private async UniTask PlayMusicAsync(SoundId sound, CancellationToken token)
        {
            var version = ++_musicVersion;
            _status = TransitionStatus;
            try
            {
                await _sound.PlayMusicAsync(sound, TransitionDuration, token);
                if (version == _musicVersion) _status = CompleteStatus;
            }
            catch (OperationCanceledException)
            {
                if (version == _musicVersion) _status = CanceledStatus;
            }
        }

        private static void DrawVolume(string label, ref float value, Action<float> apply)
        {
            GUILayout.Label($"{label}: {value:0.00}");
            var updated = GUILayout.HorizontalSlider(value, 0f, 1f);
            if (Mathf.Approximately(updated, value)) return;
            value = updated;
            apply(updated);
        }

        private static void LogError(Exception exception)
        {
            if (exception is not OperationCanceledException) Debug.LogException(exception);
        }
    }
}
