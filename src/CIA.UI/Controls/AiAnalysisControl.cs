using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Business.Engines;
using CIA.Core.DTOs;
using CIA.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class AiAnalysisControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private RichTextBox _rtbOutput;
        private Button _btnAnalyzeDriveTest;
        private Button _btnAnalyzeHts;
        private Button _btnGenerateRecommendations;
        private ComboBox _cmbDriveTest;
        private TextBox _txtPhoneNumber;
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;
        private ProgressBar _progressAi;
        private DataGridView _gridRecommendations;

        public AiAnalysisControl(UserDto currentUser)
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
                Text = "🤖 Yapay Zeka Destekli Analiz",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            var lblDisclaimer = new Label
            {
                Text = "⚠ Yapay zeka sonuçları öneri niteliğindedir. Kesin hüküm değildir. Risk ve olasılık değerlendirmesi olarak değerlendirilmelidir.",
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(255, 180, 0),
                AutoSize = true,
                Location = new Point(20, 48)
            };

            // Control panel
            var panelControls = new Panel
            {
                Location = new Point(20, 75),
                Size = new Size(this.Width - 40, 100),
                BackColor = Color.FromArgb(20, 30, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Drive Test analysis
            var lblDt = new Label { Text = "Drive Test:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(10, 18) };
            _cmbDriveTest = new ComboBox
            {
                Location = new Point(100, 15),
                Size = new Size(250, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            _btnAnalyzeDriveTest = new Button
            {
                Text = "🔍 DT Anomali Tespiti",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Size = new Size(180, 30),
                Location = new Point(365, 13),
                BackColor = Color.FromArgb(0, 100, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAnalyzeDriveTest.FlatAppearance.BorderSize = 0;
            _btnAnalyzeDriveTest.Click += BtnAnalyzeDriveTest_Click;

            _btnGenerateRecommendations = new Button
            {
                Text = "💡 RF Önerileri",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Size = new Size(150, 30),
                Location = new Point(555, 13),
                BackColor = Color.FromArgb(100, 60, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnGenerateRecommendations.FlatAppearance.BorderSize = 0;
            _btnGenerateRecommendations.Click += BtnGenerateRecommendations_Click;

            // HTS analysis
            var lblPhone = new Label { Text = "Telefon No:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(10, 60) };
            _txtPhoneNumber = new TextBox { Location = new Point(100, 57), Size = new Size(180, 25), BackColor = Color.FromArgb(35, 50, 90), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

            _dtpStart = new DateTimePicker { Location = new Point(295, 57), Size = new Size(160, 25), Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm", Value = DateTime.Today.AddDays(-1) };
            _dtpEnd = new DateTimePicker { Location = new Point(465, 57), Size = new Size(160, 25), Format = DateTimePickerFormat.Custom, CustomFormat = "dd.MM.yyyy HH:mm", Value = DateTime.Today.AddDays(1) };

            _btnAnalyzeHts = new Button
            {
                Text = "🔍 HTS Anomali Tespiti",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Size = new Size(180, 30),
                Location = new Point(640, 55),
                BackColor = Color.FromArgb(0, 100, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnAnalyzeHts.FlatAppearance.BorderSize = 0;
            _btnAnalyzeHts.Click += BtnAnalyzeHts_Click;

            _progressAi = new ProgressBar { Location = new Point(840, 15), Size = new Size(150, 25), Style = ProgressBarStyle.Marquee, Visible = false };

            panelControls.Controls.AddRange(new Control[]
            {
                lblDt, _cmbDriveTest, _btnAnalyzeDriveTest, _btnGenerateRecommendations,
                lblPhone, _txtPhoneNumber, _dtpStart, _dtpEnd, _btnAnalyzeHts, _progressAi
            });

            // Split: Output + Recommendations
            var splitContainer = new SplitContainer
            {
                Location = new Point(20, 185),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(15, 20, 40),
                BorderStyle = BorderStyle.None,
                SplitterDistance = 200
            };

            // AI Output
            var lblOutput = new Label { Text = "🤖 Yapay Zeka Çıktısı", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(0, 180, 255), AutoSize = true, Location = new Point(0, 0) };
            _rtbOutput = new RichTextBox
            {
                Location = new Point(0, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = Color.FromArgb(10, 15, 30),
                ForeColor = Color.FromArgb(0, 220, 100),
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = "Yapay zeka analizi için yukarıdaki seçeneklerden birini kullanın...\n\n" +
                       "• Drive Test Anomali Tespiti: Kapsama delikleri, overshooting, PCI çakışmaları\n" +
                       "• RF Optimizasyon Önerileri: Tilt, güç ve komşu optimizasyonu\n" +
                       "• HTS Anomali Tespiti: Olağandışı hareket ve arama örüntüleri\n\n" +
                       "NOT: Tüm sonuçlar öneri niteliğindedir."
            };

            splitContainer.Panel1.Controls.AddRange(new Control[] { lblOutput, _rtbOutput });
            splitContainer.Panel1.Resize += (s, e) => _rtbOutput.Size = new Size(splitContainer.Panel1.Width, splitContainer.Panel1.Height - 30);

            // Recommendations grid
            var lblRec = new Label { Text = "💡 RF Optimizasyon Önerileri", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(255, 180, 0), AutoSize = true, Location = new Point(0, 0) };
            _gridRecommendations = CreateGrid();
            _gridRecommendations.Location = new Point(0, 25);
            _gridRecommendations.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _gridRecommendations.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Priority", HeaderText = "Öncelik", Width = 70 },
                new DataGridViewTextBoxColumn { Name = "SiteCode", HeaderText = "Site", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Problem", HeaderText = "Problem", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Açıklama", Width = 250 },
                new DataGridViewTextBoxColumn { Name = "Recommendation", HeaderText = "Öneri", Width = 250 },
                new DataGridViewTextBoxColumn { Name = "Parameter", HeaderText = "Parametre", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Current", HeaderText = "Mevcut", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Recommended", HeaderText = "Önerilen", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Probability", HeaderText = "Olasılık", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Risk", HeaderText = "Risk", Width = 150 }
            });

            splitContainer.Panel2.Controls.AddRange(new Control[] { lblRec, _gridRecommendations });
            splitContainer.Panel2.Resize += (s, e) => _gridRecommendations.Size = new Size(splitContainer.Panel2.Width, splitContainer.Panel2.Height - 30);

            this.Controls.AddRange(new Control[] { lblTitle, lblDisclaimer, panelControls, splitContainer });

            this.Resize += (s, e) =>
            {
                panelControls.Width = this.Width - 40;
                splitContainer.Size = new Size(this.Width - 40, this.Height - 195);
            };
        }

        private async void LoadDriveTestsAsync()
        {
            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var tests = await unitOfWork.DriveTests.GetAllWithStatsAsync();

                _cmbDriveTest.Items.Clear();
                foreach (var test in tests)
                    _cmbDriveTest.Items.Add($"[{test.Id}] {test.TestName} ({test.TestDate:dd.MM.yyyy})");

                if (_cmbDriveTest.Items.Count > 0)
                    _cmbDriveTest.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Drive Test listesi yükleme hatası");
            }
        }

        private async void BtnAnalyzeDriveTest_Click(object sender, EventArgs e)
        {
            if (_cmbDriveTest.SelectedIndex < 0) return;

            var selectedText = _cmbDriveTest.SelectedItem.ToString();
            if (!int.TryParse(selectedText.Split(']')[0].TrimStart('['), out int driveTestId)) return;

            _btnAnalyzeDriveTest.Enabled = false;
            _progressAi.Visible = true;
            _rtbOutput.Text = "Anomali tespiti yapılıyor...\n";

            try
            {
                var engine = Program.ServiceProvider.GetRequiredService<IAiAnalysisEngine>();
                var anomalies = await engine.DetectAnomaliesAsync(driveTestId);

                _rtbOutput.Clear();
                _rtbOutput.AppendText($"=== DRIVE TEST ANOMALİ TESPİT RAPORU ===\n");
                _rtbOutput.AppendText($"Analiz Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n");
                _rtbOutput.AppendText($"Toplam Anomali: {anomalies.Count}\n\n");

                foreach (var anomaly in anomalies)
                {
                    _rtbOutput.AppendText($"[{anomaly.Type}] Şiddet: {anomaly.Severity:F0}/100 | Olasılık: {anomaly.Probability:P0}\n");
                    _rtbOutput.AppendText($"  Açıklama: {anomaly.Description}\n");
                    _rtbOutput.AppendText($"  Öneri: {anomaly.Recommendation}\n\n");
                }

                if (!anomalies.Any())
                    _rtbOutput.AppendText("Herhangi bir anomali tespit edilmedi.\n");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "AI Drive Test analizi hatası");
                _rtbOutput.Text = $"Hata: {ex.Message}";
            }
            finally
            {
                _btnAnalyzeDriveTest.Enabled = true;
                _progressAi.Visible = false;
            }
        }

        private async void BtnGenerateRecommendations_Click(object sender, EventArgs e)
        {
            if (_cmbDriveTest.SelectedIndex < 0) return;

            var selectedText = _cmbDriveTest.SelectedItem.ToString();
            if (!int.TryParse(selectedText.Split(']')[0].TrimStart('['), out int driveTestId)) return;

            _btnGenerateRecommendations.Enabled = false;
            _progressAi.Visible = true;
            _gridRecommendations.Rows.Clear();

            try
            {
                var engine = Program.ServiceProvider.GetRequiredService<IAiAnalysisEngine>();
                var recommendations = await engine.GenerateRfOptimizationRecommendationsAsync(driveTestId);

                foreach (var rec in recommendations)
                {
                    var row = _gridRecommendations.Rows.Add(
                        rec.Priority,
                        rec.SiteCode ?? "-",
                        rec.ProblemType.ToString(),
                        rec.ProblemDescription,
                        rec.Recommendation,
                        rec.ParameterName,
                        rec.CurrentValue.ToString("F1"),
                        rec.RecommendedValue.ToString("F1"),
                        $"{rec.Probability:P0}",
                        rec.Risk
                    );

                    if (rec.Priority <= 3)
                        _gridRecommendations.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(60, 20, 20);
                }

                _rtbOutput.Text = $"=== RF OPTİMİZASYON ÖNERİLERİ ===\n";
                _rtbOutput.AppendText($"Toplam {recommendations.Count} öneri oluşturuldu.\n\n");
                _rtbOutput.AppendText("NOT: Bu öneriler yapay zeka analizi sonucunda oluşturulmuştur.\n");
                _rtbOutput.AppendText("Uygulamadan önce RF mühendisi tarafından değerlendirilmelidir.\n");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "RF öneri oluşturma hatası");
                _rtbOutput.Text = $"Hata: {ex.Message}";
            }
            finally
            {
                _btnGenerateRecommendations.Enabled = true;
                _progressAi.Visible = false;
            }
        }

        private async void BtnAnalyzeHts_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPhoneNumber.Text)) return;

            _btnAnalyzeHts.Enabled = false;
            _progressAi.Visible = true;
            _rtbOutput.Text = "HTS anomali analizi yapılıyor...\n";

            try
            {
                var engine = Program.ServiceProvider.GetRequiredService<IAiAnalysisEngine>();
                var anomalies = await engine.DetectHtsAnomaliesAsync(
                    _txtPhoneNumber.Text.Trim(), _dtpStart.Value, _dtpEnd.Value);

                _rtbOutput.Clear();
                _rtbOutput.AppendText($"=== HTS ANOMALİ TESPİT RAPORU ===\n");
                _rtbOutput.AppendText($"Hedef: {_txtPhoneNumber.Text}\n");
                _rtbOutput.AppendText($"Dönem: {_dtpStart.Value:dd.MM.yyyy} - {_dtpEnd.Value:dd.MM.yyyy}\n");
                _rtbOutput.AppendText($"Toplam Anomali: {anomalies.Count}\n\n");

                foreach (var anomaly in anomalies)
                {
                    _rtbOutput.AppendText($"[{anomaly.Type}] Şiddet: {anomaly.Severity:F0}/100 | Olasılık: {anomaly.Probability:P0}\n");
                    _rtbOutput.AppendText($"  {anomaly.Description}\n");
                    _rtbOutput.AppendText($"  Öneri: {anomaly.Recommendation}\n\n");
                }

                if (!anomalies.Any())
                    _rtbOutput.AppendText("Herhangi bir anomali tespit edilmedi.\n");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "HTS anomali analizi hatası");
                _rtbOutput.Text = $"Hata: {ex.Message}";
            }
            finally
            {
                _btnAnalyzeHts.Enabled = true;
                _progressAi.Visible = false;
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
