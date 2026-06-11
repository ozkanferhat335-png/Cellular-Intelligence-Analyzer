# Cellular Intelligence Analyzer

**Saha Ölçümüne Dayalı Daraltılmış Baz Analiz Modülü**

RF Planning / Drive Test analiz yazılımı — 4G/5G/GSM mobil şebeke ölçümlerini analiz ederek baz istasyonu tespiti yapar.

## Özellikler

### Veri Yükleme
- **Drive Test CSV**: NetMonster, Network Cell Info, CellMapper, G-NetTrack formatları
- **HTS Kayıtları**: CSV/Excel formatında arama kayıtları
- Otomatik sütun eşleştirme (auto-mapping)
- Örnek veri ile hızlı test

### Harita Motoru (Leaflet.js + OpenStreetMap)
- Ölçüm noktaları (RSRP renk skalası)
- HTS kayıt noktaları
- Baz istasyonu konumları (tahmini)
- Sektör fanı görselleştirme (0°/120°/240°)
- Hareket rotası ve durak noktaları
- Katman kontrolü

### Daraltılmış Baz Analizi
Ağırlıklı puanlama algoritması:
- **Cell ID Eşleşmesi** — %40 ağırlık
- **TAC/LAC Eşleşmesi** — %25 ağırlık
- **Sinyal Kalitesi (RSRP)** — %20 ağırlık
- **Coğrafi Yakınlık** — %15 ağırlık

Sonuç: %95 / %80 / %60 olasılık sıralaması

### Isı Haritası
- RSRP, RSRQ, SINR metriklerinde renk skalası
- Opaklık kontrolü
- Anlık istatistikler

### Raporlama
- **PDF**: Analiz özeti, sinyal istatistikleri, baz adayları tablosu
- **Excel**: Ham veriler, HTS kayıtları, analiz sonuçları (çoklu sayfa)

## Kurulum

```bash
cd cellular-analyzer
npm install
npm start
```

## Teknoloji Stack

- **Frontend**: React 18 + TypeScript
- **Harita**: Leaflet.js + OpenStreetMap
- **Grafikler**: Recharts
- **CSV Parsing**: PapaParse
- **Excel**: SheetJS (xlsx)
- **PDF**: jsPDF + jspdf-autotable
- **Stil**: Tailwind CSS 3

## Desteklenen CSV Formatları

### Ölçüm Verisi
```csv
time,lat,lon,cell_id,tac,pci,rsrp,rsrq,sinr,rat
2024-01-15 10:30:00,41.015,28.979,12345,1001,10,-85,-10,15,LTE
```

### HTS Kaydı
```csv
call_time,start_time,end_time,cell_id,tac,msisdn
2024-01-15 10:28:00,10:28:00,10:35:00,12345,1001,5551234567
```

## Analiz Algoritması

1. Ölçüm verilerinden benzersiz Cell ID'ler çıkarılır
2. Her Cell ID için baz istasyonu tahmini yapılır (ölçüm centroid'i)
3. HTS kayıtlarındaki Cell ID, TAC/LAC ile eşleştirme yapılır
4. RSRP bazlı sinyal kalitesi puanlanır
5. Adaylar olasılık sırasına göre listelenir
