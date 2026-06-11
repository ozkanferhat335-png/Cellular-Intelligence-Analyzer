import React from 'react';
import { AppTab } from '../types';

interface SidebarProps {
  activeTab: AppTab;
  onTabChange: (tab: AppTab) => void;
  measurementCount: number;
  htsCount: number;
  hasAnalysis: boolean;
}

const navItems: { id: AppTab; label: string; icon: string; desc: string }[] = [
  { id: 'dashboard', label: 'Dashboard', icon: '◈', desc: 'Genel Bakış' },
  { id: 'upload', label: 'Veri Yükle', icon: '⬆', desc: 'CSV / Excel' },
  { id: 'map', label: 'Harita', icon: '◉', desc: 'Canlı Harita' },
  { id: 'analysis', label: 'Baz Analizi', icon: '◎', desc: 'Daraltılmış Analiz' },
  { id: 'heatmap', label: 'Isı Haritası', icon: '▣', desc: 'RSRP / SINR' },
  { id: 'report', label: 'Rapor', icon: '▤', desc: 'PDF / Excel' },
];

export default function Sidebar({ activeTab, onTabChange, measurementCount, htsCount, hasAnalysis }: SidebarProps) {
  return (
    <aside className="w-64 bg-dark-800 border-r border-gray-800 flex flex-col">
      {/* Logo */}
      <div className="p-5 border-b border-gray-800">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-lg bg-gradient-to-br from-blue-500 to-cyan-400 flex items-center justify-center text-white font-bold text-sm">
            RF
          </div>
          <div>
            <div className="font-semibold text-white text-sm leading-tight">Cellular</div>
            <div className="text-xs text-blue-400 font-mono">Intelligence Analyzer</div>
          </div>
        </div>
      </div>

      {/* Status badges */}
      <div className="px-4 py-3 border-b border-gray-800 space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-xs text-gray-500">Ölçüm Kayıtları</span>
          <span className={`text-xs font-mono px-2 py-0.5 rounded-full ${
            measurementCount > 0 ? 'bg-blue-900/50 text-blue-300' : 'bg-gray-800 text-gray-600'
          }`}>
            {measurementCount.toLocaleString()}
          </span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-xs text-gray-500">HTS Kayıtları</span>
          <span className={`text-xs font-mono px-2 py-0.5 rounded-full ${
            htsCount > 0 ? 'bg-purple-900/50 text-purple-300' : 'bg-gray-800 text-gray-600'
          }`}>
            {htsCount.toLocaleString()}
          </span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-xs text-gray-500">Analiz Durumu</span>
          <span className={`text-xs px-2 py-0.5 rounded-full ${
            hasAnalysis ? 'bg-green-900/50 text-green-300' : 'bg-gray-800 text-gray-600'
          }`}>
            {hasAnalysis ? 'Hazır' : 'Bekliyor'}
          </span>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 p-3 space-y-1">
        {navItems.map(item => (
          <button
            key={item.id}
            onClick={() => onTabChange(item.id)}
            className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-left transition-all duration-150 group ${
              activeTab === item.id
                ? 'bg-blue-600/20 border border-blue-500/30 text-blue-300'
                : 'text-gray-400 hover:bg-gray-800/60 hover:text-gray-200 border border-transparent'
            }`}
          >
            <span className={`text-lg w-6 text-center ${activeTab === item.id ? 'text-blue-400' : 'text-gray-600 group-hover:text-gray-400'}`}>
              {item.icon}
            </span>
            <div>
              <div className="text-sm font-medium">{item.label}</div>
              <div className="text-xs text-gray-600">{item.desc}</div>
            </div>
            {item.id === 'analysis' && hasAnalysis && (
              <span className="ml-auto w-2 h-2 rounded-full bg-green-400"></span>
            )}
          </button>
        ))}
      </nav>

      {/* Footer */}
      <div className="p-4 border-t border-gray-800">
        <div className="text-xs text-gray-600 text-center">
          <div className="font-mono">v1.0.0</div>
          <div className="mt-1">RF Planning Module</div>
        </div>
      </div>
    </aside>
  );
}
