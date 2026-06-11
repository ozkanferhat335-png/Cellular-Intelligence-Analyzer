import React, { useState } from 'react';
import { MeasurementRecord, HTSRecord, AnalysisResult } from '../types';
import { computeMeasurementStats, rsrpLabel } from '../utils/analysisEngine';
import * as XLSX from 'xlsx';

interface ReportModuleProps {
  measurements: MeasurementRecord[];
  htsRecords: HTSRecord[];
  analysisResult: AnalysisResult | null;
}

export default function ReportModule({ measurements, htsRecords, analysisResult }: ReportModuleProps) {
  const [generating, setGenerating] = useState<'pdf' | 'excel' | null>(null);
  const [generated, setGenerated] = useState<string | null>(null);

  const stats = measurements.length > 0 ? computeMeasurementStats(measurements) : null;

  const exportExcel = async () => {
    setGenerating('excel');
    try {
      const wb = XLSX.utils.book_new();

      // Sheet 1: Measurements
      if (measurements.length > 0) {
        const measData = measurements.map(m => ({
          'Zaman': m.timestamp,
          'Latitude': m.latitude,
          'Longitude': m.longitude,
          'Cell ID': m.cellId,
          'eNodeB ID': m.eNodeBId || '',
          'TAC': m.tac || '',
          'PCI': m.pci || '',
          'EARFCN': m.earfcn || '',
          'RSRP (dBm)': m.rsrp || '',
          'RSRQ (dB)': m.rsrq || '',
          'SINR (dB)': m.sinr || '',
          'Teknoloji': m.technology,
          'Kaynak': m.source,
        }));
        const ws1 = XLSX.utils.json_to_sheet(measData);
        XLSX.utils.book_append_sheet(wb, ws1, 'Ölçüm Verileri');
      }

      // Sheet 2: HTS Records
      if (htsRecords.length > 0) {
        const htsData = htsRecords.map(h => ({
          'Arama Zamanı': h.callTime,
          'Başlangıç': h.startTime || '',
          'Bitiş': h.endTime || '',
          'Cell ID': h.cellId,
          'TAC/LAC': h.tac || h.lac || '',
          'Süre (sn)': h.duration || '',
          'MSISDN': h.msisdn ? '***' + h.msisdn.slice(-4) : '',
        }));
        const ws2 = XLSX.utils.json_to_sheet(htsData);
        XLSX.utils.book_append_sheet(wb, ws2, 'HTS Kayıtları');
      }

      // Sheet 3: Analysis Results
      if (analysisResult) {
        const analysisData = analysisResult.candidates.map((c, i) => ({
          'Sıra': i + 1,
          'Cell ID': c.baseStation.cellId,
          'eNodeB ID': c.baseStation.eNodeBId || '',
          'TAC': c.baseStation.tac || '',
          'PCI': c.baseStation.pci || '',
          'Teknoloji': c.baseStation.technology,
          'Olasılık (%)': c.probability,
          'Ort. RSRP (dBm)': c.avgRSRP?.toFixed(1) || '',
          'Ort. RSRQ (dB)': c.avgRSRQ?.toFixed(1) || '',
          'Ort. SINR (dB)': c.avgSINR?.toFixed(1) || '',
          'Eşleşen Ölçüm': c.matchedMeasurements.length,
          'Eşleşen HTS': c.matchedHTS.length,
          'Tahmini Lat': c.baseStation.latitude?.toFixed(5) || '',
          'Tahmini Lng': c.baseStation.longitude?.toFixed(5) || '',
        }));
        const ws3 = XLSX.utils.json_to_sheet(analysisData);
        XLSX.utils.book_append_sheet(wb, ws3, 'Baz Analizi');
      }

      // Sheet 4: Statistics
      if (stats) {
        const statsData = [
          { 'Metrik': 'RSRP Min (dBm)', 'Değer': stats.rsrp.min },
          { 'Metrik': 'RSRP Max (dBm)', 'Değer': stats.rsrp.max },
          { 'Metrik': 'RSRP Ort. (dBm)', 'Değer': stats.rsrp.avg },
          { 'Metrik': 'RSRP Medyan (dBm)', 'Değer': stats.rsrp.median },
          { 'Metrik': 'RSRQ Min (dB)', 'Değer': stats.rsrq.min },
          { 'Metrik': 'RSRQ Max (dB)', 'Değer': stats.rsrq.max },
          { 'Metrik': 'RSRQ Ort. (dB)', 'Değer': stats.rsrq.avg },
          { 'Metrik': 'SINR Min (dB)', 'Değer': stats.sinr.min },
          { 'Metrik': 'SINR Max (dB)', 'Değer': stats.sinr.max },
          { 'Metrik': 'SINR Ort. (dB)', 'Değer': stats.sinr.avg },
          { 'Metrik': 'Toplam Ölçüm', 'Değer': measurements.length },
          { 'Metrik': 'Benzersiz Hücre', 'Değer': stats.cellIds.length },
          { 'Metrik': 'HTS Kaydı', 'Değer': htsRecords.length },
        ];
        const ws4 = XLSX.utils.json_to_sheet(statsData);
        XLSX.utils.book_append_sheet(wb, ws4, 'İstatistikler');
      }

      XLSX.writeFile(wb, `cellular-analysis-${new Date().toISOString().slice(0, 10)}.xlsx`);
      setGenerated('excel');
    } catch (e) {
      console.error(e);
    }
    setGenerating(null);
  };

  const exportPDF = async () => {
    setGenerating('pdf');
    try {
      const { jsPDF } = await import('jspdf');
      const autoTable = (await import('jspdf-autotable')).default;

      const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
      const pageW = doc.internal.pageSize.getWidth();
      let y = 15;

      // Header
      doc.setFillColor(13, 21, 38);
      doc.rect(0, 0, pageW, 35, 'F');
      doc.setTextColor(96, 165, 250);
      doc.setFontSize(18);
      doc.setFont('helvetica', 'bold');
      doc.text('Cellular Intelligence Analyzer', 15, 15);
      doc.setFontSize(10);
      doc.setTextColor(156, 163, 175);
      doc.text('Saha Ölçümüne Dayalı Daraltılmış Baz Analiz Raporu', 15, 23);
      doc.text(`Rapor Tarihi: ${new Date().toLocaleString('tr-TR')}`, 15, 30);
      y = 45;

      // Summary
      doc.setTextColor(30, 30, 30);
      doc.setFontSize(13);
      doc.setFont('helvetica', 'bold');
      doc.text('1. Analiz Özeti', 15, y);
      y += 8;

      const summaryData = [
        ['Toplam Ölçüm Noktası', measurements.length.toLocaleString()],
        ['HTS Kaydı', htsRecords.length.toLocaleString()],
        ['Benzersiz Hücre ID', stats?.cellIds.length.toLocaleString() || '0'],
        ['Analiz Tarihi', new Date().toLocaleString('tr-TR')],
        ...(analysisResult ? [
          ['Eşleşen Çift', analysisResult.matchedPairs.toLocaleString()],
          ['Aday Baz İstasyonu', analysisResult.candidates.length.toLocaleString()],
        ] : []),
      ];

      autoTable(doc, {
        startY: y,
        head: [['Parametre', 'Değer']],
        body: summaryData,
        theme: 'striped',
        headStyles: { fillColor: [37, 99, 235], textColor: 255 },
        margin: { left: 15, right: 15 },
      });

      y = (doc as any).lastAutoTable.finalY + 10;

      // Signal Stats
      if (stats) {
        doc.setFontSize(13);
        doc.setFont('helvetica', 'bold');
        doc.text('2. Sinyal İstatistikleri', 15, y);
        y += 5;

        autoTable(doc, {
          startY: y,
          head: [['Metrik', 'Min', 'Max', 'Ortalama', 'Medyan', 'Sayı']],
          body: [
            ['RSRP (dBm)', stats.rsrp.min.toFixed(1), stats.rsrp.max.toFixed(1), stats.rsrp.avg.toFixed(1), stats.rsrp.median.toFixed(1), stats.rsrp.count.toString()],
            ['RSRQ (dB)', stats.rsrq.min.toFixed(1), stats.rsrq.max.toFixed(1), stats.rsrq.avg.toFixed(1), stats.rsrq.median.toFixed(1), stats.rsrq.count.toString()],
            ['SINR (dB)', stats.sinr.min.toFixed(1), stats.sinr.max.toFixed(1), stats.sinr.avg.toFixed(1), stats.sinr.median.toFixed(1), stats.sinr.count.toString()],
          ],
          theme: 'striped',
          headStyles: { fillColor: [37, 99, 235], textColor: 255 },
          margin: { left: 15, right: 15 },
        });

        y = (doc as any).lastAutoTable.finalY + 10;
      }

      // Analysis Results
      if (analysisResult && analysisResult.candidates.length > 0) {
        if (y > 220) { doc.addPage(); y = 15; }

        doc.setFontSize(13);
        doc.setFont('helvetica', 'bold');
        doc.text('3. Baz İstasyonu Adayları', 15, y);
        y += 5;

        autoTable(doc, {
          startY: y,
          head: [['Sıra', 'Cell ID', 'TAC', 'PCI', 'Tech', 'Olasılık', 'Ort. RSRP', 'Eşleşme']],
          body: analysisResult.candidates.slice(0, 20).map((c, i) => [
            (i + 1).toString(),
            c.baseStation.cellId.toString(),
            c.baseStation.tac?.toString() || 'N/A',
            c.baseStation.pci?.toString() || 'N/A',
            c.baseStation.technology,
            `%${c.probability}`,
            c.avgRSRP ? `${c.avgRSRP.toFixed(1)} dBm` : 'N/A',
            `${c.matchedHTS.length} HTS`,
          ]),
          theme: 'striped',
          headStyles: { fillColor: [37, 99, 235], textColor: 255 },
          margin: { left: 15, right: 15 },
          didParseCell: (data: any) => {
            if (data.column.index === 5 && data.section === 'body') {
              const prob = parseInt(data.cell.text[0].replace('%', ''));
              if (prob >= 80) data.cell.styles.textColor = [34, 197, 94];
              else if (prob >= 60) data.cell.styles.textColor = [234, 179, 8];
              else data.cell.styles.textColor = [249, 115, 22];
            }
          },
        });

        y = (doc as any).lastAutoTable.finalY + 10;
      }

      // Footer
      const pageCount = doc.getNumberOfPages();
      for (let i = 1; i <= pageCount; i++) {
        doc.setPage(i);
        doc.setFontSize(8);
        doc.setTextColor(150);
        doc.text(
          `Cellular Intelligence Analyzer · Sayfa ${i}/${pageCount}`,
          pageW / 2, doc.internal.pageSize.getHeight() - 8,
          { align: 'center' }
        );
      }

      doc.save(`cellular-analysis-${new Date().toISOString().slice(0, 10)}.pdf`);
      setGenerated('pdf');
    } catch (e) {
      console.error(e);
    }
    setGenerating(null);
  };

  const isEmpty = measurements.length === 0 && htsRecords.length === 0;

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-white">Rapor Oluştur</h1>
        <p className="text-gray-500 text-sm mt-1">PDF ve Excel formatında analiz raporu indirin</p>
      </div>

      {isEmpty && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <div className="text-5xl mb-4 opacity-20">▤</div>
          <h2 className="text-lg font-semibold text-gray-400 mb-2">Rapor için veri gerekli</h2>
          <p className="text-gray-600 text-sm">Önce ölçüm verisi ve HTS kayıtlarını yükleyin.</p>
        </div>
      )}

      {!isEmpty && (
        <>
          {/* Report Cards */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* PDF Report */}
            <div className="bg-dark-800 rounded-xl border border-gray-800 p-6">
              <div className="flex items-start gap-4 mb-4">
                <div className="w-12 h-12 rounded-xl bg-red-900/20 border border-red-800/30 flex items-center justify-center text-red-400 text-2xl">
                  ▤
                </div>
                <div>
                  <h3 className="font-semibold text-white">PDF Raporu</h3>
                  <p className="text-xs text-gray-500 mt-0.5">Analiz özeti, tablolar ve istatistikler</p>
                </div>
              </div>

              <div className="space-y-2 mb-5">
                {[
                  'Analiz özeti ve parametreler',
                  'Sinyal istatistikleri (RSRP, RSRQ, SINR)',
                  'Baz istasyonu adayları tablosu',
                  'Olasılık sıralaması',
                  'Eşleşme kriterleri detayı',
                ].map(item => (
                  <div key={item} className="flex items-center gap-2 text-xs text-gray-400">
                    <span className="text-green-400">✓</span>
                    {item}
                  </div>
                ))}
              </div>

              <button
                onClick={exportPDF}
                disabled={generating !== null}
                className="w-full py-2.5 bg-red-600/20 hover:bg-red-600/30 border border-red-700/30 text-red-300 rounded-lg text-sm font-medium transition-colors flex items-center justify-center gap-2 disabled:opacity-50"
              >
                {generating === 'pdf' ? (
                  <>
                    <span className="w-4 h-4 border-2 border-red-400/30 border-t-red-400 rounded-full animate-spin"></span>
                    Oluşturuluyor...
                  </>
                ) : '⬇ PDF İndir'}
              </button>
            </div>

            {/* Excel Report */}
            <div className="bg-dark-800 rounded-xl border border-gray-800 p-6">
              <div className="flex items-start gap-4 mb-4">
                <div className="w-12 h-12 rounded-xl bg-green-900/20 border border-green-800/30 flex items-center justify-center text-green-400 text-2xl">
                  ▣
                </div>
                <div>
                  <h3 className="font-semibold text-white">Excel Raporu</h3>
                  <p className="text-xs text-gray-500 mt-0.5">Ham veriler ve analiz sonuçları</p>
                </div>
              </div>

              <div className="space-y-2 mb-5">
                {[
                  'Tüm ölçüm verileri (ham)',
                  'HTS kayıtları',
                  'Baz analizi sonuçları',
                  'İstatistik özeti',
                  'Filtrelenebilir tablolar',
                ].map(item => (
                  <div key={item} className="flex items-center gap-2 text-xs text-gray-400">
                    <span className="text-green-400">✓</span>
                    {item}
                  </div>
                ))}
              </div>

              <button
                onClick={exportExcel}
                disabled={generating !== null}
                className="w-full py-2.5 bg-green-600/20 hover:bg-green-600/30 border border-green-700/30 text-green-300 rounded-lg text-sm font-medium transition-colors flex items-center justify-center gap-2 disabled:opacity-50"
              >
                {generating === 'excel' ? (
                  <>
                    <span className="w-4 h-4 border-2 border-green-400/30 border-t-green-400 rounded-full animate-spin"></span>
                    Oluşturuluyor...
                  </>
                ) : '⬇ Excel İndir'}
              </button>
            </div>
          </div>

          {/* Data Summary */}
          <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
            <h3 className="text-sm font-semibold text-gray-300 mb-4">Rapor İçeriği Özeti</h3>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <SummaryItem label="Ölçüm Noktası" value={measurements.length.toLocaleString()} />
              <SummaryItem label="HTS Kaydı" value={htsRecords.length.toLocaleString()} />
              <SummaryItem label="Baz Adayı" value={(analysisResult?.candidates.length || 0).toLocaleString()} />
              <SummaryItem label="Eşleşme" value={(analysisResult?.matchedPairs || 0).toLocaleString()} />
            </div>

            {stats && (
              <div className="mt-4 pt-4 border-t border-gray-800 grid grid-cols-3 gap-4">
                <div>
                  <div className="text-xs text-gray-500 mb-1">RSRP Aralığı</div>
                  <div className="text-sm font-mono text-gray-300">
                    {stats.rsrp.min.toFixed(1)} ~ {stats.rsrp.max.toFixed(1)} dBm
                  </div>
                  <div className="text-xs text-gray-600">Ort: {stats.rsrp.avg.toFixed(1)} dBm · {rsrpLabel(stats.rsrp.avg)}</div>
                </div>
                <div>
                  <div className="text-xs text-gray-500 mb-1">RSRQ Aralığı</div>
                  <div className="text-sm font-mono text-gray-300">
                    {stats.rsrq.min.toFixed(1)} ~ {stats.rsrq.max.toFixed(1)} dB
                  </div>
                </div>
                <div>
                  <div className="text-xs text-gray-500 mb-1">SINR Aralığı</div>
                  <div className="text-sm font-mono text-gray-300">
                    {stats.sinr.min.toFixed(1)} ~ {stats.sinr.max.toFixed(1)} dB
                  </div>
                </div>
              </div>
            )}
          </div>

          {generated && (
            <div className="flex items-center gap-3 p-3 bg-green-900/20 border border-green-800/30 rounded-xl text-sm text-green-400">
              <span>✓</span>
              <span>{generated === 'pdf' ? 'PDF' : 'Excel'} raporu başarıyla indirildi.</span>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function SummaryItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-dark-900 rounded-lg p-3 border border-gray-800">
      <div className="text-xs text-gray-500 mb-1">{label}</div>
      <div className="text-xl font-bold text-white font-mono">{value}</div>
    </div>
  );
}
