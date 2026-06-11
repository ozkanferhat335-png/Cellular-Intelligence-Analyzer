import Papa from 'papaparse';
import { MeasurementRecord, HTSRecord, ColumnMapping } from '../types';

// ─── Auto-detect source format ─────────────────────────────────────────────
function detectSource(headers: string[]): MeasurementRecord['source'] {
  const h = headers.map(x => x.toLowerCase());
  if (h.some(x => x.includes('netmonster'))) return 'NetMonster';
  if (h.some(x => x.includes('nci') || x.includes('network cell'))) return 'NetworkCellInfo';
  if (h.some(x => x.includes('cellmapper'))) return 'CellMapper';
  if (h.some(x => x.includes('gnet') || x.includes('g-net'))) return 'GNetTrack';
  return 'Generic';
}

// ─── Auto-map columns ──────────────────────────────────────────────────────
function autoMapColumns(headers: string[]): ColumnMapping {
  const h = headers.map(x => x.toLowerCase().trim());
  const find = (patterns: string[]): string | undefined => {
    for (const p of patterns) {
      const idx = h.findIndex(x => x.includes(p));
      if (idx >= 0) return headers[idx];
    }
    return undefined;
  };

  return {
    timestamp: find(['time', 'date', 'timestamp', 'measured_at']),
    latitude: find(['lat', 'latitude']),
    longitude: find(['lon', 'lng', 'longitude']),
    cellId: find(['cell_id', 'cellid', 'cid', 'cell id']),
    eNodeBId: find(['enodeb', 'enb_id', 'enodebid', 'nodeb']),
    tac: find(['tac', 'tracking area']),
    pci: find(['pci', 'physical cell']),
    earfcn: find(['earfcn', 'arfcn', 'frequency']),
    rsrp: find(['rsrp', 'signal strength', 'ss_rsrp']),
    rsrq: find(['rsrq', 'ss_rsrq']),
    sinr: find(['sinr', 'snr', 'ss_sinr']),
    technology: find(['tech', 'rat', 'network type', 'type', 'generation']),
  };
}

function parseNum(val: any): number | undefined {
  if (val === undefined || val === null || val === '') return undefined;
  const n = parseFloat(String(val).replace(',', '.'));
  return isNaN(n) ? undefined : n;
}

function parseTech(val: any): MeasurementRecord['technology'] {
  if (!val) return 'Unknown';
  const v = String(val).toUpperCase();
  if (v.includes('5G') || v.includes('NR')) return '5G';
  if (v.includes('4G') || v.includes('LTE')) return '4G';
  if (v.includes('3G') || v.includes('UMTS') || v.includes('WCDMA')) return '3G';
  if (v.includes('2G') || v.includes('GSM') || v.includes('EDGE')) return '2G';
  return 'Unknown';
}

// ─── Parse Measurement CSV ─────────────────────────────────────────────────
export function parseMeasurementCSV(
  file: File,
  customMapping?: ColumnMapping
): Promise<MeasurementRecord[]> {
  return new Promise((resolve, reject) => {
    Papa.parse(file, {
      header: true,
      skipEmptyLines: true,
      complete: (results) => {
        const headers = results.meta.fields || [];
        const source = detectSource(headers);
        const mapping = customMapping || autoMapColumns(headers);
        const records: MeasurementRecord[] = [];

        results.data.forEach((row: any, idx: number) => {
          const lat = parseNum(mapping.latitude ? row[mapping.latitude] : undefined);
          const lng = parseNum(mapping.longitude ? row[mapping.longitude] : undefined);
          const cellId = parseNum(mapping.cellId ? row[mapping.cellId] : undefined);

          if (!lat || !lng || !cellId) return;

          records.push({
            id: `m-${idx}`,
            timestamp: mapping.timestamp ? row[mapping.timestamp] || new Date().toISOString() : new Date().toISOString(),
            latitude: lat,
            longitude: lng,
            cellId: Math.round(cellId),
            eNodeBId: mapping.eNodeBId ? Math.round(parseNum(row[mapping.eNodeBId]) || 0) || undefined : undefined,
            tac: mapping.tac ? Math.round(parseNum(row[mapping.tac]) || 0) || undefined : undefined,
            pci: mapping.pci ? Math.round(parseNum(row[mapping.pci]) || 0) || undefined : undefined,
            earfcn: mapping.earfcn ? Math.round(parseNum(row[mapping.earfcn]) || 0) || undefined : undefined,
            rsrp: mapping.rsrp ? parseNum(row[mapping.rsrp]) : undefined,
            rsrq: mapping.rsrq ? parseNum(row[mapping.rsrq]) : undefined,
            sinr: mapping.sinr ? parseNum(row[mapping.sinr]) : undefined,
            technology: mapping.technology ? parseTech(row[mapping.technology]) : '4G',
            source,
          });
        });

        resolve(records);
      },
      error: reject,
    });
  });
}

