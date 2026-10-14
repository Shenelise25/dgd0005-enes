# DGD0005 - Enes - Week 03 Pong

Unity **6000.3.2f1**, Universal 3D ve FishNet resmi **4.7.3** etiketi ile ağ üzerinden Pong.

## Oyundaki üç sahiplik türü

Bu Pong oyununda her oyuncu kendi raketinin sahibidir; klavyeyi yalnız o raketin sahibi okur ve bu haftaki client-authoritative NetworkTransform konumu sunucuya iletir. Topun sahibi bir oyuncu değildir: topu sunucu oluşturur ve hareket ettirir. Skor, servis sırası ve son altı olay herkesin paylaştığı durumdur; bunları yalnız sunucu yazar, SyncVar ve SyncList istemcilere güncel değerleri taşır.

Ödevde ayrıca başka bir kişisel oyun fikrin isteniyorsa, bu paragrafı o oyunun oyuncu nesnesi, sunucu nesnesi ve ortak durumuyla kendi cümlelerinle değiştir.

## Sunucu ve istemci: üç satır

1. Unity Hub'dan `Pong` klasörünü 6000.3.2f1 ile aç, `Assets/Scenes/Pong.unity` sahnesini çalıştır; sunucu penceresinde **Start Server**.
2. Diğer oyuncu pencerelerinde sunucunun IP adresini girip **Start Client**; aynı bilgisayarda `127.0.0.1`, port `7770`.
3. **W/S veya oklar** ile hareket et; servis sırası sendeyse **Space**. Ayrı sunucu için Multiplayer Play Mode'da Player 2'ye `Server` etiketi ver.

## Önce oku

- [Basit, sıralı kullanım rehberi](Week03/ADIM_ADIM.md)
- [Ödev cevapları, tasarım tablosu ve deney kayıtları](Week03/README.md)
- [Bölüm 14 çalışma cevapları](Week03/SELF_REVIEW.md)
- [Doğrulama durumu](Week03/TEST_RESULTS.md)

## Dosyalar

`Pong/Assets/Scripts/Network/` içindeki beş script ödevin ağ mantığıdır. `Starter/` verilen yerel Pong kodudur; Court.Paint'a headless çalışma ve build'de shader korunması düzeltmesi eklenmiştir. `Tools/` klasöründeki HUD ve bootstrap, eksik ders dosyalarının yerine hazırlanmıştır. `Assets/Editor/` sahne/prefab oluşturma ve Windows development build yardımcılarını içerir. `Assets/Tests/PongSmokeProbe.cs` yalnız development build'de `-smoke` argümanıyla çalışır; normal oyunda otomatik raket veya servis kontrolü yapmaz.

`Pong-Start.unity` yerel oyundur; `Pong.unity` tamamlanan ağ sahnesidir. Hazır sahneleri doğrudan aç; `Pong/Build Connect Scenes` menüsü sahneleri başlangıç aşamasına döndürür.

## Sürümler ve kaynak farkları

- FishNet: [resmi 4.7.3 etiketi](https://github.com/FirstGearGames/FishNet/tree/4.7.3), commit `73f30cf2425dc808a4f463f0a233d386010810b3`. Etiketin kendi package.json ve sürüm sabiti 4.7.2 yazıyor; upstream dosyalar değiştirilmedi. Dersin Asset Store 4.7.3R paketiyle birebir eşdeğerlik ayrıca doğrulanmadı.
- Input System 1.17.0; şablondaki eski sürüm Unity 6000.3.2f1'de derlenmedi. [Unity sürüm notları](https://release-notes.ds.unity3d.com/search?from_release=6000.3.1f1&to_release=6000.3.1f1).
- Multiplayer Play Mode 1.6.2; diğer kesin sürümler `Packages/packages-lock.json` içinde.
- `Tools` ve `Pong-Start.unitypackage` verilmediği için özgün ders HUD'ı/sahnesi kullanılmadı.
- `Library`, `Temp`, `Logs`, `Builds` Git'e dahil edilmez. Yerel Windows build'i `Pong/Builds/Windows/Pong.exe` altında üretilebilir.

## Teslimde kalan kişisel çalışmalar

İki gerçek bilgisayarda iki yönlü hotspot deneyi ve partner demosu, 10/30/60 tick ölçüm tablosu ve kendi tahminlerinin değerlendirmesi öğrenci tarafından tamamlanmalıdır. Otomatik loopback testi bu deneylerin yerine geçmez.
