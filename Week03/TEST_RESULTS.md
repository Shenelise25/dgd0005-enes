# Doğrulama sonuçları

Tarih: 14 Ekim 2026. Unity 6000.3.2f1, Windows development build, Tugboat/UDP, aynı bilgisayarda loopback bağlantısı.

## Gerçekte yapılan kontroller

- Unity C# derlemesi ve Windows player build başarılı.
- Ayrı sunucu + LEFT + RIGHT + SPECTATOR süreçleriyle gerçek FishNet transport bağlantısı kuruldu.
- İki oyuncuya farklı taraflar, üçüncü oyuncuya spectator rolü atandı.
- Sahip istemcinin değiştirdiği raket konumu sunucuda ve diğer istemcilerde görüldü.
- Sunucuda ve bağlı istemcilerde birer top kopyası görüldü; tek yetkili top sunucuda simüle edildi.
- RIGHT'ın başlangıçta gönderdiği yanlış servis isteği reddedildi; doğru oyuncu servis atınca top hareket etti.
- Goller ve skorlar sunucu, iki oyuncu ve izleyiciye ulaştı.
- İki oyuncu da kendisine sıra geldiğinde YOU SERVE aldı; izleyici bu özel bildirimi almadı.
- Feed altı satırla sınırlı kaldı.
- RIGHT, ClientManager.StopConnection ile normal ayrıldı. Sunucu ayrılmayı kaydetti; yeniden gelen istemci boşalan RIGHT rolünü aldı.
- Yeniden bağlanan istemci mevcut sıfırdan farklı skoru ve altı olaylık geçmişi aldı.
- Bu testin beş süreç günlüğünde runtime exception bulunmadı.

Otomatik doğrulayıcı: **11/11 kontrol başarılı**. Özet: [test-summary.json](test-summary.json).

Ek test: uygulama zorla kapatıldıktan sonra yeni bağlantı da mevcut skoru aldı. Zorla kapanan bağlantı zaman aşımına kadar hâlâ bağlı sayıldığından, o anda yeni gelen istemci spectator oldu. Normal Stop Client akışında taraf hemen boşaldı.

## Tekrar çalıştırma

1. Unity'de `PongReplicateSetup.BuildPlayer` metodunu çalıştır veya proje için Windows development build oluştur: `Pong/Builds/Windows/Pong.exe`.
2. Repo kökünden `python tests/run_smoke.py` çalıştır. Yaklaşık 40 saniyede süreçler kapanır; günlükler `tests/logs/` altındadır.
3. `python tests/check_smoke.py` çalıştır. Bütün kontroller true ve çıkış kodu 0 olmalıdır.

Probe, gerçek hareket kontrolünün yerine yalnız `-smoke` ile devreye giren test girdileri uygular. Klavye ve pencere odağı kontrolü bu otomatik testte yapılmadı.

## Görsel kontrol

Windows oyun penceresinde saha, duvarlar, orta çizgi ve bağlantı arayüzü görüldü. Bağlantı panelinin raketi kapatmaması için kamera alanı panelin yanına ayrıldı. Gizli/minimize pencereden alınan siyah görüntü teslim dosyalarına dahil edilmedi.

## Henüz yapılmayanlar

- İki gerçek bilgisayarda iki yönlü hotspot bağlantısı ve partner demosu.
- Aynı koşullarda 10/30/60 tick için 10 saniyelik rally trafik ölçümleri ve görsel akıcılık gözlemi.
- Öğrencinin deneyden önce yaptığı kişisel tahminlerin sonradan değerlendirilmesi.
- Dersin orijinal Tools dosyaları ve Asset Store 4.7.3R paketiyle birebir karşılaştırma; bunlar paylaşılmadı.

Bu kayıtlar tamamlanmadan ödevin bütün teslim koşullarının karşılandığı iddia edilmez.
