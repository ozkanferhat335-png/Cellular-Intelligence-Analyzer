using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Core.Helpers;
using CIA.Data.Repositories;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class MapControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private GMapControl _gmap;
        private Panel _panelToolbar;
        private Panel _panelLayers;
        private Panel _panelInfo;

        private CheckBox _chkSites;
        private CheckBox _chkSectors;
        private CheckBox _chkHts;
        private CheckBox _chkDriveTest;
        private CheckBox _chkCoverage;

        private Button _btnZoomIn;
        private Button _btnZoomOut;
        private Button _btnFitAll;
        private Button _btnRefresh;
        private ComboBox _cmbMapProvider;

        private Label _lblCoordinates;
        private Label _lblZoom;
        private Label _lblMarkerCount;

        private GMapOverlay _sitesOverlay;
        private GMapOverlay _sectorsOverlay;
        private GMapOverlay _htsOverlay;
        private GMapOverlay _driveTestOverlay;
        private GMapOverlay _coverageOverlay;

        public MapControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            InitializeMapAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            // Toolbar
            _panelToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(20, 30, 60)
            };

            var lblTitle = new Label
            {
                Text = "🗺 Harita Görünümü",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(10, 14)
            };

            _cmbMapProvider = new ComboBox
            {
                Location = new Point(220, 13),
                Size = new Size(180, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9)
            };
            _cmbMapProvider.Items.AddRange(new object[]
            {
                "OpenStreetMap", "Google Maps", "Google Satellite", "Bing Maps"
            });
            _cmbMapProvider.SelectedIndex = 0;
            _cmbMapProvider.SelectedIndexChanged += CmbMapProvider_Changed;

            _btnZoomIn = CreateToolButton("+", 420, Color.FromArgb(0, 100, 180));
            _btnZoomIn.Click += (s, e) => { if (_gmap != null) _gmap.Zoom++; };

            _btnZoomOut = CreateToolButton("-", 460, Color.FromArgb(0, 100, 180));
            _btnZoomOut.Click += (s, e) => { if (_gmap != null) _gmap.Zoom--; };

            _btnFitAll = CreateToolButton("⊞ Tümü", 510, Color.FromArgb(0, 120, 80));
            _btnFitAll.Width = 80;
            _btnFitAll.Click += BtnFitAll_Click;

            _btnRefresh = CreateToolButton("↻ Yenile", 600, Color.FromArgb(80, 60, 0));
            _btnRefresh.Width = 80;
            _btnRefresh.Click += async (s, e) => await RefreshMapAsync();

            _panelToolbar.Controls.AddRange(new Control[]
            {
                lblTitle, _cmbMapProvider, _btnZoomIn, _btnZoomOut, _btnFitAll, _btnRefresh
            });

            // Layers panel (left side)
            _panelLayers = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(18, 25, 50)
            };

            BuildLayersPanel();

            // Info panel (bottom)
            _panelInfo = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(10, 15, 35)
            };

            _lblCoordinates = new Label
            {
                Text = "Koordinat: -",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(10, 7)
            };

            _lblZoom = new Label
            {
                Text = "Zoom: -",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(250, 7)
            };

            _lblMarkerCount = new Label
            {
                Text = "Marker: 0",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(380, 7)
            };

            _panelInfo.Controls.AddRange(new Control[] { _lblCoordinates, _lblZoom, _lblMarkerCount });

            // GMap control
            _gmap = new GMapControl
            {
                Dock = DockStyle.Fill,
                MinZoom = AppConstants.MinMapZoom,
                MaxZoom = AppConstants.MaxMapZoom,
                Zoom = AppConstants.DefaultMapZoom,
                Position = new PointLatLng(AppConstants.DefaultMapLatitude, AppConstants.DefaultMapLongitude),
                ShowCenter = false,
                DragButton = MouseButtons.Left
            };

            _gmap.OnPositionChanged += (point) =>
            {
                _lblCoordinates.Text = $"Koordinat: {point.Lat:F6}, {point.Lng:F6}";
            };

            _gmap.OnMapZoomChanged += () =>
            {
                _lblZoom.Text = $"Zoom: {_gmap.Zoom}";
            };

            _gmap.MouseMove += (s, e) =>
            {
                var pos = _gmap.FromLocalToLatLng(e.X, e.Y);
                _lblCoordinates.Text = $"Koordinat: {pos.Lat:F6}, {pos.Lng:F6}";
            };

            this.Controls.AddRange(new Control[]
            {
                _gmap, _panelLayers, _panelToolbar, _panelInfo
            });
        }

        private void BuildLayersPanel()
        {
            var lblLayers = new Label
            {
                Text = "KATMANLAR",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(10, 15)
            };

            _chkSites = CreateLayerCheckbox("📡 Baz İstasyonları", 45, Color.FromArgb(0, 180, 255));
            _chkSectors = CreateLayerCheckbox("🔺 Sektörler", 75, Color.FromArgb(255, 180, 0));
            _chkHts = CreateLayerCheckbox("📱 HTS Kayıtları", 105, Color.FromArgb(0, 200, 100));
            _chkDriveTest = CreateLayerCheckbox("🚗 Drive Test", 135, Color.FromArgb(255, 100, 0));
            _chkCoverage = CreateLayerCheckbox("📶 Kapsama", 165, Color.FromArgb(150, 0, 200));

            _chkSites.CheckedChanged += async (s, e) => await ToggleLayerAsync("sites", _chkSites.Checked);
            _chkSectors.CheckedChanged += async (s, e) => await ToggleLayerAsync("sectors", _chkSectors.Checked);
            _chkHts.CheckedChanged += async (s, e) => await ToggleLayerAsync("hts", _chkHts.Checked);
            _chkDriveTest.CheckedChanged += async (s, e) => await ToggleLayerAsync("drivetest", _chkDriveTest.Checked);
            _chkCoverage.CheckedChanged += async (s, e) => await ToggleLayerAsync("coverage", _chkCoverage.Checked);

            var btnClearAll = new Button
            {
                Text = "Tümünü Temizle",
                Font = new Font("Segoe UI", 8),
                Size = new Size(180, 28),
                Location = new Point(10, 210),
                BackColor = Color.FromArgb(60, 30, 30),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClearAll.FlatAppearance.BorderSize = 0;
            btnClearAll.Click += (s, e) => ClearAllLayers();

            // Legend
            var lblLegend = new Label
            {
                Text = "AÇIKLAMA",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(10, 260)
            };

            var legendItems = new[]
            {
                ("🔵", "Aktif Baz İstasyonu"),
                ("🔴", "Pasif Baz İstasyonu"),
                ("🟡", "HTS Konumu"),
                ("🟢", "İyi Sinyal (DT)"),
                ("🟠", "Orta Sinyal (DT)"),
                ("🔴", "Zayıf Sinyal (DT)")
            };

            int legendY = 285;
            foreach (var (icon, text) in legendItems)
            {
                var lbl = new Label
                {
                    Text = $"{icon} {text}",
                    Font = new Font("Segoe UI", 8),
                    ForeColor = Color.FromArgb(150, 180, 220),
                    AutoSize = true,
                    Location = new Point(10, legendY)
                };
                _panelLayers.Controls.Add(lbl);
                legendY += 22;
            }

            _panelLayers.Controls.AddRange(new Control[]
            {
                lblLayers, _chkSites, _chkSectors, _chkHts, _chkDriveTest, _chkCoverage,
                btnClearAll, lblLegend
            });
        }

        private CheckBox CreateLayerCheckbox(string text, int y, Color color)
        {
            return new CheckBox
            {
                Text = text,
                Font = new Font("Segoe UI", 9),
                ForeColor = color,
                Location = new Point(10, y),
                AutoSize = true,
                Checked = false
            };
        }

        private Button CreateToolButton(string text, int x, Color color)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9),
                Size = new Size(36, 30),
                Location = new Point(x, 10),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private async void InitializeMapAsync()
        {
            try
            {
                GMapProvider.WebProxy = null;
                GMaps.Instance.Mode = AccessMode.ServerAndCache;

                _gmap.MapProvider = GMapProviders.OpenStreetMap;
                _gmap.Position = new PointLatLng(AppConstants.DefaultMapLatitude, AppConstants.DefaultMapLongitude);
                _gmap.Zoom = AppConstants.DefaultMapZoom;

                // Initialize overlays
                _sitesOverlay = new GMapOverlay("sites");
                _sectorsOverlay = new GMapOverlay("sectors");
                _htsOverlay = new GMapOverlay("hts");
                _driveTestOverlay = new GMapOverlay("drivetest");
                _coverageOverlay = new GMapOverlay("coverage");

                _gmap.Overlays.Add(_coverageOverlay);
                _gmap.Overlays.Add(_driveTestOverlay);
                _gmap.Overlays.Add(_htsOverlay);
                _gmap.Overlays.Add(_sectorsOverlay);
                _gmap.Overlays.Add(_sitesOverlay);

                Logger.Info("Harita başarıyla başlatıldı.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Harita başlatma hatası");
            }

            await Task.CompletedTask;
        }

        private async Task ToggleLayerAsync(string layer, bool visible)
        {
            try
            {
                switch (layer)
                {
                    case "sites":
                        if (visible) await LoadSitesAsync();
                        else _sitesOverlay.Markers.Clear();
                        _sitesOverlay.IsVisibile = visible;
                        break;
                    case "sectors":
                        if (visible) await LoadSectorsAsync();
                        else _sectorsOverlay.Polygons.Clear();
                        _sectorsOverlay.IsVisibile = visible;
                        break;
                    case "hts":
                        _htsOverlay.IsVisibile = visible;
                        break;
                    case "drivetest":
                        if (visible) await LoadDriveTestAsync();
                        else _driveTestOverlay.Markers.Clear();
                        _driveTestOverlay.IsVisibile = visible;
                        break;
                    case "coverage":
                        if (visible) await LoadCoverageAsync();
                        else _coverageOverlay.Polygons.Clear();
                        _coverageOverlay.IsVisibile = visible;
                        break;
                }

                UpdateMarkerCount();
                _gmap.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Katman değiştirme hatası: {layer}");
            }
        }

        private async Task LoadSitesAsync()
        {
            _sitesOverlay.Markers.Clear();

            var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var filter = new SiteFilterDto { PageSize = AppConstants.MaxMapPoints };
            var sites = await unitOfWork.Sites.GetSitesWithSectorsAsync(filter);

            int count = 0;
            foreach (var site in sites)
            {
                if (count >= AppConstants.MaxMapPoints) break;

                var marker = new GMarkerGoogle(
                    new PointLatLng(site.Latitude, site.Longitude),
                    GMarkerGoogleType.blue_dot);

                marker.ToolTipText = $"{site.SiteCode}\n{site.SiteName}\n{site.City}";
                marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;

                _sitesOverlay.Markers.Add(marker);
                count++;
            }

            Logger.Info($"Haritaya {count} baz istasyonu yüklendi.");
        }

        private async Task LoadSectorsAsync()
        {
            _sectorsOverlay.Polygons.Clear();

            var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var filter = new SiteFilterDto { PageSize = 5000 };
            var sites = await unitOfWork.Sites.GetSitesWithSectorsAsync(filter);

            foreach (var site in sites)
            {
                foreach (var sector in site.Sectors)
                {
                    double radiusKm = GeoHelper.EstimateCoverageRadiusKm(
                        sector.TxPowerDbm > 0 ? sector.TxPowerDbm : AppConstants.DefaultAntennaPowerDbm,
                        site.AntennaHeightM ?? AppConstants.DefaultAntennaHeightM,
                        sector.FrequencyMhz > 0 ? sector.FrequencyMhz : AppConstants.DefaultFrequencyMhz);

                    var polygonPoints = GeoHelper.GenerateSectorPolygon(
                        site.Latitude, site.Longitude,
                        sector.Azimuth, sector.BeamWidthDegrees,
                        radiusKm, 20);

                    var gmapPoints = polygonPoints.Select(p => new PointLatLng(p.Latitude, p.Longitude)).ToList();
                    var polygon = new GMapPolygon(gmapPoints, sector.SectorName)
                    {
                        Fill = new SolidBrush(Color.FromArgb(30, 255, 180, 0)),
                        Stroke = new Pen(Color.FromArgb(150, 255, 180, 0), 1)
                    };

                    _sectorsOverlay.Polygons.Add(polygon);
                }
            }

            Logger.Info($"Haritaya {_sectorsOverlay.Polygons.Count} sektör yüklendi.");
        }

        private async Task LoadDriveTestAsync()
        {
            _driveTestOverlay.Markers.Clear();

            var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var driveTests = await unitOfWork.DriveTests.GetAllWithStatsAsync();
            var latestDt = driveTests.FirstOrDefault();
            if (latestDt == null) return;

            var query = new DriveTestQueryDto { DriveTestId = latestDt.Id, PageSize = AppConstants.MaxMapPoints };
            var records = await unitOfWork.DriveTests.GetRecordsAsync(query);

            int count = 0;
            foreach (var record in records)
            {
                if (count >= AppConstants.MaxMapPoints) break;

                Color markerColor;
                if (record.RSRP >= AppConstants.RsrpGood) markerColor = Color.Green;
                else if (record.RSRP >= AppConstants.RsrpFair) markerColor = Color.Yellow;
                else if (record.RSRP >= AppConstants.RsrpPoor) markerColor = Color.Orange;
                else markerColor = Color.Red;

                var marker = new GMarkerGoogle(
                    new PointLatLng(record.Latitude, record.Longitude),
                    GMarkerGoogleType.green_dot);

                marker.ToolTipText = $"RSRP: {record.RSRP:F1} dBm\nRSRQ: {record.RSRQ:F1} dB\nSINR: {record.SINR:F1} dB\nPCI: {record.PCI}";
                marker.ToolTipMode = MarkerTooltipMode.OnMouseOver;

                _driveTestOverlay.Markers.Add(marker);
                count++;
            }

            Logger.Info($"Haritaya {count} Drive Test noktası yüklendi.");
        }

        private async Task LoadCoverageAsync()
        {
            _coverageOverlay.Polygons.Clear();

            var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var models = await unitOfWork.CoverageModels.GetAllWithSectorInfoAsync();

            foreach (var model in models)
            {
                if (string.IsNullOrEmpty(model.CoveragePolygonJson)) continue;

                try
                {
                    var points = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GeoPointDto>>(model.CoveragePolygonJson);
                    if (points == null || !points.Any()) continue;

                    var gmapPoints = points.Select(p => new PointLatLng(p.Latitude, p.Longitude)).ToList();
                    var polygon = new GMapPolygon(gmapPoints, $"Coverage_{model.SectorId}")
                    {
                        Fill = new SolidBrush(Color.FromArgb(20, 0, 150, 200)),
                        Stroke = new Pen(Color.FromArgb(80, 0, 150, 200), 1)
                    };

                    _coverageOverlay.Polygons.Add(polygon);
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, $"Kapsama poligonu yükleme hatası: {model.SectorId}");
                }
            }

            Logger.Info($"Haritaya {_coverageOverlay.Polygons.Count} kapsama modeli yüklendi.");
        }

        private async Task RefreshMapAsync()
        {
            if (_chkSites.Checked) await LoadSitesAsync();
            if (_chkSectors.Checked) await LoadSectorsAsync();
            if (_chkDriveTest.Checked) await LoadDriveTestAsync();
            if (_chkCoverage.Checked) await LoadCoverageAsync();
            UpdateMarkerCount();
            _gmap.Refresh();
        }

        private void ClearAllLayers()
        {
            _sitesOverlay?.Markers.Clear();
            _sectorsOverlay?.Polygons.Clear();
            _htsOverlay?.Markers.Clear();
            _driveTestOverlay?.Markers.Clear();
            _coverageOverlay?.Polygons.Clear();

            _chkSites.Checked = false;
            _chkSectors.Checked = false;
            _chkHts.Checked = false;
            _chkDriveTest.Checked = false;
            _chkCoverage.Checked = false;

            UpdateMarkerCount();
            _gmap.Refresh();
        }

        private void BtnFitAll_Click(object sender, EventArgs e)
        {
            try
            {
                _gmap.ZoomAndCenterMarkers("sites");
            }
            catch
            {
                _gmap.Position = new PointLatLng(AppConstants.DefaultMapLatitude, AppConstants.DefaultMapLongitude);
                _gmap.Zoom = AppConstants.DefaultMapZoom;
            }
        }

        private void CmbMapProvider_Changed(object sender, EventArgs e)
        {
            switch (_cmbMapProvider.SelectedIndex)
            {
                case 0: _gmap.MapProvider = GMapProviders.OpenStreetMap; break;
                case 1: _gmap.MapProvider = GMapProviders.GoogleMap; break;
                case 2: _gmap.MapProvider = GMapProviders.GoogleSatelliteMap; break;
                case 3: _gmap.MapProvider = GMapProviders.BingMap; break;
            }
            _gmap.ReloadMap();
        }

        private void UpdateMarkerCount()
        {
            int total = (_sitesOverlay?.Markers.Count ?? 0) +
                       (_htsOverlay?.Markers.Count ?? 0) +
                       (_driveTestOverlay?.Markers.Count ?? 0);
            _lblMarkerCount.Text = $"Marker: {total:N0}";
        }
    }
}
