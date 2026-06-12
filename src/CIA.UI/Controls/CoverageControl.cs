using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CIA.Business.Engines;
using CIA.Core.DTOs;
using CIA.Core.Enums;
using CIA.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.UI.Controls
{
    public class CoverageControl : UserControl
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly UserDto _currentUser;

        private DataGridView _gridModels;
        private Button _btnModelAll;
        private Button _btnModelSelected;
        private ComboBox _cmbTerrain;
        private ProgressBar _progressModeling;
        private Label _lblStatus;

        public CoverageControl(UserDto currentUser)
        {
            _currentUser = currentUser;
            InitializeComponent();
            LoadModelsAsync();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(15, 20, 40);
            this.Dock = DockStyle.Fill;

            var lblTitle = new Label
            {
                Text = "📶 Kapsama Modelleme Motoru",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 255),
                AutoSize = true,
                Location = new Point(20, 15)
            };

            var panelControls = new Panel
            {
                Location = new Point(20, 55),
                Size = new Size(800, 60),
                BackColor = Color.FromArgb(20, 30, 60)
            };

            var lblTerrain = new Label { Text = "Arazi Tipi:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 180, 220), AutoSize = true, Location = new Point(10, 18) };
            _cmbTerrain = new ComboBox
            {
                Location = new Point(90, 15),
                Size = new Size(150, 25),
                BackColor = Color.FromArgb(35, 50, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbTerrain.Items.AddRange(new object[] { "Kentsel", "Yarı Kentsel", "Kırsal", "Açık Alan", "Yoğun Kentsel" });
            _cmbTerrain.SelectedIndex = 0;

            _btnModelAll = new Button
            {
                Text = "📶  Tüm Sektörleri Modelle",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(220, 35),
                Location = new Point(260, 12),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnModelAll.FlatAppearance.BorderSize = 0;
            _btnModelAll.Click += BtnModelAll_Click;

            _btnModelSelected = new Button
            {
                Text = "Seçili Sektörü Modelle",
                Font = new Font("Segoe UI", 9),
                Size = new Size(180, 35),
                Location = new Point(490, 12),
                BackColor = Color.FromArgb(0, 100, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnModelSelected.FlatAppearance.BorderSize = 0;
            _btnModelSelected.Click += BtnModelSelected_Click;

            _progressModeling = new ProgressBar { Location = new Point(690, 18), Size = new Size(200, 25), Visible = false };
            _lblStatus = new Label { Text = "", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(150, 200, 150), AutoSize = true, Location = new Point(10, 45) };

            panelControls.Controls.AddRange(new Control[] { lblTerrain, _cmbTerrain, _btnModelAll, _btnModelSelected, _progressModeling, _lblStatus });

            _gridModels = CreateGrid();
            _gridModels.Location = new Point(20, 130);
            _gridModels.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            _gridModels.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "SiteCode", HeaderText = "Site Kodu", Width = 120 },
                new DataGridViewTextBoxColumn { Name = "SectorName", HeaderText = "Sektör", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "Azimuth", HeaderText = "Azimuth", Width = 80 },
                new DataGridViewTextBoxColumn { Name = "Radius", HeaderText = "Yarıçap (km)", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "RsrpAtEdge", HeaderText = "RSRP Kenar (dBm)", Width = 130 },
                new DataGridViewTextBoxColumn { Name = "Terrain", HeaderText = "Arazi", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "ModeledAt", HeaderText = "Modelleme Tarihi", Width = 140 },
                new DataGridViewTextBoxColumn { Name = "Validated", HeaderText = "DT Doğrulandı", Width = 110 },
                new DataGridViewTextBoxColumn { Name = "Accuracy", HeaderText = "Doğruluk", Width = 80 }
            });

            this.Controls.AddRange(new Control[] { lblTitle, panelControls, _gridModels });

            this.Resize += (s, e) =>
            {
                _gridModels.Size = new Size(this.Width - 40, this.Height - 140);
            };
        }

        private async Task LoadModelsAsync()
        {
            try
            {
                var unitOfWork = Program.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var models = await unitOfWork.CoverageModels.GetAllWithSectorInfoAsync();

                _gridModels.Rows.Clear();
                foreach (var model in models)
                {
                    _gridModels.Rows.Add(
                        model.Sector?.Site?.SiteCode ?? "-",
                        model.Sector?.SectorName ?? "-",
                        model.Sector?.Azimuth.ToString("F0") + "°" ?? "-",
                        model.EstimatedRadiusKm.ToString("F2"),
                        model.EstimatedRsrpAtEdge.ToString("F1"),
                        ((TerrainType)model.TerrainType).ToString(),
                        model.ModeledAt.ToString("dd.MM.yyyy HH:mm"),
                        model.IsValidatedByDriveTest ? "✅ Evet" : "Hayır",
                        model.IsValidatedByDriveTest ? $"{model.ValidationAccuracy:P0}" : "-"
                    );
                }

                _lblStatus.Text = $"Toplam {models.Count()} kapsama modeli yüklendi.";
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Kapsama modeli yükleme hatası");
            }
        }

        private async void BtnModelAll_Click(object sender, EventArgs e)
        {
            _btnModelAll.Enabled = false;
            _progressModeling.Visible = true;
            _progressModeling.Style = ProgressBarStyle.Marquee;
            _lblStatus.Text = "Tüm sektörler modelleniyor...";

            try
            {
                var engine = Program.ServiceProvider.GetRequiredService<ICoverageModelingEngine>();
                var terrainType = (TerrainType)(_cmbTerrain.SelectedIndex + 1);

                var progress = new Progress<int>(count =>
                {
                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => _lblStatus.Text = $"{count} sektör modellendi..."));
                    else
                        _lblStatus.Text = $"{count} sektör modellendi...";
                });

                var models = await engine.ModelAllSectorsAsync(terrainType, progress);
                _lblStatus.Text = $"✅ {models.Count} sektör başarıyla modellendi.";
                await LoadModelsAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Kapsama modelleme hatası");
                _lblStatus.Text = $"❌ Hata: {ex.Message}";
            }
            finally
            {
                _btnModelAll.Enabled = true;
                _progressModeling.Visible = false;
            }
        }

        private async void BtnModelSelected_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Sektör seçimi için baz istasyonu modülünden sektör seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
