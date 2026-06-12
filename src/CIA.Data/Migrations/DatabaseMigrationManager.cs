using System;
using System.IO;
using CIA.Core.Constants;
using CIA.Core.Enums;
using CIA.Core.Helpers;
using CIA.Data.Context;
using CIA.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace CIA.Data.Migrations
{
    public class DatabaseMigrationManager
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;

        public DatabaseMigrationManager(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void InitializeDatabase()
        {
            try
            {
                Logger.Info("Veritabanı başlatılıyor...");

                using (var context = new CiaDbContext(_connectionString))
                {
                    context.Database.EnsureCreated();
                    ApplyPragmas(context);
                    SeedInitialData(context);
                }

                Logger.Info("Veritabanı başarıyla başlatıldı.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Veritabanı başlatma hatası.");
                throw;
            }
        }

        private void ApplyPragmas(CiaDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                // Performance optimizations
                cmd.CommandText = "PRAGMA journal_mode=WAL;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA synchronous=NORMAL;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA cache_size=-64000;"; // 64MB cache
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA temp_store=MEMORY;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA mmap_size=268435456;"; // 256MB mmap
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA page_size=4096;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "PRAGMA foreign_keys=ON;";
                cmd.ExecuteNonQuery();
            }
        }

        private void SeedInitialData(CiaDbContext context)
        {
            // Seed Roles
            if (!context.Roles.AnyAsync().Result)
            {
                context.Roles.AddRange(
                    new Role { Id = 1, Name = "Admin", Description = "Sistem Yöneticisi - Tüm yetkiler" },
                    new Role { Id = 2, Name = "Analyst", Description = "Analist - Analiz ve raporlama yetkileri" },
                    new Role { Id = 3, Name = "Viewer", Description = "Görüntüleyici - Sadece okuma yetkisi" },
                    new Role { Id = 4, Name = "Operator", Description = "Operatör - Veri girişi yetkileri" }
                );
                context.SaveChanges();
            }

            // Seed Admin User
            if (!context.Users.AnyAsync().Result)
            {
                var adminUser = new User
                {
                    Username = "admin",
                    PasswordHash = SecurityHelper.HashPassword("Admin@123!"),
                    Email = "admin@cia.local",
                    FullName = "Sistem Yöneticisi",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(adminUser);
                context.SaveChanges();

                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = 1,
                    AssignedAt = DateTime.UtcNow
                });
                context.SaveChanges();
            }

            // Seed Default Settings
            if (!context.Settings.AnyAsync().Result)
            {
                context.Settings.AddRange(
                    new Setting { Key = SettingKeys.MapProvider, Value = "OpenStreetMap", Description = "Harita sağlayıcısı", Category = "Map" },
                    new Setting { Key = SettingKeys.DefaultLatitude, Value = "39.9334", Description = "Varsayılan harita enlemi", Category = "Map" },
                    new Setting { Key = SettingKeys.DefaultLongitude, Value = "32.8597", Description = "Varsayılan harita boylamı", Category = "Map" },
                    new Setting { Key = SettingKeys.DefaultZoom, Value = "7", Description = "Varsayılan harita zoom seviyesi", Category = "Map" },
                    new Setting { Key = SettingKeys.MaxQueryResults, Value = "100000", Description = "Maksimum sorgu sonucu", Category = "Performance" },
                    new Setting { Key = SettingKeys.EnableAiAnalysis, Value = "true", Description = "Yapay zeka analizi aktif", Category = "AI" },
                    new Setting { Key = SettingKeys.ReportOutputPath, Value = "Reports", Description = "Rapor çıktı dizini", Category = "Reports" },
                    new Setting { Key = SettingKeys.ImportBatchSize, Value = "5000", Description = "İçe aktarma toplu işlem boyutu", Category = "Performance" },
                    new Setting { Key = SettingKeys.LogLevel, Value = "Info", Description = "Log seviyesi", Category = "Logging" },
                    new Setting { Key = SettingKeys.Theme, Value = "Dark", Description = "Uygulama teması", Category = "UI" },
                    new Setting { Key = SettingKeys.Language, Value = "tr-TR", Description = "Uygulama dili", Category = "UI" }
                );
                context.SaveChanges();
            }
        }

        public void OptimizeDatabase()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "VACUUM;";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "ANALYZE;";
                        cmd.ExecuteNonQuery();
                    }
                }
                Logger.Info("Veritabanı optimizasyonu tamamlandı.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Veritabanı optimizasyon hatası.");
            }
        }

        public long GetDatabaseSizeBytes()
        {
            try
            {
                var builder = new SqliteConnectionStringBuilder(_connectionString);
                var dbPath = builder.DataSource;
                if (File.Exists(dbPath))
                    return new FileInfo(dbPath).Length;
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}
