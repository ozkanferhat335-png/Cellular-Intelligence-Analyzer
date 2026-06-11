import React, { useEffect, useRef, useState, useMemo } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { MeasurementRecord, MapLayerConfig } from '../types';
import { generateHeatmapData, rsrpColor } from '../utils/analysisEngine';

interface HeatmapModuleProps {
  measurements: MeasurementRecord[];
  layers: MapLayerConfig;
  onLayersChange: (layers: MapLayerConfig) => void;
}

export default function HeatmapModule({ measurements, layers, onLayersChange }: HeatmapModuleProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const circleLayerRef = useRef<L.LayerGroup | null>(null);
  const [metric, setMetric] = useState<'rsrp' | 'rsrq' | 'sinr'>('rsrp');
  const [opacity, setOpacity] = useState(0.75);

  const heatData = useMemo(() => generateHeatmapData(measurements, metric), [measurements, metric]);

  // Initialize map
  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;

    const map = L.map(mapRef.current, {
      center: [41.015, 28.979],
      zoom: 13,
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 19,
    }).addTo(map);

    mapInstanceRef.current = map;
    circleLayerRef.current = L.layerGroup().addTo(map);

    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, []);

  // Render heatmap as colored circles
  useEffect(() => {
    const group = circleLayerRef.current;
    if (!group) return;
    group.clearLayers();

    if (heatData.length === 0) return;

    heatData.forEach(point => {
      let color: string;
      let value: number | undefined;

      if (metric === 'rsrp') {
        value = point.rsrp;
        color = value !== undefined ? rsrpColor(value) : '#6b7280';
      } else if (metric === 'rsrq') {
        value = point.rsrq;
        const v = value || -20;
        color = v >= -5 ? '#22c55e' : v >= -10 ? '#84cc16' : v >= -15 ? '#eab308' : '#ef4444';
      } else {
        value = point.sinr;
        const v = value || -20;
        color = v >= 20 ? '#22c55e' : v >= 10 ? '#84cc16' : v >= 0 ? '#eab308' : v >= -5 ? '#f97316' : '#ef4444';
      }

      const circle = L.circleMarker([point.lat, point.lng], {
        radius: 8,
        fillColor: color,
        color: 'transparent',
        fillOpacity: opacity * point.intensity * 0.8 + 0.2,
      });

      circle.bindTooltip(
        `${metric.toUpperCase()}: ${value?.toFixed(1) || 'N/A'} ${metric === 'rsrp' ? 'dBm' : 'dB'}`,
        { permanent: false, direction: 'top' }
      );

      group.addLayer(circle);
    });

    // Fit bounds
    if (heatData.length > 0 && mapInstanceRef.current) {
      const lats = heatData.map(p => p.lat);
      const lngs = heatData.map(p => p.lng);
      mapInstanceRef.current.fitBounds([
        [Math.min(...lats), Math.min(...lngs)],
        [Math.max(...lats), Math.max(...lngs)],
      ], { padding: [30, 30] });
    }
  }, [heatData, metric, opacity]);

  const metricStats = useMemo(() => {
    if (measurements.length === 0) return null;
    const vals = measurements.filter(m => m[metric] !== undefined).map(m => m[metric]!);
    if (vals.length === 0) return null;
    return {
      min: Math.min(...vals).toFixed(1),
      max: Math.max(...vals).toFixed(1),
      avg: (vals.reduce((a, b) => a + b, 0) / vals.length).toFixed(1),
      count: vals.length,
    };
  }, [measurements, metric]);

  const metricConfig = {
    rsrp: {
      label: 'RSRP (dBm)',
      scale: [
        { color: '#22c55e', label: '≥ -80 (Mükemmel)' },
        { color: '#84cc16', label: '-90 ~ -80 (İyi)' },
        { color: '#eab308', label: '-100 ~ -90 (Orta)' },
        { color: '#f97316', label: '-110 ~ -100 (Zayıf)' },
        { color: '#ef4444', label: '< -110 (Çok Zayıf)' },
      ],
    },
    rsrq: {
      label: 'RSRQ (dB)',
      scale: [
        { color: '#22c55e', label: '≥ -5 (Mükemmel)' },
        { color: '#84cc16', label: '-10 ~ -5 (İyi)' },
        { color: '#eab308', label: '-15 ~ -10 (Orta)' },
        { color: '#ef4444', label: '< -15 (Zayıf)' },
      ],
    },
    sinr: {
      label: 'SINR (dB)',
      scale: [
        { color: '#22c55e', label: '≥ 20 (Mükemmel)' },
        { color: '#84cc16', label: '10 ~ 20 (İyi)' },
        { color: '#eab308', label: '0 ~ 10 (Orta)' },
        { color: '#f97316', label: '-5 ~ 0 (Zayıf)' },
        { color: '#ef4444', label: '< -5 (Çok Zayıf)' },
      ],
    },
  };

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      {/* Toolbar */}
      <div className="bg-dark-800 border-b border-gray-800 px-4 py-2 flex items-center gap-6 flex-wrap">
        <div className="flex items-center gap-2">
          <span className="text-xs text-gray-500">Metrik:</span>
          {(['rsrp', 'rsrq', 'sinr'] as const).map(m => (
            <button
              key={m}
              onClick={() => setMetric(m)}
              className={`px-3 py-1 rounded-full text-xs border transition-all ${
                metric === m
                  ? 'bg-blue-600/20 border-blue-500/40 text-blue-300'
                  : 'border-gray-700 text-gray-500 hover:border-gray-600'
              }`}
            >
              {m.toUpperCase()}
            </button>
          ))}
        </div>

        <div className="flex items-center gap-2">
          <span className="text-xs text-gray-500">Opaklık:</span>
          <input
            type="range"
            min="0.2"
            max="1"
            step="0.1"
            value={opacity}
            onChange={e => setOpacity(parseFloat(e.target.value))}
            className="w-24 accent-blue-500"
          />
          <span className="text-xs text-gray-400 font-mono">{Math.round(opacity * 100)}%</span>
        </div>

        {metricStats && (
          <div className="flex items-center gap-4 ml-auto text-xs text-gray-500 font-mono">
            <span>Min: <span className="text-gray-300">{metricStats.min}</span></span>
            <span>Max: <span className="text-gray-300">{metricStats.max}</span></span>
            <span>Ort: <span className="text-gray-300">{metricStats.avg}</span></span>
            <span>N: <span className="text-gray-300">{metricStats.count}</span></span>
          </div>
        )}
      </div>

      {/* Map */}
      <div className="flex-1 relative">
        <div ref={mapRef} className="w-full h-full" />

        {/* Legend */}
        <div className="absolute bottom-4 right-4 bg-dark-800/90 backdrop-blur border border-gray-700 rounded-xl p-3 text-xs space-y-1.5 z-[1000]">
          <div className="text-gray-400 font-semibold mb-2">{metricConfig[metric].label}</div>
          {metricConfig[metric].scale.map(({ color, label }) => (
            <div key={label} className="flex items-center gap-2">
              <span className="w-3 h-3 rounded-full flex-shrink-0" style={{ background: color }}></span>
              <span className="text-gray-400">{label}</span>
            </div>
          ))}
        </div>

        {/* Stats overlay */}
        {metricStats && (
          <div className="absolute top-4 left-4 bg-dark-800/90 backdrop-blur border border-gray-700 rounded-xl p-3 text-xs z-[1000]">
            <div className="text-gray-400 font-semibold mb-2">{metricConfig[metric].label} İstatistikleri</div>
            <div className="space-y-1 font-mono">
              <div className="flex justify-between gap-6">
                <span className="text-gray-500">Minimum</span>
                <span className="text-gray-200">{metricStats.min}</span>
              </div>
              <div className="flex justify-between gap-6">
                <span className="text-gray-500">Maksimum</span>
                <span className="text-gray-200">{metricStats.max}</span>
              </div>
              <div className="flex justify-between gap-6">
                <span className="text-gray-500">Ortalama</span>
                <span className="text-gray-200">{metricStats.avg}</span>
              </div>
              <div className="flex justify-between gap-6">
                <span className="text-gray-500">Nokta Sayısı</span>
                <span className="text-gray-200">{metricStats.count}</span>
              </div>
            </div>
          </div>
        )}

        {/* No data */}
        {measurements.length === 0 && (
          <div className="absolute inset-0 flex items-center justify-center bg-dark-900/60 z-[999]">
            <div className="text-center">
              <div className="text-4xl mb-3 opacity-30">▣</div>
              <div className="text-gray-400 text-sm">Isı haritası için veri yok</div>
              <div className="text-gray-600 text-xs mt-1">Önce ölçüm verisi yükleyin</div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
