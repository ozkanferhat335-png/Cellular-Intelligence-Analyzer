import {
  MeasurementRecord,
  HTSRecord,
  BaseStation,
  AnalysisCandidate,
  AnalysisResult,
  MatchReason,
  MeasurementStats,
  SignalStats,
  HeatmapPoint,
  RoutePoint,
} from '../types';

// ─── Haversine Distance (meters) ───────────────────────────────────────────
export function haversineDistance(
  lat1: number, lng1: number,
  lat2: number, lng2: number
): number {
  const R = 6371000;
  const dLat = ((lat2 - lat1) * Math.PI) / 180;
  const dLng = ((lng2 - lng1) * Math.PI) / 180;
  const a =
    Math.sin(dLat / 2) ** 2 +
    Math.cos((lat1 * Math.PI) / 180) *
    Math.cos((lat2 * Math.PI) / 180) *
    Math.sin(dLng / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

// ─── Bearing between two points ────────────────────────────────────────────
export function bearing(lat1: number, lng1: number, lat2: number, lng2: number): number {
  const dLng = ((lng2 - lng1) * Math.PI) / 180;
  const y = Math.sin(dLng) * Math.cos((lat2 * Math.PI) / 180);
  const x =
    Math.cos((lat1 * Math.PI) / 180) * Math.sin((lat2 * Math.PI) / 180) -
    Math.sin((lat1 * Math.PI) / 180) * Math.cos((lat2 * Math.PI) / 180) * Math.cos(dLng);
  return ((Math.atan2(y, x) * 180) / Math.PI + 360) % 360;
}

// ─── Signal quality label ──────────────────────────────────────────────────
export function rsrpLabel(rsrp: number): string {
  if (rsrp >= -80) return 'Mükemmel';
  if (rsrp >= -90) return 'İyi';
  if (rsrp >= -100) return 'Orta';
  if (rsrp >= -110) return 'Zayıf';
  return 'Çok Zayıf';
}

export function rsrpColor(rsrp: number): string {
  if (rsrp >= -80) return '#22c55e';
  if (rsrp >= -90) return '#84cc16';
  if (rsrp >= -100) return '#eab308';
  if (rsrp >= -110) return '#f97316';
  return '#ef4444';
}

// ─── Statistics Calculator ─────────────────────────────────────────────────
function calcStats(values: number[]): SignalStats {
  if (values.length === 0) return { min: 0, max: 0, avg: 0, median: 0, count: 0 };
  const sorted = [...values].sort((a, b) => a - b);
  const sum = values.reduce((a, b) => a + b, 0);
  return {
    min: sorted[0],
    max: sorted[sorted.length - 1],
    avg: Math.round((sum / values.length) * 10) / 10,
    median: sorted[Math.floor(sorted.length / 2)],
    count: values.length,
  };
}

export function computeMeasurementStats(records: MeasurementRecord[]): MeasurementStats {
  const rsrpVals = records.filter(r => r.rsrp !== undefined).map(r => r.rsrp!);
  const rsrqVals = records.filter(r => r.rsrq !== undefined).map(r => r.rsrq!);
  const sinrVals = records.filter(r => r.sinr !== undefined).map(r => r.sinr!);

  const techCount: Record<string, number> = {};
  records.forEach(r => {
    techCount[r.technology] = (techCount[r.technology] || 0) + 1;
  });

  const lats = records.map(r => r.latitude);
  const lngs = records.map(r => r.longitude);
  const cellIdSet = new Set(records.map(r => r.cellId));
  const cellIds = Array.from(cellIdSet);

  const timestamps = records.map(r => r.timestamp).sort();

  return {
    rsrp: calcStats(rsrpVals),
    rsrq: calcStats(rsrqVals),
    sinr: calcStats(sinrVals),
    technologies: techCount,
    cellIds,
    timeRange: {
      start: timestamps[0] || '',
      end: timestamps[timestamps.length - 1] || '',
    },
    boundingBox: {
      minLat: Math.min(...lats),
      maxLat: Math.max(...lats),
      minLng: Math.min(...lngs),
      maxLng: Math.max(...lngs),
    },
  };
}

// ─── Extract Base Stations from Measurements ───────────────────────────────
export function extractBaseStations(measurements: MeasurementRecord[]): BaseStation[] {
  const bsMap = new Map<number, BaseStation>();

  measurements.forEach(m => {
    if (!bsMap.has(m.cellId)) {
      // Estimate BS location as centroid of measurements with this cellId
      const sameCellMeasurements = measurements.filter(x => x.cellId === m.cellId);
      const avgLat = sameCellMeasurements.reduce((s, x) => s + x.latitude, 0) / sameCellMeasurements.length;
      const avgLng = sameCellMeasurements.reduce((s, x) => s + x.longitude, 0) / sameCellMeasurements.length;

      // Estimate azimuth from strongest signal point
      const strongest = sameCellMeasurements.reduce((best, x) =>
        (x.rsrp || -999) > (best.rsrp || -999) ? x : best
      );

      bsMap.set(m.cellId, {
        id: `bs-${m.cellId}`,
        cellId: m.cellId,
        eNodeBId: m.eNodeBId,
        tac: m.tac,
        pci: m.pci,
        latitude: avgLat,
        longitude: avgLng,
        azimuth: bearing(avgLat, avgLng, strongest.latitude, strongest.longitude),
        beamWidth: 120,
        technology: m.technology,
        sectors: [
          { azimuth: 0, beamWidth: 120, range: 1000 },
          { azimuth: 120, beamWidth: 120, range: 1000 },
          { azimuth: 240, beamWidth: 120, range: 1000 },
        ],
      });
    }
  });

  return Array.from(bsMap.values());
}

// ─── Main Analysis Engine ──────────────────────────────────────────────────
export function runAnalysis(
  measurements: MeasurementRecord[],
  htsRecords: HTSRecord[]
): AnalysisResult {
  const baseStations = extractBaseStations(measurements);
  const candidates: AnalysisCandidate[] = [];

  // Get unique cell IDs from HTS
  const htsCellIds = new Set(htsRecords.map(h => h.cellId));
  const htsTacs = new Set(htsRecords.filter(h => h.tac).map(h => h.tac!));
  const htslacs = new Set(htsRecords.filter(h => h.lac).map(h => h.lac!));

  baseStations.forEach(bs => {
    const reasons: MatchReason[] = [];
    let score = 0;

    // 1. Cell ID exact match
    const cellIdMatch = htsCellIds.has(bs.cellId);
    reasons.push({
      type: 'CELL_ID',
      description: `Cell ID ${bs.cellId} ${cellIdMatch ? 'eşleşti' : 'eşleşmedi'}`,
      weight: 40,
      matched: cellIdMatch,
    });
    if (cellIdMatch) score += 40;

    // 2. TAC match
    const tacMatch = bs.tac !== undefined && (htsTacs.has(bs.tac) || htslacs.has(bs.tac));
    reasons.push({
      type: 'TAC',
      description: `TAC/LAC ${bs.tac} ${tacMatch ? 'eşleşti' : 'eşleşmedi'}`,
      weight: 25,
      matched: tacMatch,
    });
    if (tacMatch) score += 25;

    // 3. PCI match (if available in HTS - rare but possible)
    const bsMeasurements = measurements.filter(m => m.cellId === bs.cellId);
    const avgRSRP = bsMeasurements.length > 0
      ? bsMeasurements.filter(m => m.rsrp).reduce((s, m) => s + m.rsrp!, 0) / bsMeasurements.filter(m => m.rsrp).length
      : undefined;

    // 4. Signal quality score
    const signalScore = avgRSRP !== undefined
      ? avgRSRP >= -80 ? 20
        : avgRSRP >= -90 ? 15
        : avgRSRP >= -100 ? 10
        : avgRSRP >= -110 ? 5
        : 0
      : 0;
    reasons.push({
      type: 'SIGNAL',
      description: `Ortalama RSRP: ${avgRSRP !== undefined ? avgRSRP.toFixed(1) + ' dBm' : 'N/A'}`,
      weight: 20,
      matched: signalScore > 0,
    });
    score += signalScore;

    // 5. Geographic proximity (if HTS has location data)
    const htsWithLocation = htsRecords.filter(h => (h as any).latitude && (h as any).longitude);
    if (htsWithLocation.length > 0 && bs.latitude && bs.longitude) {
      const minDist = Math.min(...htsWithLocation.map(h =>
        haversineDistance(bs.latitude!, bs.longitude!, (h as any).latitude, (h as any).longitude)
      ));
      const geoScore = minDist < 500 ? 15 : minDist < 1000 ? 10 : minDist < 2000 ? 5 : 0;
      reasons.push({
        type: 'GEOGRAPHIC',
        description: `En yakın HTS noktasına mesafe: ${Math.round(minDist)}m`,
        weight: 15,
        matched: geoScore > 0,
      });
      score += geoScore;
    }

    const matchedHTS = htsRecords.filter(h => h.cellId === bs.cellId);
    const avgRSRQ = bsMeasurements.filter(m => m.rsrq).length > 0
      ? bsMeasurements.filter(m => m.rsrq).reduce((s, m) => s + m.rsrq!, 0) / bsMeasurements.filter(m => m.rsrq).length
      : undefined;
    const avgSINR = bsMeasurements.filter(m => m.sinr).length > 0
      ? bsMeasurements.filter(m => m.sinr).reduce((s, m) => s + m.sinr!, 0) / bsMeasurements.filter(m => m.sinr).length
      : undefined;

    candidates.push({
      baseStation: bs,
      probability: Math.min(score, 100),
      matchReasons: reasons,
      matchedMeasurements: bsMeasurements,
      matchedHTS,
      avgRSRP,
      avgRSRQ,
      avgSINR,
    });
  });

  // Sort by probability descending
  candidates.sort((a, b) => b.probability - a.probability);

  const matchedPairs = candidates.filter(c => c.matchedHTS.length > 0).length;

  return {
    candidates,
    totalMeasurements: measurements.length,
    totalHTS: htsRecords.length,
    matchedPairs,
    analysisTime: new Date(),
  };
}

// ─── Heatmap Data Generator ────────────────────────────────────────────────
export function generateHeatmapData(
  measurements: MeasurementRecord[],
  metric: 'rsrp' | 'rsrq' | 'sinr'
): HeatmapPoint[] {
  return measurements
    .filter(m => m[metric] !== undefined)
    .map(m => {
      let intensity: number;
      const val = m[metric]!;

      if (metric === 'rsrp') {
        // RSRP: -140 to -44 dBm → normalize to 0-1
        intensity = Math.max(0, Math.min(1, (val + 140) / 96));
      } else if (metric === 'rsrq') {
        // RSRQ: -20 to 0 dB → normalize
        intensity = Math.max(0, Math.min(1, (val + 20) / 20));
      } else {
        // SINR: -20 to 30 dB → normalize
        intensity = Math.max(0, Math.min(1, (val + 20) / 50));
      }

      return {
        lat: m.latitude,
        lng: m.longitude,
        intensity,
        rsrp: m.rsrp,
        rsrq: m.rsrq,
        sinr: m.sinr,
      };
    });
}

// ─── Route Analysis ────────────────────────────────────────────────────────
export function analyzeRoute(measurements: MeasurementRecord[]): RoutePoint[] {
  const sorted = [...measurements].sort(
    (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
  );

  return sorted.map((m, i) => {
    let speed: number | undefined;
    let isStop = false;

    if (i > 0) {
      const prev = sorted[i - 1];
      const dist = haversineDistance(prev.latitude, prev.longitude, m.latitude, m.longitude);
      const timeDiff = (new Date(m.timestamp).getTime() - new Date(prev.timestamp).getTime()) / 1000;
      if (timeDiff > 0) {
        speed = (dist / timeDiff) * 3.6; // km/h
        isStop = speed < 2;
      }
    }

    return {
      lat: m.latitude,
      lng: m.longitude,
      timestamp: m.timestamp,
      speed,
      isStop,
    };
  });
}

// ─── Sector Fan GeoJSON ────────────────────────────────────────────────────
export function sectorFanGeoJSON(
  lat: number,
  lng: number,
  azimuth: number,
  beamWidth: number,
  rangeMeters: number
): [number, number][] {
  const points: [number, number][] = [[lng, lat]];
  const startAngle = azimuth - beamWidth / 2;
  const endAngle = azimuth + beamWidth / 2;
  const steps = 20;

  for (let i = 0; i <= steps; i++) {
    const angle = startAngle + (i / steps) * (endAngle - startAngle);
    const rad = (angle * Math.PI) / 180;
    const dLat = (rangeMeters / 111320) * Math.cos(rad);
    const dLng = (rangeMeters / (111320 * Math.cos((lat * Math.PI) / 180))) * Math.sin(rad);
    points.push([lng + dLng, lat + dLat]);
  }

  points.push([lng, lat]);
  return points;
}
