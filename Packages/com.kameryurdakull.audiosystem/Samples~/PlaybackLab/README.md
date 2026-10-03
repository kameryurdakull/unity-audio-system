# Playback Lab

**Package Manager > Audio System > Samples > Playback Lab > Import** ile örneği projene al. Ardından **Tools > Audio System > Samples > Select Playback Lab** menüsünü kullan; seçilen `Scenes/AudioPlaybackLab.unity` dosyasını aç ve Play Mode’a gir.

Sahnede 2D, sabit 3D, hareketli hedef takibi, loop pause/resume, effect havuzu taşması, Event Bus ve müzik geçişlerini deneyebilirsin. Ses dosyaları sentetik test tonlarıdır.

- `Data/SoundCatalog.asset`: Örneğin dört ses kaydı.
- `Data/DemoSettings.asset`: 4 prewarm ve 16 maksimum effect kaynağı.
- `Data/EffectsBus.asset`, `MusicBus.asset`: Volume/mute grupları.
- `Generated/PlaybackSoundId.cs`: Örneğe özel enum ve helper’lar.
- `Runtime/AudioDemoScope.cs`: DI, Event Bus ve demo controller kaydı.
- `Runtime/AudioDemoController.cs`: API çağrıları ve sahnedeki butonlar.

Örnek enum’u `AudioSystem.PlaybackLab.Audio.PlaybackSoundId` namespace’inde bulunur. Katalogdaki `Generated Folder = ../Generated` ayarı, tekrar üretimi import edilen örneğin kendi `Generated` klasörüne yönlendirir. Oyunun `SoundId` enum’undan bağımsızdır.

Bağımlılıklar ve ayrıntılı açıklamalar için [Türkçe kullanım rehberine](https://github.com/kameryurdakull/unity-audio-system/blob/main/README.md) bak.
