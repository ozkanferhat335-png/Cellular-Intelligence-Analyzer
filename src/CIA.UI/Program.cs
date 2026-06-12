using System;
using System.IO;
using System.Windows.Forms;
using CIA.Services.DependencyInjection;
using CIA.UI.Forms;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace CIA.UI
{
    static class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        public static IServiceProvider ServiceProvider { get; private set; }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Configure NLog
            ConfigureLogging();

            // Handle unhandled exceptions
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                Logger.Info($"=== {CIA.Core.Constants.AppConstants.AppName} v{CIA.Core.Constants.AppConstants.AppVersion} başlatılıyor ===");

                // Build DI container
                ServiceProvider = ServiceRegistration.BuildServiceProvider();

                // Initialize database
                ServiceRegistration.InitializeDatabase(ServiceProvider);

                Logger.Info("Uygulama başarıyla başlatıldı.");

                // Show login form
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Uygulama başlatma hatası.");
                MessageBox.Show(
                    $"Uygulama başlatılırken kritik bir hata oluştu:\n\n{ex.Message}\n\nDetaylar için log dosyasını inceleyiniz.",
                    "Kritik Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static void ConfigureLogging()
        {
            var config = new LoggingConfiguration();

            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CellularIntelligenceAnalyzer", "Logs");
            Directory.CreateDirectory(logDir);

            var fileTarget = new FileTarget("file")
            {
                FileName = Path.Combine(logDir, "CIA_${shortdate}.log"),
                Layout = "${longdate} | ${level:uppercase=true:padding=5} | ${logger:shortName=true} | ${message} ${exception:format=tostring}",
                ArchiveEvery = FileArchivePeriod.Day,
                MaxArchiveFiles = 90,
                Encoding = System.Text.Encoding.UTF8
            };

            var consoleTarget = new ConsoleTarget("console")
            {
                Layout = "${time} | ${level:uppercase=true} | ${message}"
            };

            config.AddTarget(fileTarget);
            config.AddTarget(consoleTarget);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);

            LogManager.Configuration = config;
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Logger.Error(e.Exception, "UI Thread exception");
            MessageBox.Show(
                $"Beklenmeyen bir hata oluştu:\n\n{e.Exception.Message}",
                "Hata",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Logger.Fatal(ex, "Unhandled domain exception");
        }
    }
}
