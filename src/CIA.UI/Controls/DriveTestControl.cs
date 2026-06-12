using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Business.Engines;
using CIA.Core.DTOs;
using CIA.Data.Repositories;
using CIA.Services.FileImport;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class DriveTestControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private TabControl _tabControl;
        private TabPage _tabList;
        private TabPage _tabAnalysis;
        private TabPage _tabImport;

        private DataGridView _gridDriveTests;
        private DataGridView _gridAnalysis;
        private Button _btnAnalyze;
        private Button _btnImportDt;
        private TextBox _txtDtFile;
        private TextBox _txtTestName;
        private ProgressBar _progressImport;
        private Label _lblImportStatus;
        private Panel _panelStats;

        public DriveTestControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            LoadDriveTestsAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            var lblTitle = new Label
            {
                Text = "🚗 Drive Test Analiz Modülü",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            _tabControl = new TabControl
            {
                Location = new Point(20, 55),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Font = new Font("Segoe UI", 10)
            };

            _tabList = new TabPage("📋  Drive Test Listesi");
            _tabAnalysis = new TabPage("🔍  Analiz Sonuçları");
            _tabImport = new TabPage("📥  Veri Aktarımı");

            BuildListTab();
            BuildAnalysisTab();
            BuildImportTab();

            _tabControl.TabPages.AddRange(new TabPage[] { _tabList, _tabAnalysis, _tabImport });

            this.Controls.AddRange(new Control[] { lblTitle, _tabControl });
            this.Resize += (s, e) => _tabControl.Size = new Size(this.Width - 40, this.Height - 65);
        }

        private void BuildListTab()
        {
            _tabList.BackColor = Color.FromArgb(20, 30, 60);

            _gridDriveTests = CreateGrid();
            _gridDriveTests.Dock = DockStyle.Fill;
            _gridDriveTests.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Width = 50 },
                new DataGridViewTextBoxColumn { Name = "TestName", HeaderText = "Test Adı", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "TestDate", HeaderText = "Tarih", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "TotalRecords", HeaderText = "Kayıt Sayısı", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "AvgRSRP", HeaderText = "Ort. RSRP", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "AvgRSRQ", HeaderText = "Ort. RSRQ", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "AvgSINR", HeaderText = "Ort. SINR", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Region", HeaderText = "Bölge", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "Engineer", HeaderText = "Mühendis", Width = 150 }
            });

            var panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(20, 30, 60)
            };

            _btnAnalyze = new Button
            {
                Text = "🔍  Seçili Testi Analiz Et",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(200, 35),
                Location = new Point(10, 5),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAnalyze.FlatAppearance.BorderSize = 0;
            _btnAnalyze.Click += BtnAnalyze_Click;

            panelButtons.Controls.Add(_btnAnalyze);
            _tabList.Controls.AddRange(new Control[] { _gridDriveTests, panelButtons });
        }

        private void BuildAnalysisTab()
        {
            _tabAnalysis.BackColor = Color.FromArgb(20, 30, 60);

            _panelStats = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.FromArgb(25, 35, 70)
            };

            var lblStatsTitle = new Label
            {
                Text = "Analiz İstatistikleri",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(10, 10)
            };
            _panelStats.Controls.Add(lblStatsTitle);

            _gridAnalysis = CreateGrid();
            _gridAnalysis.Dock = DockStyle.Fill;
            _gridAnalysis.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "AnomalyType", HeaderText = "Anomali Türü", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", Width = 300 },
                new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Şiddet", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Probability", HeaderText = "Olasılık", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Recommendation", HeaderText = "Öneri", Width = 300 }
            });

            _tabAnalysis.Controls.AddRange(new Control[] { _panelStats, _gridAnalysis });
        }

        private void BuildImportTab()
        {
            _tabImport.BackColor = Color.FromArgb(20, 30, 60);

            var panelImport = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(700, 180),
                BackColor = Color.FromArgb(25, 35, 70)
            };

            var lblTitle = new Label
            {
                Text = "Drive Test Dosyası İçe Aktar",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(10, 10)
            };

            var lblFile = new Label { Text = "Dosya:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(10, 50) };
            _txtDtFile = new TextBox { Location = new Point(80, 47), Size = new Size(450, 25), BackColor = Color.FromArgb(35, 50, 90), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, ReadOnly = true };

            var btnBrowse = new Button { Text = "Gözat...", Location = new Point(540, 46), Size = new Size(80, 27), BackColor = Color.FromArgb(60, 80, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnBrowse.FlatAppearance.BorderSize = 0;
            btnBrowse.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = "CSV Dosyaları (*.csv)|*.csv|Tüm Dosyalar (*.*)|*.*" })
                {
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        _txtDtFile.Text = dlg.FileName;
                        if (string.IsNullOrEmpty(_txtTestName.Text))
                            _txtTestName.Text = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                    }
                }
            };

            var lblName = new Label { Text = "Test Adı:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(10, 90) };
            _txtTestName = new TextBox { Location = new Point(80, 87), Size = new Size(300, 25), BackColor = Color.FromArgb(35, 50, 90), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            _btnImportDt = new Button
            {
                Text = "📥  İçe Aktar",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(140, 35),
                Location = new Point(10, 130),
                BackColor = Color.FromArgb(0, 150, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnImportDt.FlatAppearance.BorderSize = 0;
            _btnImportDt.Click += BtnImportDt_Click;

            _progressImport = new ProgressBar { Location = new Point(160, 137), Size = new Size(300, 20), Visible = false };
            _lblImportStatus = new Label { Text = "", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 200, 150), AutoSize = true, Location = new Point(160, 140) };

            panelImport.Controls.AddRange(new Control[]
            {
                lblTitle, lblFile, _txtDtFile, btnBrowse, lblName, _txtTestName,
                _btnImportDt, _progressImport, _lblImportStatus
            });

            _tabImport.Controls.Add(panelImport);
        }

        private async void LoadDriveTestsAsync()
        {
            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var tests = await unitOfWork.DriveTests.GetAllWithStatsAsync();

                _gridDriveTests.Rows.Clear();
                foreach (var test in tests)
                {
                    _gridDriveTests.Rows.Add(
                        test.Id,
                        test.TestName,
                        test.TestDate.ToString("dd.MM.yyyy"),
                        test.TotalRecords.ToString("N0"),
                        test.AvgRSRP?.ToString("F1") ?? "-",
                        test.AvgRSRQ?.ToString("F1") ?? "-",
                        test.AvgSINR?.ToString("F1") ?? "-",
                        test.Region ?? "-",
                        test.Engineer ?? "-"
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Drive Test listesi yükleme hatası");
            }
        }

        private async void BtnAnalyze_Click(object sender, EventArgs e)
        {
            if (_gridDriveTests.SelectedRows.Count == 0)
            {
                MessageBox.Show("Lütfen analiz edilecek bir Drive Test seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int driveTestId = (int)_gridDriveTests.SelectedRows[0].Cells["Id"].Value;
            _btnAnalyze.Enabled = false;
            _gridAnalysis.Rows.Clear();

            try
            {
                var engine = Program.ServiceProvider.GetRequiredService<IDriveTestAnalysisEngine>();
                var analysis = await engine.AnalyzeAsync(driveTestId);

                if (analysis != null)
                {
                    // Update stats panel
                    UpdateStatsPanel(analysis.Statistics);

                    // Populate anomalies
                    foreach (var anomaly in analysis.Anomalies)
                    {
                        var row = _gridAnalysis.Rows.Add(
                            anomaly.Type.ToString(),
                            anomaly.Description,
                            $"{anomaly.Severity:F0}",
                            $"{anomaly.Probability:P0}",
                            anomaly.Recommendation
                        );

                        if (anomaly.Severity > 70)
                            _gridAnalysis.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(60, 20, 20);
                        else if (anomaly.Severity > 40)
                            _gridAnalysis.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(60, 50, 10);
                    }

                    _tabControl.SelectedTab = _tabAnalysis;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Drive Test analizi hatası");
                MessageBox.Show($"Analiz hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnAnalyze.Enabled = true;
            }
        }

        private void UpdateStatsPanel(DriveTestStatisticsDto stats)
        {
            _panelStats.Controls.Clear();

            var items = new[]
            {
                ($"Toplam Nokta: {stats.TotalPoints:N0}", Color.White),
                ($"Ort. RSRP: {stats.AvgRSRP:F1} dBm", Color.FromArgb(150, 200, 255)),
                ($"Mükemmel: {stats.ExcellentCoveragePercent:F1}%", Color.FromArgb(0, 200, 100)),
                ($"İyi: {stats.GoodCoveragePercent:F1}%", Color.FromArgb(100, 200, 100)),
                ($"Orta: {stats.FairCoveragePercent:F1}%", Color.FromArgb(255, 200, 0)),
                ($"Zayıf: {stats.PoorCoveragePercent:F1}%", Color.FromArgb(255, 100, 0)),
                ($"Yok: {stats.NoCoveragePercent:F1}%", Color.FromArgb(255, 50, 50)),
                ($"Benzersiz PCI: {stats.UniquePCIs}", Color.FromArgb(200, 150, 255))
            };

            int x = 10;
            foreach (var (text, color) in items)
            {
                var lbl = new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = color,
                    AutoSize = true,
                    Location = new Point(x, 40)
                };
                _panelStats.Controls.Add(lbl);
                x += 150;
            }
        }

        private async void BtnImportDt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtDtFile.Text)) return;
            if (string.IsNullOrEmpty(_txtTestName.Text))
            {
                MessageBox.Show("Lütfen test adı girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _btnImportDt.Enabled = false;
            _progressImport.Visible = true;
            _progressImport.Style = ProgressBarStyle.Marquee;
            _lblImportStatus.Text = "İçe aktarılıyor...";

            try
            {
                var importService = Program.ServiceProvider.GetRequiredService<IDriveTestImportService>();
                var result = await importService.ImportAsync(
                    _txtDtFile.Text, _txtTestName.Text, "", _currentUser.Id);

                _lblImportStatus.Text = $"✅ Tamamlandı: {result.ProcessedRows:N0} kayıt";
                await Task.Run(() => LoadDriveTestsAsync());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Drive Test içe aktarma hatası");
                _lblImportStatus.Text = $"❌ Hata: {ex.Message}";
            }
            finally
            {
                _btnImportDt.Enabled = true;
                _progressImport.Visible = false;
            }
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
