declare module 'leaflet.heat' {
  import * as L from 'leaflet';
  function heatLayer(latlngs: [number, number, number][], options?: any): L.Layer;
  export = heatLayer;
}

declare module 'jspdf-autotable' {
  import { jsPDF } from 'jspdf';
  function autoTable(doc: jsPDF, options: any): void;
  export default autoTable;
}
