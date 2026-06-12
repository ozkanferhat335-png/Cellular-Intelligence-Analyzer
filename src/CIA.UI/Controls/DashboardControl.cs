using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class DashboardControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private Panel _panelStats;
        private Panel _panelRecent;
        private Panel _panelAnomalies;
        private Label _lblWelcome;

        // Stat cards
        private StatCard _cardSites;
        private StatCard _cardHts;
        private StatCard _cardDriveTests;
        private StatCard _cardAnalyses;

        public DashboardControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            LoadDataAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            // Welcome header
            _lblWelcome = new Label
            {
                Text = $"Hoş Geldiniz, {_currentUser.FullName ?? _currentUser.Username}",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            var lblDate = new Label
            {
                Text = $"📅 {DateTime.Now:dddd, dd MMMM yyyy}",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(20, 55)
            };

            // Stats panel
            _panelStats = new Panel
            {
                Location = new Point(20, 90),
                Size = new Size(this.Width - 40, 140),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _cardSites = new StatCard("📡", "Baz İstasyonları", "Yükleniyor...", Color.FromArgb(0, 120, 215));
            _cardHts = new StatCard("📱", "HTS Kayıtları", "Yükleniyor...", Color.FromArgb(0, 180, 100));
            _cardDriveTests = new StatCard("🚗", "Drive Test", "Yükleniyor...", Color.FromArgb(200, 100, 0));
            _cardAnalyses = new StatCard("🔍", "Analizler", "Yükleniyor...", Color.FromArgb(150, 0, 200));

            _panelStats.Controls.AddRange(new Control[] { _cardSites, _cardHts, _cardDriveTests, _cardAnalyses });

            // Recent activity panel
            _panelRecent = new Panel
            {
                Location = new Point(20, 250),
                Size = new Size(600, 400),
                BackColor = Color.FromArgb(20, 30, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };

            var lblRecent = new Label
            {
                Text = "Son Aktiviteler",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(15, 15)
            };

            var listRecent = new ListView
            {
                Location = new Point(10, 45),
                Size = new Size(580, 340),
                BackColor = Color.FromArgb(25, 35, 65),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                Font = new Font("Segoe UI", 9)
            };
            listRecent.Columns.Add("Zaman", 130);
            listRecent.Columns.Add("İşlem", 200);
            listRecent.Columns.Add("Kullanıcı", 150);
            listRecent.Columns.Add("Detay", 100);

            _panelRecent.Controls.AddRange(new Control[] { lblRecent, listRecent });

            // Anomalies panel
            _panelAnomalies = new Panel
            {
                Location = new Point(640, 250),
                Size = new Size(600, 400),
                BackColor = Color.FromArgb(20, 30, 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right
            };

            var lblAnomalies = new Label
            {
                Text = "⚠ Son Anomaliler",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 180, 0),
                AutoSize = true,
                Location = new Point(15, 15)
            };

            var listAnomalies = new ListView
            {
                Location = new Point(10, 45),
                Size = new Size(580, 340),
                BackColor = Color.FromArgb(25, 35, 65),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                View = View.Details,
                FullRowSelect = true,
                Font = new Font("Segoe UI", 9)
            };
            listAnomalies.Columns.Add("Tür", 150);
            listAnomalies.Columns.Add("Açıklama", 250);
            listAnomalies.Columns.Add("Şiddet", 80);
            listAnomalies.Columns.Add("Olasılık", 80);

            _panelAnomalies.Controls.AddRange(new Control[] { lblAnomalies, listAnomalies });

            this.Controls.AddRange(new Control[]
            {
                _lblWelcome, lblDate, _panelStats, _panelRecent, _panelAnomalies
            });

            this.Resize += DashboardControl_Resize;
        }

        private async void LoadDataAsync()
        {
            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var totalSites = await unitOfWork.Sites.GetTotalCountAsync();
                var totalHts = await unitOfWork.HtsRecords.GetTotalCountAsync();
                var driveTests = await unitOfWork.DriveTests.GetAllWithStatsAsync();
                var analyses = await unitOfWork.AnalysisResults.GetRecentAsync(5);

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateUI(totalSites, totalHts, driveTests, analyses)));
                }
                else
                {
                    UpdateUI(totalSites, totalHts, driveTests, analyses);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Dashboard veri yükleme hatası");
            }
        }

        private void UpdateUI(int totalSites, long totalHts,
            System.Collections.Generic.IEnumerable<CIA.Data.Entities.DriveTest> driveTests,
            System.Collections.Generic.IEnumerable<CIA.Data.Entities.AnalysisResult> analyses)
        {
            _cardSites.UpdateValue(totalSites.ToString("N0"));
            _cardHts.UpdateValue(totalHts.ToString("N0"));
            _cardDriveTests.UpdateValue(System.Linq.Enumerable.Count(driveTests).ToString("N0"));
            _cardAnalyses.UpdateValue(System.Linq.Enumerable.Count(analyses).ToString("N0"));
        }

        private void DashboardControl_Resize(object sender, EventArgs e)
        {
            if (_panelStats == null) return;
            _panelStats.Width = this.Width - 40;

            int cardWidth = (_panelStats.Width - 30) / 4;
            _cardSites.SetBounds(0, 0, cardWidth, 130);
            _cardHts.SetBounds(cardWidth + 10, 0, cardWidth, 130);
            _cardDriveTests.SetBounds((cardWidth + 10) * 2, 0, cardWidth, 130);
            _cardAnalyses.SetBounds((cardWidth + 10) * 3, 0, cardWidth, 130);

            if (_panelRecent != null)
            {
                _panelRecent.Width = (this.Width - 60) / 2;
                _panelAnomalies.Left = _panelRecent.Right + 20;
                _panelAnomalies.Width = this.Width - _panelAnomalies.Left - 20;
                _panelRecent.Height = this.Height - 270;
                _panelAnomalies.Height = this.Height - 270;
            }
        }
    }

    public class StatCard : Panel
    {
        private readonly Label _lblIcon;
        private readonly Label _lblTitle;
        private readonly Label _lblValue;
        private readonly Color _accentColor;

        public StatCard(string icon, string title, string value, Color accentColor)
        {
            _accentColor = accentColor;
            this.BackColor = Color.FromArgb(20, 30, 60);
            this.Size = new Size(200, 130);

            _lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 24),
                ForeColor = accentColor,
                AutoSize = true,
                Location = new Point(15, 15)
            };

            _lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(15, 65)
            };

            _lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(15, 85)
            };

            this.Controls.AddRange(new Control[] { _lblIcon, _lblTitle, _lblValue });
            this.Paint += StatCard_Paint;
        }

        public void UpdateValue(string value)
        {
            _lblValue.Text = value;
        }

        private void StatCard_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(_accentColor, 3))
            {
                e.Graphics.DrawLine(pen, 0, this.Height - 3, this.Width, this.Height - 3);
            }
        }
    }
}
