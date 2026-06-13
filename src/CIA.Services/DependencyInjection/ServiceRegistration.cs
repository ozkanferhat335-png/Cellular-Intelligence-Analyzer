using System;
using System.IO;
using CIA.Business.Engines;
using CIA.Data.Context;
using CIA.Data.Migrations;
using CIA.Data.Repositories;
using CIA.Services.Auth;
using CIA.Services.FileImport;
using CIA.Services.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace CIA.Services.DependencyInjection
{
    public static class ServiceRegistration
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public static IServiceProvider BuildServiceProvider(string databasePath = null)
        {
            var services = new ServiceCollection();

            // Database
            var dbPath = databasePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CellularIntelligenceAnalyzer",
                "CIA_Database.db");

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
            var connectionString = $"Data Source={dbPath};";

            services.AddDbContext<CiaDbContext>(options =>
                options.UseSqlite(connectionString), ServiceLifetime.Scoped);

            services.AddScoped<IUnitOfWork>(sp =>
            {
                var context = sp.GetRequiredService<CiaDbContext>();
                return new UnitOfWork(context, connectionString);
            });

            // Migration Manager
            services.AddSingleton(new DatabaseMigrationManager(connectionString));

            // Business Engines
            services.AddScoped<IHtsAnalysisEngine, HtsAnalysisEngine>();
            services.AddScoped<IDriveTestAnalysisEngine, DriveTestAnalysisEngine>();
            services.AddScoped<ICoverageModelingEngine, CoverageModelingEngine>();
            services.AddScoped<IAiAnalysisEngine, AiAnalysisEngine>();
            services.AddScoped<INarrowedBaseAnalysisEngine, NarrowedBaseAnalysisEngine>();

            // Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IHtsImportService, HtsImportService>();
            services.AddScoped<IDriveTestImportService, DriveTestImportService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IReportService, ReportService>();

            Logger.Info("Servisler başarıyla kaydedildi.");
            return services.BuildServiceProvider();
        }

        public static void InitializeDatabase(IServiceProvider serviceProvider)
        {
            try
            {
                var migrationManager = serviceProvider.GetRequiredService<DatabaseMigrationManager>();
                migrationManager.InitializeDatabase();
                Logger.Info("Veritabanı başarıyla başlatıldı.");
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Veritabanı başlatma kritik hatası.");
                throw;
            }
        }
    }
}
