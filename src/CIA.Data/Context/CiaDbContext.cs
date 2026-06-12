using System;
using System.IO;
using CIA.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CIA.Data.Context
{
    public class CiaDbContext : DbContext
    {
        private readonly string _connectionString;

        public CiaDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public CiaDbContext(DbContextOptions<CiaDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<Sector> Sectors { get; set; }
        public DbSet<Cell> Cells { get; set; }
        public DbSet<DriveTest> DriveTests { get; set; }
        public DbSet<DriveTestRecord> DriveTestRecords { get; set; }
        public DbSet<HtsRecord> HtsRecords { get; set; }
        public DbSet<ImportedFile> ImportedFiles { get; set; }
        public DbSet<AnalysisResult> AnalysisResults { get; set; }
        public DbSet<CoverageModel> CoverageModels { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(_connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email);

            // Site indexes
            modelBuilder.Entity<Site>()
                .HasIndex(s => s.SiteCode)
                .IsUnique();

            modelBuilder.Entity<Site>()
                .HasIndex(s => new { s.Latitude, s.Longitude });

            modelBuilder.Entity<Site>()
                .HasIndex(s => s.Region);

            modelBuilder.Entity<Site>()
                .HasIndex(s => s.City);

            // Sector indexes
            modelBuilder.Entity<Sector>()
                .HasIndex(s => s.SiteId);

            // Cell indexes
            modelBuilder.Entity<Cell>()
                .HasIndex(c => c.CellId);

            modelBuilder.Entity<Cell>()
                .HasIndex(c => c.CGI);

            modelBuilder.Entity<Cell>()
                .HasIndex(c => c.PCI);

            modelBuilder.Entity<Cell>()
                .HasIndex(c => c.SectorId);

            // HTS Record indexes - critical for performance
            modelBuilder.Entity<HtsRecord>()
                .HasIndex(h => h.PhoneNumber);

            modelBuilder.Entity<HtsRecord>()
                .HasIndex(h => h.IMEI);

            modelBuilder.Entity<HtsRecord>()
                .HasIndex(h => h.IMSI);

            modelBuilder.Entity<HtsRecord>()
                .HasIndex(h => h.CellId);

            modelBuilder.Entity<HtsRecord>()
                .HasIndex(h => h.CGI);

            modelBuilder.Entity<HtsRecord>()
                .HasIndex(h => h.CallDateTime);

            modelBuilder.Entity<HtsRecord>()
                .HasIndex(h => new { h.PhoneNumber, h.CallDateTime });

            modelBuilder.Entity<HtsRecord>()
                .HasIndex(h => h.ImportedFileId);

            // DriveTest Record indexes
            modelBuilder.Entity<DriveTestRecord>()
                .HasIndex(d => d.DriveTestId);

            modelBuilder.Entity<DriveTestRecord>()
                .HasIndex(d => d.Timestamp);

            modelBuilder.Entity<DriveTestRecord>()
                .HasIndex(d => new { d.Latitude, d.Longitude });

            modelBuilder.Entity<DriveTestRecord>()
                .HasIndex(d => d.PCI);

            modelBuilder.Entity<DriveTestRecord>()
                .HasIndex(d => d.ServingCellId);

            // Analysis Result indexes
            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(a => a.AnalysisType);

            modelBuilder.Entity<AnalysisResult>()
                .HasIndex(a => a.CreatedAt);

            // System Log indexes
            modelBuilder.Entity<SystemLog>()
                .HasIndex(l => l.Timestamp);

            modelBuilder.Entity<SystemLog>()
                .HasIndex(l => l.Level);

            // Settings unique key
            modelBuilder.Entity<Setting>()
                .HasIndex(s => s.Key)
                .IsUnique();

            // Coverage Model indexes
            modelBuilder.Entity<CoverageModel>()
                .HasIndex(c => c.SectorId);
        }
    }
}
