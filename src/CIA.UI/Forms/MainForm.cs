using System;
using System.Drawing;
using System.Windows.Forms;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.UI.Controls;
using NLog;

namespace CIA.UI.Forms
{
    public class MainForm : Form
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly UserDto _currentUser;
        private Panel _panelSidebar;
        private Panel _panelContent;
        private Panel _panelHeader;
        private Panel _panelStatusBar;
        private Label _lblCurrentUser;
        private Label _lblCurrentTime;
        private Label _lblStatus;
        private Timer _clockTimer;

        // Navigation buttons
        private Button _btnDashboard;
        private Button _btnHtsAnalysis;
        private Button _btnDriveTest;
        private Button _btnBaseStations;
        private Button _btnNarrowedBase;
        private Button _btnCoverage;
        private Button _btnMap;
        private Button _btnReports;
        private Button _btnAiAnalysis;
        private Button _btnSettings;
        private Button _btnLogout;

        private Button _activeNavButton;

        public MainForm(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            LoadDashboard();
        }

        private void InitializeComponent()
        {
            this.Text = $"{AppConstants.AppName} v{AppConstants.AppVersion}";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.MinimumSize = new Size(1200, 700);

            // Header
            _panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(20, 30, 60)
            };

            var lblAppName = new Label
            {
                Text = "⬡ CELLULAR INTELLIGENCE ANALYZER",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(15, 15)
            };

            _lblCurrentUser = new Label
            {
                Text = $"👤 {_currentUser.FullName ?? _currentUser.Username}  |  {string.Join(", ", _currentUser.Roles)}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(0, 20)
            };
            _lblCurrentUser.Left = this.Width - _lblCurrentUser.Width - 200;

