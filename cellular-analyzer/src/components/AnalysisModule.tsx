import React, { useState } from 'react';
import { MeasurementRecord, HTSRecord, AnalysisResult, AnalysisCandidate } from '../types';
import { rsrpColor, rsrpLabel } from '../utils/analysisEngine';
import { ProbabilityBadge } from './Dashboard';

interface AnalysisModuleProps {
  measurements: MeasurementRecord[];
  htsRecords: HTSRecord[];
  analysisResult: AnalysisResult | null;
  onRunAnalysis: () => void;
  isAnalyzing: boolean;
}

export default function AnalysisModule({
  measurements, htsRecords, analysisResult, onRunAnalysis, isAnalyzing
}: AnalysisModuleProps) {
  const [selectedCandidate, setSelectedCandidate] = useState<AnalysisCandidate | null>(null);
  const [filterMin, setFilterMin] = useState(0);

  const filtered = analysisResult?.candidates.filter(c => c.probability >= filterMin) || [];

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Daraltılmış Baz Analizi</h1>
          <p className="text-gray-500 text-sm mt-1">
            HTS kayıtları ile ölçüm verilerini eşleştirerek baz istasyonu adaylarını belirler
          </p>
        </div>
        <button
          onClick={onRunAnalysis}
          disabled={isAnalyzing || (measurements.length === 0 && htsRecords.length === 0)}
          className="px-5 py-2.5 bg-blue-600 hover:bg-blue-500 disabled:opacity-40 text-white rounded-lg text-sm font-medium transition-colors flex items-center gap-2"
        >
          {isAnalyzing ? (
            <>
              <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              Analiz Ediliyor...
            </>
          ) : '▶ Analizi Yenile'}
        </button>
      </div>

      {/* No data */}
      {measurements.length === 0 && htsRecords.length === 0 && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <div className="text-5xl mb-4 opacity-20">◎</div>
          <h2 className="text-lg font-semibold text-gray-400 mb-2">Analiz için veri gerekli</h2>
          <p className="text-gray-600 text-sm max-w-md">
            Ölçüm verisi ve HTS kayıtlarını yükledikten sonra analizi başlatın.
          </p>
        </div>
      )}

      {/* Analysis Summary */}
      {analysisResult && (
        <>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <SummaryCard label="Toplam Ölçüm" value={analysisResult.totalMeasurements.toLocaleString()} color="blue" />
            <SummaryCard label="HTS Kaydı" value={analysisResult.totalHTS.toLocaleString()} color="purple" />
            <SummaryCard label="Eşleşen Çift" value={analysisResult.matchedPairs.toLocaleString()} color="green" />
            <SummaryCard label="Aday Baz İst." value={analysisResult.candidates.length.toLocaleString()} color="yellow" />
          </div>

          {/* Algorithm Info */}
          <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
            <h3 className="text-sm font-semibold text-gray-300 mb-3">Analiz Algoritması</h3>
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
              {[
                { label: 'Cell ID Eşleşmesi', weight: '40%', color: '#3b82f6', desc: 'Tam eşleşme' },
                { label: 'TAC/LAC Eşleşmesi', weight: '25%', color: '#8b5cf6', desc: 'Alan kodu' },
                { label: 'Sinyal Kalitesi', weight: '20%', color: '#10b981', desc: 'RSRP bazlı' },
                { label: 'Coğrafi Yakınlık', weight: '15%', color: '#f59e0b', desc: 'Mesafe analizi' },
              ].map(item => (
                <div key={item.label} className="bg-dark-900 rounded-lg p-3 border border-gray-800">
                  <div className="flex items-center justify-between mb-2">
                    <span className="text-xs text-gray-400">{item.label}</span>
                    <span className="text-xs font-bold" style={{ color: item.color }}>{item.weight}</span>
                  </div>
                  <div className="h-1.5 bg-gray-800 rounded-full overflow-hidden">
                    <div className="h-full rounded-full" style={{ width: item.weight, background: item.color }}></div>
                  </div>
                  <div className="text-xs text-gray-600 mt-1">{item.desc}</div>
                </div>
              ))}
            </div>
          </div>

          {/* Filter */}
          <div className="flex items-center gap-4">
            <span className="text-sm text-gray-400">Min. Olasılık:</span>
            {[0, 60, 80, 95].map(v => (
              <button
                key={v}
                onClick={() => setFilterMin(v)}
                className={`px-3 py-1 rounded-full text-xs border transition-all ${
                  filterMin === v
                    ? 'bg-blue-600/20 border-blue-500/40 text-blue-300'
                    : 'border-gray-700 text-gray-500 hover:border-gray-600'
                }`}
              >
                {v === 0 ? 'Tümü' : `≥%${v}`}
              </button>
            ))}
            <span className="ml-auto text-xs text-gray-600">{filtered.length} aday</span>
          </div>

          {/* Candidates List */}
          <div className="space-y-3">
            {filtered.map((candidate, i) => (
              <CandidateCard
                key={candidate.baseStation.id}
                candidate={candidate}
                rank={i + 1}
                isSelected={selectedCandidate?.baseStation.id === candidate.baseStation.id}
                onClick={() => setSelectedCandidate(
                  selectedCandidate?.baseStation.id === candidate.baseStation.id ? null : candidate
                )}
              />
            ))}
            {filtered.length === 0 && (
              <div className="text-center py-10 text-gray-600 text-sm">
                Bu filtre için aday bulunamadı
              </div>
            )}
          </div>
        </>
      )}

      {/* Only measurements loaded, no analysis yet */}
      {measurements.length > 0 && !analysisResult && !isAnalyzing && (
        <div className="flex flex-col items-center justify-center py-16 text-center">
          <div className="text-5xl mb-4 opacity-20">◎</div>
          <h2 className="text-lg font-semibold text-gray-400 mb-2">Analiz Başlatılmadı</h2>
          <p className="text-gray-600 text-sm max-w-md mb-6">
            {measurements.length} ölçüm ve {htsRecords.length} HTS kaydı yüklendi. Analizi başlatmak için butona tıklayın.
          </p>
          <button
            onClick={onRunAnalysis}
            className="px-6 py-3 bg-blue-600 hover:bg-blue-500 text-white rounded-lg font-medium transition-colors"
          >
            ▶ Analizi Başlat
          </button>
        </div>
      )}
    </div>
  );
}