// ─── Parse HTS CSV ─────────────────────────────────────────────────────────
export function parseHTSCSV(file: File): Promise<HTSRecord[]> {
  return new Promise((resolve, reject) => {
    Papa.parse(file, {
      header: true,
      skipEmptyLines: true,
      complete: (results) => {
        const headers = results.meta.fields || [];
        const h = headers.map(x => x.toLowerCase().trim());

        const find = (patterns: string[]): string | undefined => {
          for (const p of patterns) {
            const idx = h.findIndex(x => x.includes(p));
            if (idx >= 0) return headers[idx];
          }
          return undefined;
        };

        const callTimeCol = find(['call_time', 'calltime', 'arama', 'time', 'date']);
        const startCol = find(['start', 'baslangic', 'başlangıç']);
        const endCol = find(['end', 'bitis', 'bitiş']);
        const cellIdCol = find(['cell_id', 'cellid', 'cid', 'cell id']);
        const lacCol = find(['lac', 'location area']);
        const tacCol = find(['tac', 'tracking area']);
        const imsiCol = find(['imsi']);
        const msisdnCol = find(['msisdn', 'phone', 'number']);

        const records: HTSRecord[] = [];

        results.data.forEach((row: any, idx: number) => {
          const cellId = parseNum(cellIdCol ? row[cellIdCol] : undefined);
          if (!cellId) return;

          const start = startCol ? row[startCol] : undefined;
          const end = endCol ? row[endCol] : undefined;
          let duration: number | undefined;
          if (start && end) {
            const diff = new Date(end).getTime() - new Date(start).getTime();
            if (!isNaN(diff)) duration = Math.round(diff / 1000);
          }

          records.push({
            id: `h-${idx}`,
            callTime: callTimeCol ? row[callTimeCol] || '' : '',
            startTime: start,
            endTime: end,
            cellId: Math.round(cellId),
            lac: lacCol ? Math.round(parseNum(row[lacCol]) || 0) || undefined : undefined,
            tac: tacCol ? Math.round(parseNum(row[tacCol]) || 0) || undefined : undefined,
            imsi: imsiCol ? row[imsiCol] : undefined,
            msisdn: msisdnCol ? row[msisdnCol] : undefined,
            duration,
          });
        });

        resolve(records);
      },
      error: reject,
    });
  });
}

// ─── Generate Sample Data ──────────────────────────────────────────────────
export function generateSampleMeasurements(): MeasurementRecord[] {
  const records: MeasurementRecord[] = [];
  const baseLat = 41.015;
  const baseLng = 28.979;
  const cellIds = [12345, 12346, 12347, 23456, 23457];
  const tacs = [1001, 1001, 1001, 2002, 2002];

  for (let i = 0; i < 200; i++) {
    const cellIdx = Math.floor(Math.random() * cellIds.length);
    const t = new Date(Date.now() - (200 - i) * 30000);
    records.push({
      id: `sample-${i}`,
      timestamp: t.toISOString(),
      latitude: baseLat + (Math.random() - 0.5) * 0.05,
      longitude: baseLng + (Math.random() - 0.5) * 0.05,
      cellId: cellIds[cellIdx],
      eNodeBId: Math.floor(cellIds[cellIdx] / 256),
      tac: tacs[cellIdx],
      pci: 10 + cellIdx * 3,
      earfcn: 1800,
      rsrp: -75 - Math.random() * 40,
      rsrq: -10 - Math.random() * 10,
      sinr: 20 - Math.random() * 25,
      technology: '4G',
      source: 'Generic',
    });
  }
  return records;
}

export function generateSampleHTS(): HTSRecord[] {
  const records: HTSRecord[] = [];
  const cellIds = [12345, 12346, 23456];

  for (let i = 0; i < 30; i++) {
    const t = new Date(Date.now() - (30 - i) * 120000);
    const end = new Date(t.getTime() + 60000 + Math.random() * 300000);
    records.push({
      id: `hts-${i}`,
      callTime: t.toISOString(),
      startTime: t.toISOString(),
      endTime: end.toISOString(),
      cellId: cellIds[Math.floor(Math.random() * cellIds.length)],
      tac: 1001,
      duration: Math.round((end.getTime() - t.getTime()) / 1000),
    });
  }
  return records;
}
