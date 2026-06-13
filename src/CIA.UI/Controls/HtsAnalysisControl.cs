using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Business.Engines;
using CIA.Core.DTOs;
using CIA.Core.Helpers;
using CIA.Data.Repositories;
using CIA.Services.FileImport;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class HtsAnalysisControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private TabControl _tabControl;
        private TabPage _tabQuery;
        private TabPage _tabMovement;
        private TabPage _tabImport;
        private TabPage _tabRelations;

        // Query tab
        private TextBox _txtPhoneNumber;
        private TextBox _txtImei;
        private TextBox _txtImsi;
        private TextBox _txtCellId;
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;
        private Button _btnSearch;
        private Button _btnClear;
        private DataGridView _gridResults;
        private Label _lblResultCount;
        private ProgressBar _progressSearch;

        // Movement tab
        private TextBox _txtMovementPhone;
        private DateTimePicker _dtpMovStart;
        private DateTimePicker _dtpMovEnd;
        private Button _btnAnalyzeMovement;
        private DataGridView _gridMovement;
        private Label _lblMovementSummary;

        // Import tab
        private TextBox _txtImportFile;
        private Button _btnBrowseFile;
        private Button _btnImport;
        private ProgressBar _progressImport;
        private Label _lblImportStatus;
        private DataGridView _gridImportHistory;
        private ComboBox _cmbDelimiter;
        private Button _btnExportHistoryExcel;

        // Export buttons
        private Button _btnExportCsv;
        private Button _btnExportExcel;
        private Button _btnExportMovCsv;
        private Button _btnExportMovExcel;

        // Cached search results for export
        private List<CIA.Core.DTOs.HtsRecordDto> _lastSearchResults = new List<CIA.Core.DTOs.HtsRecordDto>();
        private CIA.Core.DTOs.HtsMovementDto _lastMovementResult = null;

        private CancellationTokenSource _searchCts;

        public HtsAnalysisControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            LoadImportHistoryAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            var lblTitle = new Label
            {
                Text = "📱 HTS Analiz Modülü",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            _tabControl = new TabControl
            {
                Location = new Point(20, 55),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Segoe UI", 10),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                ItemSize = new Size(160, 36)
            };
            _tabControl.DrawItem += TabControl_DrawItem;

            _tabQuery = new TabPage("🔍  Kayıt Sorgulama");
            _tabMovement = new TabPage("🗺  Hareket Analizi");
            _tabImport = new TabPage("📥  Veri Aktarımı");
            _tabRelations = new TabPage("🔗  İlişki Analizi");

            BuildQueryTab();
            BuildMovementTab();
            BuildImportTab();
            BuildRelationsTab();

            _tabControl.TabPages.AddRange(new TabPage[] { _tabQuery, _tabMovement, _tabImport, _tabRelations });

            this.Controls.AddRange(new Control[] { lblTitle, _tabControl });
            this.Resize += (s, e) =>
            {
                _tabControl.Size = new Size(this.Width - 40, this.Height - 65);
            };
        }

        private void BuildQueryTab()
        {
            _tabQuery.BackColor = Color.FromArgb(20, 30, 60);
            _tabQuery.ForeColor = Color.White;

            // Search criteria panel
            var panelCriteria = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(_tabQuery.Width - 20, 130),
                BackColor = Color.FromArgb(25, 35, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblCriteria = new Label
            {
                Text = "Arama Kriterleri",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            // Row 1
            AddFormLabel(panelCriteria, "Telefon No:", 10, 40);
            _txtPhoneNumber = AddFormTextBox(panelCriteria, 110, 37, 180);
            _txtPhoneNumber.PlaceholderText = "05XX XXX XX XX";

            AddFormLabel(panelCriteria, "IMEI:", 310, 40);
            _txtImei = AddFormTextBox(panelCriteria, 360, 37, 180);
            _txtImei.PlaceholderText = "15 haneli IMEI";

            AddFormLabel(panelCriteria, "IMSI:", 560, 40);
            _txtImsi = AddFormTextBox(panelCriteria, 610, 37, 180);

            AddFormLabel(panelCriteria, "Cell ID:", 810, 40);
            _txtCellId = AddFormTextBox(panelCriteria, 860, 37, 180);

            // Row 2
            AddFormLabel(panelCriteria, "Başlangıç:", 10, 80);
            _dtpStart = new DateTimePicker
            {
                Location = new Point(110, 77),
                Size = new Size(180, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                ShowUpDown = false,
                Value = DateTime.Today.AddDays(-7)
            };
            panelCriteria.Controls.Add(_dtpStart);

            AddFormLabel(panelCriteria, "Bitiş:", 310, 80);
            _dtpEnd = new DateTimePicker
            {
                Location = new Point(360, 77),
                Size = new Size(180, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                ShowUpDown = false,
                Value = DateTime.Today.AddDays(1)
            };
            panelCriteria.Controls.Add(_dtpEnd);

            _btnSearch = new Button
            {
                Text = "🔍  Ara",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(120, 32),
                Location = new Point(560, 75),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnSearch.FlatAppearance.BorderSize = 0;
            _btnSearch.Click += BtnSearch_Click;

            _btnClear = new Button
            {
                Text = "Temizle",
                Font = new Font("Segoe UI", 9),
                Size = new Size(80, 32),
                Location = new Point(690, 75),
                BackColor = Color.FromArgb(60, 70, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnClear.FlatAppearance.BorderSize = 0;
            _btnClear.Click += (s, e) => ClearSearch();

            panelCriteria.Controls.AddRange(new Control[] { lblCriteria, _btnSearch, _btnClear });

            _progressSearch = new ProgressBar
            {
                Location = new Point(10, 150),
                Size = new Size(_tabQuery.Width - 20, 4),
                Style = ProgressBarStyle.Marquee,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _lblResultCount = new Label
            {
                Text = "Arama yapmak için kriterleri girin ve 'Ara' butonuna tıklayın.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(410, 160)
            };

            // Results grid
            _gridResults = CreateDataGrid();
            _gridResults.Location = new Point(10, 195);
            _gridResults.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            _gridResults.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "CallDateTime", HeaderText = "Tarih/Saat", Width = 140 },
                new DataGridViewTextBoxColumn { Name = "PhoneNumber", HeaderText = "Telefon No", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "IMEI", HeaderText = "IMEI", Width = 130 },
                new DataGridViewTextBoxColumn { Name = "CellId", HeaderText = "Cell ID", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "CGI", HeaderText = "CGI", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "LAC", HeaderText = "LAC", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "DurationSeconds", HeaderText = "Süre (sn)", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "CallType", HeaderText = "Tür", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "CalledNumber", HeaderText = "Aranan", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "SiteCode", HeaderText = "Site Kodu", Width = 100 }
            });

            // Export buttons panel
            var panelExport = new Panel
            {
                Location = new Point(10, 155),
                Size = new Size(500, 30),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            _btnExportCsv = new Button
            {
                Text = "📄  CSV Olarak Dışa Aktar",
                Font = new Font("Segoe UI", 9),
                Size = new Size(180, 26),
                Location = new Point(0, 2),
                BackColor = Color.FromArgb(40, 100, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnExportCsv.FlatAppearance.BorderSize = 0;
            _btnExportCsv.Click += BtnExportCsv_Click;

            _btnExportExcel = new Button
            {
                Text = "📊  Excel Olarak Dışa Aktar",
                Font = new Font("Segoe UI", 9),
                Size = new Size(190, 26),
                Location = new Point(190, 2),
                BackColor = Color.FromArgb(30, 100, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnExportExcel.FlatAppearance.BorderSize = 0;
            _btnExportExcel.Click += BtnExportExcel_Click;

            panelExport.Controls.AddRange(new Control[] { _btnExportCsv, _btnExportExcel });

            _tabQuery.Controls.AddRange(new Control[]
            {
                panelCriteria, _progressSearch, panelExport, _lblResultCount, _gridResults
            });

            _tabQuery.Resize += (s, e) =>
            {
                panelCriteria.Width = _tabQuery.Width - 20;
                _progressSearch.Width = _tabQuery.Width - 20;
                _gridResults.Size = new Size(_tabQuery.Width - 20, _tabQuery.Height - 200);
                _gridResults.Location = new Point(10, 185);
            };
        }

        private void BuildMovementTab()
        {
            _tabMovement.BackColor = Color.FromArgb(20, 30, 60);

            var panelMovCriteria = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(_tabMovement.Width - 20, 80),
                BackColor = Color.FromArgb(25, 35, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            AddFormLabel(panelMovCriteria, "Telefon No:", 10, 25);
            _txtMovementPhone = AddFormTextBox(panelMovCriteria, 110, 22, 200);
            _txtMovementPhone.PlaceholderText = "05XX XXX XX XX";

            AddFormLabel(panelMovCriteria, "Başlangıç:", 330, 25);
            _dtpMovStart = new DateTimePicker
            {
                Location = new Point(420, 22),
                Size = new Size(180, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                Value = DateTime.Today
            };
            panelMovCriteria.Controls.Add(_dtpMovStart);

            AddFormLabel(panelMovCriteria, "Bitiş:", 620, 25);
            _dtpMovEnd = new DateTimePicker
            {
                Location = new Point(680, 22),
                Size = new Size(180, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                Value = DateTime.Today.AddDays(1)
            };
            panelMovCriteria.Controls.Add(_dtpMovEnd);

            _btnAnalyzeMovement = new Button
            {
                Text = "🗺  Analiz Et",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(130, 32),
                Location = new Point(880, 20),
                BackColor = Color.FromArgb(0, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAnalyzeMovement.FlatAppearance.BorderSize = 0;
            _btnAnalyzeMovement.Click += BtnAnalyzeMovement_Click;
            panelMovCriteria.Controls.Add(_btnAnalyzeMovement);

            _lblMovementSummary = new Label
            {
                Text = "Hareket analizi için telefon numarası ve tarih aralığı girin.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = false,
                Size = new Size(_tabMovement.Width - 20, 30),
                Location = new Point(10, 135),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _gridMovement = CreateDataGrid();
            _gridMovement.Location = new Point(10, 170);
            _gridMovement.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            _gridMovement.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Sequence", HeaderText = "#", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "Zaman", Width = 140 },
                new DataGridViewTextBoxColumn { Name = "CellId", HeaderText = "Cell ID", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "SiteCode", HeaderText = "Site", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "SectorName", HeaderText = "Sektör", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Azimuth", HeaderText = "Azimuth", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Latitude", HeaderText = "Enlem", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Longitude", HeaderText = "Boylam", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "TimeDelta", HeaderText = "Süre Farkı", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Distance", HeaderText = "Mesafe (km)", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Speed", HeaderText = "Hız (km/h)", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Suspicious", HeaderText = "Şüpheli", Width = 80 }
            });

            // Movement export buttons
            var panelMovExport = new Panel
            {
                Location = new Point(10, 100),
                Size = new Size(400, 30),
                BackColor = Color.Transparent
            };

            _btnExportMovCsv = new Button
            {
                Text = "📄  CSV Dışa Aktar",
                Font = new Font("Segoe UI", 9),
                Size = new Size(160, 26),
                Location = new Point(0, 2),
                BackColor = Color.FromArgb(40, 100, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnExportMovCsv.FlatAppearance.BorderSize = 0;
            _btnExportMovCsv.Click += BtnExportMovCsv_Click;

            _btnExportMovExcel = new Button
            {
                Text = "📊  Excel Dışa Aktar",
                Font = new Font("Segoe UI", 9),
                Size = new Size(160, 26),
                Location = new Point(170, 2),
                BackColor = Color.FromArgb(30, 100, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnExportMovExcel.FlatAppearance.BorderSize = 0;
            _btnExportMovExcel.Click += BtnExportMovExcel_Click;

            panelMovExport.Controls.AddRange(new Control[] { _btnExportMovCsv, _btnExportMovExcel });

            _tabMovement.Controls.AddRange(new Control[]
            {
                panelMovCriteria, panelMovExport, _lblMovementSummary, _gridMovement
            });

            _tabMovement.Resize += (s, e) =>
            {
                panelMovCriteria.Width = _tabMovement.Width - 20;
                _lblMovementSummary.Width = _tabMovement.Width - 20;
                _gridMovement.Location = new Point(10, 165);
                _gridMovement.Size = new Size(_tabMovement.Width - 20, _tabMovement.Height - 175);
            };
        }

        private void BuildImportTab()
        {
            _tabImport.BackColor = Color.FromArgb(20, 30, 60);

            var panelImport = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(700, 160),
                BackColor = Color.FromArgb(25, 35, 70)
            };

            var lblImportTitle = new Label
            {
                Text = "HTS Dosyası İçe Aktar",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            AddFormLabel(panelImport, "Dosya:", 10, 50);
            _txtImportFile = new TextBox
            {
                Location = new Point(80, 47),
                Size = new Size(450, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true
            };

            _btnBrowseFile = new Button
            {
                Text = "Gözat...",
                Location = new Point(540, 46),
                Size = new Size(80, 27),
                BackColor = Color.FromArgb(60, 80, 120),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnBrowseFile.FlatAppearance.BorderSize = 0;
            _btnBrowseFile.Click += BtnBrowseFile_Click;

            AddFormLabel(panelImport, "Ayraç:", 10, 90);
            _cmbDelimiter = new ComboBox
            {
                Location = new Point(80, 87),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbDelimiter.Items.AddRange(new object[] { "Noktalı Virgül (;)", "Virgül (,)", "Tab", "Pipe (|)" });
            _cmbDelimiter.SelectedIndex = 0;

            _btnImport = new Button
            {
                Text = "📥  İçe Aktar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(140, 35),
                Location = new Point(10, 115),
                BackColor = Color.FromArgb(0, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnImport.FlatAppearance.BorderSize = 0;
            _btnImport.Click += BtnImport_Click;

            _progressImport = new ProgressBar
            {
                Location = new Point(160, 122),
                Size = new Size(400, 20),
                Visible = false
            };

            _lblImportStatus = new Label
            {
                Text = "Dosya seçin ve içe aktarma işlemini başlatın.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(160, 125)
            };

            panelImport.Controls.AddRange(new Control[]
            {
                lblImportTitle, _txtImportFile, _btnBrowseFile, _cmbDelimiter,
                _btnImport, _progressImport, _lblImportStatus
            });

            // Import history
            var panelHistoryHeader = new Panel
            {
                Location = new Point(10, 185),
                Size = new Size(700, 30),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblHistory = new Label
            {
                Text = "İçe Aktarma Geçmişi",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(0, 5)
            };

            _btnExportHistoryExcel = new Button
            {
                Text = "📊  Geçmişi Excel'e Aktar",
                Font = new Font("Segoe UI", 9),
                Size = new Size(190, 26),
                Location = new Point(250, 2),
                BackColor = Color.FromArgb(30, 100, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnExportHistoryExcel.FlatAppearance.BorderSize = 0;
            _btnExportHistoryExcel.Click += BtnExportHistoryExcel_Click;

            panelHistoryHeader.Controls.AddRange(new Control[] { lblHistory, _btnExportHistoryExcel });

            _gridImportHistory = CreateDataGrid();
            _gridImportHistory.Location = new Point(10, 220);
            _gridImportHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            _gridImportHistory.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "Dosya Adı", Width = 250 },
                new DataGridViewTextBoxColumn { Name = "ImportedAt", HeaderText = "Tarih", Width = 140 },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Durum", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "TotalRows", HeaderText = "Toplam", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "ProcessedRows", HeaderText = "İşlenen", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "FailedRows", HeaderText = "Hatalı", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Süre", Width = 100 }
            });

            _tabImport.Controls.AddRange(new Control[]
            {
                panelImport, panelHistoryHeader, _gridImportHistory
            });

            _tabImport.Resize += (s, e) =>
            {
                _gridImportHistory.Size = new Size(_tabImport.Width - 20, _tabImport.Height - 230);
                panelHistoryHeader.Width = _tabImport.Width - 20;
            };
        }

        private void BuildRelationsTab()
        {
            _tabRelations.BackColor = Color.FromArgb(20, 30, 60);

            var lblInfo = new Label
            {
                Text = "Abone İlişki Analizi\n\nBu modül, belirli bir abonenin iletişim kurduğu diğer aboneleri ve\nortak konumları analiz eder.",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            _tabRelations.Controls.Add(lblInfo);
        }

        private async void BtnSearch_Click(object sender, EventArgs e)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();

            _btnSearch.Enabled = false;
            _progressSearch.Visible = true;
            _gridResults.Rows.Clear();
            _lblResultCount.Text = "Aranıyor...";

            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var query = new HtsQueryDto
                {
                    PhoneNumber = _txtPhoneNumber.Text.Trim(),
                    IMEI = _txtImei.Text.Trim(),
                    IMSI = _txtImsi.Text.Trim(),
                    CellId = _txtCellId.Text.Trim(),
                    StartDate = _dtpStart.Value,
                    EndDate = _dtpEnd.Value,
                    PageSize = 10000
                };

                var result = await unitOfWork.HtsRecords.QueryAsync(query);

                // Cache for export
                _lastSearchResults = result.Records?.ToList() ?? new List<CIA.Core.DTOs.HtsRecordDto>();

                _gridResults.Rows.Clear();
                foreach (var record in result.Records)
                {
                    _gridResults.Rows.Add(
                        record.CallDateTime.ToString("dd.MM.yyyy HH:mm:ss"),
                        record.PhoneNumber,
                        record.IMEI,
                        record.CellId,
                        record.CGI,
                        record.LAC,
                        record.DurationSeconds,
                        record.CallType,
                        record.CalledNumber,
                        record.MatchedSite?.SiteCode ?? "-"
                    );
                }

                _lblResultCount.Text = $"Toplam {result.TotalCount:N0} kayıt bulundu. " +
                                      $"Gösterilen: {result.Records.Count:N0}. " +
                                      $"Sorgu süresi: {result.QueryDuration.TotalMilliseconds:F0} ms";

                // Color suspicious rows
                foreach (DataGridViewRow row in _gridResults.Rows)
                {
                    if (row.Cells["SiteCode"].Value?.ToString() == "-")
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(255, 150, 50);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HTS arama hatası");
                _lblResultCount.Text = $"Hata: {ex.Message}";
            }
            finally
            {
                _btnSearch.Enabled = true;
                _progressSearch.Visible = false;
            }
        }

        private async void BtnAnalyzeMovement_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtMovementPhone.Text))
            {
                MessageBox.Show("Lütfen telefon numarası girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnAnalyzeMovement.Enabled = false;
            _gridMovement.Rows.Clear();
            _lblMovementSummary.Text = "Hareket analizi yapılıyor...";

            try
            {
                var engine = Program.ServiceProvider.GetRequiredService<IHtsAnalysisEngine>();
                var movement = await engine.AnalyzeMovementAsync(
                    _txtMovementPhone.Text.Trim(),
                    _dtpMovStart.Value,
                    _dtpMovEnd.Value);

                if (movement == null || !movement.MovementPoints.Any())
                {
                    _lblMovementSummary.Text = "Bu kriterlere uygun hareket verisi bulunamadı.";
                    return;
                }

                // Cache for export
                _lastMovementResult = movement;

                _lblMovementSummary.Text = $"📊 Hareket Özeti: {movement.MovementPoints.Count} nokta | " +
                                          $"Benzersiz Baz: {movement.UniqueBasesCount} | " +
                                          $"Toplam Mesafe: {movement.TotalDistanceKm:F1} km | " +
                                          $"Ort. Hız: {movement.AverageSpeedKmh:F1} km/h | " +
                                          $"Model: {movement.Pattern}";

                foreach (var transition in movement.Transitions)
                {
                    var row = _gridMovement.Rows.Add(
                        transition.From.SequenceNumber,
                        transition.From.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"),
                        transition.From.CellId,
                        transition.From.SiteCode ?? "-",
                        transition.From.SectorName ?? "-",
                        transition.From.Azimuth.ToString("F0") + "°",
                        transition.From.Latitude?.ToString("F6") ?? "-",
                        transition.From.Longitude?.ToString("F6") ?? "-",
                        transition.TimeDelta.ToString(@"hh\:mm\:ss"),
                        transition.DistanceKm?.ToString("F2") ?? "-",
                        transition.EstimatedSpeedKmh?.ToString("F0") ?? "-",
                        transition.IsSuspicious ? "⚠ EVET" : "Hayır"
                    );

                    if (transition.IsSuspicious)
                    {
                        _gridMovement.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(80, 30, 30);
                        _gridMovement.Rows[row].DefaultCellStyle.ForeColor = Color.FromArgb(255, 100, 100);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Hareket analizi hatası");
                _lblMovementSummary.Text = $"Hata: {ex.Message}";
            }
            finally
            {
                _btnAnalyzeMovement.Enabled = true;
            }
        }

        private void BtnBrowseFile_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "HTS Dosyası Seç";
                dlg.Filter = "CSV Dosyaları (*.csv)|*.csv|Excel Dosyaları (*.xlsx;*.xls)|*.xlsx;*.xls|Metin Dosyaları (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*";
                dlg.FilterIndex = 1;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _txtImportFile.Text = dlg.FileName;
                    _btnImport.Enabled = true;

                    // Auto-detect delimiter from extension
                    var ext = Path.GetExtension(dlg.FileName).ToLower();
                    if (ext == ".xlsx" || ext == ".xls")
                    {
                        _lblImportStatus.Text = "ℹ Excel dosyası seçildi. İçe aktarma için CSV formatına dönüştürülecek.";
                    }
                }
            }
        }

        private async void BtnImport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtImportFile.Text)) return;

            _btnImport.Enabled = false;
            _progressImport.Visible = true;
            _progressImport.Style = ProgressBarStyle.Marquee;
            _lblImportStatus.Text = "İçe aktarma başlatılıyor...";

            try
            {
                var importService = Program.ServiceProvider.GetRequiredService<IHtsImportService>();

                string delimiter;
                switch (_cmbDelimiter.SelectedIndex)
                {
                    case 0: delimiter = ";"; break;
                    case 1: delimiter = ","; break;
                    case 2: delimiter = "\t"; break;
                    case 3: delimiter = "|"; break;
                    default: delimiter = ";"; break;
                }

                var config = new HtsImportConfigDto
                {
                    FilePath = _txtImportFile.Text,
                    Delimiter = delimiter,
                    HasHeader = true,
                    BatchSize = 5000
                };

                var progress = new Progress<ImportProgressDto>(p =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            _lblImportStatus.Text = p.CurrentStatus;
                            if (p.TotalRows > 0)
                            {
                                _progressImport.Style = ProgressBarStyle.Continuous;
                                _progressImport.Value = Math.Min(100, (int)p.ProgressPercent);
                            }
                        }));
                    }
                });

                var result = await importService.ImportAsync(config, _currentUser.Id, progress);

                _lblImportStatus.Text = $"✅ Tamamlandı: {result.ProcessedRows:N0} kayıt aktarıldı, {result.FailedRows} hata";
                await LoadImportHistoryAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HTS içe aktarma hatası");
                _lblImportStatus.Text = $"❌ Hata: {ex.Message}";
            }
            finally
            {
                _btnImport.Enabled = true;
                _progressImport.Visible = false;
            }
        }

        private async Task LoadImportHistoryAsync()
        {
            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var files = await unitOfWork.ImportedFiles.GetByTypeAsync("HTS");

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => PopulateImportHistory(files)));
                }
                else
                {
                    PopulateImportHistory(files);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "İçe aktarma geçmişi yükleme hatası");
            }
        }

        private void PopulateImportHistory(IEnumerable<CIA.Data.Entities.ImportedFile> files)
        {
            _gridImportHistory.Rows.Clear();
            foreach (var file in files)
            {
                _gridImportHistory.Rows.Add(
                    file.FileName,
                    file.ImportedAt.ToString("dd.MM.yyyy HH:mm"),
                    ((CIA.Core.Enums.ImportStatus)file.Status).ToString(),
                    file.TotalRows.ToString("N0"),
                    file.ProcessedRows.ToString("N0"),
                    file.FailedRows.ToString("N0"),
                    file.ImportDurationMs.HasValue ? $"{file.ImportDurationMs.Value / 1000.0:F1}s" : "-"
                );
            }
        }

        private void ClearSearch()
        {
            _txtPhoneNumber.Clear();
            _txtImei.Clear();
            _txtImsi.Clear();
            _txtCellId.Clear();
            _dtpStart.Value = DateTime.Today.AddDays(-7);
            _dtpEnd.Value = DateTime.Today.AddDays(1);
            _gridResults.Rows.Clear();
            _lblResultCount.Text = "Arama yapmak için kriterleri girin ve 'Ara' butonuna tıklayın.";
        }

        private DataGridView CreateDataGrid()
        {
            var grid = new DataGridView
            {
                BackgroundColor = Color.FromArgb(20, 30, 60),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(40, 60, 100),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Segoe UI", 9),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 26 }
            };

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(31, 73, 125),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(20, 30, 60),
                ForeColor = Color.FromArgb(200, 220, 255),
                SelectionBackColor = Color.FromArgb(0, 80, 160),
                SelectionForeColor = Color.White,
                Padding = new Padding(3, 0, 0, 0)
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(25, 38, 70),
                ForeColor = Color.FromArgb(200, 220, 255)
            };

            return grid;
        }

        private Label AddFormLabel(Panel panel, string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(x, y + 3)
            };
            panel.Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddFormTextBox(Panel panel, int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            panel.Controls.Add(txt);
            return txt;
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tab = (TabControl)sender;
            var tabPage = tab.TabPages[e.Index];
            var tabRect = tab.GetTabRect(e.Index);

            bool isSelected = tab.SelectedIndex == e.Index;
            var bgColor = isSelected ? Color.FromArgb(0, 80, 160) : Color.FromArgb(25, 35, 70);
            var fgColor = isSelected ? Color.White : Color.FromArgb(150, 180, 220);

            e.Graphics.FillRectangle(new SolidBrush(bgColor), tabRect);
            TextRenderer.DrawText(e.Graphics, tabPage.Text, new Font("Segoe UI", 9, isSelected ? FontStyle.Bold : FontStyle.Regular),
                tabRect, fgColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // ─── Export Handlers ────────────────────────────────────────────────────

        private async void BtnExportCsv_Click(object sender, EventArgs e)
        {
            if (_lastSearchResults == null || _lastSearchResults.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak kayıt yok. Önce arama yapın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "HTS Kayıtlarını CSV Olarak Kaydet";
                dlg.Filter = "CSV Dosyaları (*.csv)|*.csv";
                dlg.FileName = $"HTS_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _btnExportCsv.Enabled = false;
                        var exportService = Program.ServiceProvider.GetRequiredService<IExportService>();
                        await exportService.ExportHtsRecordsToCsvAsync(_lastSearchResults, dlg.FileName);
                        MessageBox.Show($"✅ {_lastSearchResults.Count:N0} kayıt başarıyla dışa aktarıldı:\n{dlg.FileName}",
                            "Dışa Aktarma Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "CSV dışa aktarma hatası");
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _btnExportCsv.Enabled = true;
                    }
                }
            }
        }

        private async void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (_lastSearchResults == null || _lastSearchResults.Count == 0)
            {
                MessageBox.Show("Dışa aktarılacak kayıt yok. Önce arama yapın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "HTS Kayıtlarını Excel Olarak Kaydet";
                dlg.Filter = "Excel Dosyaları (*.xlsx)|*.xlsx";
                dlg.FileName = $"HTS_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _btnExportExcel.Enabled = false;
                        var exportService = Program.ServiceProvider.GetRequiredService<IExportService>();
                        await exportService.ExportHtsRecordsToExcelAsync(_lastSearchResults, dlg.FileName);
                        MessageBox.Show($"✅ {_lastSearchResults.Count:N0} kayıt başarıyla dışa aktarıldı:\n{dlg.FileName}",
                            "Dışa Aktarma Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Excel dışa aktarma hatası");
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _btnExportExcel.Enabled = true;
                    }
                }
            }
        }

        private async void BtnExportMovCsv_Click(object sender, EventArgs e)
        {
            if (_lastMovementResult == null || !_lastMovementResult.MovementPoints.Any())
            {
                MessageBox.Show("Dışa aktarılacak hareket verisi yok. Önce analiz yapın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Hareket Verilerini CSV Olarak Kaydet";
                dlg.Filter = "CSV Dosyaları (*.csv)|*.csv";
                dlg.FileName = $"HTS_Movement_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _btnExportMovCsv.Enabled = false;
                        await ExportMovementToCsvAsync(_lastMovementResult, dlg.FileName);
                        MessageBox.Show($"✅ Hareket verileri başarıyla dışa aktarıldı:\n{dlg.FileName}",
                            "Dışa Aktarma Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Hareket CSV dışa aktarma hatası");
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _btnExportMovCsv.Enabled = true;
                    }
                }
            }
        }

        private async void BtnExportMovExcel_Click(object sender, EventArgs e)
        {
            if (_lastMovementResult == null || !_lastMovementResult.MovementPoints.Any())
            {
                MessageBox.Show("Dışa aktarılacak hareket verisi yok. Önce analiz yapın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Hareket Verilerini Excel Olarak Kaydet";
                dlg.Filter = "Excel Dosyaları (*.xlsx)|*.xlsx";
                dlg.FileName = $"HTS_Movement_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _btnExportMovExcel.Enabled = false;
                        await ExportMovementToExcelAsync(_lastMovementResult, dlg.FileName);
                        MessageBox.Show($"✅ Hareket verileri başarıyla dışa aktarıldı:\n{dlg.FileName}",
                            "Dışa Aktarma Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Hareket Excel dışa aktarma hatası");
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _btnExportMovExcel.Enabled = true;
                    }
                }
            }
        }

        private Task ExportMovementToCsvAsync(CIA.Core.DTOs.HtsMovementDto movement, string outputPath)
        {
            return Task.Run(() =>
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Sıra;Zaman;CellId;CGI;SiteKodu;Sektör;Azimut;Enlem;Boylam");
                foreach (var pt in movement.MovementPoints)
                {
                    sb.AppendLine(string.Join(";", new[]
                    {
                        pt.SequenceNumber.ToString(),
                        pt.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        pt.CellId ?? "",
                        pt.CGI ?? "",
                        pt.SiteCode ?? "",
                        pt.SectorName ?? "",
                        pt.Azimuth?.ToString("F0") ?? "",
                        pt.Latitude?.ToString("F6") ?? "",
                        pt.Longitude?.ToString("F6") ?? ""
                    }));
                }
                File.WriteAllText(outputPath, sb.ToString(), System.Text.Encoding.UTF8);
            });
        }

        private Task ExportMovementToExcelAsync(CIA.Core.DTOs.HtsMovementDto movement, string outputPath)
        {
            return Task.Run(() =>
            {
                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var ws = package.Workbook.Worksheets.Add("Hareket Noktaları");
                    var headers = new[] { "Sıra", "Zaman", "Cell ID", "CGI", "Site Kodu", "Sektör", "Azimut", "Enlem", "Boylam" };
                    for (int i = 0; i < headers.Length; i++)
                        ws.Cells[1, i + 1].Value = headers[i];

                    using (var range = ws.Cells[1, 1, 1, headers.Length])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 73, 125));
                        range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    }

                    int row = 2;
                    foreach (var pt in movement.MovementPoints)
                    {
                        ws.Cells[row, 1].Value = pt.SequenceNumber;
                        ws.Cells[row, 2].Value = pt.Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
                        ws.Cells[row, 3].Value = pt.CellId;
                        ws.Cells[row, 4].Value = pt.CGI;
                        ws.Cells[row, 5].Value = pt.SiteCode;
                        ws.Cells[row, 6].Value = pt.SectorName;
                        ws.Cells[row, 7].Value = pt.Azimuth;
                        ws.Cells[row, 8].Value = pt.Latitude;
                        ws.Cells[row, 9].Value = pt.Longitude;
                        row++;
                    }

                    // Summary
                    var wsSummary = package.Workbook.Worksheets.Add("Özet");
                    wsSummary.Cells[1, 1].Value = "Hareket Analizi Özeti";
                    wsSummary.Cells[1, 1].Style.Font.Bold = true;
                    wsSummary.Cells[2, 1].Value = "Telefon No:"; wsSummary.Cells[2, 2].Value = movement.PhoneNumber;
                    wsSummary.Cells[3, 1].Value = "Başlangıç:"; wsSummary.Cells[3, 2].Value = movement.StartTime.ToString("dd.MM.yyyy HH:mm:ss");
                    wsSummary.Cells[4, 1].Value = "Bitiş:"; wsSummary.Cells[4, 2].Value = movement.EndTime.ToString("dd.MM.yyyy HH:mm:ss");
                    wsSummary.Cells[5, 1].Value = "Toplam Nokta:"; wsSummary.Cells[5, 2].Value = movement.MovementPoints.Count;
                    wsSummary.Cells[6, 1].Value = "Benzersiz Baz:"; wsSummary.Cells[6, 2].Value = movement.UniqueBasesCount;
                    wsSummary.Cells[7, 1].Value = "Toplam Mesafe:"; wsSummary.Cells[7, 2].Value = $"{movement.TotalDistanceKm:F2} km";
                    wsSummary.Cells[8, 1].Value = "Ort. Hız:"; wsSummary.Cells[8, 2].Value = $"{movement.AverageSpeedKmh:F1} km/h";
                    wsSummary.Cells[9, 1].Value = "Hareket Deseni:"; wsSummary.Cells[9, 2].Value = movement.Pattern.ToString();
                    wsSummary.Cells.AutoFitColumns();

                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                    package.SaveAs(new System.IO.FileInfo(outputPath));
                }
            });
        }

        private async void BtnExportHistoryExcel_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "İçe Aktarma Geçmişini Excel Olarak Kaydet";
                dlg.Filter = "Excel Dosyaları (*.xlsx)|*.xlsx";
                dlg.FileName = $"HTS_ImportHistory_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _btnExportHistoryExcel.Enabled = false;
                        var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var files = await unitOfWork.ImportedFiles.GetByTypeAsync("HTS");
                        var exportService = Program.ServiceProvider.GetRequiredService<IExportService>();
                        await exportService.ExportImportHistoryToExcelAsync(files, dlg.FileName);
                        MessageBox.Show($"✅ İçe aktarma geçmişi başarıyla dışa aktarıldı:\n{dlg.FileName}",
                            "Dışa Aktarma Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Geçmiş Excel dışa aktarma hatası");
                        MessageBox.Show($"Dışa aktarma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _btnExportHistoryExcel.Enabled = true;
                    }
                }
            }
        }
    }
}
