# Audio System

Tek `SoundCatalog` üzerinden enum ID ile 2D/3D ses çalma, effect havuzu, Event Bus komutları ve iptal edilebilir müzik geçişleri.

**Gereksinimler:** UniTask 2.5.11, VContainer 1.19.0, Event Bus 1.0.0 ve ayrı import edilen DOTween. Paket manifest’inin minimum Unity sürümü 2022.3’tür; doğrulanan sürüm 6000.3.15f1’dir.

1. Bağımlılıkları kur ve DOTween Setup işlemini tamamla.
2. **Tools > Audio System > Setup** penceresinden settings ve catalog oluştur.
3. Kataloğa clip ve ID ekle, **Generate Sound IDs** çalıştır.
4. Settings ve catalog ile sahne manager’ını kur.
5. Ses isteyen component’leri VContainer ile inject et.

```csharp
using Game.Audio;

_soundPlayer.Play(SoundId.UiClick);
_soundPlayer.PlayAttached(SoundId.EngineLoop, transform);
_events.PublishSound(SoundId.UiClick);
```

`_soundPlayer`, `ISoundPlayer`; `_events`, `IEventBus` örneğidir. Örnek için Package Manager’daki **Samples > Playback Lab > Import** seçeneğini kullan.

Kurulum URL’leri, her ayarın görevi, VContainer component örneği, tüm API’ler, testler ve sorun giderme açıklamaları için [Türkçe kullanım rehberini](https://github.com/kameryurdakull/unity-audio-system/blob/main/README.md) aç.
