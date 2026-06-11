import React, { useMemo } from 'react';
import { MeasurementRecord, HTSRecord, AnalysisResult, AppTab } from '../types';
import { computeMeasurementStats, rsrpColor, rsrpLabel } from '../utils/analysisEngine';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, LineChart, Line, Legend
} from 'recharts';

interface DashboardProps {
  measurements: MeasurementRecord[];
  htsRecords: HTSRecord[];
  analysisResult: AnalysisResult | null;
  onNavigate: (tab: AppTab) => void;
  onRunAnalysis: () => void;
  isAnalyzing: boolean;
}

const COLORS = ['#3b82f6', '#8b5cf6', '#06b6d4', '#10b981', '#f59e0b', '#ef4444'];

export default function Dashboard({
  measurements, htsRecords, analysisResult, onNavigate, onRunAnalysis, isAnalyzing
}: DashboardProps) {
  const stats = useMemo(() =>
    measurements.length > 0 ? computeMeasurementStats(measurements) : null,
    [measurements]
  );

  const techData = useMemo(() => {
    if (!stats) return [];
    return Object.entries(stats.technologies).map(([name, value]) => ({ name, value }));
  }, [stats]);

  const rsrpDistribution = useMemo(() => {
    if (measurements.length === 0) return [];
    const bins = [
      { range: '≥-80', label: 'Mükemmel', count: 0, color: '#22c55e' },
      { range: '-90~-80', label: 'İyi', count: 0, color: '#84cc16' },
      { range: '-100~-90', label: 'Orta', count: 0, color: '#eab308' },
      { range: '-110~-100', label: 'Zayıf', count: 0, color: '#f97316' },
      { range: '<-110', label: 'Çok Zayıf', count: 0, color: '#ef4444' },
    ];
    measurements.forEach(m => {
      if (m.rsrp === undefined) return;
      if (m.rsrp >= -80) bins[0].count++;
      else if (m.rsrp >= -90) bins[1].count++;
      else if (m.rsrp >= -100) bins[2].count++;
      else if (m.rsrp >= -110) bins[3].count++;
      else bins[4].count++;
    });
    return bins;
  }, [measurements]);

  const timelineData = useMemo(() => {
    if (measurements.length === 0) return [];
    const sorted = [...measurements].sort((a, b) =>
      new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    );
    const step = Math.max(1, Math.floor(sorted.length / 30));
    return sorted.filter((_, i) => i % step === 0).map(m => ({
      time: new Date(m.timestamp).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }),
      rsrp: m.rsrp,
      sinr: m.sinr,
    }));
  }, [measurements]);

  const isEmpty = measurements.length === 0 && htsRecords.length === 0;

  return (
    <div className="flex-1 overflow-y-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Dashboard</h1>
          <p className="text-gray-500 text-sm mt-1">Saha Ölçümüne Dayalı Daraltılmış Baz Analiz Modülü</p>
        </div>
        <div className="flex gap-3">
          <button
            onClick={() => onNavigate('upload')}
            className="px-4 py-2 bg-gray-800 hover:bg-gray-700 text-gray-300 rounded-lg text-sm border border-gray-700 transition-colors"
          >
            Veri Yükle
          </button>
          <button
            onClick={onRunAnalysis}
            disabled={isEmpty || isAnalyzing}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-500 disabled:opacity-40 disabled:cursor-not-allowed text-white rounded-lg text-sm font-medium transition-colors flex items-center gap-2"
          >
            {isAnalyzing ? (
              <>
                <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
                Analiz Ediliyor...
              </>
            ) : '▶ Analizi Başlat'}
          </button>
        </div>
      </div>

      {/* Empty state */}
      {isEmpty && (
        <div className="flex flex-col items-center justify-center py-20 text-center">
          <div className="w-20 h-20 rounded-2xl bg-blue-900/20 border border-blue-800/30 flex items-center justify-center text-4xl mb-4">
            ◉
          </div>
          <h2 className="text-xl font-semibold text-gray-300 mb-2">Veri Yüklenmedi</h2>
          <p className="text-gray-600 max-w-md mb-6">
            Drive test ölçüm verilerinizi (NetMonster, CellMapper, G-NetTrack) ve HTS kayıtlarınızı yükleyerek analizi başlatın.
          </p>
          <button
            onClick={() => onNavigate('upload')}
            className="px-6 py-3 bg-blue-600 hover:bg-blue-500 text-white rounded-lg font-medium transition-colors"
          >
            Veri Yüklemeye Başla
          </button>
        </div>
      )}

      {/* KPI Cards */}
      {!isEmpty && (
        <>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <KPICard
              label="Ölçüm Noktası"
              value={measurements.length.toLocaleString()}
              sub={stats ? `${stats.cellIds.length} benzersiz hücre` : ''}
              color="blue"
              icon="◉"
            />
            <KPICard
              label="HTS Kaydı"
              value={htsRecords.length.toLocaleString()}
              sub={analysisResult ? `${analysisResult.matchedPairs} eşleşme` : 'Analiz bekleniyor'}
              color="purple"
              icon="▣"
            />
            <KPICard
              label="Ort. RSRP"
              value={stats?.rsrp.avg ? `${stats.rsrp.avg} dBm` : 'N/A'}
              sub={stats?.rsrp.avg ? rsrpLabel(stats.rsrp.avg) : ''}
              color={stats?.rsrp.avg ? (stats.rsrp.avg >= -90 ? 'green' : stats.rsrp.avg >= -100 ? 'yellow' : 'red') : 'gray'}
              icon="◎"
            />
            <KPICard
              label="Ort. SINR"
              value={stats?.sinr.avg ? `${stats.sinr.avg} dB` : 'N/A'}
              sub={stats?.sinr.avg ? (stats.sinr.avg >= 10 ? 'İyi' : stats.sinr.avg >= 0 ? 'Orta' : 'Zayıf') : ''}
              color={stats?.sinr.avg ? (stats.sinr.avg >= 10 ? 'green' : stats.sinr.avg >= 0 ? 'yellow' : 'red') : 'gray'}
              icon="◈"
            />
          </div>

          {/* Charts Row */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
            {/* RSRP Distribution */}
            <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
              <h3 className="text-sm font-semibold text-gray-300 mb-4">RSRP Dağılımı</h3>
              <ResponsiveContainer width="100%" height={180}>
                <BarChart data={rsrpDistribution} barSize={28}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" />
                  <XAxis dataKey="label" tick={{ fill: '#6b7280', fontSize: 10 }} />
                  <YAxis tick={{ fill: '#6b7280', fontSize: 10 }} />
                  <Tooltip
                    contentStyle={{ background: '#1f2937', border: '1px solid #374151', borderRadius: 8 }}
                    labelStyle={{ color: '#f9fafb' }}
                  />
                  <Bar dataKey="count" radius={[4, 4, 0, 0]}>
                    {rsrpDistribution.map((entry, index) => (
                      <Cell key={index} fill={entry.color} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>

            {/* Technology Pie */}
            <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
              <h3 className="text-sm font-semibold text-gray-300 mb-4">Teknoloji Dağılımı</h3>
              {techData.length > 0 ? (
                <ResponsiveContainer width="100%" height={180}>
                  <PieChart>
                    <Pie data={techData} cx="50%" cy="50%" innerRadius={45} outerRadius={70} dataKey="value" label={(props: any) => `${props.name} ${((props.percent || 0) * 100).toFixed(0)}%`} labelLine={false}>
                      {techData.map((_, index) => (
                        <Cell key={index} fill={COLORS[index % COLORS.length]} />
                      ))}
                    </Pie>
                    <Tooltip contentStyle={{ background: '#1f2937', border: '1px solid #374151', borderRadius: 8 }} />
                  </PieChart>
                </ResponsiveContainer>
              ) : (
                <div className="h-44 flex items-center justify-center text-gray-600 text-sm">Veri yok</div>
              )}
            </div>

            {/* Signal Timeline */}
            <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
              <h3 className="text-sm font-semibold text-gray-300 mb-4">Sinyal Zaman Serisi</h3>
              <ResponsiveContainer width="100%" height={180}>
                <LineChart data={timelineData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#1f2937" />
                  <XAxis dataKey="time" tick={{ fill: '#6b7280', fontSize: 9 }} interval="preserveStartEnd" />
                  <YAxis tick={{ fill: '#6b7280', fontSize: 10 }} />
                  <Tooltip contentStyle={{ background: '#1f2937', border: '1px solid #374151', borderRadius: 8 }} />
                  <Legend wrapperStyle={{ fontSize: 11 }} />
                  <Line type="monotone" dataKey="rsrp" stroke="#3b82f6" dot={false} strokeWidth={1.5} name="RSRP (dBm)" />
                  <Line type="monotone" dataKey="sinr" stroke="#10b981" dot={false} strokeWidth={1.5} name="SINR (dB)" />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Analysis Result Preview */}
          {analysisResult && (
            <div className="bg-dark-800 rounded-xl border border-gray-800 p-4">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-sm font-semibold text-gray-300">Baz İstasyonu Adayları</h3>
                <button
                  onClick={() => onNavigate('analysis')}
                  className="text-xs text-blue-400 hover:text-blue-300"
                >
                  Tümünü Gör →
                </button>
              </div>
              <div className="space-y-2">
                {analysisResult.candidates.slice(0, 5).map((c, i) => (
                  <div key={i} className="flex items-center gap-4 p-3 bg-dark-700 rounded-lg border border-gray-800">
                    <div className="w-8 h-8 rounded-lg bg-blue-900/30 border border-blue-800/30 flex items-center justify-center text-blue-400 font-bold text-sm">
                      {i + 1}
                    </div>
                    <div className="flex-1">
                      <div className="text-sm font-medium text-gray-200">Cell ID: {c.baseStation.cellId}</div>
                      <div className="text-xs text-gray-500">
                        TAC: {c.baseStation.tac || 'N/A'} · PCI: {c.baseStation.pci || 'N/A'} · {c.baseStation.technology}
                      </div>
                    </div>
                    <div className="text-right">
                      <ProbabilityBadge probability={c.probability} />
                      <div className="text-xs text-gray-600 mt-1">{c.matchedMeasurements.length} ölçüm</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Quick Stats */}
          {stats && (
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
              <StatCard label="Min RSRP" value={`${stats.rsrp.min.toFixed(1)} dBm`} />
              <StatCard label="Max RSRP" value={`${stats.rsrp.max.toFixed(1)} dBm`} />
              <StatCard label="Ort. RSRQ" value={`${stats.rsrq.avg.toFixed(1)} dB`} />
              <StatCard label="Benzersiz Hücre" value={stats.cellIds.length.toString()} />
            </div>
          )}
        </>
      )}
    </div>
  );
}

function KPICard({ label, value, sub, color, icon }: {
  label: string; value: string; sub: string; color: string; icon: string;
}) {
  const colorMap: Record<string, string> = {
    blue: 'from-blue-600/20 to-blue-800/10 border-blue-800/30 text-blue-400',
    purple: 'from-purple-600/20 to-purple-800/10 border-purple-800/30 text-purple-400',
    green: 'from-green-600/20 to-green-800/10 border-green-800/30 text-green-400',
    yellow: 'from-yellow-600/20 to-yellow-800/10 border-yellow-800/30 text-yellow-400',
    red: 'from-red-600/20 to-red-800/10 border-red-800/30 text-red-400',
    gray: 'from-gray-700/20 to-gray-800/10 border-gray-700/30 text-gray-500',
  };
  return (
    <div className={`bg-gradient-to-br ${colorMap[color] || colorMap.gray} border rounded-xl p-4`}>
      <div className="flex items-start justify-between">
        <div>
          <div className="text-xs text-gray-500 mb-1">{label}</div>
          <div className="text-2xl font-bold text-white">{value}</div>
          <div className="text-xs text-gray-500 mt-1">{sub}</div>
        </div>
        <span className="text-2xl opacity-40">{icon}</span>
      </div>
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-dark-800 border border-gray-800 rounded-xl p-3">
      <div className="text-xs text-gray-500 mb-1">{label}</div>
      <div className="text-lg font-semibold text-gray-200 font-mono">{value}</div>
    </div>
  );
}

export function ProbabilityBadge({ probability }: { probability: number }) {
  const cls = probability >= 80 ? 'prob-high' : probability >= 60 ? 'prob-medium' : 'prob-low';
  return (
    <span className={`text-xs font-bold px-2 py-1 rounded-full ${cls}`}>
      %{probability}
    </span>
  );
}
