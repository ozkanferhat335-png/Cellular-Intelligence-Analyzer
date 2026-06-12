using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Data.Repositories;
using CIA.Services.Reporting;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class ReportsControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private Panel _panelReportTypes;
        private Panel _panelParams;
        private Panel _panelHistory;
        private DataGridView _gridReports;
        private Button _btnGenerate;
        private ComboBox _cmbReportType;
        private ComboBox _cmbFormat;
        private TextBox _txtTitle;
        private DateTimePicker _dtpStart;
        private DateTimePicker _dtpEnd;
        private TextBox _txtPhoneNumber;
        private ProgressBar _progressGenerate;
        private Label _lblStatus;

        public ReportsControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            LoadReportHistoryAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            var lblTitle = new Label
            {
                Text = "📄 Raporlama Modülü",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            // Report generation panel
            _panelParams = new Panel
            {
                Location = new Point(20, 55),
                Size = new Size(800, 220),
                BackColor = Color.FromArgb(20, 30, 60)
            };

            var lblParamsTitle = new Label
            {
                Text = "Rapor Oluştur",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(15, 12)
            };

            // Row 1
            AddLabel(_panelParams, "Rapor Türü:", 15, 50);
            _cmbReportType = new ComboBox
            {
                Location = new Point(120, 47),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbReportType.Items.AddRange(new object[]
            {
                "HTS Analiz Raporu",
                "Drive Test Raporu",
                "Kapsama Analiz Raporu",
                "Daraltılmış Baz Raporu",
                "Yönetici Özeti"
            });
            _cmbReportType.SelectedIndex = 0;

            AddLabel(_panelParams, "Format:", 340, 50);
            _cmbFormat = new ComboBox
            {
                Location = new Point(400, 47),
                Size = new Size(130, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbFormat.Items.AddRange(new object[] { "PDF", "Excel", "CSV" });
            _cmbFormat.SelectedIndex = 0;

            // Row 2
            AddLabel(_panelParams, "Başlık:", 15, 90);
            _txtTitle = new TextBox
            {
                Location = new Point(120, 87),
                Size = new Size(400, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Analiz Raporu"
            };

            // Row 3
            AddLabel(_panelParams, "Başlangıç:", 15, 130);
            _dtpStart = new DateTimePicker
            {
                Location = new Point(120, 127),
                Size = new Size(180, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                Value = DateTime.Today.AddDays(-7)
            };

            AddLabel(_panelParams, "Bitiş:", 320, 130);
            _dtpEnd = new DateTimePicker
            {
                Location = new Point(380, 127),
                Size = new Size(180, 25),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy HH:mm",
                Value = DateTime.Today.AddDays(1)
            };

            // Row 4
            AddLabel(_panelParams, "Telefon No:", 15, 170);
            _txtPhoneNumber = new TextBox
            {
                Location = new Point(120, 167),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Opsiyonel"
            };

            _btnGenerate = new Button
            {
                Text = "📄  RAPOR OLUŞTUR",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Size = new Size(200, 40),
                Location = new Point(580, 47),
                BackColor = Color.FromArgb(0, 100, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnGenerate.FlatAppearance.BorderSize = 0;
            _btnGenerate.Click += BtnGenerate_Click;

            _progressGenerate = new ProgressBar
            {
                Location = new Point(580, 100),
                Size = new Size(200, 20),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 200, 150),
                AutoSize = true,
                Location = new Point(580, 130)
            };

            _panelParams.Controls.AddRange(new Control[]
            {
                lblParamsTitle, _cmbReportType, _cmbFormat, _txtTitle,
                _dtpStart, _dtpEnd, _txtPhoneNumber,
                _btnGenerate, _progressGenerate, _lblStatus
            });

            // Report history
            var lblHistory = new Label
            {
                Text = "Rapor Geçmişi",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(20, 290)
            };

            _gridReports = CreateGrid();
            _gridReports.Location = new Point(20, 315);
            _gridReports.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _gridReports.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Başlık", Width = 250 },
                new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Tür", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "Format", HeaderText = "Format", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Oluşturma Tarihi", Width = 140 },
                new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Durum", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Boyut", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "FilePath", HeaderText = "Dosya Yolu", Width = 300 }
            });

            _gridReports.DoubleClick += GridReports_DoubleClick;

            this.Controls.AddRange(new Control[] { lblTitle, _panelParams, lblHistory, _gridReports });

            this.Resize += (s, e) =>
            {
                _panelParams.Width = Math.Min(800, this.Width - 40);
                _gridReports.Size = new Size(this.Width - 40, this.Height - 325);
            };
        }

        private async void BtnGenerate_Click(object sender, EventArgs e)
        {
            _btnGenerate.Enabled = false;
            _progressGenerate.Visible = true;
            _lblStatus.Text = "Rapor oluşturuluyor...";

            try
            {
                var reportService = Program.ServiceProvider.GetRequiredService<IReportService>();

                var format = _cmbFormat.SelectedIndex switch
                {
                    0 => ReportFormat.PDF,
                    1 => ReportFormat.Excel,
                    2 => ReportFormat.CSV,
                    _ => ReportFormat.PDF
                };

                var reportType = (ReportType)(_cmbReportType.SelectedIndex + 1);

                var request = new ReportRequestDto
                {
                    Type = reportType,
                    Format = format,
                    Title = _txtTitle.Text,
                    StartDate = _dtpStart.Value,
                    EndDate = _dtpEnd.Value,
                    PhoneNumber = _txtPhoneNumber.Text.Trim(),
                    OrganizationName = "CIA Platform"
                };

                ReportDto report = null;
                switch (reportType)
                {
                    case ReportType.HTSReport:
                        report = await reportService.GenerateHtsReportAsync(request, _currentUser.Id);
                        break;
                    case ReportType.DriveTestReport:
                        report = await reportService.GenerateDriveTestReportAsync(request, _currentUser.Id);
                        break;
                    case ReportType.ExecutiveSummary:
                        report = await reportService.GenerateExecutiveSummaryAsync(request, _currentUser.Id);
                        break;
                    default:
                        report = await reportService.GenerateExecutiveSummaryAsync(request, _currentUser.Id);
                        break;
                }

                if (report?.IsGenerated == true)
                {
                    _lblStatus.Text = $"✅ Rapor oluşturuldu: {System.IO.Path.GetFileName(report.FilePath)}";
                    await LoadReportHistoryAsync();

                    var openResult = MessageBox.Show(
                        "Rapor başarıyla oluşturuldu. Açmak ister misiniz?",
                        "Rapor Hazır", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (openResult == DialogResult.Yes)
                        await reportService.OpenReportAsync(report.FilePath);
                }
                else
                {
                    _lblStatus.Text = $"❌ Hata: {report?.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Rapor oluşturma hatası");
                _lblStatus.Text = $"❌ Hata: {ex.Message}";
            }
            finally
            {
                _btnGenerate.Enabled = true;
                _progressGenerate.Visible = false;
            }
        }

        private async Task LoadReportHistoryAsync()
        {
            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var reports = await unitOfWork.Reports.GetRecentAsync(50);

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => PopulateGrid(reports)));
                }
                else
                {
                    PopulateGrid(reports);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Rapor geçmişi yükleme hatası");
            }
        }

        private void PopulateGrid(System.Collections.Generic.IEnumerable<CIA.Data.Entities.Report> reports)
        {
            _gridReports.Rows.Clear();
            foreach (var report in reports)
            {
                var row = _gridReports.Rows.Add(
                    report.Title,
                    ((ReportType)report.ReportType).ToString(),
                    ((ReportFormat)report.ReportFormat).ToString(),
                    report.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    report.IsGenerated ? "✅ Hazır" : "❌ Hata",
                    report.FileSizeBytes > 0 ? $"{report.FileSizeBytes / 1024.0:F1} KB" : "-",
                    report.FilePath ?? "-"
                );

                if (!report.IsGenerated)
                    _gridReports.Rows[row].DefaultCellStyle.ForeColor = Color.FromArgb(255, 100, 100);
            }
        }

        private async void GridReports_DoubleClick(object sender, EventArgs e)
        {
            if (_gridReports.SelectedRows.Count == 0) return;

            var filePath = _gridReports.SelectedRows[0].Cells["FilePath"].Value?.ToString();
            if (string.IsNullOrEmpty(filePath) || filePath == "-") return;

            var reportService = Program.ServiceProvider.GetRequiredService<IReportService>();
            await reportService.OpenReportAsync(filePath);
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
