# VTCStockManagementCase - Uygulama Kullanım Dokümanı

Bu doküman, projeyi yerelde ayağa kaldırıp uçtan uca test etmeniz için pratik adımları içerir.

## 1) Önkoşullar

- .NET SDK (proje şu an `net10.0` hedefli)
- Docker Desktop (PostgreSQL ve integration testler için)
- Postman

## 2) Yerel kurulum

### 2.1 Veritabanını başlat

```bash
docker compose up -d
```

### 2.2 Migration uygula

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef database update --project src/VTCStockManagementCase.Infrastructure --startup-project src/VTCStockManagementCase.Api
```

### 2.3 API'yi çalıştır

```bash
dotnet run --project src/VTCStockManagementCase.Api
```

Not: Development ortamında ilk açılışta migration uygulanır ve `App:SeedOnStartup=true` ise örnek seed veriler basılır (idempotent).

Varsayılan adres:

- `http://localhost:5134`
- Swagger: `http://localhost:5134/swagger`
- Health: `http://localhost:5134/health`

## 3) API uç noktaları (kısa özet)

Aşağıdaki tablolar her endpoint'in ne yaptığını özetler. Tam şema için Swagger kullanın.

### 3.1 Sistem

| Metot | Yol | Özet |
|--------|-----|------|
| **GET** | `/health` | Sağlık kontrolü (basit uptime). |

### 3.2 Ürünler — `api/products`

| Metot | Yol | Özet |
|--------|-----|------|
| **POST** | `/api/products` | Yeni ürün oluşturur (SKU benzersiz olmalı; aksi 409). |
| **GET** | `/api/products` | Tüm ürünleri listeler. |
| **GET** | `/api/products/{id}` | ID ile tek ürün; yoksa 404. |

### 3.3 Envanter — `api/inventory`

| Metot | Yol | Özet |
|--------|-----|------|
| **POST** | `/api/inventory/stock-in` | Ürüne stok girişi (`OnHand` artar, stok hareketi yazılır). |
| **GET** | `/api/inventory/{productId}` | O ürün için OnHand / Reserved / Available; yoksa 404. |

### 3.4 Siparişler — `api/orders`

| Metot | Yol | Özet |
|--------|-----|------|
| **POST** | `/api/orders` | Sipariş oluşturur; atomik rezervasyon; yetersiz stok 409. |
| **GET** | `/api/orders/{id}` | Sipariş detayı; yoksa 404. |
| **POST** | `/api/orders/{id}/payments/simulate-success` | Ödeme başarı simülasyonu: stok commit, sipariş Completed, outbox mesajı. |
| **POST** | `/api/orders/{id}/payments/simulate-failure` | Ödeme başarısız: rezerv iptal, sipariş Failed. |
| **POST** | `/api/orders/{id}/cancel` | Yalnızca Pending siparişi iptal eder, rezerv iade. |

### 3.5 Kargo hazırlığı — `api/shipping-preparations`

| Metot | Yol | Özet |
|--------|-----|------|
| **GET** | `/api/shipping-preparations/{orderId}` | Outbox işlendikten sonra oluşan hazırlık kaydı; yoksa 404. |

### 3.6 Raporlar — `api/reports`

| Metot | Yol | Özet |
|--------|-----|------|
| **GET** | `/api/reports/daily-sales?date=yyyy-MM-dd` | Arşivlenmiş günlük satış (UTC takvim günü); veri yoksa 404. |
| **GET** | `/api/reports/critical-stock` | Kritik stok kayıtlarından ürün bazında son tespitler. |

### 3.7 Outbox (gözlem) — `api/outbox`

| Metot | Yol | Özet |
|--------|-----|------|
| **GET** | `/api/outbox/pending-count` | İşlenmemiş outbox mesaj sayısı (`pendingCount`). |

## 4) Postman ile test

### 4.1 Dosyaları içe aktar

- `.postman/VTCStockManagementCase.postman_collection.json`
- `.postman/VTCStockManagementCase.postman_environment.json`

Environment içinde:

- `baseUrl = http://localhost:5134`
- `reportDate = YYYY-MM-DD` (isteğe göre bugünün veya geçmiş bir günün tarihi)

### 4.2 Önerilen çalışma sırası

Koleksiyon aşağıdaki klasörlerle gelir:

1. `00 - System`
2. `01 - Happy Path (Create -> Reserve -> Success -> Shipping)`
3. `02 - Failure Path (Reserve -> Payment Fail -> Release)`
4. `03 - Cancel Path (Pending -> Cancel)`
5. `04 - Reports`

Öneri: önce 00 ve 01 klasörlerini, sonra sırasıyla 02, 03 ve 04'ü çalıştırın.

### 4.3 Otomatik değişkenler

Koleksiyon test scriptleri otomatik olarak şunları doldurur:

- `productId`
- `orderId`
- `orderId2`
- `pendingOrderId`

Bu sayede request'leri tek tek manuel kopyalama yapmadan çalıştırabilirsiniz.

## 5) Beklenen davranışlar

- Başarılı ödeme sonrası:
  - sipariş `Completed`
  - rezerv düşer, `OnHand` kalıcı azalır
  - shipping preparation kaydı oluşur
- Başarısız ödeme sonrası:
  - sipariş `Failed`
  - rezerv hemen serbest bırakılır
- Cancel akışında:
  - yalnızca `Pending` sipariş iptal edilir
  - rezerv serbest bırakılır

## 6) Hızlı sorun giderme

- `connection refused`:
  - API ayakta mı (`dotnet run`)?
  - `baseUrl` doğru mu?
- Veritabanı hatası: 
  - `docker compose ps` ile postgres up mı?
  - migration uygulandı mı?
- Shipping kaydı görünmüyor:
  - ilgili sipariş için `simulate-success` çağrısı yapıldı mı?
  - `GET /api/outbox/pending-count` ile outbox durumu kontrol edin

## 7) Komut özeti

```bash
docker compose up -d
dotnet build
dotnet run --project src/VTCStockManagementCase.Api
dotnet test
```
