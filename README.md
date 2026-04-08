VTCStockManagementCase uygulama kullanım notu

local çalışmasına uyumlu

sistem gereksinimleri;

.NET SDK lazım proje net10.0 hedefli
Docker Desktop lazım postgres için
Postman 

Yerel kurulum

1 veritabanını 
docker compose up -d

`Host=localhost;Port=5432;Database=vtcstock;Username=postgres;Password=postgres`

2 migration 
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef database update --project src/VTCStockManagementCase.Infrastructure --startup-project src/VTCStockManagementCase.Api

3 api runß
dotnet run --project src/VTCStockManagementCase.Api

not development ortamında ilk açılışta migration çalışır eğer App:SeedOnStartup=true ise örnek seed datalar da basılır aynı şeyi tekrar tekrar bozmaz

default adresler
http://localhost:5134
swagger için
http://localhost:5134/swagger
health için
http://localhost:5134/health


api kısa özet

sistem
GET /health
uygulama ayakta mı onu kontrol eder

ürünler api/products
POST /api/products
yeni ürün oluşturur sku benzersiz olmalı yoksa 409 döner
GET /api/products
tüm ürünleri listeler
GET /api/products/{id}
id ye göre tek ürün getirir yoksa 404

envanter api/inventory
POST /api/inventory/stock-in
ürüne stok girişi yapar OnHand artar stok hareketi oluşur
GET /api/inventory/{productId}
ürünün OnHand Reserved Available bilgisini getirir yoksa 404

siparişler api/orders
POST /api/orders
sipariş oluşturur rezervasyon yapar stok yetmiyorsa 409 döner
GET /api/orders/{id}
sipariş detayını getirir yoksa 404
POST /api/orders/{id}/payments/simulate-success
ödeme başarılı olmuş gibi davranır stok commit olur sipariş Completed olur outbox mesajı oluşur
POST /api/orders/{id}/payments/simulate-failure
ödeme başarısız olmuş gibi davranır rezerv iptal edilir sipariş Failed olur
POST /api/orders/{id}/cancel
sadece Pending durumundaki siparişi iptal eder rezerv geri bırakılır

kargo hazırlığı api/shipping-preparations
GET /api/shipping-preparations/{orderId}
outbox işlendikten sonra oluşan shipping preparation kaydını getirir yoksa 404

raporlar api/reports
GET /api/reports/daily-sales?date=yyyy-MM-dd
günlük satış raporunu getirir veri yoksa 404
GET /api/reports/critical-stock
kritik stok kayıtlarından ürün bazında son durumu verir

outbox api/outbox
GET /api/outbox/pending-count
işlenmemiş outbox mesaj sayısını verir

postman tarafı

şunları import et
.postman/VTCStockManagementCase.postman_collection.json
.postman/VTCStockManagementCase.postman_environment.json

environment içinde
baseUrl = http://localhost:5134
reportDate = YYYY-MM-DD

çalıştırma sırası olarak bence şöyle gitmek en rahatı
00 System
01 Happy Path Create -> Reserve -> Success -> Shipping
02 Failure Path Reserve -> Payment Fail -> Release
03 Cancel Path Pending -> Cancel
04 Reports

önce 00 ve 01 sonra 02 03 04 diye devam etmek mantıklı

koleksiyon bazı değişkenleri otomatik dolduruyor
productId
orderId
orderId2
pendingOrderId

o yüzden her şeyi elle kopyala yapıştır yapmana çok gerek kalmaz

beklenen davranışlar

ödeme başarılı olursa
sipariş Completed olur
rezerv düşer
OnHand kalıcı azalır
shipping preparation kaydı oluşur

ödeme başarısız olursa
sipariş Failed olur
rezerv hemen serbest kalır

cancel akışında
sadece Pending sipariş iptal edilir
rezerv serbest bırakılır

kısa sorun giderme

connection refused alırsan
api çalışıyor mu dotnet run ile bakılmalı
baseUrl doğru mu bakılmalı

db hatası varsa
docker compose ps ile postgres ayakta mı kontrol et
migration uygulanmış mı bakılmalı

shipping kaydı görünmüyorsa
ilgili sipariş için simulate-success çağrıldı mı bakılmalı
GET /api/outbox/pending-count ile outbox bekliyor mu kontrol et

komut özeti

docker compose up -d
dotnet build
dotnet run --project src/VTCStockManagementCase.Api