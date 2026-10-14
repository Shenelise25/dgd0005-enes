# Basit çalışma rehberi

Her aşamada önce değişen scripti oku, sonra Unity'de dene. Kodun hangi bilgisayarda çalıştığını kendi cümlenle söyle.

1. **Projeyi aç:** Unity Hub → Add project from disk → bu reponun `Pong` klasörü → 6000.3.2f1. Paketlerin yüklenmesini bekle.
2. **Yerel oyunu anla:** `Assets/Scenes/Pong-Start` → Play. W/S sol, oklar sağ raketi hareket ettirir. `Court` sahayı kurar; `LocalBall` topu hareket ettirir.
3. **Ağ sahnesini aç:** Play'i durdur → `Assets/Scenes/Pong`. NetworkManager, Hud, Court ve GameState nesnelerini incele.
4. **Sunucuyu başlat:** Play → Start Server. Bu işlem oyunun doğru kabul edilen durumunu tutacak sunucuyu açar.
5. **İstemcileri bağla:** Multiplayer Play Mode ile ayrı pencereler aç. Player 2'ye `Server` etiketi verirsen ServerBootstrap o pencerede sunucuyu otomatik açar; başka sunucu açma. Ana pencere ve diğer iki pencerede `127.0.0.1` → Start Client.
6. **Rolleri gör:** İlk oyuncu mavi LEFT, ikincisi turuncu RIGHT, üçüncüsü görünmez SPECTATOR. W/S veya oklar yalnızca kendi raketini oynatır. `IsOwner` bunu sağlar.
7. **Hareketi karşılaştır:** Kendi pencereni hareket ettirirken diğer pencereye bak. `NetworkTransform`, sahibi → sunucu → diğer istemciler yoluyla konumu gönderir.
8. **Servis at:** Başlangıçta LEFT oyuncusunda Space. RIGHT veya izleyici servis başlatamaz. İstemci isteği gönderir; sunucu servis sırasını kontrol eder.
9. **Golü gör:** Top kale çizgisini geçince skor her pencerede değişir. Gol bildirimi herkese, YOU SERVE yalnızca gol yiyene gider. Top merkezde gri olarak yeni servisi bekler.
10. **Yeniden bağlan:** Bir istemciyi Stop Client ile ayır ve tekrar bağla. Skor sıfırlanmamalı; son altı olay gelmeli. Sunucuyu kapatmak ise maçı bitirir.
11. **Gerçek ikinci bilgisayar:** İkiniz aynı hotspot'a bağlanın. Sunucunun yerel IPv4 adresini diğer bilgisayardaki HUD'a yazın. İki yönde de deneyip connection card'ı gerçek sonuçlarla doldurun.
12. **Tick deneyi:** Play'i durdur. NetworkManager/TimeManager Tick Rate'i 10 yap, başlat, rally sırasında 10 saniye bekle ve Server out B/s oku. 30 ve 60 için tekrarla. Oyuncu sayısı ve test koşulları aynı kalsın. Sonunda 30'a geri dön.
13. **Teslim öncesi:** Kendi tahminlerini değerlendir, kendi oyun fikrine ait sahiplik paragrafını yaz, README'de Bekliyor/Ölçülmedi kalan yerleri tamamla. İki bilgisayarda servis, rally, gol ve yeniden bağlantıyı göster.

## Beş scripti bir cümleyle anlat

- `ConnectionLogger`: bağlantı kurulunca veya kesilince Console'a yazar.
- `PaddleController`: oyuncunun rolünü ve kendi raketinin hareketini yönetir; üç RPC buradadır.
- `NetworkBall`: topu yalnızca sunucuda hareket ettirir.
- `BallSpawner`: sunucu başladığında bir ağ topu oluşturur.
- `GameState`: skor, servis sırası ve son altı olayın ortak durumunu tutar.

## Beklenen sunum

1. İki bilgisayar bağlanır ve iki raket her ekranda hareket eder.
2. Servis, rally, gol; skor ve gol bildirimi herkeste, YOU SERVE tek oyuncuda.
3. Bir oyuncu ayrılır ve geri gelir; skor korunur.
4. `IsOwner`, `SyncVar`, `ServerRpc`, `ObserversRpc`, `TargetRpc` kelimelerini açıkla.
