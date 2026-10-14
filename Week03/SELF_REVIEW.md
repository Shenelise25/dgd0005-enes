# Ders notları Bölüm 14 - açıklamalı çalışma cevapları

Bu cevapları demo öncesi kendi cümlelerinle anlatabilmelisin.

1. **Aynı “şimdi” neden yok?** Bilgi kabloda anında ilerlemez; uzaklık, yönlendirme ve kuyruklar gecikme ekler. Notlardaki İstanbul–Frankfurt fiziksel alt sınırı tek yön 9,5 ms, RTT 19 ms; pratik RTT 35–50 ms. 60 fps'de kare 16,67 ms: tek yön fiziksel sınır 0,57 kare, RTT sınırı 1,14 kare; pratik RTT yaklaşık 2,1–3 kare. RTT ile tek yön gecikmeyi karıştırmamak gerekir.

2. **Topolojiler:**

| | Dedicated server | Listen server | Peer-to-peer |
|---|---|---|---|
| Güven | Bağımsız sunucu | Host oyuncusu | Dağıtılmış güven |
| Maliyet | Sunucu barındırma | Oyuncunun makinesi | Oyuncuların makineleri |
| Gecikme | Sunucuya gidiş | Host avantajı | Eşler arası değişken |

Adım 5'te Server etiketli ayrı süreç dedicated server rolündedir. Adım 11'in “Start Server + Start Client” talimatı aynı süreçte uygulanırsa teknik olarak listen server/host olur; topun otoritesi yine sunucudadır. Yalnız Start Server çalışan ayrı süreç dedicated server rolünü korur.

3. **UDP:** Eski konum paketinin yeniden gelmesini beklemek yeni konumları geciktirir. UDP üzerinde FishNet gerekli mesajlara güvenilirlik ekler. Skor SyncVar'ı güvenilir; sürekli konum güncellemeleri güvenilmez iletilebilir.

4. **NetworkObject / NetworkBehaviour:** NetworkObject ağ kimliği, sahiplik ve spawn yaşam döngüsü verir. NetworkBehaviour scriptin ağ durumunu sorgulamasını, SyncVar/RPC ve ağ callback'lerini kullanmasını sağlar. HUD yalnızca yerel arayüzdür; MonoBehaviour yeterli, NetworkObject gerekmez.

5. **Client 0'ın raketi, ayrı sunucu senaryosu:**

| Süreç | IsOwner | IsServerInitialized | Callback | Klavye? |
|---|---|---|---|---|
| Sunucu | false | true | OnStartServer | Hayır |
| Client 0 | true | false | OnStartClient | Evet |
| Client 1 | false | false | OnStartClient | Hayır |

Host, kendi raketinde hem server hem client callback'lerini alır; IsOwner ve IsServerInitialized aynı anda true olabilir.

6. **Adım 6 farkı:** NetworkTransform eklendi. Yol: sahip istemci → sunucu → diğer istemci. Client Authoritative rakette açık; topu sunucu simüle ettiği için topta kapalıdır.

7. **Bir top:** Court.Awake her süreçte yerel çalışır; Local Ball açıksa her süreç kendi topunu oluşturur. BallSpawner.OnStartServer yalnız sunucuda çalışır; tek Spawn çağrısının ağ kopyaları istemcilerde görünür. Normal istemci Spawn ile yetkili bir ağ nesnesi oluşturamaz; sunucuya istek göndermelidir.

8. **Durum ve olay:**

| Bilgi | Tür | Araç |
|---|---|---|
| Skor | Durum | SyncVar |
| LEFT scored | Olay | ObserversRpc |
| Servis sırası | Durum | SyncVar |
| YOU SERVE | Olay | TargetRpc |
| Raket vuruş sesi | Olay | ObserversRpc |
| Top oyunda mı | Durum | SyncVar |
| Top konumu | Sürekli durum | NetworkTransform |

9. **Vuruşa kim karar verir?** Sunucu, kendi elindeki raket kopyalarına göre karar verir. Gecikme nedeniyle istemci topun rakete değdiğini görürken sunucu farklı sonuç verebilir. İstemci InPlay SyncVar'ına yazarsa sunucu gerçeği değişmez; bu bir servis isteğinin yerine geçmez.

10. **Trafik:** 8 × 30 × 40 = 9.600 B/s bir istemci için; dört istemciye toplam 38.400 B/s. Bu yalnız varsayılan konum yüküdür; protokol ek yükü, değişim sıkıştırması ve diğer mesajlar dahil değildir. Beklenti: 10 tick daha az trafik, diğer raket ve top için daha seyrek güncelleme; kendi raketi yerelde her karede hareket ettiği için aynı hisseder. Gerçek ölçüm tablosu deney yapılınca tamamlanmalıdır.
