import React, { useEffect, useRef, useState, useMemo } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { MeasurementRecord, HTSRecord, AnalysisResult, MapLayerConfig } from '../types';
import {
  extractBaseStations, generateHeatmapData, analyzeRoute,
  sectorFanGeoJSON, rsrpColor
} from '../utils/analysisEngine';

// Fix Leaflet default icon
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
});

interface MapModuleProps {
  measurements: MeasurementRecord[];
  htsRecords: HTSRecord[];
  analysisResult: AnalysisResult | null;
  layers: MapLayerConfig;
  onLayersChange: (layers: MapLayerConfig) => void;
}

export default function MapModule({ measurements, htsRecords, analysisResult, layers, onLayersChange }: MapModuleProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const layerGroupsRef = useRef<Record<string, L.LayerGroup>>({});
  const [selectedPoint, setSelectedPoint] = useState<MeasurementRecord | null>(null);
  const [mapStats, setMapStats] = useState({ visible: 0, total: 0 });

  const baseStations = useMemo(() => extractBaseStations(measurements), [measurements]);
  const routePoints = useMemo(() => analyzeRoute(measurements), [measurements]);

  // Initialize map
  useEffect(() => {
    if (!mapRef.current || mapInstanceRef.current) return;

    const map = L.map(mapRef.current, {
      center: [41.015, 28.979],
      zoom: 13,
      zoomControl: true,
    });

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors',
      maxZoom: 19,
    }).addTo(map);

    // Dark tile layer option
    // L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png').addTo(map);

    mapInstanceRef.current = map;

    // Create layer groups
    const groups = ['measurements', 'hts', 'baseStations', 'sectors', 'route', 'heatmap'];
    groups.forEach(g => {
      layerGroupsRef.current[g] = L.layerGroup().addTo(map);
    });

    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, []);

  // Update measurement layer
  useEffect(() => {
    const group = layerGroupsRef.current['measurements'];
    if (!group) return;
    group.clearLayers();
    if (!layers.showMeasurements) return;

    measurements.forEach(m => {
      const color = m.rsrp ? rsrpColor(m.rsrp) : '#6b7280';
      const circle = L.circleMarker([m.latitude, m.longitude], {
        radius: 5,
        fillColor: color,
        color: 'transparent',
        fillOpacity: 0.75,
      });

      circle.bindPopup(`
        <div style="font-family: monospace; font-size: 12px; min-width: 200px;">
          <div style="font-weight: bold; color: #60a5fa; margin-bottom: 6px;">Ölçüm Noktası</div>
          <div><b>Cell ID:</b> ${m.cellId}</div>
          <div><b>TAC:</b> ${m.tac || 'N/A'}</div>
          <div><b>PCI:</b> ${m.pci || 'N/A'}</div>
          <div><b>RSRP:</b> <span style="color:${color}">${m.rsrp?.toFixed(1) || 'N/A'} dBm</span></div>
          <div><b>RSRQ:</b> ${m.rsrq?.toFixed(1) || 'N/A'} dB</div>
          <div><b>SINR:</b> ${m.sinr?.toFixed(1) || 'N/A'} dB</div>
          <div><b>Tech:</b> ${m.technology}</div>
          <div><b>Zaman:</b> ${new Date(m.timestamp).toLocaleString('tr-TR')}</div>
        </div>
      `);

      circle.on('click', () => setSelectedPoint(m));
      group.addLayer(circle);
    });

    setMapStats({ visible: measurements.length, total: measurements.length });

    // Fit bounds
    if (measurements.length > 0 && mapInstanceRef.current) {
      const lats = measurements.map(m => m.latitude);
      const lngs = measurements.map(m => m.longitude);
      mapInstanceRef.current.fitBounds([
        [Math.min(...lats), Math.min(...lngs)],
        [Math.max(...lats), Math.max(...lngs)],
      ], { padding: [30, 30] });
    }
  }, [measurements, layers.showMeasurements]);

  // Update HTS layer
  useEffect(() => {
    const group = layerGroupsRef.current['hts'];
    if (!group) return;
    group.clearLayers();
    if (!layers.showHTS) return;

    htsRecords.forEach(h => {
      // HTS records don't always have coordinates; show as info markers if no location
      const marker = L.circleMarker([41.015 + Math.random() * 0.01 - 0.005, 28.979 + Math.random() * 0.01 - 0.005], {
        radius: 6,
        fillColor: '#8b5cf6',
        color: '#7c3aed',
        weight: 1,
        fillOpacity: 0.8,
      });
      marker.bindPopup(`
        <div style="font-family: monospace; font-size: 12px;">
          <div style="font-weight: bold; color: #a78bfa; margin-bottom: 6px;">HTS Kaydı</div>
          <div><b>Cell ID:</b> ${h.cellId}</div>
          <div><b>TAC:</b> ${h.tac || h.lac || 'N/A'}</div>
          <div><b>Süre:</b> ${h.duration ? h.duration + 's' : 'N/A'}</div>
          <div><b>Zaman:</b> ${h.callTime}</div>
        </div>
      `);
      group.addLayer(marker);
    });
  }, [htsRecords, layers.showHTS]);

  // Update base station layer
  useEffect(() => {
    const group = layerGroupsRef.current['baseStations'];
    if (!group) return;
    group.clearLayers();
    if (!layers.showBaseStations) return;

    baseStations.forEach(bs => {
      if (!bs.latitude || !bs.longitude) return;

      const candidate = analysisResult?.candidates.find(c => c.baseStation.cellId === bs.cellId);
      const prob = candidate?.probability || 0;
      const color = prob >= 80 ? '#22c55e' : prob >= 60 ? '#eab308' : '#6b7280';

      const icon = L.divIcon({
        html: `
          <div style="
            width: 32px; height: 32px;
            background: ${color}22;
            border: 2px solid ${color};
            border-radius: 50%;
            display: flex; align-items: center; justify-content: center;
            font-size: 14px; color: ${color};
            box-shadow: 0 0 12px ${color}44;
          ">▲</div>
        `,
        className: '',
        iconSize: [32, 32],
        iconAnchor: [16, 16],
      });

      const marker = L.marker([bs.latitude, bs.longitude], { icon });
      marker.bindPopup(`
        <div style="font-family: monospace; font-size: 12px; min-width: 220px;">
          <div style="font-weight: bold; color: ${color}; margin-bottom: 6px;">Baz İstasyonu</div>
          <div><b>Cell ID:</b> ${bs.cellId}</div>
          <div><b>eNodeB ID:</b> ${bs.eNodeBId || 'N/A'}</div>
          <div><b>TAC:</b> ${bs.tac || 'N/A'}</div>
          <div><b>PCI:</b> ${bs.pci || 'N/A'}</div>
          <div><b>Teknoloji:</b> ${bs.technology}</div>
          ${candidate ? `<div style="margin-top:6px; color:${color}"><b>Olasılık: %${candidate.probability}</b></div>` : ''}
          ${candidate?.avgRSRP ? `<div><b>Ort. RSRP:</b> ${candidate.avgRSRP.toFixed(1)} dBm</div>` : ''}
        </div>
      `);
      group.addLayer(marker);
    });
  }, [baseStations, layers.showBaseStations, analysisResult]);

  // Update sector layer
  useEffect(() => {
    const group = layerGroupsRef.current['sectors'];
    if (!group) return;
    group.clearLayers();
    if (!layers.showSectors) return;

    baseStations.forEach(bs => {
      if (!bs.latitude || !bs.longitude) return;
      const sectorColors = ['#3b82f6', '#10b981', '#f59e0b'];

      bs.sectors?.forEach((sector, i) => {
        const coords = sectorFanGeoJSON(bs.latitude!, bs.longitude!, sector.azimuth, sector.beamWidth, sector.range);
        const polygon = L.polygon(coords.map(([lng, lat]) => [lat, lng] as [number, number]), {
          fillColor: sectorColors[i % sectorColors.length],
          fillOpacity: 0.12,
          color: sectorColors[i % sectorColors.length],
          weight: 1,
          opacity: 0.5,
        });
        polygon.bindTooltip(`Sektör ${sector.azimuth}° · ${sector.beamWidth}° beam`);
        group.addLayer(polygon);
      });
    });
  }, [baseStations, layers.showSectors]);

  // Update route layer
  useEffect(() => {
    const group = layerGroupsRef.current['route'];
    if (!group) return;
    group.clearLayers();
    if (!layers.showRoute || routePoints.length < 2) return;

    const latlngs = routePoints.map(p => [p.lat, p.lng] as [number, number]);
    const polyline = L.polyline(latlngs, {
      color: '#60a5fa',
      weight: 2,
      opacity: 0.7,
      dashArray: '4 4',
    });
    group.addLayer(polyline);

    // Stop markers
    routePoints.filter(p => p.isStop).forEach(p => {
      const m = L.circleMarker([p.lat, p.lng], {
        radius: 7,
        fillColor: '#f59e0b',
        color: '#d97706',
        weight: 2,
        fillOpacity: 0.9,
      });
      m.bindTooltip(`Durak · ${new Date(p.timestamp).toLocaleTimeString('tr-TR')}`);
      group.addLayer(m);
    });
  }, [routePoints, layers.showRoute]);

  const toggleLayer = (key: keyof MapLayerConfig) => {
    onLayersChange({ ...layers, [key]: !layers[key] });
  };

  return (
    <div className="flex-1 flex flex-col overflow-hidden">
      {/* Toolbar */}
      <div className="bg-dark-800 border-b border-gray-800 px-4 py-2 flex items-center gap-4 flex-wrap">
        <span className="text-xs text-gray-500 font-semibold uppercase tracking-wider">Katmanlar:</span>
        {[
          { key: 'showMeasurements' as const, label: 'Ölçümler', color: '#3b82f6' },
          { key: 'showHTS' as const, label: 'HTS', color: '#8b5cf6' },
          { key: 'showBaseStations' as const, label: 'Baz İst.', color: '#22c55e' },
          { key: 'showSectors' as const, label: 'Sektörler', color: '#06b6d4' },
          { key: 'showRoute' as const, label: 'Rota', color: '#60a5fa' },
        ].map(({ key, label, color }) => (
          <button
            key={key}
            onClick={() => toggleLayer(key)}
            className={`flex items-center gap-1.5 px-3 py-1 rounded-full text-xs border transition-all ${
              layers[key]
                ? 'border-transparent text-white'
                : 'border-gray-700 text-gray-500 bg-transparent'
            }`}
            style={layers[key] ? { background: color + '33', borderColor: color + '66', color } : {}}
          >
            <span className="w-2 h-2 rounded-full" style={{ background: layers[key] ? color : '#374151' }}></span>
            {label}
          </button>
        ))}
        <div className="ml-auto text-xs text-gray-600 font-mono">
          {mapStats.visible} nokta
        </div>
      </div>

      {/* Map */}
      <div className="flex-1 relative">
        <div ref={mapRef} className="w-full h-full" />

        {/* Legend */}
        <div className="absolute bottom-4 right-4 bg-dark-800/90 backdrop-blur border border-gray-700 rounded-xl p-3 text-xs space-y-1.5 z-[1000]">
          <div className="text-gray-400 font-semibold mb-2">RSRP Sinyal</div>
          {[
            { color: '#22c55e', label: '≥ -80 dBm (Mükemmel)' },
            { color: '#84cc16', label: '-90 ~ -80 dBm (İyi)' },
            { color: '#eab308', label: '-100 ~ -90 dBm (Orta)' },
            { color: '#f97316', label: '-110 ~ -100 dBm (Zayıf)' },
            { color: '#ef4444', label: '< -110 dBm (Çok Zayıf)' },
          ].map(({ color, label }) => (
            <div key={label} className="flex items-center gap-2">
              <span className="w-3 h-3 rounded-full flex-shrink-0" style={{ background: color }}></span>
              <span className="text-gray-400">{label}</span>
            </div>
          ))}
          <div className="border-t border-gray-700 pt-1.5 mt-1.5 space-y-1">
            <div className="flex items-center gap-2">
              <span className="text-green-400">▲</span>
              <span className="text-gray-400">Baz İstasyonu</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-purple-400">●</span>
              <span className="text-gray-400">HTS Kaydı</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-yellow-400">●</span>
              <span className="text-gray-400">Durak Noktası</span>
            </div>
          </div>
        </div>

        {/* No data overlay */}
        {measurements.length === 0 && (
          <div className="absolute inset-0 flex items-center justify-center bg-dark-900/60 z-[999]">
            <div className="text-center">
              <div className="text-4xl mb-3 opacity-30">◉</div>
              <div className="text-gray-400 text-sm">Haritada gösterilecek veri yok</div>
              <div className="text-gray-600 text-xs mt-1">Önce ölçüm verisi yükleyin</div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
