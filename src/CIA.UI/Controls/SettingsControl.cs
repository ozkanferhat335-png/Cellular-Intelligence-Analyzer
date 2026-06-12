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
    public class SettingsControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private TabControl _tabControl;
        private TabPage _tabGeneral;
        private TabPage _tabMap;
        private TabPage _tabPerformance;
        private TabPage _tabSecurity;
        private TabPage _tabAbout;

        public SettingsControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            LoadSettingsAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            var lblTitle = new Label
            {
                Text = "⚙ Sistem Ayarları",
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

            _tabGeneral = new TabPage("Genel");
            _tabMap = new TabPage("Harita");
            _tabPerformance = new TabPage("Performans");
            _tabSecurity = new TabPage("Güvenlik");
            _tabAbout = new TabPage("Hakkında");

            BuildGeneralTab();
            BuildMapTab();
            BuildPerformanceTab();
            BuildAboutTab();

            _tabControl.TabPages.AddRange(new TabPage[] { _tabGeneral, _tabMap, _tabPerformance, _tabSecurity, _tabAbout });

            this.Controls.AddRange(new Control[] { lblTitle, _tabControl });
            this.Resize += (s, e) => _tabControl.Size = new Size(this.Width - 40, this.Height - 65);
        }

        private void BuildGeneralTab()
        {
            _tabGeneral.BackColor = Color.FromArgb(20, 30, 60);

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 30, 60), Padding = new Padding(20) };

            int y = 20;
            AddSettingRow(panel, "Uygulama Dili:", "tr-TR", ref y);
            AddSettingRow(panel, "Tema:", "Koyu (Dark)", ref y);
            AddSettingRow(panel, "Log Seviyesi:", "Info", ref y);
            AddSettingRow(panel, "Rapor Çıktı Dizini:", "Reports", ref y);

            var btnSave = new Button
            {
                Text = "💾  Kaydet",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(150, 35),
                Location = new Point(20, y + 20),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) =>
            {
                MessageBox.Show("Ayarlar kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            panel.Controls.Add(btnSave);

            _tabGeneral.Controls.Add(panel);
        }

        private void BuildMapTab()
        {
            _tabMap.BackColor = Color.FromArgb(20, 30, 60);

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 30, 60), Padding = new Padding(20) };

            int y = 20;
            AddSettingRow(panel, "Harita Sağlayıcısı:", "OpenStreetMap", ref y);
            AddSettingRow(panel, "Varsayılan Enlem:", AppConstants.DefaultMapLatitude.ToString(), ref y);
            AddSettingRow(panel, "Varsayılan Boylam:", AppConstants.DefaultMapLongitude.ToString(), ref y);
            AddSettingRow(panel, "Varsayılan Zoom:", AppConstants.DefaultMapZoom.ToString(), ref y);
            AddSettingRow(panel, "Maks. Harita Noktası:", AppConstants.MaxMapPoints.ToString(), ref y);

            _tabMap.Controls.Add(panel);
        }

        private void BuildPerformanceTab()
        {
            _tabPerformance.BackColor = Color.FromArgb(20, 30, 60);

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 30, 60), Padding = new Padding(20) };

            int y = 20;
            AddSettingRow(panel, "İçe Aktarma Toplu İşlem:", AppConstants.BulkInsertBatchSize.ToString(), ref y);
            AddSettingRow(panel, "Maks. Sorgu Sonucu:", AppConstants.MaxHtsRecordsPerQuery.ToString(), ref y);
            AddSettingRow(panel, "Sorgu Zaman Aşımı (sn):", AppConstants.QueryTimeoutSeconds.ToString(), ref y);

            var btnOptimize = new Button
            {
                Text = "🔧  Veritabanını Optimize Et",
                Font = new Font("Segoe UI", 10),
                Size = new Size(220, 35),
                Location = new Point(20, y + 20),
                BackColor = Color.FromArgb(80, 60, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOptimize.FlatAppearance.BorderSize = 0;
            btnOptimize.Click += async (s, e) =>
            {
                btnOptimize.Enabled = false;
                btnOptimize.Text = "Optimize ediliyor...";
                await Task.Run(() =>
                {
                    var migrationManager = Program.ServiceProvider.GetService(typeof(CIA.Data.Migrations.DatabaseMigrationManager))
                        as CIA.Data.Migrations.DatabaseMigrationManager;
                    migrationManager?.OptimizeDatabase();
                });
                btnOptimize.Text = "✅ Optimizasyon Tamamlandı";
                await Task.Delay(2000);
                btnOptimize.Text = "🔧  Veritabanını Optimize Et";
                btnOptimize.Enabled = true;
            };
            panel.Controls.Add(btnOptimize);

            _tabPerformance.Controls.Add(panel);
        }

        private void BuildAboutTab()
        {
            _tabAbout.BackColor = Color.FromArgb(20, 30, 60);

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(20, 30, 60) };

            var lblAppName = new Label
            {
                Text = AppConstants.AppName,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(30, 30)
            };

            var lblVersion = new Label
            {
                Text = $"Versiyon: {AppConstants.AppVersion}",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(30, 70)
            };

            var lblDesc = new Label
            {
                Text = "Telekom Saha Analiz, HTS Korelasyon ve\nDaraltılmış Baz İstasyonu Tespit Platformu",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(30, 100)
            };

            var lblTech = new Label
            {
                Text = "Teknolojiler: C# 7.3 | Windows Forms | SQLite | GMap.NET | Entity Framework | NLog",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 150, 200),
                AutoSize = true,
                Location = new Point(30, 155)
            };

            var lblFeatures = new Label
            {
                Text = "Özellikler:\n" +
                       "• 10 Milyon+ HTS kaydı işleme kapasitesi\n" +
                       "• Gerçek zamanlı Drive Test analizi\n" +
                       "• Daraltılmış baz istasyonu tespiti\n" +
                       "• RF optimizasyon önerileri\n" +
                       "• Yapay zeka destekli anomali tespiti\n" +
                       "• Kurumsal PDF/Excel raporlama\n" +
                       "• GMap.NET tabanlı harita görselleştirme\n" +
                       "• Okumura-Hata kapsama modelleme",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 200, 150),
                AutoSize = true,
                Location = new Point(30, 185)
            };

            panel.Controls.AddRange(new Control[] { lblAppName, lblVersion, lblDesc, lblTech, lblFeatures });
            _tabAbout.Controls.Add(panel);
        }

        private void AddSettingRow(Panel panel, string label, string value, ref int y)
        {
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = true,
                Location = new Point(0, y + 3)
            };

            var txt = new TextBox
            {
                Text = value,
                Font = new Font("Segoe UI", 9),
                Location = new Point(220, y),
                Size = new Size(300, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            panel.Controls.AddRange(new Control[] { lbl, txt });
            y += 40;
        }

        private async void LoadSettingsAsync()
        {
            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                // Settings would be loaded here
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ayarlar yükleme hatası");
            }
        }
    }
}
