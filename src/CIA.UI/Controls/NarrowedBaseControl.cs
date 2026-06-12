using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Business.Engines;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Services.Reporting;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class NarrowedBaseControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private Panel _panelInput;
        private Panel _panelResult;
        private Panel _panelScoring;
        private Panel _panelLocations;

        private TextBox _txtPhoneNumber;
        private TextBox _txtImei;
        private TextBox _txtImsi;
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;
        private CheckBox _chkUseDriveTest;
        private CheckBox _chkUseCoverage;
        private NumericUpDown _numMinRecords;
        private Button _btnAnalyze;
        private Button _btnExportReport;
        private ProgressBar _progressAnalysis;
        private Label _lblStatus;

        // Result panels
        private Label _lblConfidenceScore;
        private Label _lblConfidenceLevel;
        private Label _lblMovementPattern;
        private Label _lblTotalDistance;
        private Label _lblAvgSpeed;
        private Label _lblSummary;
        private DataGridView _gridLocations;
        private DataGridView _gridScoring;
        private DataGridView _gridMovement;

        private NarrowedBaseAnalysisResultDto _lastResult;

        public NarrowedBaseControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            var lblTitle = new Label
            {
                Text = "🎯 Daraltılmış Baz Analiz Motoru",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            var lblSubtitle = new Label
            {
                Text = "HTS kayıtları ve saha ölçümlerini eşleştirerek kullanıcının bulunmuş olabileceği alanı daraltır",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(20, 45)
            };

            // Input panel
            _panelInput = new Panel
            {
                Location = new Point(20, 70),
                Size = new Size(this.Width - 40, 200),
                BackColor = Color.FromArgb(20, 30, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            BuildInputPanel();

            // Results area (split)
            var splitContainer = new SplitContainer
            {
                Location = new Point(20, 285),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(15, 20, 40),
                BorderStyle = BorderStyle.None,
                SplitterDistance = 200
            };

            BuildResultPanel(splitContainer.Panel1);
            BuildDetailPanel(splitContainer.Panel2);

            this.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, _panelInput, splitContainer });

            this.Resize += (s, e) =>
            {
                _panelInput.Width = this.Width - 40;
                splitContainer.Size = new Size(this.Width - 40, this.Height - 295);
            };
        }

        private void BuildInputPanel()
        {
            var lblInputTitle = new Label
            {
                Text = "Analiz Parametreleri",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(15, 12)
            };

            // Row 1
            AddLabel(_panelInput, "Telefon No:", 15, 45);
            _txtPhoneNumber = AddTextBox(_panelInput, 110, 42, 160);
            _txtPhoneNumber.PlaceholderText = "05XX XXX XX XX";

            AddLabel(_panelInput, "IMEI:", 290, 45);
            _txtImei = AddTextBox(_panelInput, 340, 42, 160);

            AddLabel(_panelInput, "IMSI:", 520, 45);
            _txtImsi = AddTextBox(_panelInput, 570, 42, 160);

            // Row 2
            AddLabel(_panelInput, "Başlangıç:", 15, 85);
            _dtpStart = new DateTimePicker
            {
                Location = new Point(110, 82),
                Size = new Size(180, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                Value = DateTime.Today.AddDays(-1)
            };
            _panelInput.Controls.Add(_dtpStart);

            AddLabel(_panelInput, "Bitiş:", 310, 85);
            _dtpEnd = new DateTimePicker
            {
                Location = new Point(370, 82),
                Size = new Size(180, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                Value = DateTime.Today.AddDays(1)
            };
            _panelInput.Controls.Add(_dtpEnd);

            AddLabel(_panelInput, "Min. Kayıt:", 570, 85);
            _numMinRecords = new NumericUpDown
            {
                Location = new Point(650, 82),
                Size = new Size(70, 25),
                Minimum = 1,
                Maximum = 1000,
                Value = AppConstants.MinHtsRecordsForAnalysis,
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White
            };
            _panelInput.Controls.Add(_numMinRecords);

            // Row 3 - Options
            _chkUseDriveTest = new CheckBox
            {
                Text = "Drive Test Verisi Kullan",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 200, 150),
                Location = new Point(15, 125),
                AutoSize = true,
                Checked = true
            };

            _chkUseCoverage = new CheckBox
            {
                Text = "Kapsama Modeli Kullan",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 200, 150),
                Location = new Point(200, 125),
                AutoSize = true,
                Checked = true
            };

            _btnAnalyze = new Button
            {
                Text = "🎯  ANALİZ BAŞLAT",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(200, 40),
                Location = new Point(15, 155),
                BackColor = Color.FromArgb(180, 0, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAnalyze.FlatAppearance.BorderSize = 0;
            _btnAnalyze.Click += BtnAnalyze_Click;

            _btnExportReport = new Button
            {
                Text = "📄  Rapor Oluştur",
                Font = new Font("Segoe UI", 10),
                Size = new Size(160, 40),
                Location = new Point(225, 155),
                BackColor = Color.FromArgb(0, 100, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _btnExportReport.FlatAppearance.BorderSize = 0;
            _btnExportReport.Click += BtnExportReport_Click;

            _progressAnalysis = new ProgressBar
            {
                Location = new Point(400, 162),
                Size = new Size(300, 25),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 200, 150),
                AutoSize = true,
                Location = new Point(400, 165)
            };

            _panelInput.Controls.AddRange(new Control[]
            {
                lblInputTitle, _dtpStart, _dtpEnd, _numMinRecords,
                _chkUseDriveTest, _chkUseCoverage,
                _btnAnalyze, _btnExportReport, _progressAnalysis, _lblStatus
            });
        }

        private void BuildResultPanel(Panel panel)
        {
            panel.BackColor = Color.FromArgb(15, 20, 40);

            // Confidence score display
            var panelConfidence = new Panel
            {
                Location = new Point(0, 5),
                Size = new Size(250, 190),
                BackColor = Color.FromArgb(20, 30, 60)
            };

            var lblConfTitle = new Label
            {
                Text = "GÜVEN SKORU",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            _lblConfidenceScore = new Label
            {
                Text = "--",
                Font = new Font("Segoe UI", 48, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(30, 30)
            };

            _lblConfidenceLevel = new Label
            {
                Text = "Analiz bekleniyor",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(10, 110)
            };

            panelConfidence.Controls.AddRange(new Control[] { lblConfTitle, _lblConfidenceScore, _lblConfidenceLevel });

            // Stats panel
            var panelStats = new Panel
            {
                Location = new Point(260, 5),
                Size = new Size(500, 190),
                BackColor = Color.FromArgb(20, 30, 60)
            };

            var lblStatsTitle = new Label
            {
                Text = "ANALİZ SONUÇLARI",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            _lblMovementPattern = new Label
            {
                Text = "Hareket Modeli: -",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(10, 40)
            };

            _lblTotalDistance = new Label
            {
                Text = "Toplam Mesafe: -",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(10, 65)
            };

            _lblAvgSpeed = new Label
            {
                Text = "Ortalama Hız: -",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(10, 90)
            };

            _lblSummary = new Label
            {
                Text = "Analiz sonuçları burada görüntülenecek.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = false,
                Size = new Size(480, 60),
                Location = new Point(10, 120)
            };

            panelStats.Controls.AddRange(new Control[]
            {
                lblStatsTitle, _lblMovementPattern, _lblTotalDistance, _lblAvgSpeed, _lblSummary
            });

            panel.Controls.AddRange(new Control[] { panelConfidence, panelStats });
        }

        private void BuildDetailPanel(Panel panel)
        {
            panel.BackColor = Color.FromArgb(15, 20, 40);

            var tabDetail = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9)
            };

            var tabLocations = new TabPage("📍 Konum Tahminleri");
            var tabScoring = new TabPage("📊 Puanlama Detayı");
            var tabMovement = new TabPage("🗺 Hareket Geçmişi");

            // Locations grid
            _gridLocations = CreateGrid();
            _gridLocations.Dock = DockStyle.Fill;
            _gridLocations.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Rank", HeaderText = "#", Width = 40 },
                new DataGridViewTextBoxColumn { Name = "SiteCode", HeaderText = "Site Kodu", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "Latitude", HeaderText = "Enlem", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Longitude", HeaderText = "Boylam", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Radius", HeaderText = "Yarıçap (km)", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Probability", HeaderText = "Olasılık", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Azimuth", HeaderText = "Azimuth", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "DriveTest", HeaderText = "DT Onayı", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Tahmini Zaman", Width = 140 }
            });
            tabLocations.Controls.Add(_gridLocations);

            // Scoring grid
            _gridScoring = CreateGrid();
            _gridScoring.Dock = DockStyle.Fill;
            _gridScoring.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Parameter", HeaderText = "Parametre", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "Weight", HeaderText = "Ağırlık", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "RawScore", HeaderText = "Ham Skor", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "WeightedScore", HeaderText = "Ağırlıklı Skor", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", Width = 400 }
            });
            tabScoring.Controls.Add(_gridScoring);

            // Movement grid
            _gridMovement = CreateGrid();
            _gridMovement.Dock = DockStyle.Fill;
            _gridMovement.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Seq", HeaderText = "#", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Zaman", Width = 140 },
                new DataGridViewTextBoxColumn { Name = "CellId", HeaderText = "Cell ID", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Site", HeaderText = "Site", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Sector", HeaderText = "Sektör", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Lat", HeaderText = "Enlem", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Lon", HeaderText = "Boylam", Width = 100 }
            });
            tabMovement.Controls.Add(_gridMovement);

            tabDetail.TabPages.AddRange(new TabPage[] { tabLocations, tabScoring, tabMovement });
            panel.Controls.Add(tabDetail);
        }

        private async void BtnAnalyze_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPhoneNumber.Text) &&
                string.IsNullOrWhiteSpace(_txtImei.Text) &&
                string.IsNullOrWhiteSpace(_txtImsi.Text))
            {
                MessageBox.Show("Lütfen en az bir tanımlayıcı girin (Telefon No, IMEI veya IMSI).",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnAnalyze.Enabled = false;
            _btnExportReport.Enabled = false;
            _progressAnalysis.Visible = true;
            _lblStatus.Text = "Analiz yapılıyor...";
            ClearResults();

            try
            {
                var engine = Program.ServiceProvider.GetRequiredService<INarrowedBaseAnalysisEngine>();

                var request = new NarrowedBaseAnalysisRequestDto
                {
                    PhoneNumber = _txtPhoneNumber.Text.Trim(),
                    IMEI = _txtImei.Text.Trim(),
                    IMSI = _txtImsi.Text.Trim(),
                    StartDate = _dtpStart.Value,
                    EndDate = _dtpEnd.Value,
                    UsedriveTestData = _chkUseDriveTest.Checked,
                    UseCoverageModels = _chkUseCoverage.Checked,
                    MinHtsRecords = (int)_numMinRecords.Value
                };

                _lastResult = await engine.AnalyzeAsync(request);

                if (_lastResult != null)
                {
                    DisplayResults(_lastResult);
                    _btnExportReport.Enabled = true;
                    _lblStatus.Text = $"✅ Analiz tamamlandı ({_lastResult.AnalysisDuration.TotalSeconds:F1}s)";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Daraltılmış baz analizi hatası");
                _lblStatus.Text = $"❌ Hata: {ex.Message}";
                MessageBox.Show($"Analiz sırasında hata oluştu:\n{ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnAnalyze.Enabled = true;
                _progressAnalysis.Visible = false;
            }
        }

        private void DisplayResults(NarrowedBaseAnalysisResultDto result)
        {
            // Confidence score
            _lblConfidenceScore.Text = result.ConfidenceScore.ToString();
            _lblConfidenceScore.ForeColor = result.ConfidenceLevel switch
            {
                ConfidenceLevel.High => Color.FromArgb(0, 200, 100),
                ConfidenceLevel.Medium => Color.FromArgb(255, 180, 0),
                _ => Color.FromArgb(255, 80, 80)
            };

            _lblConfidenceLevel.Text = result.ConfidenceLevel switch
            {
                ConfidenceLevel.High => "✅ YÜKSEK GÜVENİLİRLİK",
                ConfidenceLevel.Medium => "⚠ ORTA GÜVENİLİRLİK",
                _ => "❌ DÜŞÜK GÜVENİLİRLİK"
            };
            _lblConfidenceLevel.ForeColor = _lblConfidenceScore.ForeColor;

            _lblMovementPattern.Text = $"Hareket Modeli: {result.MovementPattern}";
            _lblTotalDistance.Text = $"Toplam Mesafe: {result.TotalDistanceKm:F1} km";
            _lblAvgSpeed.Text = $"Ortalama Hız: {result.AverageSpeedKmh:F1} km/h";
            _lblSummary.Text = result.Summary?.Split('\n').FirstOrDefault() ?? "";

            // Location estimates
            _gridLocations.Rows.Clear();
            int rank = 1;
            foreach (var est in result.LocationEstimates)
            {
                var row = _gridLocations.Rows.Add(
                    rank++,
                    est.SiteCode ?? "-",
                    est.CenterLatitude.ToString("F6"),
                    est.CenterLongitude.ToString("F6"),
                    est.RadiusKm.ToString("F2"),
                    $"{est.Probability:P0}",
                    est.Azimuth.ToString("F0") + "°",
                    est.HasDriveTestConfirmation ? "✅ Evet" : "Hayır",
                    est.EstimatedTime.ToString("dd.MM.yyyy HH:mm")
                );

                if (est.Probability > 0.5)
                    _gridLocations.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(0, 50, 30);
            }

            // Scoring details
            _gridScoring.Rows.Clear();
            foreach (var detail in result.ScoringDetails)
            {
                _gridScoring.Rows.Add(
                    detail.Parameter,
                    $"{detail.Weight:P0}",
                    $"{detail.RawScore:F3}",
                    $"{detail.WeightedScore:F2}",
                    detail.Description
                );
            }

            // Movement history
            _gridMovement.Rows.Clear();
            foreach (var point in result.MovementHistory)
            {
                _gridMovement.Rows.Add(
                    point.SequenceNumber,
                    point.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"),
                    point.CellId,
                    point.SiteCode ?? "-",
                    point.SectorName ?? "-",
                    point.Latitude?.ToString("F6") ?? "-",
                    point.Longitude?.ToString("F6") ?? "-"
                );
            }
        }

        private async void BtnExportReport_Click(object sender, EventArgs e)
        {
            if (_lastResult == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Raporu Kaydet";
                dlg.Filter = "PDF Dosyası (*.pdf)|*.pdf|Excel Dosyası (*.xlsx)|*.xlsx";
                dlg.FileName = $"DaraltilmisBaz_{_lastResult.PhoneNumber ?? _lastResult.IMEI}_{DateTime.Now:yyyyMMdd}";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var reportService = Program.ServiceProvider.GetRequiredService<IReportService>();
                        var format = dlg.FilterIndex == 1 ? CIA.Core.Enums.ReportFormat.PDF : CIA.Core.Enums.ReportFormat.Excel;

                        var request = new ReportRequestDto
                        {
                            Type = CIA.Core.Enums.ReportType.NarrowedBaseReport,
                            Format = format,
                            Title = $"Daraltılmış Baz Analiz Raporu - {_lastResult.PhoneNumber ?? _lastResult.IMEI}",
                            OutputPath = System.IO.Path.GetDirectoryName(dlg.FileName),
                            OrganizationName = "CIA Platform"
                        };

                        var report = await reportService.GenerateNarrowedBaseReportAsync(request, _lastResult, _currentUser.Id);

                        if (report.IsGenerated)
                        {
                            var openResult = MessageBox.Show(
                                $"Rapor başarıyla oluşturuldu.\n{report.FilePath}\n\nRaporu açmak ister misiniz?",
                                "Rapor Hazır", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                            if (openResult == DialogResult.Yes)
                                await reportService.OpenReportAsync(report.FilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Rapor oluşturma hatası");
                        MessageBox.Show($"Rapor oluşturulurken hata: {ex.Message}", "Hata",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ClearResults()
        {
            _lblConfidenceScore.Text = "--";
            _lblConfidenceScore.ForeColor = Color.FromArgb(0, 180, 255);
            _lblConfidenceLevel.Text = "Analiz bekleniyor";
            _lblMovementPattern.Text = "Hareket Modeli: -";
            _lblTotalDistance.Text = "Toplam Mesafe: -";
            _lblAvgSpeed.Text = "Ortalama Hız: -";
            _lblSummary.Text = "";
            _gridLocations?.Rows.Clear();
            _gridScoring?.Rows.Clear();
            _gridMovement?.Rows.Clear();
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

        private Label AddLabel(Panel panel, string text, int x, int y)
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

        private TextBox AddTextBox(Panel panel, int x, int y, int width)
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
    }
}
