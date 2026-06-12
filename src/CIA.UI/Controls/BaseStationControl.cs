using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Data.Entities;
using CIA.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class BaseStationControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private SplitContainer _splitMain;
        private DataGridView _gridSites;
        private DataGridView _gridSectors;
        private DataGridView _gridCells;
        private Panel _panelFilter;
        private Panel _panelDetail;

        private TextBox _txtSearch;
        private ComboBox _cmbRegion;
        private ComboBox _cmbStatus;
        private Button _btnSearch;
        private Button _btnAddSite;
        private Button _btnExport;
        private Label _lblSiteCount;

        public BaseStationControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            LoadSitesAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            var lblTitle = new Label
            {
                Text = "📡 Baz İstasyonu Yönetimi",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // Filter panel
            _panelFilter = new Panel
            {
                Location = new Point(20, 55),
                Size = new Size(this.Width - 40, 55),
                BackColor = Color.FromArgb(20, 30, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblSearch = new Label { Text = "Ara:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(10, 17) };
            _txtSearch = new TextBox { Location = new Point(50, 14), Size = new Size(200, 25), BackColor = Color.FromArgb(35, 50, 90), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Site kodu, adı veya şehir..." };

            var lblRegion = new Label { Text = "Bölge:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(270, 17) };
            _cmbRegion = new ComboBox { Location = new Point(320, 14), Size = new Size(150, 25), BackColor = Color.FromArgb(35, 50, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbRegion.Items.Add("Tümü");
            _cmbRegion.SelectedIndex = 0;

            var lblStatus = new Label { Text = "Durum:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(490, 17) };
            _cmbStatus = new ComboBox { Location = new Point(545, 14), Size = new Size(130, 25), BackColor = Color.FromArgb(35, 50, 90), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbStatus.Items.AddRange(new object[] { "Tümü", "Aktif", "Pasif", "Bakımda", "Planlı" });
            _cmbStatus.SelectedIndex = 0;

            _btnSearch = new Button { Text = "🔍 Ara", Font = new Font("Segoe UI", 9, FontStyle.Bold), Size = new Size(90, 27), Location = new Point(695, 13), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            _btnSearch.FlatAppearance.BorderSize = 0;
            _btnSearch.Click += async (s, e) => await LoadSitesAsync();

            _btnAddSite = new Button { Text = "+ Yeni Site", Font = new Font("Segoe UI", 9), Size = new Size(100, 27), Location = new Point(800, 13), BackColor = Color.FromArgb(0, 150, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            _btnAddSite.FlatAppearance.BorderSize = 0;
            _btnAddSite.Click += BtnAddSite_Click;

            _lblSiteCount = new Label { Text = "Toplam: 0 site", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(920, 17) };

            _panelFilter.Controls.AddRange(new Control[] { lblSearch, _txtSearch, lblRegion, _cmbRegion, lblStatus, _cmbStatus, _btnSearch, _btnAddSite, _lblSiteCount });

            // Main split container
            _splitMain = new SplitContainer
            {
                Location = new Point(20, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Orientation = Orientation.Vertical,
                BackColor = Color.FromArgb(15, 20, 40),
                BorderStyle = BorderStyle.None,
                SplitterDistance = 500
            };

            // Left: Sites grid
            _gridSites = CreateGrid();
            _gridSites.Dock = DockStyle.Fill;
            _gridSites.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "SiteCode", HeaderText = "Site Kodu", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "SiteName", HeaderText = "Site Adı", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "City", HeaderText = "Şehir", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Region", HeaderText = "Bölge", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Latitude", HeaderText = "Enlem", Width = 90 },
                new DataGridViewTextBoxColumn { Name = "Longitude", HeaderText = "Boylam", Width = 90 },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Durum", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "SectorCount", HeaderText = "Sektör", Width = 60 }
            });
            _gridSites.SelectionChanged += GridSites_SelectionChanged;

            _splitMain.Panel1.Controls.Add(_gridSites);

            // Right: Sectors and Cells
            var splitRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(15, 20, 40),
                BorderStyle = BorderStyle.None,
                SplitterDistance = 200
            };

            var lblSectors = new Label { Text = "Sektörler", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(0, 180, 255), AutoSize = true, Location = new Point(5, 5) };
            _gridSectors = CreateGrid();
            _gridSectors.Location = new Point(0, 25);
            _gridSectors.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _gridSectors.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "SectorName", HeaderText = "Sektör", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Azimuth", HeaderText = "Azimuth", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Technology", HeaderText = "Teknoloji", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Band", HeaderText = "Band", Width = 70 },
                new DataGridViewTextBoxColumn { Name = "Frequency", HeaderText = "Frekans", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "TxPower", HeaderText = "Güç (dBm)", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Tilt", HeaderText = "Tilt", Width = 60 }
            });
            _gridSectors.SelectionChanged += GridSectors_SelectionChanged;

            splitRight.Panel1.Controls.AddRange(new Control[] { lblSectors, _gridSectors });

            var lblCells = new Label { Text = "Hücreler", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(0, 180, 255), AutoSize = true, Location = new Point(5, 5) };
            _gridCells = CreateGrid();
            _gridCells.Location = new Point(0, 25);
            _gridCells.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _gridCells.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "CellId", HeaderText = "Cell ID", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "CGI", HeaderText = "CGI", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "PCI", HeaderText = "PCI", Width = 60 },
                new DataGridViewTextBoxColumn { Name = "TAC", HeaderText = "TAC", Width = 70 },
                new DataGridViewTextBoxColumn { Name = "EARFCN", HeaderText = "EARFCN", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "ENBId", HeaderText = "eNB ID", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Technology", HeaderText = "Teknoloji", Width = 80 }
            });

            splitRight.Panel2.Controls.AddRange(new Control[] { lblCells, _gridCells });
            _splitMain.Panel2.Controls.Add(splitRight);

            splitRight.Panel1.Resize += (s, e) => _gridSectors.Size = new Size(splitRight.Panel1.Width, splitRight.Panel1.Height - 30);
            splitRight.Panel2.Resize += (s, e) => _gridCells.Size = new Size(splitRight.Panel2.Width, splitRight.Panel2.Height - 30);

            this.Controls.AddRange(new Control[] { lblTitle, _panelFilter, _splitMain });

            this.Resize += (s, e) =>
            {
                _panelFilter.Width = this.Width - 40;
                _splitMain.Size = new Size(this.Width - 40, this.Height - 130);
            };
        }

        private async Task LoadSitesAsync()
        {
            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var filter = new SiteFilterDto
                {
                    SearchText = _txtSearch.Text.Trim(),
                    Region = _cmbRegion.SelectedIndex > 0 ? _cmbRegion.SelectedItem.ToString() : null,
                    PageSize = 10000
                };

                if (_cmbStatus.SelectedIndex > 0)
                {
                    filter.Status = (SiteStatus)_cmbStatus.SelectedIndex;
                }

                var sites = await unitOfWork.Sites.GetSitesWithSectorsAsync(filter);
                var siteList = sites.ToList();

                _gridSites.Rows.Clear();
                foreach (var site in siteList)
                {
                    var row = _gridSites.Rows.Add(
                        site.SiteCode,
                        site.SiteName,
                        site.City,
                        site.Region,
                        site.Latitude.ToString("F6"),
                        site.Longitude.ToString("F6"),
                        ((SiteStatus)site.Status).ToString(),
                        site.Sectors.Count
                    );

                    if ((SiteStatus)site.Status != SiteStatus.Active)
                        _gridSites.Rows[row].DefaultCellStyle.ForeColor = Color.FromArgb(150, 150, 150);
                }

                _lblSiteCount.Text = $"Toplam: {siteList.Count:N0} site";

                // Load regions
                var regions = await unitOfWork.Sites.GetRegionsAsync();
                _cmbRegion.Items.Clear();
                _cmbRegion.Items.Add("Tümü");
                foreach (var region in regions)
                    _cmbRegion.Items.Add(region);
                _cmbRegion.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Baz istasyonu listesi yükleme hatası");
            }
        }

        private async void GridSites_SelectionChanged(object sender, EventArgs e)
        {
            if (_gridSites.SelectedRows.Count == 0) return;

            var siteCode = _gridSites.SelectedRows[0].Cells["SiteCode"].Value?.ToString();
            if (string.IsNullOrEmpty(siteCode)) return;

            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var site = await unitOfWork.Sites.GetSiteByCodeAsync(siteCode);

                _gridSectors.Rows.Clear();
                if (site?.Sectors == null) return;

                foreach (var sector in site.Sectors.OrderBy(s => s.SectorIndex))
                {
                    _gridSectors.Rows.Add(
                        sector.SectorName,
                        sector.Azimuth.ToString("F0") + "°",
                        ((TechnologyType)sector.Technology).ToString(),
                        ((BandType)sector.Band).ToString(),
                        sector.FrequencyMhz.ToString("F0") + " MHz",
                        sector.TxPowerDbm.ToString("F1") + " dBm",
                        (sector.MechanicalTilt + sector.ElectricalTilt).ToString("F1") + "°"
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Sektör yükleme hatası");
            }
        }

        private async void GridSectors_SelectionChanged(object sender, EventArgs e)
        {
            if (_gridSectors.SelectedRows.Count == 0) return;

            var siteCode = _gridSites.SelectedRows.Count > 0
                ? _gridSites.SelectedRows[0].Cells["SiteCode"].Value?.ToString()
                : null;

            if (string.IsNullOrEmpty(siteCode)) return;

            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var site = await unitOfWork.Sites.GetSiteByCodeAsync(siteCode);
                var sectorName = _gridSectors.SelectedRows[0].Cells["SectorName"].Value?.ToString();
                var sector = site?.Sectors.FirstOrDefault(s => s.SectorName == sectorName);

                _gridCells.Rows.Clear();
                if (sector?.Cells == null) return;

                foreach (var cell in sector.Cells)
                {
                    _gridCells.Rows.Add(
                        cell.CellId,
                        cell.CGI,
                        cell.PCI?.ToString() ?? "-",
                        cell.TAC?.ToString() ?? "-",
                        cell.EARFCN?.ToString() ?? "-",
                        cell.ENBId?.ToString() ?? "-",
                        ((TechnologyType)cell.Technology).ToString()
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Hücre yükleme hatası");
            }
        }

        private void BtnAddSite_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Yeni site ekleme formu açılacak.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                BackgroundColor = Color.FromArgb(20, 30, 60),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(40, 60, 100),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9),
                ColumnHeadersHeight = 30,
                RowTemplate = { Height = 24 }
            };

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(31, 73, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(20, 30, 60),
                ForeColor = Color.FromArgb(200, 220, 255),
                SelectionBackColor = Color.FromArgb(0, 80, 160),
                SelectionForeColor = Color.White
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(25, 38, 70)
            };

            return grid;
        }
    }
}
