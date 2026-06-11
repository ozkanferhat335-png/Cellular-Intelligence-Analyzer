import React, { useState, useRef, useCallback } from 'react';
import { MeasurementRecord, HTSRecord } from '../types';
import { parseMeasurementCSV, parseHTSCSV, generateSampleMeasurements, generateSampleHTS } from '../utils/csvParser';

interface UploadModuleProps {
  measurements: MeasurementRecord[];
  htsRecords: HTSRecord[];
  onMeasurementsLoaded: (records: MeasurementRecord[]) => void;
  onHTSLoaded: (records: HTSRecord[]) => void;
}

type UploadStatus = 'idle' | 'loading' | 'success' | 'error';

interface FileState {
  file: File | null;
  status: UploadStatus;
  count: number;
  error?: string;
}

export default function UploadModule({ measurements, htsRecords, onMeasurementsLoaded, onHTSLoaded }: UploadModuleProps) {
  const [measureFile, setMeasureFile] = useState<FileState>({ file: null, status: 'idle', count: 0 });
  const [htsFile, setHtsFile] = useState<FileState>({ file: null, status: 'idle', count: 0 });
  const [dragOver, setDragOver] = useState<'measure' | 'hts' | null>(null);
  const measureRef = useRef<HTMLInputElement | null>(null);
  const htsRef = useRef<HTMLInputElement | null>(null);

  const handleMeasureFile = useCallback(async (file: File) => {
    setMeasureFile({ file, status: 'loading', count: 0 });
    try {
      const records = await parseMeasurementCSV(file);
      if (records.length === 0) throw new Error('Geçerli kayıt bulunamadı. Sütun başlıklarını kontrol edin.');
      onMeasurementsLoaded(records);
      setMeasureFile({ file, status: 'success', count: records.length });
    } catch (e: any) {
      setMeasureFile({ file, status: 'error', count: 0, error: e.message });
    }
  }, [onMeasurementsLoaded]);

  const handleHTSFile = useCallback(async (file: File) => {
    setHtsFile({ file, status: 'loading', count: 0 });
    try {
      const records = await parseHTSCSV(file);
      if (records.length === 0) throw new Error('Geçerli HTS kaydı bulunamadı.');
      onHTSLoaded(records);
      setHtsFile({ file, status: 'success', count: records.length });
    } catch (e: any) {
      setHtsFile({ file, status: 'error', count: 0, error: e.message });
    }
  }, [onHTSLoaded]);

  const loadSampleData = () => {
    const m = generateSampleMeasurements();
    const h = generateSampleHTS();
    onMeasurementsLoaded(m);
    onHTSLoaded(h);
    setMeasureFile({ file: null, status: 'success', count: m.length });
    setHtsFile({ file: null, status: 'success', count: h.length });
  };

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Veri Yükleme</h1>
          <p className="text-gray-500 text-sm mt-1">Drive test ve HTS verilerini CSV formatında yükleyin</p>
        </div>
        <button
          onClick={loadSampleData}
          className="px-4 py-2 bg-purple-600/20 hover:bg-purple-600/30 text-purple-300 border border-purple-700/30 rounded-lg text-sm transition-colors"
        >
          Örnek Veri Yükle
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Measurement Upload */}
        <DropZone
          title="Ölçüm Verisi (Drive Test)"
          subtitle="NetMonster · Network Cell Info · CellMapper · G-NetTrack"
          accept=".csv,.txt"
          dragActive={dragOver === 'measure'}
          fileState={measureFile}
          inputRef={measureRef}
          onDragOver={() => setDragOver('measure')}
          onDragLeave={() => setDragOver(null)}
          onDrop={(e) => {
            setDragOver(null);
            const f = e.dataTransfer.files[0];
            if (f) handleMeasureFile(f);
          }}
          onChange={(e) => {
            const f = e.target.files?.[0];
            if (f) handleMeasureFile(f);
          }}
          onClick={() => measureRef.current?.click()}
          fields={['Tarih/Saat', 'Latitude', 'Longitude', 'Cell ID', 'eNodeB ID', 'TAC', 'PCI', 'EARFCN', 'RSRP', 'RSRQ', 'SINR', 'Teknoloji']}
          color="blue"
        />

        {/* HTS Upload */}
        <DropZone
          title="HTS Kayıtları"
          subtitle="CSV veya Excel formatında HTS verileri"
          accept=".csv,.xlsx,.xls,.txt"
          dragActive={dragOver === 'hts'}
          fileState={htsFile}
          inputRef={htsRef}
          onDragOver={() => setDragOver('hts')}
          onDragLeave={() => setDragOver(null)}
          onDrop={(e) => {
            setDragOver(null);
            const f = e.dataTransfer.files[0];
            if (f) handleHTSFile(f);
          }}
          onChange={(e) => {
            const f = e.target.files?.[0];
            if (f) handleHTSFile(f);
          }}
          onClick={() => htsRef.current?.click()}
          fields={['Arama Zamanı', 'Başlangıç', 'Bitiş', 'Cell ID', 'LAC/TAC', 'IMSI (opsiyonel)', 'MSISDN (opsiyonel)']}
          color="purple"
        />
      </div>

      {/* Loaded Data Preview */}
      {measurements.length > 0 && (
        <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
          <h3 className="text-sm font-semibold text-gray-300 mb-3">Yüklenen Ölçüm Verisi Önizleme</h3>
          <div className="overflow-x-auto">
            <table className="w-full data-table">
              <thead>
                <tr>
                  <th>Zaman</th>
                  <th>Lat</th>
                  <th>Lng</th>
                  <th>Cell ID</th>
                  <th>TAC</th>
                  <th>PCI</th>
                  <th>RSRP</th>
                  <th>RSRQ</th>
                  <th>SINR</th>
                  <th>Tech</th>
                  <th>Kaynak</th>
                </tr>
              </thead>
              <tbody>
                {measurements.slice(0, 10).map(m => (
                  <tr key={m.id}>
                    <td className="font-mono text-xs">{new Date(m.timestamp).toLocaleString('tr-TR')}</td>
                    <td className="font-mono">{m.latitude.toFixed(5)}</td>
                    <td className="font-mono">{m.longitude.toFixed(5)}</td>
                    <td className="font-mono text-blue-400">{m.cellId}</td>
                    <td className="font-mono">{m.tac || '-'}</td>
                    <td className="font-mono">{m.pci || '-'}</td>
                    <td className="font-mono" style={{ color: m.rsrp ? rsrpColorInline(m.rsrp) : '#6b7280' }}>
                      {m.rsrp?.toFixed(1) || '-'}
                    </td>
                    <td className="font-mono">{m.rsrq?.toFixed(1) || '-'}</td>
                    <td className="font-mono">{m.sinr?.toFixed(1) || '-'}</td>
                    <td>
                      <span className="px-1.5 py-0.5 rounded text-xs bg-blue-900/30 text-blue-300">{m.technology}</span>
                    </td>
                    <td className="text-xs text-gray-500">{m.source}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {measurements.length > 10 && (
              <div className="text-center text-xs text-gray-600 mt-2">
                ... ve {measurements.length - 10} kayıt daha
              </div>
            )}
          </div>
        </div>
      )}

      {htsRecords.length > 0 && (
        <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
          <h3 className="text-sm font-semibold text-gray-300 mb-3">HTS Kayıtları Önizleme</h3>
          <div className="overflow-x-auto">
            <table className="w-full data-table">
              <thead>
                <tr>
                  <th>Arama Zamanı</th>
                  <th>Cell ID</th>
                  <th>TAC/LAC</th>
                  <th>Süre (sn)</th>
                  <th>MSISDN</th>
                </tr>
              </thead>
              <tbody>
                {htsRecords.slice(0, 10).map(h => (
                  <tr key={h.id}>
                    <td className="font-mono text-xs">{h.callTime ? new Date(h.callTime).toLocaleString('tr-TR') : '-'}</td>
                    <td className="font-mono text-purple-400">{h.cellId}</td>
                    <td className="font-mono">{h.tac || h.lac || '-'}</td>
                    <td className="font-mono">{h.duration || '-'}</td>
                    <td className="font-mono text-xs">{h.msisdn ? '***' + h.msisdn.slice(-4) : '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {htsRecords.length > 10 && (
              <div className="text-center text-xs text-gray-600 mt-2">
                ... ve {htsRecords.length - 10} kayıt daha
              </div>
            )}
          </div>
        </div>
      )}

      {/* Format Guide */}
      <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
        <h3 className="text-sm font-semibold text-gray-300 mb-3">Desteklenen Format Örnekleri</h3>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          <div>
            <div className="text-xs text-blue-400 mb-2 font-semibold">NetMonster CSV</div>
            <pre className="text-xs text-gray-500 bg-dark-900 rounded p-3 overflow-x-auto font-mono">
{`time,lat,lon,cell_id,tac,pci,rsrp,rsrq,sinr,rat
2024-01-15 10:30:00,41.015,28.979,12345,1001,10,-85,-10,15,LTE`}
            </pre>
          </div>
          <div>
            <div className="text-xs text-purple-400 mb-2 font-semibold">HTS CSV</div>
            <pre className="text-xs text-gray-500 bg-dark-900 rounded p-3 overflow-x-auto font-mono">
{`call_time,start_time,end_time,cell_id,tac,msisdn
2024-01-15 10:28:00,10:28:00,10:35:00,12345,1001,5551234567`}
            </pre>
          </div>
        </div>
      </div>
    </div>
  );
}

function rsrpColorInline(rsrp: number): string {
  if (rsrp >= -80) return '#22c55e';
  if (rsrp >= -90) return '#84cc16';
  if (rsrp >= -100) return '#eab308';
  if (rsrp >= -110) return '#f97316';
  return '#ef4444';
}

interface DropZoneProps {
  title: string;
  subtitle: string;
  accept: string;
  dragActive: boolean;
  fileState: FileState;
  inputRef: React.RefObject<HTMLInputElement | null>;
  onDragOver: () => void;
  onDragLeave: () => void;
  onDrop: (e: React.DragEvent) => void;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  onClick: () => void;
  fields: string[];
  color: 'blue' | 'purple';
}

function DropZone({ title, subtitle, accept, dragActive, fileState, inputRef, onDragOver, onDragLeave, onDrop, onChange, onClick, fields, color }: DropZoneProps) {
  const borderColor = color === 'blue' ? 'border-blue-700/40 hover:border-blue-500' : 'border-purple-700/40 hover:border-purple-500';
  const activeBorder = color === 'blue' ? 'border-blue-500 bg-blue-900/10' : 'border-purple-500 bg-purple-900/10';
  const iconColor = color === 'blue' ? 'text-blue-400' : 'text-purple-400';
  const tagColor = color === 'blue' ? 'bg-blue-900/30 text-blue-400' : 'bg-purple-900/30 text-purple-400';

  return (
    <div className="bg-dark-800 rounded-xl border border-gray-800 p-4 space-y-4">
      <div>
        <h3 className="text-sm font-semibold text-gray-200">{title}</h3>
        <p className="text-xs text-gray-500 mt-0.5">{subtitle}</p>
      </div>

      <div
        className={`drop-zone rounded-xl p-8 text-center cursor-pointer transition-all ${dragActive ? activeBorder : borderColor}`}
        onDragOver={(e) => { e.preventDefault(); onDragOver(); }}
        onDragLeave={onDragLeave}
        onDrop={(e) => { e.preventDefault(); onDrop(e); }}
        onClick={onClick}
      >
        <input ref={inputRef} type="file" accept={accept} className="hidden" onChange={onChange} />

        {fileState.status === 'loading' && (
          <div className="flex flex-col items-center gap-3">
            <div className="w-8 h-8 border-2 border-blue-500/30 border-t-blue-500 rounded-full animate-spin"></div>
            <span className="text-sm text-gray-400">Dosya işleniyor...</span>
          </div>
        )}

        {fileState.status === 'success' && (
          <div className="flex flex-col items-center gap-2">
            <div className="w-10 h-10 rounded-full bg-green-900/30 border border-green-700/30 flex items-center justify-center text-green-400 text-xl">✓</div>
            <div className="text-sm font-medium text-green-400">{fileState.count.toLocaleString()} kayıt yüklendi</div>
            <div className="text-xs text-gray-500">{fileState.file?.name || 'Örnek veri'}</div>
            <div className="text-xs text-gray-600 mt-1">Değiştirmek için tıklayın</div>
          </div>
        )}

        {fileState.status === 'error' && (
          <div className="flex flex-col items-center gap-2">
            <div className="w-10 h-10 rounded-full bg-red-900/30 border border-red-700/30 flex items-center justify-center text-red-400 text-xl">✕</div>
            <div className="text-sm font-medium text-red-400">Hata</div>
            <div className="text-xs text-red-500">{fileState.error}</div>
            <div className="text-xs text-gray-600 mt-1">Tekrar denemek için tıklayın</div>
          </div>
        )}

        {fileState.status === 'idle' && (
          <div className="flex flex-col items-center gap-3">
            <div className={`text-4xl ${iconColor} opacity-60`}>⬆</div>
            <div>
              <div className="text-sm font-medium text-gray-300">Dosyayı sürükleyin veya tıklayın</div>
              <div className="text-xs text-gray-600 mt-1">{accept.split(',').join(' · ')}</div>
            </div>
          </div>
        )}
      </div>

      <div>
        <div className="text-xs text-gray-600 mb-2">Desteklenen alanlar:</div>
        <div className="flex flex-wrap gap-1.5">
          {fields.map(f => (
            <span key={f} className={`text-xs px-2 py-0.5 rounded-full ${tagColor}`}>{f}</span>
          ))}
        </div>
      </div>
    </div>
  );
}
