// ─── Measurement Record (Drive Test) ───────────────────────────────────────
export interface MeasurementRecord {
  id: string;
  timestamp: string;
  latitude: number;
  longitude: number;
  cellId: number;
  eNodeBId?: number;
  tac?: number;
  pci?: number;
  earfcn?: number;
  rsrp?: number;
  rsrq?: number;
  sinr?: number;
  technology: '2G' | '3G' | '4G' | '5G' | 'Unknown';
  operator?: string;
  source: 'NetMonster' | 'NetworkCellInfo' | 'CellMapper' | 'GNetTrack' | 'Generic';
}

// ─── HTS Record ────────────────────────────────────────────────────────────
export interface HTSRecord {
  id: string;
  callTime: string;
  startTime?: string;
  endTime?: string;
  cellId: number;
  lac?: number;
  tac?: number;
  imsi?: string;
  msisdn?: string;
  duration?: number;
}

// ─── Base Station ──────────────────────────────────────────────────────────
export interface BaseStation {
  id: string;
  cellId: number;
  eNodeBId?: number;
  tac?: number;
  pci?: number;
  latitude?: number;
  longitude?: number;
  azimuth?: number;
  beamWidth?: number;
  technology: string;
  operator?: string;
  sectors?: Sector[];
}

export interface Sector {
  azimuth: number;
  beamWidth: number;
  range: number; // meters
}

// ─── Analysis Result ───────────────────────────────────────────────────────
export interface AnalysisCandidate {
  baseStation: BaseStation;
  probability: number;
  matchReasons: MatchReason[];
  matchedMeasurements: MeasurementRecord[];
  matchedHTS: HTSRecord[];
  avgRSRP?: number;
  avgRSRQ?: number;
  avgSINR?: number;
  distance?: number;
}

export interface MatchReason {
  type: 'CELL_ID' | 'TAC' | 'PCI' | 'GEOGRAPHIC' | 'SIGNAL' | 'TIMING';
  description: string;
  weight: number;
  matched: boolean;
}

export interface AnalysisResult {
  candidates: AnalysisCandidate[];
  totalMeasurements: number;
  totalHTS: number;
  matchedPairs: number;
  analysisTime: Date;
  coverageArea?: GeoJSON.Polygon;
}

// ─── Heatmap Point ─────────────────────────────────────────────────────────
export interface HeatmapPoint {
  lat: number;
  lng: number;
  intensity: number;
  rsrp?: number;
  rsrq?: number;
  sinr?: number;
}

// ─── Route Point ───────────────────────────────────────────────────────────
export interface RoutePoint {
  lat: number;
  lng: number;
  timestamp: string;
  speed?: number;
  isStop?: boolean;
}

// ─── Map Layer Config ──────────────────────────────────────────────────────
export interface MapLayerConfig {
  showMeasurements: boolean;
  showHTS: boolean;
  showBaseStations: boolean;
  showSectors: boolean;
  showHeatmap: boolean;
  showRoute: boolean;
  heatmapMetric: 'rsrp' | 'rsrq' | 'sinr';
}

// ─── App State ─────────────────────────────────────────────────────────────
export type AppTab = 'dashboard' | 'upload' | 'map' | 'analysis' | 'heatmap' | 'report';

export interface AppState {
  measurements: MeasurementRecord[];
  htsRecords: HTSRecord[];
  analysisResult: AnalysisResult | null;
  activeTab: AppTab;
  mapLayers: MapLayerConfig;
  isAnalyzing: boolean;
}

// ─── CSV Column Mapping ────────────────────────────────────────────────────
export interface ColumnMapping {
  timestamp?: string;
  latitude?: string;
  longitude?: string;
  cellId?: string;
  eNodeBId?: string;
  tac?: string;
  pci?: string;
  earfcn?: string;
  rsrp?: string;
  rsrq?: string;
  sinr?: string;
  technology?: string;
}

// ─── Statistics ────────────────────────────────────────────────────────────
export interface SignalStats {
  min: number;
  max: number;
  avg: number;
  median: number;
  count: number;
}

export interface MeasurementStats {
  rsrp: SignalStats;
  rsrq: SignalStats;
  sinr: SignalStats;
  technologies: Record<string, number>;
  cellIds: number[];
  timeRange: { start: string; end: string };
  boundingBox: { minLat: number; maxLat: number; minLng: number; maxLng: number };
}
