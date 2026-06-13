using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Forms
{
    public class LoginForm : Form
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private Panel _panelLeft;
        private Panel _panelRight;
        private Label _lblTitle;
        private Label _lblSubtitle;
        private Label _lblVersion;
        private Label _lblUsername;
        private Label _lblPassword;
        private TextBox _txtUsername;
        private TextBox _txtPassword;
        private Button _btnLogin;
        private Label _lblError;
        private CheckBox _chkRemember;
        private ProgressBar _progressBar;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"{AppConstants.AppName} - Giriş";
            this.Size = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.MinimumSize = new Size(900, 600);

            // Left panel - branding
            _panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 420,
                BackColor = Color.FromArgb(20, 30, 60)
            };

            _lblTitle = new Label
            {
                Text = "Cellular Intelligence\nAnalyzer",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = false,
                Size = new Size(380, 100),
                Location = new Point(20, 180),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblSubtitle = new Label
            {
                Text = "Telekom Saha Analiz, HTS Korelasyon\nve Daraltılmış Baz İstasyonu Tespit Platformu",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 180, 220),
                AutoSize = false,
                Size = new Size(380, 60),
                Location = new Point(20, 290),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblVersion = new Label
            {
                Text = $"v{AppConstants.AppVersion}",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 130, 170),
                AutoSize = false,
                Size = new Size(380, 30),
                Location = new Point(20, 520),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Feature list
            var features = new[]
            {
                "✓  HTS Kayıt Analizi (10M+ kayıt)",
                "✓  Drive Test Analizi",
                "✓  Daraltılmış Baz Tespiti",
                "✓  RF Optimizasyon",
                "✓  Yapay Zeka Destekli Analiz",
                "✓  Excel/CSV İçe & Dışa Aktarım"
            };

            int featureY = 370;
            foreach (var feature in features)
            {
                var lbl = new Label
                {
                    Text = feature,
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(120, 200, 120),
                    AutoSize = false,
                    Size = new Size(360, 22),
                    Location = new Point(30, featureY)
                };
                _panelLeft.Controls.Add(lbl);
                featureY += 24;
            }

            _panelLeft.Controls.AddRange(new Control[] { _lblTitle, _lblSubtitle, _lblVersion });

            // Right panel - login form
            _panelRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 35, 65)
            };

            var lblLoginTitle = new Label
            {
                Text = "Sisteme Giriş",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(380, 50),
                Location = new Point(30, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblLoginSubtitle = new Label
            {
                Text = "Devam etmek için kimlik bilgilerinizi girin",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(150, 170, 200),
                AutoSize = false,
                Size = new Size(380, 30),
                Location = new Point(30, 150)
            };

            _lblUsername = new Label
            {
                Text = "KULLANICI ADI",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 150, 220),
                AutoSize = false,
                Size = new Size(380, 20),
                Location = new Point(30, 220)
            };

            _txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                Size = new Size(380, 40),
                Location = new Point(30, 242),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "admin"
            };

            _lblPassword = new Label
            {
                Text = "ŞİFRE",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 150, 220),
                AutoSize = false,
                Size = new Size(380, 20),
                Location = new Point(30, 300)
            };

            _txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                Size = new Size(380, 40),
                Location = new Point(30, 322),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PasswordChar = '●',
                Text = "Admin@123!"
            };

            _chkRemember = new CheckBox
            {
                Text = "Beni Hatırla",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(150, 170, 200),
                Location = new Point(30, 375),
                AutoSize = true
            };

            _btnLogin = new Button
            {
                Text = "GİRİŞ YAP",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(380, 50),
                Location = new Point(30, 410),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnLogin.FlatAppearance.BorderSize = 0;
            _btnLogin.Click += BtnLogin_Click;

            _lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(255, 80, 80),
                AutoSize = false,
                Size = new Size(380, 30),
                Location = new Point(30, 470),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _progressBar = new ProgressBar
            {
                Size = new Size(380, 4),
                Location = new Point(30, 460),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            _panelRight.Controls.AddRange(new Control[]
            {
                lblLoginTitle, lblLoginSubtitle,
                _lblUsername, _txtUsername,
                _lblPassword, _txtPassword,
                _chkRemember, _btnLogin,
                _lblError, _progressBar
            });

            this.Controls.AddRange(new Control[] { _panelLeft, _panelRight });

            // Enter key support
            this.AcceptButton = _btnLogin;
            _txtUsername.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) _txtPassword.Focus(); };

            // Ensure form is visible
            this.Visible = true;
            this.BringToFront();
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            _lblError.Text = "";
            _btnLogin.Enabled = false;
            _progressBar.Visible = true;

            try
            {
                if (Program.ServiceProvider == null)
                {
                    _lblError.Text = "Servis sağlayıcı başlatılamadı. Uygulamayı yeniden başlatın.";
                    return;
                }

                var authService = Program.ServiceProvider.GetRequiredService<IAuthService>();
                var result = await authService.LoginAsync(new LoginDto
                {
                    Username = _txtUsername.Text.Trim(),
                    Password = _txtPassword.Text
                });

                if (result.Success)
                {
                    Logger.Info($"Kullanıcı giriş yaptı: {result.User.Username}");
                    var mainForm = new MainForm(result.User);
                    this.Hide();
                    mainForm.FormClosed += (s2, e2) => this.Close();
                    mainForm.Show();
                }
                else
                {
                    _lblError.Text = result.Message;
                    _txtPassword.Clear();
                    _txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Giriş hatası");
                _lblError.Text = "Sistem hatası oluştu. Lütfen tekrar deneyin.";
            }
            finally
            {
                _btnLogin.Enabled = true;
                _progressBar.Visible = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw separator line
            using (var pen = new Pen(Color.FromArgb(0, 120, 215), 2))
            {
                e.Graphics.DrawLine(pen, 420, 0, 420, this.Height);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            // Always allow the form to become visible
            base.SetVisibleCore(value);
        }
    }
}