            _lblCurrentTime = new Label
            {
                Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 150, 200),
                AutoSize = true,
                Location = new Point(0, 20)
            };

            _panelHeader.Controls.AddRange(new Control[] { lblAppName, _lblCurrentUser, _lblCurrentTime });

            // Sidebar
            _panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(18, 25, 50)
            };

            // Content area
            _panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 20, 40),
                Padding = new Padding(10)
            };

            // Status bar
            _panelStatusBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                BackColor = Color.FromArgb(10, 15, 35)
            };

            _lblStatus = new Label
            {
                Text = "Hazır",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 150, 200),
                AutoSize = true,
                Location = new Point(10, 7)
            };

            var lblDbStatus = new Label
            {
                Text = "● Veritabanı Bağlı",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(0, 200, 100),
                AutoSize = true,
                Location = new Point(200, 7)
            };

            _panelStatusBar.Controls.AddRange(new Control[] { _lblStatus, lblDbStatus });

            // Build sidebar navigation
            BuildSidebar();

            this.Controls.AddRange(new Control[] { _panelContent, _panelSidebar, _panelHeader, _panelStatusBar });

            // Clock timer
            _clockTimer = new Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) =>
            {
                _lblCurrentTime.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                _lblCurrentTime.Left = _panelHeader.Width - _lblCurrentTime.Width - 15;
                _lblCurrentUser.Left = _panelHeader.Width - _lblCurrentUser.Width - _lblCurrentTime.Width - 30;
            };
            _clockTimer.Start();

            this.FormClosing += MainForm_FormClosing;
        }

        private void BuildSidebar()
        {
            // Logo area
            var logoPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(220, 70),
                BackColor = Color.FromArgb(10, 15, 35)
            };

            var lblLogo = new Label
            {
                Text = "⬡ CIA Platform",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = false,
                Size = new Size(220, 70),
                TextAlign = ContentAlignment.MiddleCenter
            };
            logoPanel.Controls.Add(lblLogo);
            _panelSidebar.Controls.Add(logoPanel);

            int y = 80;

            // Navigation items
            _btnDashboard = CreateNavButton("🏠  Dashboard", y); y += 46;
            _btnHtsAnalysis = CreateNavButton("📱  HTS Analizi", y); y += 46;
            _btnDriveTest = CreateNavButton("🚗  Drive Test", y); y += 46;
            _btnBaseStations = CreateNavButton("📡  Baz İstasyonları", y); y += 46;
            _btnNarrowedBase = CreateNavButton("🎯  Daraltılmış Baz", y); y += 46;
            _btnCoverage = CreateNavButton("📶  Kapsama Modeli", y); y += 46;
            _btnMap = CreateNavButton("🗺  Harita", y); y += 46;

            y += 10; // Separator
            var separator = new Panel
            {
                Location = new Point(15, y),
                Size = new Size(190, 1),
                BackColor = Color.FromArgb(40, 60, 100)
            };
            _panelSidebar.Controls.Add(separator);
            y += 15;

            _btnReports = CreateNavButton("📄  Raporlar", y); y += 46;
            _btnAiAnalysis = CreateNavButton("🤖  Yapay Zeka", y); y += 46;
            _btnSettings = CreateNavButton("⚙  Ayarlar", y); y += 46;

            // Logout at bottom
            _btnLogout = CreateNavButton("🚪  Çıkış Yap", _panelSidebar.Height - 60);
            _btnLogout.BackColor = Color.FromArgb(80, 20, 20);
            _btnLogout.ForeColor = Color.FromArgb(255, 100, 100);

            // Wire up events
            _btnDashboard.Click += (s, e) => { SetActiveNav(_btnDashboard); LoadDashboard(); };
            _btnHtsAnalysis.Click += (s, e) => { SetActiveNav(_btnHtsAnalysis); LoadHtsAnalysis(); };
            _btnDriveTest.Click += (s, e) => { SetActiveNav(_btnDriveTest); LoadDriveTest(); };
            _btnBaseStations.Click += (s, e) => { SetActiveNav(_btnBaseStations); LoadBaseStations(); };
            _btnNarrowedBase.Click += (s, e) => { SetActiveNav(_btnNarrowedBase); LoadNarrowedBase(); };
            _btnCoverage.Click += (s, e) => { SetActiveNav(_btnCoverage); LoadCoverage(); };
            _btnMap.Click += (s, e) => { SetActiveNav(_btnMap); LoadMap(); };
            _btnReports.Click += (s, e) => { SetActiveNav(_btnReports); LoadReports(); };
            _btnAiAnalysis.Click += (s, e) => { SetActiveNav(_btnAiAnalysis); LoadAiAnalysis(); };
            _btnSettings.Click += (s, e) => { SetActiveNav(_btnSettings); LoadSettings(); };
            _btnLogout.Click += BtnLogout_Click;

            _panelSidebar.Controls.AddRange(new Control[]
            {
                _btnDashboard, _btnHtsAnalysis, _btnDriveTest, _btnBaseStations,
                _btnNarrowedBase, _btnCoverage, _btnMap, _btnReports,
                _btnAiAnalysis, _btnSettings, _btnLogout
            });

            // Handle resize for logout button
            _panelSidebar.Resize += (s, e) =>
            {
                _btnLogout.Top = _panelSidebar.Height - 60;
            };
        }

        private Button CreateNavButton(string text, int y)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 200, 230),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(220, 44),
                Location = new Point(0, y),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 50, 90);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 100, 200);
            return btn;
        }

        private void SetActiveNav(Button btn)
        {
            if (_activeNavButton != null)
            {
                _activeNavButton.BackColor = Color.Transparent;
                _activeNavButton.ForeColor = Color.FromArgb(180, 200, 230);
            }

            _activeNavButton = btn;
            btn.BackColor = Color.FromArgb(0, 80, 160);
            btn.ForeColor = Color.White;
        }

        private void LoadContent(Control control)
        {
            _panelContent.Controls.Clear();
            control.Dock = DockStyle.Fill;
            _panelContent.Controls.Add(control);
        }

        private void LoadDashboard()
        {
            SetActiveNav(_btnDashboard);
            SetStatus("Dashboard yükleniyor...");
            LoadContent(new DashboardControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadHtsAnalysis()
        {
            SetStatus("HTS Analiz modülü yükleniyor...");
            LoadContent(new HtsAnalysisControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadDriveTest()
        {
            SetStatus("Drive Test modülü yükleniyor...");
            LoadContent(new DriveTestControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadBaseStations()
        {
            SetStatus("Baz İstasyonları modülü yükleniyor...");
            LoadContent(new BaseStationControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadNarrowedBase()
        {
            SetStatus("Daraltılmış Baz modülü yükleniyor...");
            LoadContent(new NarrowedBaseControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadCoverage()
        {
            SetStatus("Kapsama Modeli yükleniyor...");
            LoadContent(new CoverageControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadMap()
        {
            SetStatus("Harita yükleniyor...");
            LoadContent(new MapControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadReports()
        {
            SetStatus("Raporlar modülü yükleniyor...");
            LoadContent(new ReportsControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadAiAnalysis()
        {
            SetStatus("Yapay Zeka modülü yükleniyor...");
            LoadContent(new AiAnalysisControl(_currentUser));
            SetStatus("Hazır");
        }

        private void LoadSettings()
        {
            SetStatus("Ayarlar yükleniyor...");
            LoadContent(new SettingsControl(_currentUser));
            SetStatus("Hazır");
        }

        public void SetStatus(string message)
        {
            if (_lblStatus.InvokeRequired)
                _lblStatus.Invoke(new Action(() => _lblStatus.Text = message));
            else
                _lblStatus.Text = message;
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Sistemden çıkış yapmak istediğinizden emin misiniz?",
                "Çıkış Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Logger.Info($"Kullanıcı çıkış yaptı: {_currentUser.Username}");
                _clockTimer.Stop();
                Application.Restart();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _clockTimer?.Stop();
            _clockTimer?.Dispose();
        }
    }
}
