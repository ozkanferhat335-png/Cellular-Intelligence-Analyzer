# Cellular Intelligence Analyzer (CIA)

## Telekom Saha Analiz, HTS Korelasyon ve Daraltılmış Baz İstasyonu Tespit Platformu

---

## 📋 Proje Özeti

CIA Platform, kurumsal seviyede çalışan, C# 7.3 Windows Forms ve SQLite kullanılarak geliştirilmiş bir Telekom Analiz Platformudur. HTS kayıtları, saha ölçüm sonuçları (Drive Test), baz istasyonu verileri ve RF parametrelerini tek merkezde toplayarak gelişmiş analizler gerçekleştirir.

---

## 🏗️ Mimari

```
CellularIntelligenceAnalyzer.sln
├── src/
│   ├── CIA.Core/           # Ortak nesneler, DTOs, Enumlar, Sabitler, Yardımcılar
│   ├── CIA.Data/           # SQLite, Entity Framework, Repository Pattern
│   ├── CIA.Business/       # Analiz motorları, RF hesaplamaları, HTS korelasyonu
│   ├── CIA.Services/       # Dosya aktarım, Raporlama, Auth servisleri
│   └── CIA.UI/             # Windows Forms arayüzü
```

---

## 🚀 Özellikler

### HTS Analiz Modülü
- Telefon numarası, IMEI, IMSI, Cell ID sorgulama
- Tarih aralığı filtreleme
- Hareket analizi ve rota tahmini
- Şüpheli hareket tespiti
- Abone ilişki analizi
- **10 Milyon+ kayıt işleme kapasitesi**

### Drive Test Analiz Modülü
- RSRP, RSRQ, SINR, RSSI, PCI, EARFCN analizi
- Kapsama deliği tespiti
- Overshooting tespiti
- PCI çakışma analizi
- Eksik komşu tespiti

### Daraltılmış Baz Analiz Motoru ⭐
- HTS + Drive Test korelasyonu
- Güven skoru hesaplama (0-100)
- Kapsama poligonu oluşturma
- Hareket modeli çıkarma
- Konum tahmini

### Kapsama Modelleme
- Okumura-Hata path loss modeli
- Sektör bazlı kapsama poligonları
- Drive Test ile doğrulama
- Arazi tipi desteği

### Harita Modülü (GMap.NET)
- Baz istasyonları katmanı
- Sektör görselleştirme
- Drive Test heatmap
- Kapsama modeli görselleştirme
- **100.000+ nokta desteği**

### Yapay Zeka Destekli Analiz
- Anomali tespiti
- RF optimizasyon önerileri
- HTS hareket anomalisi
- **Sadece öneri/risk/olasılık üretir, kesin hüküm vermez**

### Raporlama
- PDF (iTextSharp)
- Excel (EPPlus)
- CSV
- HTS, Drive Test, Daraltılmış Baz, Yönetici Özeti raporları

---

## 🛠️ Teknoloji Stack

| Katman | Teknoloji |
|--------|-----------|
| Backend | C# 7.3 |
| UI | Windows Forms |
| Veritabanı | SQLite |
| ORM | Entity Framework Core 6 |
| Harita | GMap.NET |
| PDF | iTextSharp |
| Excel | EPPlus |
| Loglama | NLog |
| JSON | Newtonsoft.Json |
| DI | Microsoft.Extensions.DependencyInjection |

---

## 📦 Kurulum

### Gereksinimler
- Windows 10/11
- .NET Framework 4.7.2
- Visual Studio 2019/2022

### Adımlar

1. Çözümü açın:
```
CellularIntelligenceAnalyzer.sln
```

2. NuGet paketlerini geri yükleyin:
```
dotnet restore
```

3. CIA.UI projesini başlangıç projesi olarak ayarlayın

4. Derleyin ve çalıştırın

### Varsayılan Giriş Bilgileri
- **Kullanıcı Adı:** admin
- **Şifre:** Admin@123!

---

## 📊 Performans Hedefleri

| Metrik | Hedef |
|--------|-------|
| HTS Kayıt Kapasitesi | 10 Milyon+ |
| Harita Noktası | 100.000+ |
| Harita Açılış Süresi | < 3 saniye |
| Sorgu Süresi | < 5 saniye |
| Filtreleme Süresi | < 2 saniye |
| RAM Kullanımı | < 500 MB |

---

## 🔐 Güvenlik

- PBKDF2 + SHA256 şifre hashleme
- Rol bazlı yetkilendirme (Admin, Analyst, Viewer, Operator)
- Hesap kilitleme (5 başarısız deneme)
- AES şifreleme
- Audit logları

---

## 📁 Veritabanı Tabloları

- Users, Roles, UserRoles
- Sites, Sectors, Cells
- DriveTests, DriveTestRecords
- HTSRecords
- ImportedFiles
- AnalysisResults
- CoverageModels
- Reports
- SystemLogs
- Settings

---

## 🎯 Güven Skoru Sistemi

| Skor | Seviye | Açıklama |
|------|--------|----------|
| 0-40 | Düşük | Yetersiz veri veya düşük eşleşme |
| 41-70 | Orta | Kısmi eşleşme, dikkatli değerlendirme |
| 71-100 | Yüksek | Güçlü eşleşme, yüksek güvenilirlik |

### Puanlama Parametreleri
| Parametre | Ağırlık |
|-----------|---------|
| Cell Eşleşmesi | %30 |
| Sektör Eşleşmesi | %20 |
| Ölçüm Eşleşmesi | %20 |
| Zaman Eşleşmesi | %15 |
| Mesafe Uygunluğu | %10 |
| Hareket Tutarlılığı | %5 |

---

## ⚠️ Yasal Uyarı

Bu platform analiz ve karar destek aracıdır. Yapay zeka destekli sonuçlar dahil tüm çıktılar **öneri, risk ve olasılık** değerlendirmesi niteliğindedir. Kesin hüküm niteliği taşımaz.

---

## 📝 Lisans

Kurumsal kullanım için geliştirilmiştir.
