import React, { useState, useCallback } from 'react';
import { AppTab, MeasurementRecord, HTSRecord, AnalysisResult, MapLayerConfig } from './types';
import Sidebar from './components/Sidebar';
import Dashboard from './components/Dashboard';
import UploadModule from './components/UploadModule';
import MapModule from './components/MapModule';
import AnalysisModule from './components/AnalysisModule';
import HeatmapModule from './components/HeatmapModule';
import ReportModule from './components/ReportModule';
import { runAnalysis } from './utils/analysisEngine';

const defaultLayers: MapLayerConfig = {
  showMeasurements: true,
  showHTS: true,
  showBaseStations: true,
  showSectors: true,
  showHeatmap: false,
  showRoute: true,
  heatmapMetric: 'rsrp',
};

export default function App() {
  const [activeTab, setActiveTab] = useState<AppTab>('dashboard');
  const [measurements, setMeasurements] = useState<MeasurementRecord[]>([]);
  const [htsRecords, setHtsRecords] = useState<HTSRecord[]>([]);
  const [analysisResult, setAnalysisResult] = useState<AnalysisResult | null>(null);
  const [mapLayers, setMapLayers] = useState<MapLayerConfig>(defaultLayers);
  const [isAnalyzing, setIsAnalyzing] = useState(false);

  const handleRunAnalysis = useCallback(() => {
    if (measurements.length === 0 && htsRecords.length === 0) return;
    setIsAnalyzing(true);
    setTimeout(() => {
      const result = runAnalysis(measurements, htsRecords);
      setAnalysisResult(result);
      setIsAnalyzing(false);
      setActiveTab('analysis');
    }, 800);
  }, [measurements, htsRecords]);

  const renderContent = () => {
    switch (activeTab) {
      case 'dashboard':
        return (
          <Dashboard
            measurements={measurements}
            htsRecords={htsRecords}
            analysisResult={analysisResult}
            onNavigate={setActiveTab}
            onRunAnalysis={handleRunAnalysis}
            isAnalyzing={isAnalyzing}
          />
        );
      case 'upload':
        return (
          <UploadModule
            measurements={measurements}
            htsRecords={htsRecords}
            onMeasurementsLoaded={setMeasurements}
            onHTSLoaded={setHtsRecords}
          />
        );
      case 'map':
        return (
          <MapModule
            measurements={measurements}
            htsRecords={htsRecords}
            analysisResult={analysisResult}
            layers={mapLayers}
            onLayersChange={setMapLayers}
          />
        );
      case 'analysis':
        return (
          <AnalysisModule
            measurements={measurements}
            htsRecords={htsRecords}
            analysisResult={analysisResult}
            onRunAnalysis={handleRunAnalysis}
            isAnalyzing={isAnalyzing}
          />
        );
      case 'heatmap':
        return (
          <HeatmapModule
            measurements={measurements}
            layers={mapLayers}
            onLayersChange={setMapLayers}
          />
        );
      case 'report':
        return (
          <ReportModule
            measurements={measurements}
            htsRecords={htsRecords}
            analysisResult={analysisResult}
          />
        );
      default:
        return null;
    }
  };

  return (
    <div className="flex h-screen bg-dark-900 text-gray-100 overflow-hidden">
      <Sidebar
        activeTab={activeTab}
        onTabChange={setActiveTab}
        measurementCount={measurements.length}
        htsCount={htsRecords.length}
        hasAnalysis={!!analysisResult}
      />
      <main className="flex-1 overflow-hidden flex flex-col">
        {renderContent()}
      </main>
    </div>
  );
}
