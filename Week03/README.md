# Week 03 - Pong connects and replicates

## Durum ve kaynaklar

Bu çalışma verilen üç Starter scripti, Week03_Homework ve ders notları temel alınarak hazırlanıyor.
Tools klasörü ve Pong-Start.unitypackage paylaşılmadığı için sahne, ConnectionHud ve ServerBootstrap yeniden oluşturuldu.
FishNet resmi `4.7.3` etiketi: `73f30cf2425dc808a4f463f0a233d386010810b3`.
Bu etiketin kaynak içindeki sürüm sabiti/package.json değeri `4.7.2` olarak kalmış; kaynak sürüm metni değiştirilmedi.
Bu metin bir çalışma rehberidir; gerçek cihaz ölçümleri ve kişisel tahminler yerine uydurma sonuç yazılmaz.

## Checkpoint D

1. `NetworkObject`, sunucuda atanan ObjectId ile aynı nesnenin ağdaki kopyalarını tanımlar.
2. LEFT/RIGHT seçimini sunucudaki `PaddleController.OnStartServer` yapar. İstemci `Side` SyncVar'ının güncel değerini alır; `OnChange`/`OnStartClient` raketi yerleştirir.
3. `Update` başındaki `if (!IsOwner) return;` başka oyuncunun raketinde klavye okunmasını durdurur.

## Predictions

Aşağıdakiler açıklamalı örnek yanıtlardır. Ders notları incelendikten sonra yazıldığından öğrencinin önceden yaptığı tahmin olarak sunulmaz.

1. Sahip istemci raket konumunu sunucuya, sunucu diğer gözlemcilere iletmelidir.
2. Gerçek topu sunucu simüle etmelidir; istemci çizmek için topun konumunu ve oyunda olup olmadığını almalıdır.
3. İstemci konumuna güvenmek, raketin topun yanına ışınlanması veya hız sınırını aşması hilesini mümkün kılar.

Kendi ilk tahminlerin: **Enes dolduracak**. Doğru/yanlış/yarım değerlendirmesi: **deneyden sonra doldurulacak**.

## Connection card

Gerçek ikinci bilgisayar / ortak hotspot deneyi henüz yapılmadı. Loopback testi bunun yerine geçmez.

| Yön | Partner | Ağ | Sunucu IP | Benim ClientId / side | RTT | İlk denemede? |
|---|---|---|---|---|---|---|
| Enes sunucu → partner istemci | Bekliyor | Bekliyor | Yerelde doldur | Bekliyor | Ölçülmedi | Test edilmedi |
| Partner sunucu → Enes istemci | Bekliyor | Bekliyor | Yerelde doldur | Bekliyor | Ölçülmedi | Test edilmedi |

## Design sheet

| Thing | State or event? | FishNet piece | On which object? | Who writes / calls? |
|---|---|---|---|---|
| Ball position | State | NetworkTransform | Ball | Server |
| Ball in play? | State | SyncVar bool | Ball | Server |
| Two scores | State | SyncVar int | GameState | Server |
| Whose serve | State | SyncVar int | GameState | Server |
| Let me serve | Request event | ServerRpc | Paddle | Owner client |
| LEFT scored to all | Event | ObserversRpc | Scorer paddle | Server |
| YOU SERVE to one | Event | TargetRpc | Conceder paddle | Server |
| Event feed | State | SyncList string | GameState | Server |

Court üzerindeki `BallSpawner`, `OnStartServer` içinde top prefabını `(0, Court.BallY, 0)` konumunda Instantiate eder ve sahipsiz Spawn yapar.
Topun `Update` metodunun ilk satırı `if (!IsServerInitialized) return;` olur.

Bu tasarım tablosu, top ve skor kodundan önce ayrı commit olarak kaydedilmiştir.

## Aşamalar

0. Unity 6000.3.2f1 Universal 3D projesi, FishNet ve Multiplayer Play Mode.
1. Pong-Start sahnesinde tek bilgisayarda iki raket; sonra Pong sahnesi.
2. NetworkManager / Tugboat 7770 / localhost; Start On Headless kapalı.
3. ConnectionLogger, HUD ve ServerBootstrap.
4. Sahiplik kontrollü Paddle prefabı; PlayerSpawner ve Side SyncVar.
5. Bir sunucu, üç istemci: LEFT, RIGHT, SPECTATOR.
6. Paddle NetworkTransform: client authoritative, interpolation 2; dönüş/ölçek kapalı.
11. İkinci bilgisayarla iki yönlü bağlantı testi.
7. Sunucuda bir top, GameState içinde skor ve servis sırası.
8. ServerRpc servis isteği; ObserversRpc gol; TargetRpc YOU SERVE.
9. Sunucunun yazdığı en fazla altı satırlık SyncList olay akışı.
10. 10/30/60 tick ölçümü; teslimde tekrar 30.