function SummaryCard({ label, value, color }: { label: string; value: string; color: string }) {
  const colorMap: Record<string, string> = {
    blue: 'border-blue-800/30 text-blue-400',
    purple: 'border-purple-800/30 text-purple-400',
    green: 'border-green-800/30 text-green-400',
    yellow: 'border-yellow-800/30 text-yellow-400',
  };
  return (
    <div className={`bg-dark-800 border rounded-xl p-4 ${colorMap[color]}`}>
      <div className="text-xs text-gray-500 mb-1">{label}</div>
      <div className="text-2xl font-bold text-white">{value}</div>
    </div>
  );
}

function CandidateCard({ candidate, rank, isSelected, onClick }: {
  candidate: AnalysisCandidate;
  rank: number;
  isSelected: boolean;
  onClick: () => void;
}) {
  const bs = candidate.baseStation;
  const prob = candidate.probability;
  const probColor = prob >= 80 ? '#22c55e' : prob >= 60 ? '#eab308' : '#f97316';

  return (
    <div
      className={`bg-dark-800 rounded-xl border transition-all cursor-pointer ${
        isSelected ? 'border-blue-500/40 shadow-lg shadow-blue-900/20' : 'border-gray-800 hover:border-gray-700'
      }`}
      onClick={onClick}
    >
      <div className="p-4">
        <div className="flex items-start gap-4">
          {/* Rank */}
          <div className="w-10 h-10 rounded-xl flex items-center justify-center font-bold text-lg flex-shrink-0"
            style={{ background: probColor + '22', border: `1px solid ${probColor}44`, color: probColor }}>
            {rank}
          </div>

          {/* Info */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-3 flex-wrap">
              <span className="text-base font-semibold text-white">Cell ID: {bs.cellId}</span>
              <ProbabilityBadge probability={prob} />
              <span className="text-xs px-2 py-0.5 rounded bg-blue-900/30 text-blue-400">{bs.technology}</span>
            </div>
            <div className="flex gap-4 mt-1 text-xs text-gray-500 flex-wrap">
              <span>eNodeB: {bs.eNodeBId || 'N/A'}</span>
              <span>TAC: {bs.tac || 'N/A'}</span>
              <span>PCI: {bs.pci || 'N/A'}</span>
              <span>{candidate.matchedMeasurements.length} ölçüm</span>
              <span>{candidate.matchedHTS.length} HTS eşleşme</span>
            </div>
          </div>

          {/* Signal */}
          <div className="text-right flex-shrink-0">
            {candidate.avgRSRP !== undefined && (
              <div className="text-sm font-mono font-semibold" style={{ color: rsrpColor(candidate.avgRSRP) }}>
                {candidate.avgRSRP.toFixed(1)} dBm
              </div>
            )}
            {candidate.avgRSRP !== undefined && (
              <div className="text-xs text-gray-600">{rsrpLabel(candidate.avgRSRP)}</div>
            )}
          </div>

          <div className="text-gray-600 text-sm">{isSelected ? '▲' : '▼'}</div>
        </div>

        {/* Probability bar */}
        <div className="mt-3 h-1.5 bg-gray-800 rounded-full overflow-hidden">
          <div
            className="h-full rounded-full transition-all duration-500"
            style={{ width: `${prob}%`, background: probColor }}
          ></div>
        </div>
      </div>

      {/* Expanded detail */}
      {isSelected && (
        <div className="border-t border-gray-800 p-4 space-y-4">
          {/* Match Reasons */}
          <div>
            <h4 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3">Eşleşme Kriterleri</h4>
            <div className="space-y-2">
              {candidate.matchReasons.map((reason, i) => (
                <div key={i} className="flex items-center gap-3">
                  <div className={`w-5 h-5 rounded-full flex items-center justify-center text-xs flex-shrink-0 ${
                    reason.matched ? 'bg-green-900/30 text-green-400' : 'bg-gray-800 text-gray-600'
                  }`}>
                    {reason.matched ? '✓' : '✕'}
                  </div>
                  <div className="flex-1">
                    <div className="text-xs text-gray-300">{reason.description}</div>
                  </div>
                  <div className="text-xs text-gray-600">Ağırlık: {reason.weight}%</div>
                  <div className={`text-xs font-semibold ${reason.matched ? 'text-green-400' : 'text-gray-600'}`}>
                    {reason.matched ? `+${reason.weight}` : '0'}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Signal Stats */}
          <div className="grid grid-cols-3 gap-3">
            <SignalStat label="Ort. RSRP" value={candidate.avgRSRP?.toFixed(1)} unit="dBm" color={candidate.avgRSRP ? rsrpColor(candidate.avgRSRP) : '#6b7280'} />
            <SignalStat label="Ort. RSRQ" value={candidate.avgRSRQ?.toFixed(1)} unit="dB" color="#8b5cf6" />
            <SignalStat label="Ort. SINR" value={candidate.avgSINR?.toFixed(1)} unit="dB" color="#06b6d4" />
          </div>

          {/* Matched HTS */}
          {candidate.matchedHTS.length > 0 && (
            <div>
              <h4 className="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-2">Eşleşen HTS Kayıtları</h4>
              <div className="space-y-1">
                {candidate.matchedHTS.slice(0, 5).map(h => (
                  <div key={h.id} className="flex items-center gap-3 text-xs text-gray-500 bg-dark-900 rounded px-3 py-1.5">
                    <span className="text-purple-400">●</span>
                    <span>Cell ID: {h.cellId}</span>
                    <span>TAC: {h.tac || h.lac || 'N/A'}</span>
                    <span>{h.callTime}</span>
                    {h.duration && <span>{h.duration}s</span>}
                  </div>
                ))}
                {candidate.matchedHTS.length > 5 && (
                  <div className="text-xs text-gray-600 text-center">+{candidate.matchedHTS.length - 5} daha</div>
                )}
              </div>
            </div>
          )}

          {/* Location */}
          {bs.latitude && bs.longitude && (
            <div className="flex items-center gap-4 text-xs text-gray-500">
              <span>Tahmini Konum:</span>
              <span className="font-mono text-gray-400">{bs.latitude.toFixed(5)}, {bs.longitude.toFixed(5)}</span>
              {bs.azimuth !== undefined && <span>Azimut: {bs.azimuth.toFixed(0)}°</span>}
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function SignalStat({ label, value, unit, color }: { label: string; value?: string; unit: string; color: string }) {
  return (
    <div className="bg-dark-900 rounded-lg p-3 border border-gray-800">
      <div className="text-xs text-gray-500 mb-1">{label}</div>
      <div className="text-sm font-mono font-semibold" style={{ color }}>
        {value ? `${value} ${unit}` : 'N/A'}
      </div>
    </div>
  );
}
